using System;
using System.IO;
using System.Text;

namespace HermaLauncher.Services;

// 런처 통합 로깅(P0 foundation). logs/launcher-YYYYMMDD.log 에 타임스탬프와 함께 기록한다.
// 게임/packwiz 외부 프로세스 출력은 Raw 로 같은 파일에, 게임 stdout 전체는 별도 game-*.log 로.
// best-effort — 로깅 실패가 실행 흐름을 막지 않는다(전부 try/catch swallow). thread-safe(lock).
// 소비처: update 실패 분기(P1-3), late-crash 진단(P1-7), best-effort 가시화(P1-8), 에러 모달(P3-4).
public static class AppLog
{
    private static readonly object _gate = new();
    private static readonly UTF8Encoding _utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private static string LauncherLogPath()
        => Path.Combine(AppPaths.LogDir, $"launcher-{DateTime.Now:yyyyMMdd}.log");

    public static void Info(LaunchStage stage, string message) => Write("INFO", stage.ToString(), message);
    public static void Warn(LaunchStage stage, string message) => Write("WARN", stage.ToString(), message);

    public static void Error(LaunchStage stage, string message, Exception? ex = null)
        => Write("ERROR", stage.ToString(), ex is null ? message : message + "\n" + ex);

    // 외부 프로세스(예: packwiz) 출력 한 줄 기록.
    public static void Raw(string category, string line) => Write("RAW", category, line);

    private static void Write(string level, string category, string message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] [{level}] [{category}] {message}{Environment.NewLine}";
            lock (_gate)
                File.AppendAllText(LauncherLogPath(), line, _utf8);
        }
        catch
        {
            // 로깅 실패는 흐름 비차단.
        }
    }

    // 세션 시작 시 1회 — 최근 keep 개 외 오래된 launcher-*.log / game-*.log 정리.
    public static void RotateOnce(int keep = 10)
    {
        try
        {
            foreach (var prefix in new[] { "launcher-", "game-" })
            {
                var files = Directory.GetFiles(AppPaths.LogDir, prefix + "*.log");
                Array.Sort(files, StringComparer.Ordinal); // 파일명에 날짜 → 사전순 = 시간순
                for (var i = 0; i < files.Length - keep; i++)
                {
                    try { File.Delete(files[i]); } catch { /* best-effort */ }
                }
            }
        }
        catch
        {
            // 정리 실패 무시.
        }
    }

    // 게임 프로세스 전용 로그 경로(실행마다 새 파일 — 크래시 진단용).
    public static string NewGameLogPath()
        => Path.Combine(AppPaths.LogDir, $"game-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    // 가장 최근 launcher 로그 경로(없으면 LogDir). "로그" 버튼이 연다.
    public static string LatestLogOrDir()
    {
        try
        {
            var files = Directory.GetFiles(AppPaths.LogDir, "launcher-*.log");
            if (files.Length > 0)
            {
                Array.Sort(files, StringComparer.Ordinal);
                return files[^1];
            }
        }
        catch { /* fallthrough */ }
        return AppPaths.LogDir;
    }

    // 가장 최근 game-*.log 경로(없으면 null). 크래시 진단(FailureDiagnosis)이 본문을 읽는다.
    public static string? LatestGameLogPath()
    {
        try
        {
            var files = Directory.GetFiles(AppPaths.LogDir, "game-*.log");
            if (files.Length > 0)
            {
                Array.Sort(files, StringComparer.Ordinal);
                return files[^1];
            }
        }
        catch { /* fallthrough */ }
        return null;
    }
}
