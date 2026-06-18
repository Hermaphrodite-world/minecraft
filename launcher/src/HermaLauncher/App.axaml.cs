using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HermaLauncher.Services;
using HermaLauncher.ViewModels;
using HermaLauncher.Views;

namespace HermaLauncher;

public partial class App : Application
{
    // 트레이 아이콘 + 접속 토스트(앱 수명 동안 상주). 미지원 플랫폼은 no-op.
    private ITrayService? _tray;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // 두 번째 실행이 보낸 신호(SingleInstanceSignal)·트레이 '열기' 등에서 호출 — 기존 창을 복원·전면화.
    // 별도 스레드에서 호출될 수 있어 UI 스레드로 마샬링. 앱 초기화 전/창 부재면 안전하게 no-op.
    public static void ActivateMainWindow()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is MainWindow w)
            {
                w.RestoreFromTray(); // 숨김 해제 + 작업표시줄 복귀 + 최소화 해제 + 전면화.
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
            var vm = new MainWindowViewModel();
            desktop.MainWindow = new MainWindow { DataContext = vm };

            // 트레이 + 접속 알림 조립(미지원 플랫폼/디자이너는 no-op). VM 은 트레이를 모름(이벤트 경유).
            _tray = TrayServiceFactory.Create();
            _tray.Initialize(new TrayCallbacks(
                OnOpen: ActivateMainWindow,
                OnQuit: () => desktop.Shutdown(),
                Tooltip: "HERMA 런처 — 클릭하면 열기"));
            vm.JoinNotificationRequested += msg => _tray?.Notify("HERMA 월드", msg);

            // 트레이가 실제로 떠 있을 때만 '트레이로 숨기기' 노출 — 트레이 없이 숨기면 복원 불가(Codex HIGH).
            vm.IsTrayAvailable = _tray.IsAvailable;

            // 종료 시 트레이 아이콘 정리(잔상 방지).
            desktop.Exit += (_, _) => _tray?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
