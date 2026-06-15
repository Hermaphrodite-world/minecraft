using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace HermaLauncher.Services;

// 크래시·문제 신고용 진단 ZIP 생성(원클릭). 흩어진 launcher-*.log / game-*.log 와 시스템 정보를
// 한 파일로 묶어 사용자가 디스코드로 그대로 보낼 수 있게 한다(운영자 1:1 원격지원 마찰 제거).
// best-effort — 실패해도 throw 하지 않고 null 반환(런처 흐름 비차단).
public static class DiagnosticsBundle
{
    // 진단 ZIP 생성. 성공 시 zip 경로, 실패 시 null. 실제 경로/시스템정보를 채워 내부 오버로드 호출.
    public static string? Create() => Create(AppPaths.LogDir, AppPaths.DataRoot, SystemInfoText());

    // 테스트용 오버로드(InternalsVisibleTo) — logDir 의 로그 + systemInfo 를 outDir 에 zip 으로 묶는다.
    internal static string? Create(string logDir, string outDir, string systemInfo)
    {
        try
        {
            Directory.CreateDirectory(outDir);
            var zipPath = Path.Combine(outDir, $"herma-진단-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var info = zip.CreateEntry("system-info.txt");
                using (var w = new StreamWriter(info.Open(), new UTF8Encoding(false)))
                    w.Write(systemInfo);

                // 최근 로그만(최신 6개씩) — 오래된 로그까지 다 넣어 비대해지는 것 방지.
                foreach (var pattern in new[] { "launcher-*.log", "game-*.log" })
                {
                    string[] files;
                    try { files = Directory.GetFiles(logDir, pattern); }
                    catch { files = Array.Empty<string>(); }
                    Array.Sort(files, StringComparer.Ordinal); // 파일명에 날짜 → 사전순 = 시간순
                    var start = Math.Max(0, files.Length - 6);
                    for (var i = start; i < files.Length; i++)
                    {
                        try { zip.CreateEntryFromFile(files[i], "logs/" + Path.GetFileName(files[i])); }
                        catch { /* 한 파일 실패는 무시하고 계속 */ }
                    }
                }
            }
            return zipPath;
        }
        catch (Exception ex)
        {
            AppLog.Warn(LaunchStage.Idle, "진단 파일 생성 실패: " + ex.Message);
            return null;
        }
    }

    private static string SystemInfoText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("HermaLauncher 진단 정보");
        sb.AppendLine($"생성 시각  : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"런처 버전  : {AppInfo.Version}");
        sb.AppendLine($"Minecraft  : {LauncherConfig.MinecraftVersion} / Fabric {LauncherConfig.FabricLoaderVersion}");
        sb.AppendLine($"서버 주소  : {LauncherConfig.ServerIp}:{LauncherConfig.ServerPort}");
        sb.AppendLine($"OS         : {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        sb.AppendLine($".NET       : {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"데이터 경로: {AppPaths.DataRoot}");
        return sb.ToString();
    }
}
