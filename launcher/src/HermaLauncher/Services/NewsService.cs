using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HermaLauncher.Services;

// 운영자 공지/점검 원격 fetch(예: GitHub Pages news.json). best-effort — URL 미설정/네트워크 실패 = null(비차단).
public static class NewsService
{
    public static async Task<NewsFeed?> FetchAsync(string url, CancellationToken ct, int timeoutMs = 4000)
    {
        if (string.IsNullOrWhiteSpace(url)) return null; // 미설정 → 기능 off
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
            var json = await http.GetStringAsync(url, ct).ConfigureAwait(false);
            return NewsFeed.Parse(json);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch
        {
            return null; // 공지 fetch 실패는 조용히 무시(런처 흐름 비차단)
        }
    }
}
