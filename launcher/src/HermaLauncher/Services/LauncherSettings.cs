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

    // 서버 주소 직접 입력(고급). null/빈 값 = 자동(로컬 서버 감지 → 공개 ServerIp).
    // 같은 집/네트워크의 다른 PC 에서 서버를 켠 경우(NAT 헤어핀 미지원 → 자동 접속 실패) 서버 PC 의 LAN IP 를 지정.
    public string? ServerHostOverride { get; set; }

    // 첫 실행 환영 화면을 봤는지(1회성). false = 다음 실행 시 환영 화면 표시.
    public bool HasSeenWelcome { get; set; }

    // 정상 종료(코드 0) 후 런처를 닫지 않고 유지(반복 재접속 편의). 기본 false(현행: 자동 닫기).
    public bool KeepLauncherOpen { get; set; }

    // 친구가 서버에 접속하면 OS 알림(토스트)으로 알려줌. 기본 true(opt-out). 창이 앞에 떠 있을 땐 억제.
    // (JSON 에 키가 없는 구버전 설정 파일도 이 초기값 true 를 유지 — System.Text.Json 은 누락 속성을 덮지 않음.)
    public bool NotifyOnJoin { get; set; } = true;

    // 베타 채널: ON 이면 베타 모드팩(LauncherConfig.BetaPackTomlUrl)으로 동기화·실행. 기본 false(정식 채널).
    //   ※ 서버 상태 미동기화 단계라 베타는 멀티 자동접속/servers.dat 등록을 생략 → 게임 실행까지만, 싱글플레이 테스트.
    public bool BetaMode { get; set; }

    // 이 서버 누적 플레이 시간(초) + 마지막 플레이(표시용). 로컬·단일 사용자 기록(서버/계정 DB 아님).
    public long TotalPlaytimeSeconds { get; set; }
    public DateTime? LastPlayedUtc { get; set; }

    [JsonIgnore]
    public bool IsRamAuto => MaxRamMbOverride is null or <= 0;

    [JsonIgnore]
    public bool HasServerHostOverride => !string.IsNullOrWhiteSpace(ServerHostOverride);

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
