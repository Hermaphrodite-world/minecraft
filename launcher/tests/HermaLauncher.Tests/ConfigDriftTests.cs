using System;
using System.IO;
using HermaLauncher;
using Xunit;

namespace HermaLauncher.Tests;

// P4-2: 런처 고정값 ↔ 모드팩(pack.toml) drift 가드. MC/Fabric 버전이 어긋나면
// 런처가 모드팩과 다른 버전을 설치/실행해 침묵 실패 → 빌드(테스트)에서 차단.
public class ConfigDriftTests
{
    private static string PackTomlPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "modpack", "pack.toml");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repo 루트(modpack/pack.toml)를 찾지 못했어요.");
    }

    [Fact]
    public void Launcher_minecraft_version_matches_packtoml()
    {
        var toml = File.ReadAllText(PackTomlPath());
        Assert.Contains($"minecraft = \"{LauncherConfig.MinecraftVersion}\"", toml);
    }

    [Fact]
    public void Launcher_fabric_version_matches_packtoml()
    {
        var toml = File.ReadAllText(PackTomlPath());
        Assert.Contains($"fabric = \"{LauncherConfig.FabricLoaderVersion}\"", toml);
    }
}
