using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HermaLauncher.Services;

// 첫 설치 시 클라이언트 기본값(쉐이더/리소스팩 등)을 적용한다. "기본값" 의 핵심은 멱등 + 사용자 선택 존중:
//   이미 설정돼 있으면 손대지 않는다(사용자가 게임 내에서 끄거나 바꾼 것을 덮어쓰지 않음).
//   best-effort — 실패해도 게임 실행/설치를 막지 않는다.
public static class ClientDefaults
{
    // 양 경로(런처 Play / installer)의 단일 진입점 — packwiz 동기화 후 호출.
    public static void ApplyAll(string gameDir, IProgress<StageUpdate>? progress = null)
    {
        EnsureDefaultShader(gameDir, progress);
        EnsureDefaultResourcePacks(gameDir, progress);
    }

    // packwiz 동기화 후 shaderpacks/ 에 들어온 쉐이더팩을 Iris 기본 활성으로 설정.
    //   - config/iris.properties 가 이미 있으면 skip(사용자 선택 보존 — 첫 설치 기본값만).
    //   - shaderpacks/ 에서 LauncherConfig.DefaultShaderPackPrefix 로 시작하는 zip 우선, 없으면 첫 zip.
    //   - Iris 는 게임 시작 시 iris.properties 를 읽어 적용하므로, 실행/플레이 전에 써두면 첫 실행부터 적용됨.
    public static void EnsureDefaultShader(string gameDir, IProgress<StageUpdate>? progress = null)
    {
        try
        {
            var configDir = Path.Combine(gameDir, "config");
            var irisProps = Path.Combine(configDir, "iris.properties");
            if (File.Exists(irisProps))
                return; // 이미 설정됨 — 사용자 선택 보존

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

            Directory.CreateDirectory(configDir);
            // Java Properties 형식(key=value). 값은 영숫자/언더스코어/점만이라 이스케이프 불필요.
            File.WriteAllText(irisProps,
                "# Herma Launcher 기본 쉐이더 (끄거나 바꾸려면: 게임 내 비디오 설정 > 쉐이더팩)\n" +
                "enableShaders=true\n" +
                "shaderPack=" + name + "\n");

            progress?.Report(StageUpdate.Of(LaunchStage.Packwiz, $"쉐이더 기본 적용: {name}"));
        }
        catch
        {
            // 쉐이더 기본값은 best-effort — 실패해도 진행을 막지 않는다.
        }
    }

    // packwiz 동기화 후 resourcepacks/ 의 리소스팩(Fresh Animations 등)을 options.txt 에 기본 활성화.
    //   - options.txt 의 resourcePacks 줄이 없거나 비어있을(=[] / ["vanilla"]) 때만 설정 → 사용자 커스텀 보존.
    //   - 로드 순서: vanilla(최하단) → 베이스 팩 → 확장(Extension) 팩(최상단/override).
    //     MC options.txt 의 resourcePacks 배열은 "뒤쪽 = 위(높은 우선순위)" 이므로 Extension 을 마지막에 둔다.
    //   - 값은 "file/<파일명>"(확장자 포함). 하드코딩 대신 실제 zip 감지 → 없는 파일 가리킴 방지.
    public static void EnsureDefaultResourcePacks(string gameDir, IProgress<StageUpdate>? progress = null)
    {
        try
        {
            var rpDir = Path.Combine(gameDir, "resourcepacks");
            if (!Directory.Exists(rpDir))
                return;
            var packs = Directory.GetFiles(rpDir, "*.zip").Select(Path.GetFileName).Where(n => n is not null).ToList();
            if (packs.Count == 0)
                return;

            // 베이스 먼저, 확장(Extension/Addon) 나중(배열 뒤 = 위에서 override — Codex 검증 certain).
            var fileEntries = packs
                .OrderBy(n => IsExtensionPack(n!) ? 1 : 0)
                .ThenBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Select(n => "\"file/" + n + "\"")
                .ToList();
            var joined = string.Join(",", fileEntries);
            var rpLine = "resourcePacks:[\"vanilla\"," + joined + "]";
            // FA 1.10.x 등은 최신 MC 에서 "incompatible" 로 떠 resourcePacks 에만 두면 로드 시 조용히 제거됨 →
            // 같은 팩을 incompatibleResourcePacks 에도 화이트리스트해야 유지된다(Codex#4, certain).
            var incompatLine = "incompatibleResourcePacks:[" + joined + "]";

            var optionsPath = Path.Combine(gameDir, "options.txt");
            var nl = Environment.NewLine; // MC 는 Windows=CRLF / Unix=LF (Codex#5)
            if (!File.Exists(optionsPath))
            {
                // MC 가 부분 options.txt 를 읽고 나머지는 기본값으로 채운다.
                File.WriteAllText(optionsPath, rpLine + nl + incompatLine + nl);
                progress?.Report(StageUpdate.Of(LaunchStage.Packwiz, "리소스팩 기본 적용"));
                return;
            }

            var lines = File.ReadAllLines(optionsPath).ToList();
            var idx = lines.FindIndex(l => l.StartsWith("resourcePacks:", StringComparison.Ordinal));
            if (idx >= 0)
            {
                var val = lines[idx]["resourcePacks:".Length..].Trim();
                // 사용자가 이미 커스텀(빈/vanilla-only 아님)했으면 둘 다 보존.
                if (val.Length > 0 && val != "[]" && val != "[\"vanilla\"]")
                    return;
            }
            SetOrAddLine(lines, "resourcePacks:", rpLine);
            SetOrAddLine(lines, "incompatibleResourcePacks:", incompatLine);
            File.WriteAllText(optionsPath, string.Join(nl, lines) + nl);
            progress?.Report(StageUpdate.Of(LaunchStage.Packwiz, "리소스팩 기본 적용"));
        }
        catch
        {
            // 리소스팩 기본값도 best-effort.
        }
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
