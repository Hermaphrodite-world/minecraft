using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Avalonia.Threading;

namespace HermaLauncher.Services;

// Windows 트레이 아이콘 + 풍선 알림 — Shell_NotifyIcon P/Invoke(무의존성). Win10/11 은 레거시 풍선을
// 토스트로 렌더한다. 콜백 수신용 숨김 top-level 창(WndProc)을 만들고, 좌클릭=열기 / 우클릭=메뉴(열기·종료).
//
// 함정 회피:
//   - WndProc 델리게이트는 GC 되면 크래시 → 필드로 루팅.
//   - TrackPopupMenu 는 owner 창이 foreground 여야 바깥 클릭으로 닫힘 → message-only(HWND_MESSAGE) 대신
//     일반(top-level, 미표시) 창 + SetForegroundWindow + 직후 PostMessage(WM_NULL).
//   - 모든 Shell_NotifyIcon/창 호출은 창을 만든 UI 스레드(메시지 펌프 스레드)에서. Notify 는 방어적으로 UI 마샬링.
[SupportedOSPlatform("windows")]
public sealed class WindowsTrayService : ITrayService
{
    // 인스턴스마다 고유 클래스명 — 재초기화(드묾) 시 stale WndProc thunk 충돌 방지(Codex LOW-3).
    private static int _classSeq;
    private readonly string _className = "HermaLauncherTrayWnd_" + Interlocked.Increment(ref _classSeq);

    private const uint WM_APP = 0x8000;
    private const uint WM_TRAYICON = WM_APP + 1;
    private const uint WM_DESTROY = 0x0002;
    private const uint WM_NULL = 0x0000;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_CONTEXTMENU = 0x007B;

    private const int NIM_ADD = 0;
    private const int NIM_MODIFY = 1;
    private const int NIM_DELETE = 2;
    private const int NIF_MESSAGE = 0x01;
    private const int NIF_ICON = 0x02;
    private const int NIF_TIP = 0x04;
    private const int NIF_INFO = 0x10;
    private const int NIIF_INFO = 0x01;

    private const uint MF_STRING = 0x0000;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_RETURNCMD = 0x0100;

    private const uint IDI_APPLICATION = 32512;

    private const int ID_OPEN = 1;
    private const int ID_QUIT = 2;

    private const int TrayId = 1;

    private WndProcDelegate? _wndProc; // GC 루팅 필수
    private IntPtr _hwnd;
    private IntPtr _hIcon;
    private bool _ownIcon;
    private TrayCallbacks? _cb;
    private bool _added;
    private bool _classRegistered;
    private bool _disposed;

    public bool IsAvailable => _added && !_disposed;

    public void Initialize(TrayCallbacks callbacks)
    {
        _cb = callbacks;
        try
        {
            _wndProc = WndProc;
            var hInstance = GetModuleHandleW(null);

            var wc = new WNDCLASSW
            {
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                hInstance = hInstance,
                lpszClassName = _className,
            };
            _classRegistered = RegisterClassW(ref wc) != 0; // 고유명이라 충돌 없음.

            _hwnd = CreateWindowExW(
                0x00000080 /* WS_EX_TOOLWINDOW */, _className, "Herma Tray",
                0 /* WS_OVERLAPPED, 미표시 */, 0, 0, 0, 0,
                IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
            if (_hwnd == IntPtr.Zero)
            {
                AppLog.Warn(LaunchStage.Idle, "트레이 창 생성 실패 — 트레이/알림 비활성");
                return;
            }

            _hIcon = LoadAppIcon(hInstance, out _ownIcon);

            var data = NewData();
            data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
            data.uCallbackMessage = (int)WM_TRAYICON;
            data.hIcon = _hIcon;
            data.szTip = Truncate(callbacks.Tooltip, 127);
            _added = Shell_NotifyIconW(NIM_ADD, ref data);
            if (!_added)
                AppLog.Warn(LaunchStage.Idle, "트레이 아이콘 등록 실패(Shell_NotifyIcon NIM_ADD)");
        }
        catch (Exception ex)
        {
            AppLog.Warn(LaunchStage.Idle, "트레이 초기화 실패(무시): " + ex.Message);
        }
    }

    public void Notify(string title, string body)
    {
        if (Dispatcher.UIThread.CheckAccess())
            NotifyCore(title, body);
        else
            Dispatcher.UIThread.Post(() => NotifyCore(title, body));
    }

    private void NotifyCore(string title, string body)
    {
        if (_disposed || !_added)
            return;
        try
        {
            var data = NewData();
            data.uFlags = NIF_INFO;
            data.szInfoTitle = Truncate(title, 63);
            data.szInfo = Truncate(body, 255);
            data.dwInfoFlags = NIIF_INFO;
            Shell_NotifyIconW(NIM_MODIFY, ref data);
        }
        catch (Exception ex)
        {
            AppLog.Warn(LaunchStage.Idle, "트레이 알림 실패(무시): " + ex.Message);
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYICON)
        {
            // 레거시 모드: lParam 하위 워드 = 마우스 이벤트.
            var evt = (uint)(lParam.ToInt64() & 0xFFFF);
            if (evt is WM_LBUTTONUP or WM_LBUTTONDBLCLK)
                SafeInvoke(_cb?.OnOpen);
            else if (evt is WM_RBUTTONUP or WM_CONTEXTMENU)
                ShowContextMenu(hWnd);
            return IntPtr.Zero;
        }

        if (msg == WM_DESTROY)
            return IntPtr.Zero;

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void ShowContextMenu(IntPtr hWnd)
    {
        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero)
            return;
        try
        {
            AppendMenuW(menu, MF_STRING, ID_OPEN, "열기");
            AppendMenuW(menu, MF_STRING, ID_QUIT, "종료");

            GetCursorPos(out var pt);
            SetForegroundWindow(hWnd); // 바깥 클릭으로 메뉴가 닫히게 하는 필수 단계.
            var cmd = TrackPopupMenuEx(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD, pt.X, pt.Y, hWnd, IntPtr.Zero);
            PostMessageW(hWnd, WM_NULL, IntPtr.Zero, IntPtr.Zero);

            if (cmd == ID_OPEN)
                SafeInvoke(_cb?.OnOpen);
            else if (cmd == ID_QUIT)
                SafeInvoke(_cb?.OnQuit);
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    private static void SafeInvoke(Action? action)
    {
        if (action is null)
            return;
        // 콜백은 창/앱 조작(UI 스레드 요구) — WndProc 자체가 UI 스레드라 직접 호출 가능하나 방어적 마샬링.
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }

    private NOTIFYICONDATAW NewData() => new()
    {
        cbSize = Marshal.SizeOf<NOTIFYICONDATAW>(),
        hWnd = _hwnd,
        uID = TrayId,
        szTip = string.Empty,
        szInfo = string.Empty,
        szInfoTitle = string.Empty,
    };

    private static IntPtr LoadAppIcon(IntPtr hInstance, out bool owned)
    {
        owned = false;
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                var h = ExtractIconW(hInstance, exe, 0);
                if (h != IntPtr.Zero && h != (IntPtr)1)
                {
                    owned = true;
                    return h;
                }
            }
        }
        catch { /* best-effort — 아래 일반 아이콘으로 폴백 */ }
        return LoadIconW(IntPtr.Zero, (IntPtr)IDI_APPLICATION); // 공유 시스템 아이콘(파괴 불필요).
    }

    private static string Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;
        return s.Length <= max ? s : s[..max];
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
                if (_added)
                {
                    var data = NewData();
                    Shell_NotifyIconW(NIM_DELETE, ref data);
                    _added = false;
                }
                if (_ownIcon && _hIcon != IntPtr.Zero)
                    DestroyIcon(_hIcon);
                if (_hwnd != IntPtr.Zero)
                    DestroyWindow(_hwnd); // 클래스 unregister 전에 창부터 파괴.
                if (_classRegistered)
                {
                    UnregisterClassW(_className, GetModuleHandleW(null));
                    _classRegistered = false;
                }
            }
            catch { /* best-effort */ }
            finally
            {
                _hwnd = IntPtr.Zero;
                _hIcon = IntPtr.Zero;
                _wndProc = null; // unregister 이후 해제 — thunk 가 클래스 수명 동안 살아있게.
            }
        }

        if (Dispatcher.UIThread.CheckAccess())
            Cleanup();
        else
            Dispatcher.UIThread.Post(Cleanup);
    }

    // ── P/Invoke ──

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSW
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATAW
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassW(ref WNDCLASSW lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterClassW(string lpClassName, IntPtr hInstance);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIconW(int dwMessage, ref NOTIFYICONDATAW lpData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractIconW(IntPtr hInst, string lpszExeFileName, uint nIconIndex);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadIconW(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, int uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lptpm);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
