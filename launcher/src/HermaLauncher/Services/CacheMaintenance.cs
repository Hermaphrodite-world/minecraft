using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HermaLauncher.Services;

// 1회성 클라이언트 캐시 정리(유지보수). 토큰당 정확히 1회만 실행 — 토큰을 올리면 다음 실행에 다시 1회.
//   현재 작업: stale Bobby 청크 캐시(.bobby) 정리. worldgen 모드(바이옴/지형) 변경 후,
//   이전 지형이 캐시에 남아 새 지형과의 경계에서 팝인/버벅임을 만드는 것을 해소한다.
//   .bobby 는 서버에서 다시 받는 캐시라 삭제해도 월드/인벤토리/설정에 영향 없다.
// best-effort: 어떤 실패도 게임 실행을 막지 않는다(로그만 남김). packwiz 동기화 후·실행 전에 호출.
public static class CacheMaintenance
{
    // ※ 이 토큰을 올리면(예: "bobby-cache-clear-2") 모든 클라가 다음 실행 때 다시 1회 정리한다.
    private const string Token = "bobby-cache-clear-1";
    private const string MarkerFile = "herma_maintenance.txt";

    // packwiz 동기화 후 호출. gameDir 의 stale 캐시를 "이번 토큰 기준 1회"만 정리.
    public static void RunOnce(string gameDir, IProgress<StageUpdate>? progress = null)
    {
        try
        {
            var marker = Path.Combine(gameDir, MarkerFile);
            var done = File.Exists(marker)
                ? File.ReadAllLines(marker).Select(l => l.Trim())
                    .Where(l => l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal))
                    .ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
            if (done.Contains(Token))
                return; // 이미 1회 정리함 — 사용자 재탐험 캐시를 매번 날리지 않음

            if (!ClearBobby(gameDir, progress))
                return; // 정리 실패(파일 잠김 등) → 토큰 미기록 → 다음 실행에 재시도

            // 성공/대상없음일 때만 토큰 기록(원자적 — 부분 기록 방지).
            done.Add(Token);
            var tmp = marker + ".tmp";
            File.WriteAllText(tmp, "# Herma Launcher 1회성 유지보수 마커. 이 줄을 지우면 해당 작업이 다시 실행됩니다.\n"
                + string.Join("\n", done.Where(d => !d.StartsWith("#", StringComparison.Ordinal))) + "\n");
            if (File.Exists(marker)) File.Replace(tmp, marker, null);
            else File.Move(tmp, marker);
        }
        catch (Exception ex)
        {
            AppLog.Warn(LaunchStage.Packwiz, "캐시 정리 건너뜀(best-effort): " + ex.Message);
        }
    }

    // <gameDir>/.bobby 정리. 없으면 true(할 일 없음=완료), 삭제 성공 true, 잠김 등 실패 false.
    private static bool ClearBobby(string gameDir, IProgress<StageUpdate>? progress)
    {
        var bobby = Path.Combine(gameDir, ".bobby");
        if (!Directory.Exists(bobby))
            return true; // 캐시 없음 → 정리 완료로 간주(토큰 기록해 다음부턴 skip)
        try
        {
            Directory.Delete(bobby, recursive: true);
            AppLog.Info(LaunchStage.Packwiz, "[cache] stale Bobby 청크 캐시(.bobby) 정리 — worldgen 갱신 반영");
            progress?.Report(StageUpdate.Of(LaunchStage.Packwiz, "이전 지형 캐시 정리 완료"));
            return true;
        }
        catch (IOException ex)
        {
            AppLog.Warn(LaunchStage.Packwiz, "[cache] .bobby 삭제 실패(잠김 등) — 다음 실행에 재시도: " + ex.Message);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppLog.Warn(LaunchStage.Packwiz, "[cache] .bobby 삭제 권한 실패 — 다음 실행에 재시도: " + ex.Message);
            return false;
        }
    }
}
