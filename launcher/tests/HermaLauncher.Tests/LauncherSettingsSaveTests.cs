using System;
using System.IO;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 설정 저장/읽기 result 계약(Codex UX-R1/Test-R3) — 성공 true / 실패 false(throw 없음) / 누락 기본값.
public class LauncherSettingsSaveTests
{
    [Fact]
    public void Save_then_load_roundtrips_via_path()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"herma-settings-{Guid.NewGuid():N}.json");
        try
        {
            Assert.True(new LauncherSettings { MaxRamMbOverride = 6144 }.Save(tmp));
            Assert.Equal(6144, LauncherSettings.Load(tmp).MaxRamMbOverride);
        }
        finally { try { File.Delete(tmp); } catch { /* best-effort */ } }
    }

    [Fact]
    public void Save_returns_false_on_unwritable_path()
    {
        // 부모 디렉토리가 없는 경로 → 쓰기 실패 → false(예외 전파 안 함).
        var bad = Path.Combine(Path.GetTempPath(), $"herma-nodir-{Guid.NewGuid():N}", "sub", "settings.json");
        Assert.False(new LauncherSettings { MaxRamMbOverride = 4096 }.Save(bad));
    }

    [Fact]
    public void Load_missing_path_returns_defaults()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"herma-missing-{Guid.NewGuid():N}.json");
        Assert.True(LauncherSettings.Load(missing).IsRamAuto); // override null = 자동
    }
}
