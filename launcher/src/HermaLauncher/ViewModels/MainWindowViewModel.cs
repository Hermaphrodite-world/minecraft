using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HermaLauncher.Services;

namespace HermaLauncher.ViewModels;

// 화면 상태 머신. 시작 = 모드 선택 → 각 경로(친구 서버 바로 플레이 / 공식 런처 설치) →
// 공식 런처 설치 성공 시 OfficialDone 으로 전환해 Play 오클릭을 구조적으로 차단한다.
public enum AppView
{
    ModeSelect,      // 두 경로 카드 중 선택
    FriendPlay,      // 이 런처가 직접 실행(친구 서버)
    OfficialInstall, // 공식 런처에 프로필 설치
    OfficialDone,    // 설치 완료 — 공식 런처로 전환 안내(전용 화면)
}

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly LaunchOrchestrator _orchestrator = new();
    private readonly OfficialLauncherInstaller _installer = new();
    private CancellationTokenSource? _cts;

    // 게임 시작 후 이 시간 안에 비정상 종료하면 '인스턴트 크래시'로 보고 런처를 복원해 에러를 보여준다.
    // 그 이후 종료(정상 플레이 종료)는 조용히 런처만 닫는다(마크가 자체 크래시 화면을 띄우는 구간).
    private const int InstantCrashWindowSeconds = 90;

    // 런처 창 제어 요청(View 가 구독해 WindowState/Close 처리 — VM 은 창을 직접 모름, MVVM 경계 유지).
    public event Action? MinimizeRequested;
    public event Action? RestoreRequested;
    public event Action? CloseRequested;

    private string ReadyText => $"Minecraft {LauncherConfig.MinecraftVersion} · Fabric · 준비 완료";

    [ObservableProperty]
    private string _statusMessage;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isIndeterminate;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallToOfficialCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectFriendModeCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectOfficialModeCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackToMenuCommand))]
    [NotifyPropertyChangedFor(nameof(ShowBack))]
    private bool _isBusy;

    // 현재 화면. 변경 시 화면 분기 bool 들과 뒤로가기 가시성을 재평가.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModeSelect))]
    [NotifyPropertyChangedFor(nameof(IsFriendPlay))]
    [NotifyPropertyChangedFor(nameof(IsOfficialInstall))]
    [NotifyPropertyChangedFor(nameof(IsOfficialDone))]
    [NotifyPropertyChangedFor(nameof(ShowBack))]
    [NotifyCanExecuteChangedFor(nameof(BackToMenuCommand))]
    private AppView _view = AppView.ModeSelect;

    // 닉네임 (오프라인 모드에서 사용). 기본값 = OS 사용자명.
    [ObservableProperty]
    private string _username = Environment.UserName;

    // Azure client ID 설정됨(배포본) → 온라인 기본 / 미설정(테스트) → 오프라인 기본.
    [ObservableProperty]
    private bool _isOffline = !LauncherConfig.IsAzureClientConfigured;

    // 게임 실행 중(런처 최소화 상태). 사용자가 런처를 복원해도 Play 재클릭(더블런치) 차단.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallToOfficialCommand))]
    private bool _isGameRunning;

    public MainWindowViewModel()
    {
        _statusMessage = ReadyText;
    }

    public string Title => "HERMA";
    public string ServerLabel => $"{LauncherConfig.ServerIp}:{LauncherConfig.ServerPort}";

    // 화면 분기 (XAML IsVisible 바인딩).
    public bool IsModeSelect => View == AppView.ModeSelect;
    public bool IsFriendPlay => View == AppView.FriendPlay;
    public bool IsOfficialInstall => View == AppView.OfficialInstall;
    public bool IsOfficialDone => View == AppView.OfficialDone;
    // 뒤로가기 헤더 버튼: 실행 경로(친구/공식 설치)에서만, 작업 중이 아닐 때. (완료 화면은 자체 버튼 사용)
    public bool ShowBack => (View == AppView.FriendPlay || View == AppView.OfficialInstall) && !IsBusy;

    private bool CanNavigate() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private void SelectFriendMode()
    {
        ResetStatus();
        View = AppView.FriendPlay;
    }

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private void SelectOfficialMode()
    {
        ResetStatus();
        View = AppView.OfficialInstall;
    }

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private void BackToMenu()
    {
        ResetStatus();
        View = AppView.ModeSelect;
    }

    private void ResetStatus()
    {
        HasError = false;
        Progress = 0;
        IsIndeterminate = false;
        StatusMessage = ReadyText;
    }

    private bool CanPlay() => !IsBusy && !IsGameRunning;

    [RelayCommand(CanExecute = nameof(CanPlay))]
    private async Task PlayAsync()
    {
        IsBusy = true;
        HasError = false;
        Progress = 0;
        IsIndeterminate = true;
        _cts = new CancellationTokenSource();

        var progress = new Progress<StageUpdate>(OnStageUpdate);
        var options = new LaunchOptions(Username, IsOffline);
        Process? game = null;
        try
        {
            game = await _orchestrator.RunAsync(options, progress, _cts.Token).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
            IsIndeterminate = false;
            _cts.Dispose();
            _cts = null;
        }

        // 실행 성공(game != null) → 런처를 비켜주고(최소화) 게임 종료까지 모니터링.
        if (game is not null)
            await MonitorGameSessionAsync(game).ConfigureAwait(true);
    }

    // 게임이 떠 있는 동안 런처는 최소화로 비켜준다. 게임이 끝나면 런처를 닫되,
    // 시작 직후 비정상 종료(크래시 의심)면 런처를 복원해 에러를 보여준다(failure-path-first).
    private async Task MonitorGameSessionAsync(Process game)
    {
        IsGameRunning = true;
        var startedAt = DateTime.Now;
        using (game)
        {
            MinimizeRequested?.Invoke();
            int exitCode;
            try
            {
                await game.WaitForExitAsync().ConfigureAwait(true);
                exitCode = game.ExitCode;
            }
            catch
            {
                // 모니터링 실패(권한/플랫폼) — 게임은 떠 있으니 런처만 조용히 닫는다.
                IsGameRunning = false;
                CloseRequested?.Invoke();
                return;
            }

            IsGameRunning = false;
            var ranSeconds = (DateTime.Now - startedAt).TotalSeconds;
            var instantCrash = exitCode != 0 && ranSeconds < InstantCrashWindowSeconds;
            if (instantCrash)
            {
                RestoreRequested?.Invoke();
                HasError = true;
                StatusMessage = "게임이 실행 직후 종료됐어요(크래시 의심). 다시 시도해 주세요.";
            }
            else
            {
                CloseRequested?.Invoke();
            }
        }
    }

    // 대체 경로: 공식 마인크래프트 런처에 모드팩 프로필 설치(정품 로그인, Mojang 승인 대기 없음).
    // 성공 시 OfficialDone 화면으로 전환 → Play 버튼이 사라져 잘못된 시나리오(설치 후 이 런처로 실행) 차단.
    [RelayCommand(CanExecute = nameof(CanPlay))]
    private async Task InstallToOfficialAsync()
    {
        IsBusy = true;
        HasError = false;
        Progress = 0;
        IsIndeterminate = true;
        _cts = new CancellationTokenSource();

        var progress = new Progress<StageUpdate>(OnStageUpdate);
        var ok = false;
        try
        {
            // InstallAsync 는 throw 하지 않고 성공=true / 실패·취소=false 를 반환(계약, L39).
            // 반드시 반환값으로 판정 — HasError 는 Progress→Dispatcher 이중 지연이라 await 직후엔 stale(race).
            ok = await _installer.InstallAsync(progress, _cts.Token).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
            IsIndeterminate = false;
            _cts.Dispose();
            _cts = null;
        }

        // 성공일 때만 완료 화면으로 → Play 오클릭 구조적 차단. 실패/취소는 설치 화면 유지(에러 메시지 노출).
        if (ok)
            View = AppView.OfficialDone;
    }

    private bool CanCancel() => IsBusy;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _cts?.Cancel();

    private void OnStageUpdate(StageUpdate u)
    {
        // Progress<T> 콜백은 캡처된 UI 컨텍스트에서 호출되나, 안전하게 디스패치.
        Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = u.Message;
            HasError = u.IsError;
            if (u.Fraction is { } f)
            {
                IsIndeterminate = false;
                Progress = Math.Clamp(f, 0, 1) * 100;
            }
            else
            {
                IsIndeterminate = !u.IsError && IsBusy;
            }
        });
    }
}
