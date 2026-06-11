using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HermaLauncher.Services;

// 첫 설치/신규 팩 도착 시 클라이언트 기본값(쉐이더/리소스팩)을 적용한다.
// 핵심 정책 = "처음 1회만 자동 활성"(사용자 선택):
//   - 처음 보는(=마커에 없는) 팩/쉐이더만 자동 켠다.
//   - 한 번 자동 적용한 뒤에는 마커에 기록 → 사용자가 게임 내에서 끈 팩을 다시 켜지 않는다.
//   - 기존에 활성화된 팩과 그 순서는 보존하고, 새 팩만 additively append 한다.
//   - best-effort — 실패해도 게임 실행/설치를 막지 않는다.
// 마커: <gameDir>/herma_launcher_applied.txt ("rp:<파일명>" / "shader:<파일명>"). 줄 삭제 시 재적용.
public static class ClientDefaults
{
    private const string MarkerFile = "herma_launcher_applied.txt";

    // 양 경로(런처 Play / installer)의 단일 진입점 — packwiz 동기화 후 호출.
    public static void ApplyAll(string gameDir, IProgress<StageUpdate>? progress = null)
    {
        var applied = LoadMarker(gameDir);
        var before = applied.Count;
        EnsureDefaultShader(gameDir, applied, progress);
        EnsureDefaultResourcePacks(gameDir, applied, progress);
        if (applied.Count != before)
            SaveMarker(gameDir, applied);
    }

    // packwiz 동기화 후 shaderpacks/ 의 쉐이더팩을 Iris 기본 활성으로 설정(처음 1회).
    //   - 마커에 이미 있으면 skip(사용자 선택 보존).
    //   - iris.properties 가 이미 있으면 덮지 않되, 마커엔 기록(1회성 보장 — 기존 사용자 쉐이더 보존).
    private static void EnsureDefaultShader(string gameDir, HashSet<string> applied, IProgress<StageUpdate>? progress)
    {
        try
        {
            var shaderDir = Path.Combine(gameDir, "shaderpacks");
            if (!Directory.Exists(shaderDir))
                return;

            var packs = Directory.GetFiles(shaderDir, "*.zip");
            if (packs.Length == 0)
                return;

            var chosen = packs.FirstOrDefault(p =>
                Path.GetFileName(p).StartsWith(LauncherConfig.DefaultShaderPackPrefix, StringComparison.OrdinalIgnoreCase))
                ?? packs[0];
            var name = Path.GetFileName(chosen); // Iris 는 zip 쉐이더팩을 확장자 포함 파일명으로 가리킴
            var key = "shader:" + name;
            if (applied.Contains(key))
                return; // 이미 1회 자동 적용함 — 사용자 선택 보존

            var configDir = Path.Combine(gameDir, "config");
            var irisProps = Path.Combine(configDir, "iris.properties");
            // iris.properties 가 이미 있으면(사용자가 쉐이더 지정) 덮지 않는다 — 마커만 찍어 1회성 보장.
            if (!File.Exists(irisProps))
            {
                Directory.CreateDirectory(configDir);
                // Java Properties 형식(key=value). 값은 영숫자/언더스코어/점만이라 이스케이프 불필요.
                File.WriteAllText(irisProps,
                    "# Herma Launcher 기본 쉐이더 (끄거나 바꾸려면: 게임 내 비디오 설정 > 쉐이더팩)\n" +
                    "enableShaders=true\n" +
                    "shaderPack=" + name + "\n");
                progress?.Report(StageUpdate.Of(LaunchStage.Packwiz, $"쉐이더 기본 적용: {name}"));
            }
            applied.Add(key);
        }
        catch
        {
            // 쉐이더 기본값은 best-effort — 실패해도 진행을 막지 않는다.
        }
    }

    // packwiz 동기화 후 resourcepacks/ 의 리소스팩을 options.txt 에 기본 활성화(처음 1회, additive).
    //   - 처음 보는(마커에 없는) zip 만 활성 목록에 append → 새 팩(번역팩 등)이 기존 프로필에도 자동 적용.
    //   - 기존 활성 팩/순서는 보존, 이미 활성인 팩은 중복 추가하지 않음.
    //   - 값은 "file/<파일명>"(확장자 포함). 베이스 먼저, 확장(Extension/Addon) 나중(배열 뒤 = 위에서 override).
    //   - 일부 팩은 최신 MC 에서 "incompatible" 로 떠 resourcePacks 에만 두면 조용히 제거됨 →
    //     incompatibleResourcePacks 에도 화이트리스트해야 유지된다(Codex#4).
    private static void EnsureDefaultResourcePacks(string gameDir, HashSet<string> applied, IProgress<StageUpdate>? progress)
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

            // 처음 보는 팩만 자동 활성 대상. 이번 실행에서 본 모든 팩은 '봤음'으로 마킹(다음부턴 보존).
            var unseen = present.Where(n => !applied.Contains("rp:" + n)).ToList();
            foreach (var n in present)
                applied.Add("rp:" + n);
            if (unseen.Count == 0)
                return; // 새 팩 없음 — 사용자 목록 손대지 않음

            var optionsPath = Path.Combine(gameDir, "options.txt");
            var nl = Environment.NewLine; // MC 는 Windows=CRLF / Unix=LF
            var lines = File.Exists(optionsPath) ? File.ReadAllLines(optionsPath).ToList() : new List<string>();

            var active = ParsePackArray(lines, "resourcePacks:");
            var incompat = ParsePackArray(lines, "incompatibleResourcePacks:");
            if (active.Count == 0)
                active.Add("\"vanilla\""); // vanilla 는 항상 최하단

            // 새 팩을 base 먼저 / extension 나중으로 append(없을 때만).
            var added = 0;
            foreach (var n in unseen
                .OrderBy(x => IsExtensionPack(x) ? 1 : 0)
                .ThenBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var entry = "\"file/" + n + "\"";
                if (!active.Contains(entry)) { active.Add(entry); added++; }
                if (!incompat.Contains(entry)) incompat.Add(entry);
            }
            if (added == 0)
                return; // 새 팩이 이미 전부 활성 — 기록만 갱신(위에서 마킹됨)

            SetOrAddLine(lines, "resourcePacks:", "resourcePacks:[" + string.Join(",", active) + "]");
            SetOrAddLine(lines, "incompatibleResourcePacks:", "incompatibleResourcePacks:[" + string.Join(",", incompat) + "]");
            File.WriteAllText(optionsPath, string.Join(nl, lines) + nl);
            progress?.Report(StageUpdate.Of(LaunchStage.Packwiz, $"리소스팩 {added}개 자동 적용"));
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
                result.Add(t); // 따옴표 포함 그대로(예: "vanilla", "file/x.zip")
        }
        return result;
    }

    private static HashSet<string> LoadMarker(string gameDir)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var p = Path.Combine(gameDir, MarkerFile);
            if (File.Exists(p))
                foreach (var l in File.ReadAllLines(p))
                {
                    var t = l.Trim();
                    if (t.Length > 0 && !t.StartsWith("#"))
                        set.Add(t);
                }
        }
        catch { /* best-effort */ }
        return set;
    }

    private static void SaveMarker(string gameDir, HashSet<string> applied)
    {
        try
        {
            var body = "# Herma Launcher: 이미 1회 자동 적용한 팩/쉐이더 목록." + Environment.NewLine
                + "# 특정 줄을 지우면 그 팩이 다음 실행 때 다시 자동 활성화됩니다." + Environment.NewLine
                + string.Join(Environment.NewLine, applied.OrderBy(x => x, StringComparer.Ordinal)) + Environment.NewLine;
            File.WriteAllText(Path.Combine(gameDir, MarkerFile), body);
        }
        catch { /* best-effort */ }
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
}
