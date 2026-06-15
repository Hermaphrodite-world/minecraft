using System;
using System.Diagnostics;
using System.IO;
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

    // 메인 화면 서버 상태 pill 주기 갱신(30초). 디자이너/테스트 환경에선 미가동(네트워크 호출 방지).
    private readonly DispatcherTimer? _statusTimer;

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
    [NotifyPropertyChangedFor(nameof(CanShowErrorLog))]
    private bool _hasError;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallToOfficialCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackToMenuCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenSettingsCommand))]
    [NotifyPropertyChangedFor(nameof(CanShowErrorLog))]
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

    // ── 서버 주소 직접 입력(고급, 접속 이슈 대응) ──
    // 같은 집/네트워크의 다른 PC 에서 서버를 켠 경우 등 자동 접속이 안 될 때 서버 PC 의 IP 를 지정.
    [ObservableProperty]
    private string? _serverHostOverride;

    // ── 서버 상태 pill(메인 화면) ── 주기적으로 서버 ping 해 온라인/인원 표시(best-effort).
    [ObservableProperty]
    private string _serverStatusText = "서버 상태 확인 중…";

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
        _serverHostOverride = settings.ServerHostOverride;

        // 디자이너/테스트 환경에선 네트워크 호출 금지 — 실행 시에만 서버 상태 pill 가동.
        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _statusTimer.Tick += (_, _) => _ = RefreshServerStatusAsync();
            _statusTimer.Start();
            _ = RefreshServerStatusAsync(); // 즉시 1회
        }
    }

    // 서버 상태 pill 갱신(best-effort, 비차단). override 주소 우선, 없으면 공개 ServerIp.
    private async Task RefreshServerStatusAsync()
    {
        var host = ServerHostResolver.Normalize(LauncherSettings.Load().ServerHostOverride) ?? LauncherConfig.ServerIp;
        ServerStatus? st;
        try
        {
            st = await ServerPing.QueryStatusAsync(host, LauncherConfig.ServerPort, CancellationToken.None, 2500)
                                 .ConfigureAwait(true);
        }
        catch
        {
            st = null; // ping 실패는 '오프라인' 표시로 흡수(흐름 비차단)
        }
        ServerStatusText = st is null
            ? "🔴 서버 오프라인"
            : st is { Players: int p, MaxPlayers: int m } ? $"🟢 온라인 · {p}/{m}명" : "🟢 온라인";
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

    // 오류 진단 바로가기 노출 — 오류 상태이고 작업 중이 아닐 때만(취소 버튼과 셀 공유 겹침 방지, Codex LOW-7).
    public bool CanShowErrorLog => HasError && !IsBusy;

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
                // 게임 로그에서 흔한 원인을 한 가지 한국어 액션으로 분류(있으면 그걸 우선 안내, 없으면 일반 안내).
                var hint = TryDiagnoseLatestGameLog();
                if (hint is { } h)
                    StatusMessage = $"{h.Title} {h.Action} (자세한 원인은 설정의 '진단 파일 만들기')";
                else
                    StatusMessage = ranSeconds < InstantCrashWindowSeconds
                        ? $"게임이 실행 직후 종료됐어요(크래시 의심, 코드 {exitCode}). 설정의 '진단 파일 만들기'로 로그를 모아 디스코드에 보내거나 다시 시도해 주세요."
                        : $"게임이 비정상 종료됐어요(코드 {exitCode}). 설정의 '진단 파일 만들기'로 로그를 한 파일로 묶어 확인·공유할 수 있어요.";
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

    // 창 닫기 시점 등 외부에서 진행 중 작업을 취소한다. _cts 취소 → PackwizService 의 ct.Register 가
    // 자식 java 프로세스를 동기적으로 kill 해 고아 프로세스를 막는다(Codex SHIP-BLOCKER S1). 멱등.
    public void CancelOngoing() => _cts?.Cancel();

    // ── 푸터 내비 (URL 은 LauncherConfig 에서, 미설정 시 no-op) ──
    // 외부 링크는 OpenUrl(http/https 만 허용). 로그는 OpenPath(로컬 경로) — 둘을 분리해
    // env 주입 URL 이 임의 실행파일/커스텀 스킴을 여는 것을 차단(Codex HIGH-2).
    [RelayCommand]
    private void OpenDiscord() => OpenUrl(LauncherConfig.DiscordUrl);

    [RelayCommand]
    private void OpenGuide() => OpenUrl(LauncherConfig.GuideUrl);

    [RelayCommand]
    private void OpenWebsite() => OpenUrl(LauncherConfig.WebsiteUrl);

    [RelayCommand]
    private void OpenLogs() => OpenPath(AppLog.LatestLogOrDir());

    // 로그 폴더(파일이 아닌 디렉토리)를 연다 — 설정 화면 '로그 폴더 열기'.
    [RelayCommand]
    private void OpenLogFolder() => OpenPath(AppPaths.LogDir);

    // 진단 파일(ZIP) 생성 — 흩어진 로그 + 시스템 정보를 한 파일로 묶어 폴더를 연다(디스코드 공유용).
    // 크래시 메시지가 약속하는 '크래시 리포트'의 실제 구현(약속-구현 갭 해소).
    [RelayCommand]
    private void CreateDiagnostics()
    {
        var zip = DiagnosticsBundle.Create();
        if (zip is null)
        {
            StatusMessage = "진단 파일 생성에 실패했어요. 대신 '로그 폴더 열기'로 로그를 확인해 주세요.";
            return;
        }
        StatusMessage = "진단 파일을 만들었어요. 열린 폴더의 herma-진단-*.zip 을 디스코드에 올려 주세요.";
        OpenPath(Path.GetDirectoryName(zip) ?? AppPaths.DataRoot);
    }

    // ── 설정 화면(P3-2) ── CanNavigate(=!IsBusy)는 BackToMenu 와 공유.
    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private void OpenSettings()
    {
        // 화면 진입 시 저장된 값으로 동기화(다른 곳에서 바뀌었을 수 있음).
        var settings = LauncherSettings.Load();
        RamAuto = settings.IsRamAuto;
        MaxRamMb = RamAdvisor.EffectiveMaxRamMb();
        AccountName = AccountCache.LastUsername();
        ServerHostOverride = settings.ServerHostOverride;
        View = AppView.Settings;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var clamped = Math.Clamp(MaxRamMb, RamAdvisor.MinRamMb, RamAdvisor.MaxRamMb);
        // 슬라이더 tick(512MB)에 맞춰 라운딩 후 재clamp — double↔int 코어션으로 어긋난 값 영속화 방지(Codex LOW-5).
        clamped = (int)(Math.Round(clamped / 512.0) * 512);
        clamped = Math.Clamp(clamped, RamAdvisor.MinRamMb, RamAdvisor.MaxRamMb);
        if (clamped != MaxRamMb)
            MaxRamMb = clamped;
        // 서버 주소 정규화(공백/scheme/슬래시 제거). 빈 값이면 null(자동).
        var normalizedHost = ServerHostResolver.Normalize(ServerHostOverride);
        if (!string.Equals(normalizedHost, ServerHostOverride, StringComparison.Ordinal))
            ServerHostOverride = normalizedHost;
        // ※ 두 필드(RAM·서버주소)를 같은 객체로 저장 — 한쪽만 쓰면 다른 쪽이 지워진다.
        // 저장 실패(파일 권한/사용 중) 시 사용자에게 알리고 설정 화면 유지(Codex UX-R1).
        if (!new LauncherSettings
        {
            MaxRamMbOverride = RamAuto ? null : clamped,
            ServerHostOverride = normalizedHost,
        }.Save())
        {
            StatusMessage = "설정 저장에 실패했어요(파일 권한/사용 중일 수 있어요). 잠시 후 다시 시도해 주세요.";
            return;
        }
        // 참고(Codex MEDIUM-3, 알려진 제약): 공식 런처 프로필의 -Xmx 는 '공식 런처에 설치' 시점에
        // EffectiveMaxRamMb 로 기록된다. RAM 변경 후 공식 런처 경로에 반영하려면 '공식 런처에 설치'를
        // 다시 실행하면 된다. '바로 플레이'는 매 실행 시 EffectiveMaxRamMb 를 읽으므로 즉시 반영.
        StatusMessage = "설정을 저장했어요.";
        View = AppView.Main;
    }

    private bool CanLogout() => IsLoggedIn;

    // 계정 재설정(P3-1) — 토큰 캐시 삭제. 다음 Play 시 브라우저 재로그인.
    // 삭제 실패(파일 사용 중 등) 시 토큰이 남으므로 UI 를 '로그인됨' 으로 유지하고 재시도 안내(Codex HIGH-1).
    [RelayCommand(CanExecute = nameof(CanLogout))]
    private void Logout()
    {
        if (AccountCache.Clear())
        {
            AccountName = null;
            StatusMessage = "로그아웃했어요. 다음 플레이 시 다시 로그인해요.";
        }
        else
        {
            StatusMessage = "로그아웃에 실패했어요(로그인 정보 파일이 사용 중일 수 있어요). 잠시 후 다시 시도해 주세요.";
        }
    }

    // 외부 링크 — http/https 절대 URL 만 허용. 그 외(빈 값/잘못된 스킴/file: 등)는 무시.
    // env 로 주입되는 푸터 URL 이 임의 실행파일/커스텀 스킴을 여는 것을 차단(Codex HIGH-2).
    private static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            AppLog.Warn(LaunchStage.Idle, "허용되지 않은 링크 무시(http/https 만 허용): " + url);
            return;
        }
        Open(uri.AbsoluteUri);
    }

    // 로컬 경로(로그 파일/폴더)를 기본 앱/탐색기로 연다. 내부에서 만든 신뢰 경로에만 사용.
    private static void OpenPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        Open(path);
    }

    // 최신 game-*.log 본문을 best-effort 로 읽어 흔한 실패 원인을 분류. 실패/매칭 없음 = null.
    private static FailureDiagnosis.Hint? TryDiagnoseLatestGameLog()
    {
        try
        {
            var path = AppLog.LatestGameLogPath();
            if (path is null || !File.Exists(path)) return null;
            return FailureDiagnosis.Classify(File.ReadAllText(path));
        }
        catch
        {
            return null; // 진단은 보조 기능 — 실패해도 기본 안내로 폴백.
        }
    }

    private static void Open(string target)
    {
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
