using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Installers;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ProcessBuilder;

namespace HermaLauncher.Services;

// 대체 경로: 공식 마인크래프트 런처에 모드팩 "프로필"을 설치한다.
//   왜? 공식 런처는 Mojang 자체 인증을 쓰므로 Azure 앱/Mojang 승인이 전혀 필요 없다 →
//   정품(온라인) 로그인이 승인 대기 없이 "지금 즉시" 된다. (커스텀 런처의 온라인 로그인은
//   Mojang 앱 승인 대기가 필요하므로, 그 대기 동안의 우회 + 영구 대안.)
//
// 하는 일:
//   (1) 공식 .minecraft 위치 탐지 (Win: %APPDATA%\.minecraft / macOS: ~/Library/Application Support/minecraft)
//   (2) Fabric 로더 설치 → 공식 versions/ (공식 런처가 인식하는 표준 포맷)
//   (3) 게임 파일·Java 설치(공식 dir 에 사전 채움 + packwiz 용 Java 확보)
//   (4) packwiz 모드 동기화 → 전용 게임폴더 <.minecraft>/herma (바닐라 오염 방지)
//   (5) launcher_profiles.json 에 'Hermaphrodite World' 프로필 머지(기존 프로필 보존)
// 인증 단계 없음 — 공식 런처가 로그인을 담당.
public sealed class OfficialLauncherInstaller
{
    public const string ProfileKey = "herma-world";
    public const string ProfileName = "Hermaphrodite World";
    private const string GameDirName = "herma";

    private readonly HttpClient _http = new();
    private readonly PackwizService _packwiz = new();

    // 성공 시 true. 실패 시 progress 로 Error 보고 후 false (LaunchOrchestrator 와 동일 계약).
    public async Task<bool> InstallAsync(IProgress<StageUpdate> progress, CancellationToken ct)
    {
        try
        {
            await DoInstallAsync(progress, ct).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            progress.Report(StageUpdate.Of(LaunchStage.Idle, "취소했어요."));
            return false;
        }
        catch (LaunchStageException ex)
        {
            progress.Report(StageUpdate.Error(ex.Stage, ex.Message));
            return false;
        }
        catch (Exception ex)
        {
            progress.Report(StageUpdate.Error(LaunchStage.Failed,
                "공식 런처 설치 중 오류가 발생했어요. 다시 시도해 주세요.\n" + ex.Message));
            return false;
        }
    }

    private async Task DoInstallAsync(IProgress<StageUpdate> progress, CancellationToken ct)
    {
        // (1) 공식 .minecraft 탐지
        var mcDir = ResolveOfficialMinecraftDir();
        if (mcDir is null)
            throw new LaunchStageException(LaunchStage.Fabric,
                "공식 마인크래프트 런처를 찾지 못했어요.\n" +
                "minecraft.net 에서 공식 런처를 설치하고 한 번 실행한 뒤 다시 시도해 주세요.");

        // 쓰기 가능 여부 사전 점검(공식 런처가 열려 있으면 파일 잠금 가능) — 무거운 설치 전 빠른 실패.
        EnsureProfilesWritable(mcDir);
        PreflightChecks.EnsureDiskSpace(mcDir, LaunchStage.Java); // P1-5: 공식 .minecraft 드라이브 공간 사전점검

        var launcher = new MinecraftLauncher(new MinecraftPath(mcDir));

        // (2) Fabric 설치 → 공식 versions/
        progress.Report(StageUpdate.Of(LaunchStage.Fabric, "Fabric 로더 설치 중…"));
        var fabric = new FabricInstaller(_http);
        // P2-2: loader 버전 핀(커스텀 경로와 동일 — 양 경로 정합).
        var versionId = string.IsNullOrWhiteSpace(LauncherConfig.FabricLoaderVersion)
            ? await fabric.Install(LauncherConfig.MinecraftVersion, launcher.MinecraftPath).ConfigureAwait(false)
            : await fabric.Install(LauncherConfig.MinecraftVersion, LauncherConfig.FabricLoaderVersion, launcher.MinecraftPath).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(versionId))
            throw new LaunchStageException(LaunchStage.Fabric, "Fabric 로더 버전을 확인하지 못했어요.");

        // (3) 게임 파일·Java 설치(공식 dir 사전 채움 + packwiz 용 Java)
        progress.Report(StageUpdate.Of(LaunchStage.Java, "게임 파일·Java 설치 중…"));
        var fileProgress = new Progress<InstallerProgressChangedEventArgs>(e =>
            progress.Report(StageUpdate.Of(LaunchStage.Java, e.Name ?? "설치 중",
                e.TotalTasks > 0 ? (double)e.ProgressedTasks / e.TotalTasks : (double?)null)));
        await launcher.InstallAsync(versionId, fileProgress, NullProgress<ByteProgress>.Instance, ct)
                      .ConfigureAwait(false);

        var version = await launcher.GetVersionAsync(versionId, ct).ConfigureAwait(false);
        var javaPath = launcher.GetJavaPath(version);
        if (string.IsNullOrEmpty(javaPath) || !File.Exists(javaPath))
            javaPath = launcher.GetDefaultJavaPath();
        if (string.IsNullOrEmpty(javaPath) || !File.Exists(javaPath))
            throw new LaunchStageException(LaunchStage.Java,
                "Java 런타임을 찾지 못했어요. 잠시 후 다시 시도해 주세요.");

        // (3b) Fabric 버전 JSON 이 실제 생성됐는지 확인 — 공식 런처가 lastVersionId 로 인식하려면 필수(Codex#1).
        var versionJson = Path.Combine(mcDir, "versions", versionId, versionId + ".json");
        if (!File.Exists(versionJson))
            throw new LaunchStageException(LaunchStage.Fabric,
                "Fabric 버전 파일 생성을 확인하지 못했어요. 다시 시도해 주세요.");

        // 채널(설정): 정식/베타=Fabric 26.1.2, RPG=NeoForge 1.21.1.
        var channel = LauncherConfig.GetChannel(LauncherSettings.Load().EffectiveChannel);
        // 공식 런처 설치 경로는 Fabric 채널 전용(NeoForge 프로필 설치/머지 미구현). RPG 는 '바로 플레이' 사용.
        if (channel.Loader == LauncherConfig.Loader.NeoForge)
            throw new LaunchStageException(LaunchStage.Packwiz,
                "RPG 채널은 공식 런처 설치를 지원하지 않아요. 메인 화면의 '바로 플레이'로 실행해 주세요.");
        var packTomlUrl = channel.PackTomlUrl;

        // (4) packwiz 모드 → 전용 게임폴더(바닐라 분리)
        var gameDir = Path.Combine(mcDir, GameDirName);
        await _packwiz.RunAsync(javaPath!, packTomlUrl, progress, ct, gameDir)
                      .ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        // (4a) 자동접속 endpoint 해석 → 서버목록을 quickPlay 와 동일 주소(override=LAN IP 등)로 등록.
        //   공식 런처 경로엔 quickPlay 가 없지만, 같은 LAN 의 다른 PC 가 서버목록 항목으로 닿게 하려면 동일 해석 필요.
        //   비-정식(베타): 자동접속/등록 미사용이라 해석 생략(미사용 폴백 endpoint).
        var endpoint = !channel.AutoConnect
            ? ServerEndpoint.PublicFallback
            : await ServerEndpointResolver.ResolveAsync(progress, ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        // (4b) 첫 설치 기본 쉐이더/리소스팩 적용 + 서버목록(endpoint 주소) 등록 → 공식 런처 첫 화면부터 적용.
        //   비-정식(베타): registerServer:false 로 servers.dat 등록만 생략(쉐이더/리소스팩은 적용).
        ClientDefaults.ApplyAll(gameDir, endpoint, progress, registerServer: channel.AutoConnect);

        // (5) 프로필 머지 — 스탠드얼론 + MS Store 런처 프로필 파일 모두에 반영(Codex#7 P0).
        progress.Report(StageUpdate.Of(LaunchStage.Launch, "공식 런처 프로필 등록 중…"));
        WriteProfile(mcDir, gameDir, versionId);

        var shownAddress = string.IsNullOrWhiteSpace(endpoint.Host)
            ? $"{LauncherConfig.ServerIp}:{LauncherConfig.ServerPort}" : endpoint.Address;
        progress.Report(StageUpdate.Of(LaunchStage.Running,
            $"완료! 공식 마인크래프트 런처를 열고 좌하단에서 '{ProfileName}' 프로필을 선택해 플레이하세요. " +
            $"멀티플레이 서버: {shownAddress}", 1.0));
    }

    // 공식 런처 표준 경로(운영체제별). 디렉토리가 존재해야 "런처 설치됨" 으로 본다
    // (공식 런처는 첫 실행 시 .minecraft 와 launcher_profiles.json 을 만든다).
    private static string? ResolveOfficialMinecraftDir()
    {
        string dir;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            dir = Path.Combine(home, "Library", "Application Support", "minecraft");
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            dir = Path.Combine(home, ".minecraft");
        }

        // 디렉토리 또는 프로필 파일(스탠드얼론/MS Store) 중 하나라도 있으면 설치된 것으로 간주.
        if (Directory.Exists(dir) || ProfileFileCandidates(dir).Any(File.Exists))
            return dir;
        return null;
    }

    // 공식 런처 종류별 프로필 파일.
    //   launcher_profiles.json               = minecraft.net 다운로드(스탠드얼론) 런처
    //   launcher_profiles_microsoft_store.json = Microsoft Store / Game Pass 런처 (Codex#7 — 별도 파일)
    private static IEnumerable<string> ProfileFileCandidates(string mcDir)
    {
        yield return Path.Combine(mcDir, "launcher_profiles.json");
        yield return Path.Combine(mcDir, "launcher_profiles_microsoft_store.json");
    }

    // 머지 대상 = 실제 존재하는 프로필 파일 전부(둘 다 있으면 둘 다). 하나도 없으면 스탠드얼론 기본 생성.
    private static List<string> ResolveProfileTargets(string mcDir)
    {
        var existing = ProfileFileCandidates(mcDir).Where(File.Exists).ToList();
        return existing.Count > 0 ? existing : new List<string> { Path.Combine(mcDir, "launcher_profiles.json") };
    }

    // 공식 런처가 열려 파일을 잠갔는지 사전 점검(존재하는 프로필 파일 전부) → 무거운 설치 전 친화적 빠른 실패.
    private static void EnsureProfilesWritable(string mcDir)
    {
        foreach (var path in ProfileFileCandidates(mcDir).Where(File.Exists))
        {
            try
            {
                using var _ = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                throw new LaunchStageException(LaunchStage.Launch,
                    "공식 마인크래프트 런처가 실행 중이라 프로필을 저장할 수 없어요.\n" +
                    "공식 런처를 완전히 종료한 뒤 다시 시도해 주세요.");
            }
        }
    }

    // 존재하는 모든 프로필 파일에 우리 프로필을 머지(스탠드얼론 + MS Store).
    private static void WriteProfile(string mcDir, string gameDir, string versionId)
    {
        foreach (var path in ResolveProfileTargets(mcDir))
            MergeProfileInto(path, gameDir, versionId);
    }

    // 단일 launcher_profiles*.json 에 머지(기존 프로필/설정 보존). JsonNode DOM 으로 미지 필드까지 보존하고,
    // File.Replace(원자적 교체 + .bak 백업)로 TOCTOU/부분 쓰기 손상을 막는다(Codex#5). 쓴 뒤 재확인.
    private static void MergeProfileInto(string profilesPath, string gameDir, string versionId)
    {
        JsonObject root;
        if (File.Exists(profilesPath))
        {
            try
            {
                root = JsonNode.Parse(File.ReadAllText(profilesPath)) as JsonObject ?? NewRoot();
            }
            catch (JsonException)
            {
                // 손상된 파일이면 백업 후 새로 시작(기존 내용 손실 방지를 위해 .bak 보존).
                try { File.Copy(profilesPath, profilesPath + ".bak", overwrite: true); } catch { }
                root = NewRoot();
            }
        }
        else
        {
            root = NewRoot();
        }

        if (root["profiles"] is not JsonObject profiles)
        {
            profiles = new JsonObject();
            root["profiles"] = profiles;
        }

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var created = (profiles[ProfileKey] as JsonObject)?["created"]?.GetValue<string>() ?? now;

        profiles[ProfileKey] = new JsonObject
        {
            ["name"] = ProfileName,
            ["type"] = "custom",
            ["icon"] = "Crafting_Table",
            ["created"] = created,
            ["lastUsed"] = now,
            ["lastVersionId"] = versionId,
            ["gameDir"] = gameDir,
            ["javaArgs"] = $"-Xmx{RamAdvisor.EffectiveMaxRamMb()}M", // P3-3: 호스트 RAM 기반(설정 override 우선)
        };

        var tmp = profilesPath + ".tmp";
        File.WriteAllText(tmp, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        try
        {
            if (File.Exists(profilesPath))
                File.Replace(tmp, profilesPath, profilesPath + ".bak"); // 원자적 + 백업
            else
                File.Move(tmp, profilesPath, overwrite: true);
        }
        catch (IOException)
        {
            try { File.Delete(tmp); } catch { /* 정리 실패 무시 */ }
            throw new LaunchStageException(LaunchStage.Launch,
                "공식 런처가 프로필 파일을 사용 중이라 저장하지 못했어요. 공식 런처를 종료한 뒤 다시 시도해 주세요.");
        }

        // 쓴 직후 우리 프로필이 실제로 들어갔는지 확인(silent failure 방지 — Codex#7).
        try
        {
            var check = JsonNode.Parse(File.ReadAllText(profilesPath)) as JsonObject;
            if (check?["profiles"]?[ProfileKey] is null)
                throw new LaunchStageException(LaunchStage.Launch,
                    "프로필 저장을 확인하지 못했어요. 다시 시도해 주세요.");
        }
        catch (JsonException)
        {
            throw new LaunchStageException(LaunchStage.Launch,
                "프로필 저장을 확인하지 못했어요. 다시 시도해 주세요.");
        }
    }

    private static JsonObject NewRoot() => new() { ["profiles"] = new JsonObject(), ["version"] = 3 };
}
