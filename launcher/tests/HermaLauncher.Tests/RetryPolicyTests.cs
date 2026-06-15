using System;
using System.Threading;
using System.Threading.Tasks;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// transient 재시도 정책 — 성공/재시도/구조적 즉시실패/소진/취소.
public class RetryPolicyTests
{
    private static Task NoDelay(int attempt, CancellationToken ct) => Task.CompletedTask;

    [Fact]
    public async Task Succeeds_first_try_without_retry()
    {
        var calls = 0;
        var r = await RetryPolicy.ExecuteAsync<int>(
            (a, c) => { calls++; return Task.FromResult(42); },
            _ => true, maxAttempts: 3, NoDelay, default);
        Assert.Equal(42, r);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Retries_transient_then_succeeds()
    {
        var calls = 0;
        var r = await RetryPolicy.ExecuteAsync<int>(
            (a, c) =>
            {
                calls++;
                if (calls < 3) throw new InvalidOperationException("transient");
                return Task.FromResult(7);
            },
            _ => true, maxAttempts: 3, NoDelay, default);
        Assert.Equal(7, r);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task Non_transient_fails_immediately()
    {
        var calls = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryPolicy.ExecuteAsync<int>(
                (a, c) => { calls++; throw new InvalidOperationException(); },
                _ => false, maxAttempts: 3, NoDelay, default));
        Assert.Equal(1, calls); // 재시도 안 함
    }

    [Fact]
    public async Task Exhausts_attempts_then_throws_last()
    {
        var calls = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RetryPolicy.ExecuteAsync<int>(
                (a, c) => { calls++; throw new InvalidOperationException("t" + calls); },
                _ => true, maxAttempts: 3, NoDelay, default));
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task Cancellation_is_not_retried()
    {
        var calls = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            RetryPolicy.ExecuteAsync<int>(
                (a, c) => { calls++; throw new OperationCanceledException(); },
                _ => true, maxAttempts: 3, NoDelay, default));
        Assert.Equal(1, calls);
    }
}
