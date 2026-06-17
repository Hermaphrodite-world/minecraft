using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HermaLauncher.Services;

// packwiz 동기화 후 클라이언트 기본값을 적용한다. 정책 = "전부 적용이 기본 + 사용자 변경 존중"(사용자 요청 2026-06-15):
//   - 리소스팩: 각 팩을 "처음 1회만" 자동 활성(마커 herma_launcher_applied.txt 로 per-pack 기록) — resourcePacks 에만 추가.
//               사용자가 게임 내에서 끈 팩은 다음 실행에 다시 켜지 않는다(존중). 새 팩만 자동 활성.
//   - ※ incompatibleResourcePacks 에는 절대 추가하지 않는다. 모드팩 팩들은 supported_formats range 로 이미 호환이라
//       incompat 에 넣으면 MC 26.1.2 가 "now compatible" 로 판단해 오히려 드롭한다(실측). 진짜 incompatible 팩만 MC 가 관리.
//   - 쉐이더  : 처음 1회만 기본 쉐이더 적용(마커). 이후 사용자가 고른 유효 쉐이더/끈 상태는 보존.
//   - 서버목록: servers.dat 에 명명된 모드팩 서버 항목 보장(ServerList).
//   - best-effort — 실패해도 게임 실행/설치를 막지 않는다.
// ※ "매 실행 강제"(과거 eba1135) 는 사용자 수동 정렬까지 덮어써서 폐기 → per-pack apply-once. 그 위에 도입했던
//    whitelist-ensure(v0.1.7~0.1.9) 도 호환 팩을 드롭시키는 원인이라 폐기(실측, 2026-06-15). 활성화는 resourcePacks 만으로 충분.
public static class ClientDefaults
{
    private const string MarkerFile = "herma_launcher_applied.txt";

    // 런처 Play / 공식 런처 installer 양 경로의 단일 진입점 — packwiz 동기화 후 호출.
    //   endpoint: 자동접속 대상(오케스트레이터가 해석). servers.dat 항목을 quickPlay 와 "동일 주소"로 등록한다.
    //   ★ 이전 버그: 공개 IP(LauncherConfig.ServerIp) 로만 등록 → 같은 LAN 의 다른 PC 가 서버목록 항목으론 못 닿음.
    //     endpoint.Host(override=LAN IP / 로컬 / 공개) 로 등록해 quickPlay 와 일치시킨다.
    public static void ApplyAll(string gameDir, ServerEndpoint endpoint, IProgress<StageUpdate>? progress = null)
    {
        // 방어: endpoint.Host 가 비면 공개 IP 로 폴백(서버목록 등록이 깨지지 않도록).
        var host = string.IsNullOrWhiteSpace(endpoint.Host) ? LauncherConfig.ServerIp : endpoint.Host;
        var port = endpoint.Port > 0 ? endpoint.Port : LauncherConfig.ServerPort;
        AppLog.Info(LaunchStage.Packwiz,
            $"[servers.dat] 등록 주소 = {host}:{port} (source={endpoint.Source}) — quickPlay 인자와 동일해야 정상");
        ServerList.Ensure(gameDir, LauncherConfig.ServerListName, host, port, progress);
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
        catch (Exception ex)
        {
            // 쉐이더 기본값은 best-effort — 실패해도 진행을 막지 않는다(로그엔 남김, P1-8).
            AppLog.Warn(LaunchStage.Packwiz, "기본 쉐이더 적용 실패: " + ex.Message);
        }
    }

    // resourcepacks/ 의 팩을 options.txt 의 resourcePacks 에 보장(apply-once):
    //   - 마커에 없는 팩만 resourcePacks 에 추가(처음 1회) + 마커 기록 → 사용자가 끈 팩은 존중.
    //     베이스 먼저, 확장(Extension/Addon) 나중. 값은 "file/<파일명>". incompatibleResourcePacks 에는 손대지 않는다.
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
            var nl = DetectNewline(optionsPath, Environment.NewLine); // 원본 개행(CRLF/LF) 보존
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

            // (1a-clean) stale 정리 — resourcepacks/ 에 더 이상 없는 file/ 엔트리 제거(팩 rename/삭제 후 죽은
            //            엔트리 누적 방지). present(현재 zip)만 유효. vanilla/네임스페이스 엔트리는 보존.
            //            대소문자 무시(Windows FS) — 우연히 valid 엔트리를 지우지 않게 보수적.
            var presentSet = present.Select(n => "\"file/" + n + "\"").ToHashSet(StringComparer.OrdinalIgnoreCase);
            bool IsStaleFileEntry(string e) =>
                e.StartsWith("\"file/", StringComparison.Ordinal) && !presentSet.Contains(e);
            if (active.RemoveAll(IsStaleFileEntry) > 0) changed = true;
            if (incompat.RemoveAll(IsStaleFileEntry) > 0) changed = true;

            // (1b) 한국어 보충팩은 항상 '선택됨' 목록 맨 아래(= options.txt 첫 file 엔트리 = lowest priority)로 고정.
            //      보충팩은 다른 팩/모드 번역을 덮지 않는 fallback 이어야 하므로 최저 우선순위가 맞다. 기존 유저의
            //      중간 위치도 매 실행 교정 — 단 비활성(사용자가 끔)이면 active 에 없어 no-op(on/off 는 존중).
            EnsureTranslationPackAtBottom(active, ref changed);

            // ※ 과거 "whitelist-ensure"(active file/ 팩을 incompatibleResourcePacks 에 추가)는 폐기.
            //   실측(오프라인 MC 26.1.2 + latest.log): 모드팩 팩들은 supported_formats range 로 이미 *호환*이라
            //   incompat 에 넣으면 MC 가 "Removed ... from incompatibility list because it's now compatible" 하며
            //   오히려 active 에서 드롭 → 로드 안 됨. incompat 에는 손대지 않는다(MC 가 진짜 incompatible 팩만 관리).
            //   stale 정리(위)의 incompat 제거만 유지. 팩 활성화는 resourcePacks 에 넣는 것(apply-once)으로 충분.

            if (changed)
            {
                SetOrAddLine(lines, "resourcePacks:", "resourcePacks:[" + string.Join(",", active) + "]");
                SetOrAddLine(lines, "incompatibleResourcePacks:", "incompatibleResourcePacks:[" + string.Join(",", incompat) + "]");
                AtomicWrite(optionsPath, string.Join(nl, lines) + nl, DetectUtf8(optionsPath)); // 원본 인코딩(BOM) 보존
                progress?.Report(StageUpdate.Of(LaunchStage.Packwiz,
                    addedCount > 0 ? $"리소스팩 {addedCount}개 적용" : "리소스팩 적용"));
            }

            // 새로 적용한 팩만 마커에 기록(쓰기 여부 무관 — 다음 실행부터 존중).
            if (newlyApplied.Count > 0)
                AppendMarker(gameDir, newlyApplied);
        }
        catch (Exception ex)
        {
            // 리소스팩 기본값도 best-effort(로그엔 남김, P1-8).
            AppLog.Warn(LaunchStage.Packwiz, "기본 리소스팩 적용 실패: " + ex.Message);
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
    // 따옴표 안의 쉼표는 분리하지 않는다 — 파일명에 ',' 가 있어도 엔트리가 깨지지 않게 quote-aware.
    internal static List<string> ParsePackArray(List<string> lines, string keyPrefix) // internal: 단위 테스트 접근
    {
        var result = new List<string>();
        var i = lines.FindIndex(l => l.StartsWith(keyPrefix, StringComparison.Ordinal));
        if (i < 0)
            return result;
        var val = lines[i][keyPrefix.Length..].Trim();
        if (val.StartsWith("[")) val = val[1..];
        if (val.EndsWith("]")) val = val[..^1];

        var sb = new StringBuilder();
        var inQuotes = false;
        void Flush()
        {
            var t = sb.ToString().Trim();
            if (t.Length > 0)
                result.Add(t); // 따옴표 포함 그대로(예: "vanilla", "file/x.zip", lambdabettergrass:default)
            sb.Clear();
        }
        foreach (var ch in val)
        {
            if (ch == '"') { inQuotes = !inQuotes; sb.Append(ch); }
            else if (ch == ',' && !inQuotes) Flush();
            else sb.Append(ch);
        }
        Flush();
        return result;
    }

    private static void SetOrAddLine(List<string> lines, string keyPrefix, string fullLine)
    {
        var i = lines.FindIndex(l => l.StartsWith(keyPrefix, StringComparison.Ordinal));
        if (i < 0) lines.Add(fullLine);
        else lines[i] = fullLine;
    }

    internal static bool IsExtensionPack(string fileName) // internal: 단위 테스트 접근
        => fileName.Contains("Extension", StringComparison.OrdinalIgnoreCase)
        || fileName.Contains("Addon", StringComparison.OrdinalIgnoreCase);

    // 번역 보충팩을 active 의 첫 file 엔트리(="vanilla" 바로 다음)로 이동 → 게임 내 '선택됨' 맨 아래 = lowest priority.
    // idempotent: 이미 제자리거나 비활성이면 아무것도 안 한다(불필요한 쓰기/사용자 on/off 침해 방지).
    internal static void EnsureTranslationPackAtBottom(List<string> active, ref bool changed) // internal: 단위 테스트 접근
    {
        var ko = active.FirstOrDefault(e =>
            e.StartsWith("\"file/", StringComparison.Ordinal) &&
            e.Contains(LauncherConfig.TranslationPackToken, StringComparison.OrdinalIgnoreCase));
        if (ko is null)
            return; // 번역팩이 비활성(사용자가 끔)이거나 폴더에 없음 → 존중, no-op

        var vanillaIdx = active.FindIndex(e => e.Equals("\"vanilla\"", StringComparison.Ordinal));
        var target = vanillaIdx >= 0 ? vanillaIdx + 1 : 0; // "vanilla" 바로 다음(없으면 맨 앞)
        if (active.IndexOf(ko) == target)
            return; // 이미 제자리

        active.Remove(ko);
        var v = active.FindIndex(e => e.Equals("\"vanilla\"", StringComparison.Ordinal)); // 제거 후 재계산
        active.Insert(v >= 0 ? v + 1 : 0, ko);
        changed = true;
    }

    // 원본 개행(CRLF/LF) 감지 — 재기록 시 보존(파일이 비-플랫폼 개행이어도 깨뜨리지 않게).
    private static string DetectNewline(string path, string fallback)
    {
        try
        {
            if (!File.Exists(path)) return fallback;
            var raw = File.ReadAllText(path);
            if (raw.Contains("\r\n", StringComparison.Ordinal)) return "\r\n";
            if (raw.Contains('\n')) return "\n";
            return fallback;
        }
        catch { return fallback; }
    }

    // 원본 UTF-8 BOM 유무 감지 — 보존(MC 표준은 BOM 없음이나 외부 편집 내성). 기본은 BOM 없는 UTF-8.
    private static Encoding DetectUtf8(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var bom = new byte[3];
                using var fs = File.OpenRead(path);
                if (fs.Read(bom, 0, 3) == 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                    return new UTF8Encoding(true);
            }
        }
        catch { /* fallthrough → no-BOM */ }
        return new UTF8Encoding(false);
    }

    // 텍스트 파일 원자적 쓰기(.tmp → replace) — crash/전원손실 시 torn file 방지(Codex).
    // encoding 미지정 시 기본(UTF-8 no BOM). 지정 시 원본 인코딩 보존용.
    private static void AtomicWrite(string path, string content, Encoding? encoding = null)
    {
        var tmp = path + ".tmp";
        if (encoding is null) File.WriteAllText(tmp, content);
        else File.WriteAllText(tmp, content, encoding);
        if (File.Exists(path)) File.Replace(tmp, path, null);
        else File.Move(tmp, path);
    }
}
