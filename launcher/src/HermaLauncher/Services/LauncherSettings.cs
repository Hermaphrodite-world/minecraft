using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HermaLauncher.Services;

// P3-2: 사용자 설정 영속화(JSON, DataRoot/settings.json). 모두 best-effort —
// 읽기 실패=기본값, 쓰기 실패=로그만(흐름 비차단). 현재는 RAM override 만, 확장 여지.
public sealed class LauncherSettings
{
    // null = 자동(RamAdvisor 권장값 사용). 값이 있으면 사용자 지정 MaxRamMb.
    public int? MaxRamMbOverride { get; set; }

    [JsonIgnore]
    public bool IsRamAuto => MaxRamMbOverride is null or <= 0;

    public static LauncherSettings Load() => Load(AppPaths.SettingsJson);

    // path 주입 오버로드(단위 테스트용 — InternalsVisibleTo).
    internal static LauncherSettings Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var s = JsonSerializer.Deserialize<LauncherSettings>(json);
                if (s is not null)
                    return s;
            }
        }
        catch (Exception ex)
        {
            AppLog.Warn(LaunchStage.Idle, "설정 읽기 실패(기본값 사용): " + ex.Message);
        }
        return new LauncherSettings();
    }

    // 성공 여부 반환(Codex UX-R1) — 호출자가 실패를 사용자에게 알릴 수 있게. 실패해도 throw 안 함.
    public bool Save() => Save(AppPaths.SettingsJson);

    internal bool Save(string path)
    {
        try
        {
            var tmp = path + ".tmp";
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true); // 원자적 교체
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warn(LaunchStage.Idle, "설정 저장 실패: " + ex.Message);
            return false;
        }
    }
}
