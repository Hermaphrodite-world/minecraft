using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HermaLauncher.Services;

// 모든 런타임 데이터(게임 디렉토리/토큰 캐시/packwiz mods/업데이트 staging)는
// **서명된 앱 번들 바깥**에 둔다 (구현계획 §4 불변식 — 번들 내부 쓰기 시 codesign/staple 파손).
//   Windows : %APPDATA%\HermaLauncher
//   macOS   : ~/Library/Application Support/HermaLauncher
//   Linux   : ~/.local/share/HermaLauncher (XDG)
public static class AppPaths
{
    public const string AppFolderName = "HermaLauncher";

    public static string DataRoot { get; } = ResolveDataRoot();

    // 게임 인스턴스(.minecraft 상당). packwiz --pack-folder + CmlLib MinecraftPath 가 여기를 본다.
    public static string GameDir => EnsureDir(Path.Combine(DataRoot, "instance"));

    // packwiz-installer-bootstrap.jar 위치 (런처가 동봉/캐시)
    public static string BootstrapJar => Path.Combine(DataRoot, "packwiz-installer-bootstrap.jar");

    // MS 계정/토큰 캐시 (JELoginHandler.WithAccountManager) — 번들 밖
    public static string AccountsJson => Path.Combine(DataRoot, "accounts.json");

    // 로그
    public static string LogDir => EnsureDir(Path.Combine(DataRoot, "logs"));

    private static string ResolveDataRoot()
    {
        string baseDir;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            baseDir = Path.Combine(home, "Library", "Application Support");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else
        {
            var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            baseDir = string.IsNullOrEmpty(xdg)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share")
                : xdg;
        }

        return EnsureDir(Path.Combine(baseDir, AppFolderName));
    }

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
