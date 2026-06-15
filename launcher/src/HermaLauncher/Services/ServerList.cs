using System;
using System.IO;
using System.Linq;

namespace HermaLauncher.Services;

// servers.dat(멀티플레이 서버 목록, 비압축 NBT)에 모드팩 서버를 "명명된 항목"으로 보장한다.
//   - 같은 host 를 가리키는 기존 항목은 제거(quickPlayMultiplayer 가 만든 generic "Minecraft " 중복 정리).
//   - 우리 항목을 목록 맨 위에 추가 → 친구가 멀티플레이 메뉴에서 바로 본다.
//   - 다른 서버 항목(host 불일치)은 보존.
//   - best-effort — 실패해도 게임 실행을 막지 않는다.
public static class ServerList
{
    public static void Ensure(string gameDir, string displayName, string host, int port,
                              IProgress<StageUpdate>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(host))
            return;
        try
        {
            var path = Path.Combine(gameDir, "servers.dat");

            NbtCompound root;
            if (File.Exists(path))
            {
                NbtCompound? parsed = null;
                try { parsed = Nbt.ReadFile(path); } catch { parsed = null; }
                if (parsed is not null && IsWellFormed(parsed))
                {
                    root = parsed;
                }
                else
                {
                    // 손상/형식 이상 → 백업이 성공해야만 재작성(원본 데이터 보호). 백업 실패 시 수정 포기.
                    if (!TryBackup(path)) return;
                    root = new NbtCompound();
                }
            }
            else
            {
                root = new NbtCompound();
            }

            // servers 리스트 확보(없거나 비-list 였던 경우 신규로 교체).
            NbtList servers;
            if (root.Get("servers") is NbtList existing)
            {
                servers = existing;
            }
            else
            {
                root.Children.RemoveAll(c => string.Equals(c.Name, "servers", StringComparison.Ordinal));
                servers = new NbtList { Name = "servers", ElementId = 10 };
                root.Children.Add(servers);
            }

            // quickPlayMultiplayer 인자({ip}:{port})와 동일 형식 → MC 가 같은 서버로 인식해 generic 중복 추가 회피.
            var ip = $"{host}:{port}";

            // 같은 host 를 가리키는 기존 항목 제거(중복/generic 정리). host 만 비교(포트 무관).
            servers.Items.RemoveAll(t =>
                t is NbtCompound c && HostOf(c.GetString("ip")).Equals(host, StringComparison.OrdinalIgnoreCase));

            // 위에서 같은 host 항목을 모두 제거했으니, 우리 항목을 맨 앞(Insert 0)에 새로 추가한다.
            // (dedup 키는 host 한정 — name/port 는 비교하지 않음. 코드리뷰: 기존 'name+ip 중복방지' 주석은 실제
            //  로직과 불일치라 정정.)
            var entry = new NbtCompound();
            entry.Children.Add(new NbtString { Name = "name", Value = displayName });
            entry.Children.Add(new NbtString { Name = "ip", Value = ip });
            servers.Items.Insert(0, entry);
            servers.ElementId = 10; // 항목이 생겼으니 compound 타입 보장

            Nbt.WriteFile(path, root);
            progress?.Report(StageUpdate.Of(LaunchStage.Packwiz, $"서버 목록에 '{displayName}' 등록"));
        }
        catch (Exception ex)
        {
            // best-effort — servers.dat 갱신 실패가 게임 실행을 막지 않는다. 단 로그엔 남긴다(P1-8).
            AppLog.Warn(LaunchStage.Packwiz, "서버 목록(servers.dat) 갱신 실패: " + ex.Message);
        }
    }

    // servers 키가 없거나(신규 추가 가능) TAG_List(of compound) 이면 정상으로 본다.
    private static bool IsWellFormed(NbtCompound root)
    {
        var s = root.Get("servers");
        if (s is null) return true;
        if (s is not NbtList list) return false;
        return list.Items.All(it => it is NbtCompound);
    }

    private static bool TryBackup(string path)
    {
        try { File.Copy(path, path + ".bak", overwrite: true); return File.Exists(path + ".bak"); }
        catch { return false; }
    }

    // "host" 또는 "host:port" 에서 host 부분만. (IPv4/도메인 전제 — IPv6 미사용.)
    private static string HostOf(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return string.Empty;
        var idx = ip.LastIndexOf(':');
        // 마지막 ':' 뒤가 숫자면 포트로 보고 제거.
        if (idx > 0 && idx < ip.Length - 1 && ip[(idx + 1)..].All(char.IsDigit))
            return ip[..idx];
        return ip;
    }
}
