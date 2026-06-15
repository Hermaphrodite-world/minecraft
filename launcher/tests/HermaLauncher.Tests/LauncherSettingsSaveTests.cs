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

    [Fact]
    public void ServerHostOverride_roundtrips_via_path()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"herma-settings-{Guid.NewGuid():N}.json");
        try
        {
            Assert.True(new LauncherSettings { ServerHostOverride = "192.168.219.102" }.Save(tmp));
            var back = LauncherSettings.Load(tmp);
            Assert.Equal("192.168.219.102", back.ServerHostOverride);
            Assert.True(back.HasServerHostOverride);
        }
        finally { try { File.Delete(tmp); } catch { /* best-effort */ } }
    }

    [Fact]
    public void Both_ram_and_server_override_persist_together()
    {
        // SaveSettings 가 두 필드를 한 객체로 저장 — 한쪽 저장이 다른 쪽을 지우지 않는지 회귀 가드.
        var tmp = Path.Combine(Path.GetTempPath(), $"herma-settings-{Guid.NewGuid():N}.json");
        try
        {
            Assert.True(new LauncherSettings { MaxRamMbOverride = 6144, ServerHostOverride = "10.0.0.2" }.Save(tmp));
            var back = LauncherSettings.Load(tmp);
            Assert.Equal(6144, back.MaxRamMbOverride);
            Assert.Equal("10.0.0.2", back.ServerHostOverride);
        }
        finally { try { File.Delete(tmp); } catch { /* best-effort */ } }
    }

    [Fact]
    public void Default_has_no_server_override()
        => Assert.False(new LauncherSettings().HasServerHostOverride);

    [Fact]
    public void HasSeenWelcome_defaults_false_and_roundtrips()
    {
        Assert.False(new LauncherSettings().HasSeenWelcome);
        var tmp = Path.Combine(Path.GetTempPath(), $"herma-settings-{Guid.NewGuid():N}.json");
        try
        {
            Assert.True(new LauncherSettings { HasSeenWelcome = true }.Save(tmp));
            Assert.True(LauncherSettings.Load(tmp).HasSeenWelcome);
        }
        finally { try { File.Delete(tmp); } catch { /* best-effort */ } }
    }

    [Fact]
    public void Save_preserves_unrelated_fields_on_load_modify_save()
    {
        // SaveSettings 의 load-modify-save 패턴 회귀 가드: RAM 만 바꿔 저장해도 HasSeenWelcome/서버주소 보존.
        var tmp = Path.Combine(Path.GetTempPath(), $"herma-settings-{Guid.NewGuid():N}.json");
        try
        {
            Assert.True(new LauncherSettings { HasSeenWelcome = true, ServerHostOverride = "10.0.0.9" }.Save(tmp));
            var s = LauncherSettings.Load(tmp);
            s.MaxRamMbOverride = 4096; // RAM 만 변경
            Assert.True(s.Save(tmp));
            var back = LauncherSettings.Load(tmp);
            Assert.True(back.HasSeenWelcome);                    // 보존
            Assert.Equal("10.0.0.9", back.ServerHostOverride);   // 보존
            Assert.Equal(4096, back.MaxRamMbOverride);           // 변경 반영
        }
        finally { try { File.Delete(tmp); } catch { /* best-effort */ } }
    }
}
