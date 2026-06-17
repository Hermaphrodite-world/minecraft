using System;
using System.IO;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 자동접속 endpoint + servers.dat 등록 일관성 회귀 가드.
//   핵심 버그: quickPlay 와 servers.dat 가 다른 주소를 써서 같은 LAN 의 다른 PC 가 서버목록 항목으론 못 닿던 문제.
//   이제 둘 다 동일 ServerEndpoint.Host 를 쓴다 — 아래 테스트로 "넘긴 host 가 그대로 servers.dat 에 기록됨"을 고정.
public class ServerEndpointTests
{
    [Fact]
    public void Address_is_host_colon_port()
        => Assert.Equal("192.168.219.102:25565",
            new ServerEndpoint("192.168.219.102", 25565, ServerHostResolver.Source.UserOverride, true, 5).Address);

    [Fact]
    public void PublicFallback_uses_config_public_ip()
    {
        var fb = ServerEndpoint.PublicFallback;
        Assert.Equal(LauncherConfig.ServerIp, fb.Host);
        Assert.Equal(LauncherConfig.ServerPort, fb.Port);
        Assert.Equal(ServerHostResolver.Source.Public, fb.Source);
    }

    // ★ 핵심 회귀 가드: ServerList.Ensure 에 넘긴 host 가 servers.dat 의 ip 로 그대로 기록되는지.
    //   (이전엔 ApplyAll 이 항상 공개 IP 를 넘겨, override 사용자의 서버목록 항목이 안 닿는 주소였음.)
    [Fact]
    public void Ensure_writes_passed_host_as_servers_dat_ip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "herma-srvlist-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            ServerList.Ensure(dir, "Hermaphrodite World", "192.168.219.102", 25565);

            var root = Nbt.ReadFile(Path.Combine(dir, "servers.dat"));
            var servers = Assert.IsType<NbtList>(root.Get("servers"));
            var entry = Assert.IsType<NbtCompound>(servers.Items[0]);
            Assert.Equal("192.168.219.102:25565", entry.GetString("ip"));   // override host 가 그대로 기록
            Assert.Equal("Hermaphrodite World", entry.GetString("name"));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    // 빈 host 는 기록하지 않는다(방어) — Ensure 가 조용히 무시.
    [Fact]
    public void Ensure_ignores_blank_host()
    {
        var dir = Path.Combine(Path.GetTempPath(), "herma-srvlist-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            ServerList.Ensure(dir, "Hermaphrodite World", "  ", 25565);
            Assert.False(File.Exists(Path.Combine(dir, "servers.dat"))); // 빈 host → 미기록
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
