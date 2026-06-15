using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HermaLauncher.ViewModels;
using HermaLauncher.Views;

namespace HermaLauncher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // 두 번째 실행이 보낸 신호(SingleInstanceSignal)에 의해 호출 — 기존 창을 복원·전면화.
    // 별도 스레드에서 호출되므로 UI 스레드로 마샬링. 앱 초기화 전/창 부재면 안전하게 no-op.
    public static void ActivateMainWindow()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is { } w)
            {
                if (w.WindowState == WindowState.Minimized)
                    w.WindowState = WindowState.Normal;
                w.Show();
                w.Activate();
                // 일시 Topmost 토글로 전면 끌어올린 뒤 해제(Windows 포커스 강제 휴리스틱).
                w.Topmost = true;
                w.Topmost = false;
            }
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
