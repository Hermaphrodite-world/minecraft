using System;
using System.IO;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 1회성 캐시 정리 — stale .bobby 를 토큰당 정확히 1회만 삭제. 이후 사용자가 다시 쌓은 캐시는 보존.
public class CacheMaintenanceTests
{
    [Fact]
    public void Clears_bobby_once_then_respects_recached()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), $"herma-cache-{Guid.NewGuid():N}");
        var bobby = Path.Combine(gameDir, ".bobby");
        Directory.CreateDirectory(bobby);
        try
        {
            File.WriteAllText(Path.Combine(bobby, "old-chunk.dat"), "stale");

            // 1회차: 정리됨 + 마커 기록
            CacheMaintenance.RunOnce(gameDir);
            Assert.False(Directory.Exists(bobby)); // stale 캐시 삭제됨
            Assert.True(File.Exists(Path.Combine(gameDir, "herma_maintenance.txt"))); // 1회 실행 기록

            // 사용자가 다시 캐시를 쌓음(재탐험)
            Directory.CreateDirectory(bobby);
            File.WriteAllText(Path.Combine(bobby, "fresh-chunk.dat"), "fresh");

            // 2회차: 같은 토큰이라 no-op — 사용자 캐시 보존(매번 안 날림)
            CacheMaintenance.RunOnce(gameDir);
            Assert.True(Directory.Exists(bobby));
            Assert.True(File.Exists(Path.Combine(bobby, "fresh-chunk.dat")));
        }
        finally { try { Directory.Delete(gameDir, recursive: true); } catch { /* best-effort */ } }
    }

    [Fact]
    public void No_bobby_still_marks_done()
    {
        var gameDir = Path.Combine(Path.GetTempPath(), $"herma-cache-{Guid.NewGuid():N}");
        Directory.CreateDirectory(gameDir);
        try
        {
            CacheMaintenance.RunOnce(gameDir); // .bobby 없음 → 할 일 없지만 토큰은 기록(다음부터 skip)
            Assert.True(File.Exists(Path.Combine(gameDir, "herma_maintenance.txt")));
        }
        finally { try { Directory.Delete(gameDir, recursive: true); } catch { /* best-effort */ } }
    }
}
