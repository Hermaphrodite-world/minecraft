using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 게임 로그 → 한 가지 한국어 액션 분류. 매칭 없음/빈 입력 = null(억지 추측 금지).
public class FailureDiagnosisTests
{
    [Fact]
    public void Null_or_empty_is_null()
    {
        Assert.Null(FailureDiagnosis.Classify(null));
        Assert.Null(FailureDiagnosis.Classify(""));
    }

    [Fact]
    public void Unknown_log_is_null()
        => Assert.Null(FailureDiagnosis.Classify("[Render thread] Joining world\n[Sound] OK"));

    [Fact]
    public void OutOfMemory_classified()
        => Assert.Contains("메모리", FailureDiagnosis.Classify(
               "Exception in thread \"main\" java.lang.OutOfMemoryError: Java heap space")!.Value.Title);

    [Fact]
    public void Whitelist_classified()
        => Assert.Contains("화이트리스트", FailureDiagnosis.Classify(
               "Disconnected: You are not white-listed on this server!")!.Value.Title);

    [Fact]
    public void InvalidSession_classified()
        => Assert.Contains("세션", FailureDiagnosis.Classify(
               "Internal Exception: Invalid session (Try restarting your game)")!.Value.Title);

    [Fact]
    public void ModMismatch_classified()
        => Assert.Contains("모드", FailureDiagnosis.Classify(
               "This server requires the following mods: fabric-api 0.100.0")!.Value.Title);

    [Fact]
    public void ConnectionRefused_classified()
        => Assert.Contains("연결", FailureDiagnosis.Classify(
               "io.netty.channel.AbstractChannel$AnnotatedConnectException: Connection refused: no further information")!.Value.Title);

    [Fact]
    public void Case_insensitive_match()
        => Assert.NotNull(FailureDiagnosis.Classify("FATAL: OUTOFMEMORYERROR"));
}
