using System;
using Avalonia;

namespace HermaLauncher;

internal static class Program
{
    // 진입점. 자체 업데이트(Velopack)는 추후 통합 시 이 메서드 "최상단 첫 줄"에서
    // VelopackApp.Build().Run(); 을 호출해야 한다(구현계획 §4 불변식 (1)).
    [STAThread]
    public static void Main(string[] args)
    {
        // TODO(R7): Velopack 통합 시 — 반드시 Avalonia 초기화 이전, 이 위치 첫 줄:
        //   VelopackApp.Build().Run();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia 디자이너/테스트가 참조하는 표준 빌더.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
