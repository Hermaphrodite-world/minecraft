using System;
using System.Threading;
using Avalonia;
using Velopack;
using HermaLauncher.Services;

namespace HermaLauncher;

internal static class Program
{
    private static Mutex? _singleInstance; // 앱 수명 동안 보유(GC/해제 방지).

    // 진입점. Velopack 자체 업데이트 훅은 반드시 Avalonia 초기화 이전 "첫 줄"
    // (구현계획 §4 불변식 (1) — 원자적 교체/재시작이 UI 와 race 하지 않도록).
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();      // Velopack 보다 단일인스턴스 락을 먼저 잡지 않는다(재시작 훅 비차단).
        AppLog.RotateOnce();            // 오래된 로그 정리(P0)

        // 단일 인스턴스(P1-4) — 중복 실행 시 packwiz/servers.dat/Velopack 동시쓰기 race 방지.
        // 업데이트 재시작 직후 기존 프로세스 종료 지연(overlap)을 위해 짧게 대기 후 판정.
        _singleInstance = new Mutex(initiallyOwned: false, "HermaLauncher_SingleInstance");
        bool acquired;
        try { acquired = _singleInstance.WaitOne(TimeSpan.FromSeconds(3)); }
        catch (AbandonedMutexException) { acquired = true; } // 이전 인스턴스가 비정상 종료하며 남긴 것 → 인수
        if (!acquired)
        {
            // 두 번째 인스턴스: 기존 창을 앞으로 가져오라고 신호 후 종료(Windows). 신호 실패해도 그냥 종료.
            var signaled = SingleInstanceSignal.SignalExisting();
            AppLog.Info(LaunchStage.Idle, $"이미 실행 중 — 기존 창 활성화 신호({(signaled ? "전송" : "미지원/실패")}) 후 종료");
            return;
        }

        AppLog.Info(LaunchStage.Idle, $"런처 시작 v{AppInfo.Version}");
        // 첫 인스턴스: 두 번째 실행의 활성화 신호를 받아 기존 창을 앞으로(Windows 전용, fail-safe).
        SingleInstanceSignal.StartListener(App.ActivateMainWindow);
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            try { _singleInstance.ReleaseMutex(); } catch { /* 미보유 등 */ }
            _singleInstance.Dispose();
        }
    }

    // Avalonia 디자이너/테스트가 참조하는 표준 빌더.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
