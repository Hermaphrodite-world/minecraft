using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

namespace HermaLauncher.Services;

// macOS 트레이(메뉴바 extra) + 네이티브 토스트.
//   - 아이콘/메뉴/클릭: Avalonia 내장 TrayIcon(크로스플랫폼이지만 여기선 macOS 전용으로만 사용).
//   - 토스트: osascript `display notification`(Notification Center 네이티브). 무의존성.
//     제약: 알림의 앱 귀속이 'Script Editor'/osascript 로 표시됨(UNUserNotificationCenter 는 ObjC interop
//     필요라 보류). 본문/제목은 정상 표시 — 친구용 런처엔 충분(릴리스 전 실기기 스모크 권장).
[SupportedOSPlatform("macos")]
public sealed class MacTrayService : ITrayService
{
    private TrayIcon? _tray;
    private bool _disposed;

    public bool IsAvailable => _tray is not null && !_disposed;

    public void Initialize(TrayCallbacks callbacks)
    {
        try
        {
            var menu = new NativeMenu();
            var open = new NativeMenuItem("열기");
            open.Click += (_, _) => callbacks.OnOpen();
            var quit = new NativeMenuItem("종료");
            quit.Click += (_, _) => callbacks.OnQuit();
            menu.Items.Add(open);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(quit);

            _tray = new TrayIcon
            {
                Icon = LoadIcon(),
                ToolTipText = callbacks.Tooltip,
                Menu = menu,
                IsVisible = true,
            };
            _tray.Clicked += (_, _) => callbacks.OnOpen();

            if (Application.Current is { } app)
                TrayIcon.SetIcons(app, new TrayIcons { _tray });
        }
        catch (Exception ex)
        {
            AppLog.Warn(LaunchStage.Idle, "트레이 초기화 실패(무시): " + ex.Message);
        }
    }

    public void Notify(string title, string body)
    {
        try
        {
            // ArgumentList 로 셸 미경유 — 인젝션/escaping 방지. AppleScript 문자열 내 " 와 \ 만 이스케이프.
            var script = $"display notification \"{Escape(body)}\" with title \"{Escape(title)}\"";
            var psi = new ProcessStartInfo("osascript")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-e");
            psi.ArgumentList.Add(script);
            var p = Process.Start(psi);
            if (p is not null)
            {
                // 자식 프로세스 자원 회수 — osascript 는 즉시 끝나므로 종료 시 dispose(좀비 누적 방지, Codex LOW-4).
                p.EnableRaisingEvents = true;
                p.Exited += (_, _) => p.Dispose();
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn(LaunchStage.Idle, "트레이 알림 실패(무시): " + ex.Message);
        }
    }

    private static string Escape(string s) =>
        (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static WindowIcon? LoadIcon()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://HermaLauncher/Assets/app.png"));
            return new WindowIcon(stream);
        }
        catch
        {
            return null; // 아이콘 없이도 메뉴바 항목은 동작.
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        void Cleanup()
        {
            try
            {
                if (_tray is not null)
                {
                    _tray.IsVisible = false;
                    _tray.Dispose();
                }
                if (Application.Current is { } app)
                    TrayIcon.SetIcons(app, new TrayIcons());
            }
            catch { /* best-effort */ }
            finally { _tray = null; }
        }

        if (Dispatcher.UIThread.CheckAccess())
            Cleanup();
        else
            Dispatcher.UIThread.Post(Cleanup);
    }
}
