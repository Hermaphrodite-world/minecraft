using System;
using System.IO;

namespace HermaLauncher.Services;

// 무거운 설치 전 빠른 실패용 사전 점검(P1-5). 비개발자에게 generic 오류 대신 명확한 한국어 안내.
public static class PreflightChecks
{
    private const long Gb = 1024L * 1024 * 1024;
    private const long FirstInstallBytes = 3 * Gb; // MC + Java(JRE) + 77 모드 첫 설치 보수 추정.
    private const long UpdateHeadroomBytes = 1 * Gb; // 이미 설치됨 → 업데이트/temp 여유.

    // gameDir 드라이브 여유공간이 부족하면 LaunchStageException(한국어). 첫 설치 여부로 임계 조정.
    // 공간 계산 자체 실패(권한 등)는 차단하지 않고 로그만 남긴다(false-block 방지).
    public static void EnsureDiskSpace(string gameDir, LaunchStage stage)
    {
        try
        {
            var full = Path.GetFullPath(gameDir);
            var root = Path.GetPathRoot(full);
            if (string.IsNullOrEmpty(root))
                return;
            var drive = new DriveInfo(root);
            if (!drive.IsReady)
                return;

            var required = IsFirstInstall(full) ? FirstInstallBytes : UpdateHeadroomBytes;
            if (drive.AvailableFreeSpace >= required)
                return;

            var needGb = required / (double)Gb;
            var freeGb = drive.AvailableFreeSpace / (double)Gb;
            AppLog.Warn(stage, $"디스크 공간 부족: {drive.Name} 여유 {freeGb:F1}GB < 필요 {needGb:F1}GB");
            throw new LaunchStageException(stage,
                $"디스크 공간이 부족해요. 약 {needGb:F0}GB 가 필요한데 {drive.Name} 드라이브에 {freeGb:F1}GB 만 남아 있어요.\n" +
                "공간을 확보한 뒤 다시 시도해 주세요.");
        }
        catch (LaunchStageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Warn(stage, "디스크 공간 사전점검 생략(계산 실패): " + ex.Message);
        }
    }

    private static bool IsFirstInstall(string gameDir)
    {
        try
        {
            var versions = Path.Combine(gameDir, "versions");
            return !Directory.Exists(versions) || Directory.GetDirectories(versions).Length == 0;
        }
        catch
        {
            return true; // 알 수 없으면 보수적으로 첫 설치 취급(더 큰 여유 요구).
        }
    }
}
