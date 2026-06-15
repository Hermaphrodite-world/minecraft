using System;
using System.Threading;
using System.Threading.Tasks;

namespace HermaLauncher.Services;

// 일시적(transient) 네트워크 오류에 한해 지수 백오프로 재시도(순수 유틸 — 단위 테스트 가능).
// 취소(OperationCanceledException)는 절대 재시도하지 않고 즉시 전파. 구조적 오류(4xx 등)는 isTransient=false 로 즉시 실패.
// 근거: CLAUDE.md '자율 fix 루프 중단 조건' — transient 만 재시도, structural/cancel 은 즉시 중단.
public static class RetryPolicy
{
    // action(attempt 1-based, ct) 실행. transient 면 maxAttempts 까지 재시도. delayAsync=null 이면 기본 지수 백오프.
    public static async Task<T> ExecuteAsync<T>(
        Func<int, CancellationToken, Task<T>> action,
        Func<Exception, bool> isTransient,
        int maxAttempts,
        Func<int, CancellationToken, Task>? delayAsync,
        CancellationToken ct)
    {
        delayAsync ??= static (attempt, c) =>
            Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1)), c);

        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return await action(attempt, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw; // 취소는 재시도 안 함
            }
            catch (Exception ex) when (isTransient(ex) && attempt < maxAttempts)
            {
                last = ex;
                await delayAsync(attempt, ct).ConfigureAwait(false);
            }
        }
        // 도달 불가(마지막 시도는 위에서 return 하거나 throw) — 컴파일러 만족용.
        throw last ?? new InvalidOperationException("재시도 정책: 시도 없음");
    }
}
