using System;

namespace HermaLauncher.Services;

// 시스템 트레이(알림 영역) 아이콘 + OS 네이티브 접속 토스트의 추상화.
//   - 트레이 아이콘은 앱 수명 동안 상주(persistent) → 창을 트레이로 숨겼든 그냥 최소화했든 풍선 알림이 동작.
//   - Initialize 로 콜백(열기/종료/툴팁)을 받고, Notify 로 토스트를 띄운다. 모두 best-effort(실패해도 throw 금지).
public interface ITrayService : IDisposable
{
    // 트레이 아이콘을 띄우고 콜백을 연결. UI 스레드에서 1회 호출.
    void Initialize(TrayCallbacks callbacks);

    // OS 네이티브 알림(토스트). 트레이가 없거나 미지원 플랫폼이면 no-op.
    void Notify(string title, string body);

    // 트레이 아이콘이 실제로 떠 있나(Initialize 성공). false 면 '트레이로 숨기기' 를 막아야 한다 —
    // 트레이 없이 숨기면 복원 수단이 사라져 앱이 보이지 않게 갇힌다(Codex HIGH).
    bool IsAvailable { get; }
}

// 트레이 메뉴/클릭이 호출할 동작들. OnOpen=창 복원, OnQuit=앱 종료.
public sealed record TrayCallbacks(Action OnOpen, Action OnQuit, string Tooltip);

public static class TrayServiceFactory
{
    // 플랫폼별 구현 선택. Windows=Shell_NotifyIcon, macOS=Avalonia TrayIcon+osascript, 그 외=no-op.
    public static ITrayService Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsTrayService();
        if (OperatingSystem.IsMacOS())
            return new MacTrayService();
        return new NoopTrayService();
    }
}

// Linux/기타/디자이너 — 트레이 미지원. 모든 동작 무시. IsAvailable=false → 숨기기 버튼 비표시.
public sealed class NoopTrayService : ITrayService
{
    public bool IsAvailable => false;

    public void Initialize(TrayCallbacks callbacks) { }

    public void Notify(string title, string body) { }

    public void Dispose() { }
}
