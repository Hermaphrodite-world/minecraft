using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HermaLauncher.Services;

// packwiz 동기화 후 클라이언트 기본값을 적용한다. 정책 = "런처 실행 시 모드팩 기본값을 강제"(사용자 요청):
//   - 리소스팩: resourcepacks/ 의 모든 팩을 매 실행 options.txt 에 활성 보장(빠졌으면 재추가).
//   - 쉐이더  : iris.properties 의 shaderPack 이 비었거나 없는 팩을 가리키면 기본 쉐이더로 보정.
//               단 사용자가 고른 "유효한"(현재 존재하는) 쉐이더는 보존(번들 쉐이더 간 전환 허용).
//   - 서버목록: servers.dat 에 명명된 모드팩 서버 항목 보장(ServerList).
//   - best-effort — 실패해도 게임 실행/설치를 막지 않는다.
// ※ 과거의 "1회만 적용(herma_launcher_applied.txt 마커)" 정책은 폐기 — 친구가 게임 내에서 끈 팩이
//    다음 실행에 다시 켜져야 한다는 요구에 따라 매 실행 강제로 변경.
public static class ClientDefaults
{
    // 런처 Play / 공식 런처 installer 양 경로의 단일 진입점 — packwiz 동기화 후 호출.
    public static void ApplyAll(string gameDir, IProgress<StageUpdate>? progress = null)
    {
        ServerList.Ensure(gameDir, LauncherConfig.ServerListName,
                          LauncherConfig.ServerIp, LauncherConfig.ServerPort, progress);
        EnsureDefaultShader(gameDir, progress);
        EnsureDefaultResourcePacks(gameDir, progress);
    }

    // shaderpacks/ 의 쉐이더팩을 Iris 기본 활성으로 보장.
    //   - iris.properties 없음 → 기본 쉐이더로 생성.
    //   - 있음 + shaderPack 비었거나 더 이상 없는 팩 → 기본 쉐이더로 보정(다른 Iris 설정 키는 보존).
    //   - 있음 + shaderPack 이 현재 존재하는 팩 → 사용자 선택으로 보존.
    private static void EnsureDefaultShader(string gameDir, IProgress<StageUpdate>? progress)
    {
        try
        {
            var shaderDir = Path.Combine(gameDir, "shaderpacks");
            if (!Directory.Exists(shaderDir))
                return;

            var packs = Directory.GetFiles(shaderDir, "*.zip");
            if (packs.Length == 0)
                return;

            var presentNames = packs.Select(Path.GetFileName).Where(n => n is not null)
                                    .Select(n => n!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var chosen = packs.FirstOrDefault(p =>
                Path.GetFileName(p).StartsWith(LauncherConfig.DefaultShaderPackPrefix, StringComparison.OrdinalIgnoreCase))
                ?? packs[0];
            var name = Path.GetFileName(chosen); // Iris 는 zip 쉐이더팩을 확장자 포함 파일명으로 가리킴

            var configDir = Path.Combine(gameDir, "config");
            var irisProps = Path.Combine(configDir, "iris.properties");

            if (!File.Exists(irisProps))
            {
                Directory.CreateDirectory(configDir);
                AtomicWrite(irisProps,
                    "# Herma Launcher 기본 쉐이더 (끄거나 바꾸려면: 게임 내 비디오 설정 > 쉐이더팩)\n" +
                    "enableShaders=true\n" +
                    "shaderPack=" + name + "\n");
                progress?.Report(StageUpdate.Of(LaunchStage.Packwiz, $"쉐이더 기본 적용: {name}"));
                return;
            }

            // 기존 iris.properties 의 shaderPack 검사 — 유효한 사용자 선택은 보존.
            var lines = File.ReadAllLines(irisProps).ToList();
            var spIdx = lines.FindIndex(l => l.StartsWith("shaderPack=", StringComparison.Ordinal));
            var current = spIdx >= 0 ? lines[spIdx]["shaderPack=".Length..].Trim() : string.Empty;
            if (current.Length > 0 && presentNames.Contains(current))
                return; // 사용자가 고른 유효 쉐이더 → 보존

            // 비었거나(또는 없어진 팩) → 기본 쉐이더로 보정. 다른 키(colorSpace 등)는 그대로 둔다.
            if (spIdx >= 0) lines[spIdx] = "shaderPack=" + name;
            else lines.Add("shaderPack=" + name);
            var enIdx = lines.FindIndex(l => l.StartsWith("enableShaders=", StringComparison.Ordinal));
            if (enIdx >= 0) lines[enIdx] = "enableShaders=true";
            else lines.Add("enableShaders=true");
            AtomicWrite(irisProps, string.Join("\n", lines) + "\n");
            progress?.Report(StageUpdate.Of(LaunchStage.Packwiz, $"쉐이더 기본 적용: {name}"));
        }
        catch
        {
            // 쉐이더 기본값은 best-effort — 실패해도 진행을 막지 않는다.
        }
    }

    // resourcepacks/ 의 모든 팩을 options.txt 에 활성 보장(매 실행 강제, additive).
    //   - 빠진 팩만 추가(이미 활성인 팩/순서/다른 항목은 보존). 베이스 먼저, 확장(Extension/Addon) 나중.
    //   - 값은 "file/<파일명>". 일부 팩은 "incompatible" 로 떠 조용히 제거되므로 incompatibleResourcePacks 에도 화이트리스트.
    private static void EnsureDefaultResourcePacks(string gameDir, IProgress<StageUpdate>? progress)
    {
        try
        {
            var rpDir = Path.Combine(gameDir, "resourcepacks");
            if (!Directory.Exists(rpDir))
                return;
            var present = Directory.GetFiles(rpDir, "*.zip")
                .Select(Path.GetFileName).Where(n => n is not null).Select(n => n!).ToList();
            if (present.Count == 0)
                return;

            var optionsPath = Path.Combine(gameDir, "options.txt");
            var nl = Environment.NewLine; // MC 는 Windows=CRLF / Unix=LF
            var lines = File.Exists(optionsPath) ? File.ReadAllLines(optionsPath).ToList() : new List<string>();

            var active = ParsePackArray(lines, "resourcePacks:");
            var incompat = ParsePackArray(lines, "incompatibleResourcePacks:");
            if (active.Count == 0)
                active.Add("\"vanilla\""); // vanilla 는 항상 최하단

            var changed = false;
            var addedCount = 0;
            foreach (var n in present
                .OrderBy(x => IsExtensionPack(x) ? 1 : 0)
                .ThenBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var entry = "\"file/" + n + "\"";
                if (!active.Contains(entry)) { active.Add(entry); changed = true; addedCount++; }
                if (!incompat.Contains(entry)) { incompat.Add(entry); changed = true; }
            }
            if (!changed)
                return; // 모든 팩이 이미 활성 + 화이트리스트됨 — 손대지 않음(불필요한 쓰기 방지)

            SetOrAddLine(lines, "resourcePacks:", "resourcePacks:[" + string.Join(",", active) + "]");
            SetOrAddLine(lines, "incompatibleResourcePacks:", "incompatibleResourcePacks:[" + string.Join(",", incompat) + "]");
            AtomicWrite(optionsPath, string.Join(nl, lines) + nl);
            progress?.Report(StageUpdate.Of(LaunchStage.Packwiz,
                addedCount > 0 ? $"리소스팩 {addedCount}개 적용" : "리소스팩 적용"));
        }
        catch
        {
            // 리소스팩 기본값도 best-effort.
        }
    }

    // options.txt 의 "key:[\"a\",\"b\"]" 배열을 따옴표 포함 항목 리스트로 파싱(없으면 빈 리스트).
    private static List<string> ParsePackArray(List<string> lines, string keyPrefix)
    {
        var result = new List<string>();
        var i = lines.FindIndex(l => l.StartsWith(keyPrefix, StringComparison.Ordinal));
        if (i < 0)
            return result;
        var val = lines[i][keyPrefix.Length..].Trim();
        if (val.StartsWith("[")) val = val[1..];
        if (val.EndsWith("]")) val = val[..^1];
        foreach (var part in val.Split(','))
        {
            var t = part.Trim();
            if (t.Length > 0)
                result.Add(t); // 따옴표 포함 그대로(예: "vanilla", "file/x.zip", lambdabettergrass:default)
        }
        return result;
    }

    private static void SetOrAddLine(List<string> lines, string keyPrefix, string fullLine)
    {
        var i = lines.FindIndex(l => l.StartsWith(keyPrefix, StringComparison.Ordinal));
        if (i < 0) lines.Add(fullLine);
        else lines[i] = fullLine;
    }

    private static bool IsExtensionPack(string fileName)
        => fileName.Contains("Extension", StringComparison.OrdinalIgnoreCase)
        || fileName.Contains("Addon", StringComparison.OrdinalIgnoreCase);

    // 텍스트 파일 원자적 쓰기(.tmp → replace) — crash/전원손실 시 torn file 방지(Codex).
    private static void AtomicWrite(string path, string content)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content);
        if (File.Exists(path)) File.Replace(tmp, path, null);
        else File.Move(tmp, path);
    }
}
