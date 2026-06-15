using System;
using System.Threading;

namespace HermaLauncher.Services;

// 2번째 실행 시 silent-exit 대신 기존 창을 앞으로 가져오기 위한 프로세스 간 신호.
// named EventWaitHandle 사용 → Windows 전용(비-Windows 는 named 핸들 미지원 → no-op, 기존 동작 유지).
// 모든 경로를 try/catch 로 감싸 실패해도 시작 흐름을 절대 막지 않는다(fail-safe — 최악의 경우 기존 silent-exit).
public static class SingleInstanceSignal
{
    private const string EventName = "HermaLauncher_Activate";

    // 앱 수명 동안 핸들 보유(GC/해제 방지). 첫 인스턴스에서만 설정.
    private static EventWaitHandle? _handle;

    // 첫 인스턴스: 활성화 신호 리스너 시작. onSignal 은 신호 수신 시 별도 스레드에서 호출(호출자가 UI 마샬링 책임).
    public static void StartListener(Action onSignal)
    {
        try { _handle = CreateAndListen(EventName, onSignal); }
        catch { _handle = null; } // 신호 기능 비활성 — 기존 동작 유지
    }

    // 두 번째 인스턴스: 기존 인스턴스에 활성화 신호 전송. 성공 여부 반환(실패해도 호출자는 그냥 종료).
    public static bool SignalExisting() => SignalExisting(EventName);

    // 테스트용 오버로드(InternalsVisibleTo) — 커스텀 이벤트명으로 리스너 시작. 핸들 반환(호출자가 수명/Dispose 관리).
    // 비-Windows 또는 실패 시 null.
    internal static EventWaitHandle? CreateAndListen(string eventName, Action onSignal)
    {
        if (!OperatingSystem.IsWindows()) return null; // named EventWaitHandle 은 Windows 전용
        var handle = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, eventName);
        var listener = new Thread(() =>
        {
            try
            {
                while (true)
                {
                    handle.WaitOne();
                    try { onSignal(); } catch { /* 콜백 실패는 무시하고 계속 대기 */ }
                }
            }
            catch { /* 핸들 폐기/앱 종료 등 → 리스너 종료 */ }
        })
        {
            IsBackground = true,
            Name = "HermaActivateListener",
        };
        listener.Start();
        return handle;
    }

    // 테스트용 오버로드 — 커스텀 이벤트명으로 신호. 기존 리스너 없거나 비-Windows 면 false.
    internal static bool SignalExisting(string eventName)
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            if (EventWaitHandle.TryOpenExisting(eventName, out var h))
            {
                using (h)
                    h.Set();
                return true;
            }
        }
        catch { /* 무시 — 신호 실패해도 두 번째 인스턴스는 그냥 종료 */ }
        return false;
    }
}
