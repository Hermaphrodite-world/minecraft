using System;
using System.IO;
using System.Linq;

namespace HermaLauncher.Services;

// 첫 설치 시 클라이언트 기본값(쉐이더 등)을 적용한다. "기본값" 의 핵심은 멱등 + 사용자 선택 존중:
//   이미 설정 파일이 있으면 손대지 않는다(사용자가 게임 내에서 끄거나 바꾼 것을 덮어쓰지 않음).
//   best-effort — 실패해도 게임 실행/설치를 막지 않는다.
public static class ClientDefaults
{
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
}
