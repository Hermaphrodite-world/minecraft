using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HermaLauncher.Services;

// packwiz 동기화 후 클라이언트 기본값을 적용한다. 정책 = "전부 적용이 기본 + 사용자 변경 존중"(사용자 요청 2026-06-15):
//   - 리소스팩: 각 팩을 "처음 1회만" 자동 활성(마커 herma_launcher_applied.txt 로 per-pack 기록).
//               사용자가 게임 내에서 끈 팩은 다음 실행에 다시 켜지 않는다(존중). 새 팩만 자동 활성.
//   - whitelist: 현재 활성(resourcePacks)인 file/ 팩은 incompatibleResourcePacks 에도 보장(매번 idempotent).
//               → MC 26.1.2 가 호환성 미달이라며 조용히 드롭하는 것을 막는다(빨강이라도 로드). 비활성 팩은 안 건드림.
//   - 쉐이더  : 처음 1회만 기본 쉐이더 적용(마커). 이후 사용자가 고른 유효 쉐이더/끈 상태는 보존.
//   - 서버목록: servers.dat 에 명명된 모드팩 서버 항목 보장(ServerList).
//   - best-effort — 실패해도 게임 실행/설치를 막지 않는다.
// ※ "매 실행 강제"(과거 eba1135) 는 사용자 수동 정렬까지 매번 덮어써서 폐기. 대신 per-pack apply-once 로 복귀하되
//    실제 로드 버그(whitelist 누락으로 vanilla-connected-glass 외 전부 드롭)는 whitelist-ensure 로 별도 해결.
public static class ClientDefaults
{
    private const string MarkerFile = "herma_launcher_applied.txt";

    // 런처 Play / 공식 런처 installer 양 경로의 단일 진입점 — packwiz 동기화 후 호출.
    public static void ApplyAll(string gameDir, IProgress<StageUpdate>? progress = null)
    {
        ServerList.Ensure(gameDir, LauncherConfig.ServerListName,
                          LauncherConfig.ServerIp, LauncherConfig.ServerPort, progress);
        EnsureDefaultShader(gameDir, progress);
        EnsureDefaultResourcePacks(gameDir, progress);
    }

    // shaderpacks/ 의 쉐이더팩을 Iris 기본 활성으로 보장 — 처음 1회만(마커).
    //   - 마커에 shader 기록 있음 → 사용자가 이미 1회 적용받음 → 손대지 않음(끄거나 바꾼 상태 존중).
    //   - iris.properties 없음 → 기본 쉐이더로 생성 + 마커 기록.
    //   - 있음 + shaderPack 비었거나 더 이상 없는 팩 → 기본 쉐이더로 보정 + 마커 기록(다른 Iris 키는 보존).
    //   - 있음 + shaderPack 이 현재 존재하는 팩 → 사용자 선택으로 보존(+ 마커 기록만).
    private static void EnsureDefaultShader(string gameDir, IProgress<StageUpdate>? progress)
    {
        try
        {
            var applied = LoadMarker(gameDir);
            if (applied.Any(k => k.StartsWith("shader:", StringComparison.Ordinal)))
                return; // 이미 1회 자동 적용함 → 사용자 결정 존중

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
                AppendMarker(gameDir, new[] { "shader:" + name });
                progress?.Report(StageUpdate.Of(LaunchStage.Packwiz, $"쉐이더 기본 적용: {name}"));
                return;
            }

            // 기존 iris.properties 의 shaderPack 검사 — 유효한 사용자 선택은 보존.
            var lines = File.ReadAllLines(irisProps).ToList();
            var spIdx = lines.FindIndex(l => l.StartsWith("shaderPack=", StringComparison.Ordinal));
            var current = spIdx >= 0 ? lines[spIdx]["shaderPack=".Length..].Trim() : string.Empty;
            if (current.Length > 0 && presentNames.Contains(current))
            {
                AppendMarker(gameDir, new[] { "shader:" + current }); // 이미 유효 선택 → 1회 적용 처리만
                return;
            }

            // 비었거나(또는 없어진 팩) → 기본 쉐이더로 보정. 다른 키(colorSpace 등)는 그대로 둔다.
            if (spIdx >= 0) lines[spIdx] = "shaderPack=" + name;
            else lines.Add("shaderPack=" + name);
            var enIdx = lines.FindIndex(l => l.StartsWith("enableShaders=", StringComparison.Ordinal));
            if (enIdx >= 0) lines[enIdx] = "enableShaders=true";
            else lines.Add("enableShaders=true");
            AtomicWrite(irisProps, string.Join("\n", lines) + "\n");
            AppendMarker(gameDir, new[] { "shader:" + name });
            progress?.Report(StageUpdate.Of(LaunchStage.Packwiz, $"쉐이더 기본 적용: {name}"));
        }
        catch
        {
            // 쉐이더 기본값은 best-effort — 실패해도 진행을 막지 않는다.
        }
    }

    // resourcepacks/ 의 팩을 options.txt 에 보장. 두 단계:
    //   (1) apply-once: 마커에 없는 팩만 resourcePacks 에 추가(처음 1회) + 마커 기록 → 사용자가 끈 팩은 존중.
    //       베이스 먼저, 확장(Extension/Addon) 나중. 값은 "file/<파일명>".
    //   (2) whitelist-ensure: 현재 활성인 file/ 팩은 incompatibleResourcePacks 에도 보장(매번, idempotent).
    //       일부 팩은 MC 26.1.2 에서 "incompatible" 로 떠 화이트리스트 없으면 조용히 드롭되므로 강제 로드.
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

            var applied = LoadMarker(gameDir);
            var optionsPath = Path.Combine(gameDir, "options.txt");
            var nl = Environment.NewLine; // MC 는 Windows=CRLF / Unix=LF
            var lines = File.Exists(optionsPath) ? File.ReadAllLines(optionsPath).ToList() : new List<string>();

            var active = ParsePackArray(lines, "resourcePacks:");
            var incompat = ParsePackArray(lines, "incompatibleResourcePacks:");
            if (active.Count == 0)
                active.Add("\"vanilla\""); // vanilla 는 항상 최하단

            var changed = false;
            var addedCount = 0;
            var newlyApplied = new List<string>();

            // (1) apply-once — 마커에 없는 팩만 처음 1회 활성(사용자가 끈 팩 존중).
            foreach (var n in present
                .OrderBy(x => IsExtensionPack(x) ? 1 : 0)
                .ThenBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var markerKey = "rp:" + n;
                if (applied.Contains(markerKey))
                    continue; // 이미 1회 자동 적용함 → 다시 켜지 않음
                var entry = "\"file/" + n + "\"";
                if (!active.Contains(entry)) { active.Add(entry); changed = true; addedCount++; }
                newlyApplied.Add(markerKey);
            }

            // (2) whitelist-ensure — 현재 활성(resourcePacks)인 file/ 팩은 incompat 에도 보장(매번).
            //     → MC 호환성 미달 드롭 방지. 비활성(사용자가 끈) 팩은 active 에 없으니 건드리지 않음.
            foreach (var entry in active)
            {
                if (!entry.StartsWith("\"file/", StringComparison.Ordinal))
                    continue; // "vanilla", lambdabettergrass:default 등 내장/네임스페이스 팩 제외(file/ 로 시작하는 zip 팩만)
                if (!incompat.Contains(entry)) { incompat.Add(entry); changed = true; }
            }

            if (changed)
            {
                SetOrAddLine(lines, "resourcePacks:", "resourcePacks:[" + string.Join(",", active) + "]");
                SetOrAddLine(lines, "incompatibleResourcePacks:", "incompatibleResourcePacks:[" + string.Join(",", incompat) + "]");
                AtomicWrite(optionsPath, string.Join(nl, lines) + nl);
                progress?.Report(StageUpdate.Of(LaunchStage.Packwiz,
                    addedCount > 0 ? $"리소스팩 {addedCount}개 적용" : "리소스팩 적용"));
            }

            // 새로 적용한 팩만 마커에 기록(쓰기 여부 무관 — 다음 실행부터 존중).
            if (newlyApplied.Count > 0)
                AppendMarker(gameDir, newlyApplied);
        }
        catch
        {
            // 리소스팩 기본값도 best-effort.
        }
    }

    // ── apply-once 마커(herma_launcher_applied.txt) — per-pack/shader "1회 적용" 기록 ──
    // 사용자가 특정 줄을 지우면 그 팩이 다음 실행 때 다시 자동 활성화된다.
    private static HashSet<string> LoadMarker(string gameDir)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var path = Path.Combine(gameDir, MarkerFile);
            if (!File.Exists(path))
                return set;
            foreach (var line in File.ReadAllLines(path))
            {
                var t = line.Trim();
                if (t.Length > 0 && !t.StartsWith("#", StringComparison.Ordinal))
                    set.Add(t);
            }
        }
        catch { /* 마커 읽기 실패 → 빈 집합(최악의 경우 1회 더 적용 시도, 무해) */ }
        return set;
    }

    private static void AppendMarker(string gameDir, IEnumerable<string> keys)
    {
        try
        {
            var existing = LoadMarker(gameDir);
            var toAdd = keys.Where(k => !existing.Contains(k)).Distinct().ToList();
            if (toAdd.Count == 0)
                return;
            // 기존 줄(주석/사용자 편집 포함)을 보존하고 새 키만 덧붙여 **원자적**으로 재기록.
            // append 가 부분 기록되면(전원손실 등) truncated 줄이 남아 그 팩의 "1회 적용" 보장이
            // 깨지므로(Codex C1-1), File.AppendAllText 대신 full-rewrite + AtomicWrite 로 fail-atomic.
            var path = Path.Combine(gameDir, MarkerFile);
            var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();
            if (lines.Count == 0)
            {
                lines.Add("# Herma Launcher: 이미 1회 자동 적용한 팩/쉐이더 목록.");
                lines.Add("# 특정 줄을 지우면 그 팩이 다음 실행 때 다시 자동 활성화됩니다.");
            }
            lines.AddRange(toAdd);
            AtomicWrite(path, string.Join("\n", lines) + "\n");
        }
        catch { /* 마커 쓰기 실패 → best-effort */ }
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
