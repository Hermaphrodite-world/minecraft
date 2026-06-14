using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HermaLauncher.ViewModels;

namespace HermaLauncher.Views;

public partial class MainWindow : Window
{
    private bool _vmWired;

    public MainWindow()
    {
        InitializeComponent();

        // 커스텀 크롬(ExtendClientArea + NoChrome): 타이틀바 드래그 + 최소화/닫기 직접 구현.
        TitleBar.PointerPressed += (_, e) =>
        {
            // 버튼(최소화/닫기) 위 클릭은 드래그 제외 — PointerPressed bubble-up 으로 BeginMoveDrag 가
            // 버튼 클릭과 동시 발동하는 충돌 방지(Codex).
            if (e.Source is Visual src && src.FindAncestorOfType<Button>(includeSelf: true) is not null)
                return;
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        };
        MinBtn.Click += (_, _) => WindowState = WindowState.Minimized;
        CloseBtn.Click += (_, _) => Close();

        // 진행 중(설치/동기화) 닫기 시 작업을 먼저 취소 → PackwizService 자식 java 가 고아로 남는 것 방지
        // (Codex SHIP-BLOCKER S1). ct.Register kill 은 Cancel() 에서 동기 실행되므로 닫기 진행 전 정리됨.
        Closing += (_, _) =>
        {
            if (DataContext is MainWindowViewModel { IsBusy: true } vm)
                vm.CancelOngoing();
        };

        DataContextChanged += OnDataContextChanged;
    }

    // VM 의 창 제어 요청(게임 모니터링: 최소화/복원/닫기)을 창에 연결. DataContext 는 App 에서 1회
    // 주입되나 중복 구독 방지로 가드. VM 은 창을 직접 참조하지 않아 MVVM 경계를 유지(이벤트 경유).
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
