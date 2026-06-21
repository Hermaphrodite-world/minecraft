using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// LaunchOrchestrator 의 단계 순서 / 단축회로 / 취소 / 단계오류 매핑 검증(Codex Test-R1).
// fake 서비스로 격리 — 실제 네트워크/파일시스템 미접촉(packwiz 직후 sentinel 로 끊어 ClientDefaults.ApplyAll
// 의 실 GameDir 접근을 피한다 → 테스트가 사용자 런처 상태를 건드리지 않음).
public class LaunchOrchestratorTests
{
    private sealed class RecordingProgress : IProgress<StageUpdate>
    {
        public readonly List<StageUpdate> Items = new();
        public void Report(StageUpdate value) { lock (Items) Items.Add(value); }
    }

    private sealed class FakeUpdate(List<string> log, bool restart = false) : IUpdateService
    {
        public Task<bool> CheckAndApplyAsync(IProgress<StageUpdate> p, CancellationToken ct)
        { log.Add("update"); return Task.FromResult(restart); }
    }

    private sealed class FakeAuth(List<string> log, Exception? throwOnAuth = null) : IAuthService
    {
        public Task<AuthSession> AuthenticateAsync(LaunchOptions o, IProgress<StageUpdate> p, CancellationToken ct)
        {
            log.Add("auth");
            if (throwOnAuth is not null) throw throwOnAuth;
            return Task.FromResult(new AuthSession("Tester", "uuid", "tok", false));
        }
        public Task<AuthSession> RevalidateAsync(AuthSession s, IProgress<StageUpdate> p, CancellationToken ct)
        { log.Add("revalidate"); return Task.FromResult(s); }
    }

    private sealed class FakeMinecraft(List<string> log) : IMinecraftService
    {
        public Task<string> EnsureJavaAsync(IProgress<StageUpdate> p, CancellationToken ct)
        { log.Add("ensureJava"); return Task.FromResult("java"); }
        public Task<Process> LaunchAsync(AuthSession s, ServerEndpoint endpoint, IProgress<StageUpdate> p, CancellationToken ct, bool autoConnect = true)
        { log.Add("launch"); return Task.FromResult(Process.GetCurrentProcess()); }
    }

    // packwiz 단계에서 기록 후 sentinel 던짐 → ApplyAll 이전에 흐름을 끊어 실 파일시스템 미접촉.
    private sealed class SentinelPackwiz(List<string> log) : IPackwizService
    {
        public Task RunAsync(string java, string url, IProgress<StageUpdate> p, CancellationToken ct, string? folder = null)
        { log.Add("packwiz"); throw new InvalidOperationException("sentinel"); }
    }

    private sealed class RecordingPackwiz(List<string> log) : IPackwizService
    {
        public Task RunAsync(string java, string url, IProgress<StageUpdate> p, CancellationToken ct, string? folder = null)
        { log.Add("packwiz"); return Task.CompletedTask; }
    }

    private static readonly LaunchOptions Opts = new("Tester", false);

    [Fact]
    public async Task Steps_run_in_documented_order_up_to_packwiz()
    {
        // 핵심 불변식: update → auth → ensureJava → packwiz (Java-before-packwiz 닭/달걀).
        var log = new List<string>();
        var orch = new LaunchOrchestrator(new FakeUpdate(log), new FakeAuth(log), new FakeMinecraft(log), new SentinelPackwiz(log));
        var game = await orch.RunAsync(Opts, new RecordingProgress(), CancellationToken.None);
        Assert.Null(game); // sentinel → generic catch → null
        Assert.Equal(new[] { "update", "auth", "ensureJava", "packwiz" }, log);
    }

    [Fact]
    public async Task Update_restart_short_circuits_before_auth()
    {
        var log = new List<string>();
        var orch = new LaunchOrchestrator(new FakeUpdate(log, restart: true), new FakeAuth(log), new FakeMinecraft(log), new RecordingPackwiz(log));
        var game = await orch.RunAsync(Opts, new RecordingProgress(), CancellationToken.None);
        Assert.Null(game);
        Assert.Equal(new[] { "update" }, log); // auth 미호출(재시작 단축회로)
    }

    // 주의(Codex S1 재리뷰): 본 테스트는 orchestrator 의 취소 단축회로(null 반환 + "취소" 보고)만 검증한다.
    // S1 의 실제 자식 java 프로세스 kill 경로는 PackwizService 의 ct.Register 가 담당하며, 실 자식
    // 프로세스가 필요해 여기서 단위 검증하지 않는다(integration 영역). 이 테스트를 S1 kill 커버리지로 오인 금지.
    [Fact]
    public async Task Precancelled_returns_null_and_reports_cancel()
    {
        var log = new List<string>();
        var prog = new RecordingProgress();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var orch = new LaunchOrchestrator(new FakeUpdate(log), new FakeAuth(log), new FakeMinecraft(log), new RecordingPackwiz(log));
        var game = await orch.RunAsync(Opts, prog, cts.Token);
        Assert.Null(game);
        Assert.Contains(prog.Items, u => u.Message.Contains("취소"));
    }

    [Fact]
    public async Task Stage_error_is_reported_with_its_stage()
    {
        var log = new List<string>();
        var prog = new RecordingProgress();
        var orch = new LaunchOrchestrator(
            new FakeUpdate(log), new FakeAuth(log, new LaunchStageException(LaunchStage.Auth, "소유 안 함")),
            new FakeMinecraft(log), new RecordingPackwiz(log));
        var game = await orch.RunAsync(Opts, prog, CancellationToken.None);
        Assert.Null(game);
        Assert.Contains(prog.Items, u => u.IsError && u.Stage == LaunchStage.Auth && u.Message.Contains("소유 안 함"));
    }
}
