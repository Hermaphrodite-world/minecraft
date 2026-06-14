using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HermaLauncher.Services;

namespace HermaLauncher.ViewModels;

// 화면 상태. Main = 두 카드(바로 플레이 / 공식 설치) 동시 표시 1화면. 공식 설치 성공 시 OfficialDone
// 으로 전환해 안내 화면을 보여준다(Play 오클릭은 Main 의 카드 분리 + IsBusy 게이트로 차단).
public enum AppView
{
    Main,         // 두 경로 카드 + 상태/진행 (기본 화면)
    OfficialDone, // 공식 런처 설치 완료 — 전환 안내 전용 화면
    Settings,     // 설정/복구 — 계정·RAM·로그(P3-2)
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
    [NotifyCanExecuteChangedFor(nameof(BackToMenuCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenSettingsCommand))]
    private bool _isBusy;

    // 현재 화면. 변경 시 화면 분기 bool 들을 재평가.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMain))]
    [NotifyPropertyChangedFor(nameof(IsOfficialDone))]
    [NotifyPropertyChangedFor(nameof(IsSettings))]
    [NotifyCanExecuteChangedFor(nameof(BackToMenuCommand))]
    private AppView _view = AppView.Main;

    // ── 계정(P3-1) ──
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggedIn))]
    [NotifyPropertyChangedFor(nameof(AccountLabel))]
    [NotifyCanExecuteChangedFor(nameof(LogoutCommand))]
    private string? _accountName;

    // ── 설정/RAM(P3-2/P3-3) ──
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RamSummary))]
    private bool _ramAuto;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RamSummary))]
    private int _maxRamMb;

    // RAM 자동 토글 시 권장값으로 되돌린다(수동 입력은 자동 OFF 시 NumericUpDown 으로).
    partial void OnRamAutoChanged(bool value)
    {
        if (value)
            MaxRamMb = RamAdvisor.RecommendedMaxRamMb();
    }

    // 게임 실행 중(런처 최소화 상태). 사용자가 런처를 복원해도 Play 재클릭(더블런치) 차단.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallToOfficialCommand))]
    private bool _isGameRunning;

    public MainWindowViewModel()
    {
        _statusMessage = ReadyText;
        _accountName = AccountCache.LastUsername();
        var settings = LauncherSettings.Load();
        _ramAuto = settings.IsRamAuto;
        _maxRamMb = RamAdvisor.EffectiveMaxRamMb();
    }

    public string Title => "HERMA LAUNCHER";
    public string Subtitle => "클릭 한 번으로 바로 플레이";
    public string ServerLabel => $"{LauncherConfig.ServerIp}:{LauncherConfig.ServerPort}";

    // 상태 칩 (헤더 하단). Fabric / Mods / 자동 업데이트 는 XAML 리터럴.
    public string VersionChip => $"Minecraft {LauncherConfig.MinecraftVersion}";

    // 런처 자체 버전 (푸터/설정 표시, P3-1).
    public string LauncherVersionLabel => $"런처 v{AppInfo.Version}";

    // 계정 표시(P3-1). 로그인 캐시 없으면 안내.
    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(AccountName);
    public string AccountLabel => IsLoggedIn ? $"{AccountName} 님으로 로그인됨" : "로그인되어 있지 않아요";

    // RAM 요약(설정 화면, P3-3).
    public string RamSummary => RamAuto
        ? $"자동 ({MaxRamMb} MB)"
        : $"수동 ({MaxRamMb} MB)";

    public int RamMinMb => RamAdvisor.MinRamMb;
    public int RamMaxMb => RamAdvisor.MaxRamMb;

    // 푸터 외부 링크 표시 여부(P3-6 — 빈 URL 버튼 숨김).
    public bool HasDiscord => !string.IsNullOrWhiteSpace(LauncherConfig.DiscordUrl);
    public bool HasGuide => !string.IsNullOrWhiteSpace(LauncherConfig.GuideUrl);
    public bool HasWebsite => !string.IsNullOrWhiteSpace(LauncherConfig.WebsiteUrl);

    // 화면 분기 (XAML IsVisible 바인딩).
    public bool IsMain => View == AppView.Main;
    public bool IsOfficialDone => View == AppView.OfficialDone;
    public bool IsSettings => View == AppView.Settings;

    private bool CanNavigate() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private void BackToMenu()
    {
        ResetStatus();
        View = AppView.Main;
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
        // 온라인 전용(MS 정품 인증). 닉네임/오프라인 UI 제거됨 — username 은 온라인 경로에서 미사용.
        var options = new LaunchOptions(Environment.UserName, Offline: false);
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
            catch (Exception ex)
            {
                // 모니터링 실패(권한/플랫폼) — 게임은 떠 있으니 런처만 조용히 닫는다.
                AppLog.Warn(LaunchStage.Running, "게임 종료 모니터링 실패: " + ex.Message);
                IsGameRunning = false;
                CloseRequested?.Invoke();
                return;
            }

            IsGameRunning = false;
            var ranSeconds = (DateTime.Now - startedAt).TotalSeconds;
            AppLog.Info(LaunchStage.Running, $"게임 종료 (코드={exitCode}, 실행 {ranSeconds:F0}s)");

            // P1-7: 즉시 크래시뿐 아니라 한참 뒤 비정상 종료도 진단 보존(로그 버튼 + 재시도 안내).
            if (exitCode != 0)
            {
                RestoreRequested?.Invoke();
                HasError = true;
                StatusMessage = ranSeconds < InstantCrashWindowSeconds
                    ? $"게임이 실행 직후 종료됐어요(크래시 의심, 코드 {exitCode}). 아래 '로그' 버튼에서 원인을 확인하거나 다시 시도해 주세요."
                    : $"게임이 비정상 종료됐어요(코드 {exitCode}). 아래 '로그' 버튼에서 게임 로그·크래시 리포트를 확인할 수 있어요.";
            }
            else
            {
                CloseRequested?.Invoke(); // 정상 종료(코드 0) → 런처 닫기
            }
        }
    }

    // 대체 경로: 공식 마인크래프트 런처에 모드팩 프로필 설치(정품 로그인, Mojang 승인 대기 없음).
    // 성공 시 OfficialDone 화면으로 전환 → 안내 화면 표시.
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
            // InstallAsync 는 throw 하지 않고 성공=true / 실패·취소=false 를 반환(계약).
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

        if (ok)
            View = AppView.OfficialDone;
    }

    private bool CanCancel() => IsBusy;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _cts?.Cancel();

    // ── 푸터 내비 (스텁 — URL 은 LauncherConfig 에서, 미설정 시 no-op) ──
    [RelayCommand]
    private void OpenDiscord() => OpenExternal(LauncherConfig.DiscordUrl);

    [RelayCommand]
    private void OpenGuide() => OpenExternal(LauncherConfig.GuideUrl);

    [RelayCommand]
    private void OpenWebsite() => OpenExternal(LauncherConfig.WebsiteUrl);

    [RelayCommand]
    private void OpenLogs() => OpenExternal(AppLog.LatestLogOrDir());

    // 로그 폴더(파일이 아닌 디렉토리)를 연다 — 설정 화면 '로그 폴더 열기'.
    [RelayCommand]
    private void OpenLogFolder() => OpenExternal(AppPaths.LogDir);

    // ── 설정 화면(P3-2) ── CanNavigate(=!IsBusy)는 BackToMenu 와 공유.
    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private void OpenSettings()
    {
        // 화면 진입 시 저장된 값으로 동기화(다른 곳에서 바뀌었을 수 있음).
        var settings = LauncherSettings.Load();
        RamAuto = settings.IsRamAuto;
        MaxRamMb = RamAdvisor.EffectiveMaxRamMb();
        AccountName = AccountCache.LastUsername();
        View = AppView.Settings;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var clamped = Math.Clamp(MaxRamMb, RamAdvisor.MinRamMb, RamAdvisor.MaxRamMb);
        if (clamped != MaxRamMb)
            MaxRamMb = clamped;
        new LauncherSettings { MaxRamMbOverride = RamAuto ? null : clamped }.Save();
        StatusMessage = "설정을 저장했어요.";
        View = AppView.Main;
    }

    private bool CanLogout() => IsLoggedIn;

    // 계정 재설정(P3-1) — 토큰 캐시 삭제. 다음 Play 시 브라우저 재로그인.
    [RelayCommand(CanExecute = nameof(CanLogout))]
    private void Logout()
    {
        AccountCache.Clear();
        AccountName = null;
        StatusMessage = "로그아웃했어요. 다음 플레이 시 다시 로그인해요.";
    }

    // URL/폴더를 기본 앱으로 연다. 빈 값(스텁 미설정)이면 조용히 무시.
    private static void OpenExternal(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return;
        try
        {
            // 반환 Process 핸들 즉시 해제(누수 방지 — Codex). UseShellExecute=true 면 null 일 수 있어 using 안전.
            using var proc = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch
        {
            // 외부 열기 실패는 사용자 흐름을 막지 않는다.
        }
    }

    private void OnStageUpdate(StageUpdate u)
    {
        // 모든 단계/오류를 로그 파일에 기록(P0) — 진단 SoT.
        if (u.IsError) AppLog.Error(u.Stage, u.Message);
        else AppLog.Info(u.Stage, u.Message);

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
