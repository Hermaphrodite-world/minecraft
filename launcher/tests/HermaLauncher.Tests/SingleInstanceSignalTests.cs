using System;
using System.Threading;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 2번째 실행 → 기존 인스턴스 활성화 신호 IPC. named EventWaitHandle 은 Windows 전용이라
// 신호 메커니즘 검증은 Windows 에서만 의미(비-Windows 는 no-op 계약). 창 전면화 시각 동작은 런타임 스모크 영역.
public class SingleInstanceSignalTests
{
    [Fact]
    public void Signal_reaches_listener_on_windows()
    {
        if (!OperatingSystem.IsWindows()) return; // 비-Windows: named 핸들 미지원 → 메커니즘 검증 불가(스킵)

        var name = "HermaTest_" + Guid.NewGuid().ToString("N");
        using var got = new ManualResetEventSlim(false);
        using var handle = SingleInstanceSignal.CreateAndListen(name, () => got.Set());

        Assert.NotNull(handle);          // 리스너 생성 성공
        Thread.Sleep(50);                // 리스너 스레드 WaitOne 진입 여유
        Assert.True(SingleInstanceSignal.SignalExisting(name));      // 신호 전송 성공
        Assert.True(got.Wait(TimeSpan.FromSeconds(2)));             // 콜백 수신
    }

    [Fact]
    public void SignalExisting_returns_false_when_no_listener()
    {
        if (!OperatingSystem.IsWindows()) return;
        Assert.False(SingleInstanceSignal.SignalExisting("HermaTest_absent_" + Guid.NewGuid().ToString("N")));
    }
}
