using System;
using Avalonia;
using Velopack;

namespace HermaLauncher;

internal static class Program
{
    // 진입점. Velopack 자체 업데이트 훅은 반드시 Avalonia 초기화 이전 "첫 줄"
    // (구현계획 §4 불변식 (1) — 원자적 교체/재시작이 UI 와 race 하지 않도록).
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia 디자이너/테스트가 참조하는 표준 빌더.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
