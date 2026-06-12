using System;
using Avalonia.Controls;
using Avalonia.Threading;
using HermaLauncher.ViewModels;

namespace HermaLauncher.Views;

public partial class MainWindow : Window
{
    private bool _vmWired;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // VM 의 창 제어 요청(최소화/복원/닫기)을 창에 연결. DataContext 는 App 에서 1회 주입되나,
    // 중복 구독 방지로 가드. VM 은 창을 직접 참조하지 않아 MVVM 경계를 유지(이벤트 경유).
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vmWired || DataContext is not MainWindowViewModel vm)
            return;
        _vmWired = true;

        vm.MinimizeRequested += () => Dispatcher.UIThread.Post(() => WindowState = WindowState.Minimized);
        vm.RestoreRequested += () => Dispatcher.UIThread.Post(() =>
        {
            WindowState = WindowState.Normal;
            Activate();
        });
        vm.CloseRequested += () => Dispatcher.UIThread.Post(Close);
    }
}
