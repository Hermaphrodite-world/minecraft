using System;
using System.IO;

namespace HermaLauncher.Services;

// 토큰/계정 캐시 파일 권한 강화(P2-3 MVP). best-effort — 실패해도 흐름을 막지 않는다(로그만).
public static class SecureFile
{
    // 파일을 현재 사용자 전용으로 제한. macOS/Linux 는 0600.
    // Windows 는 파일이 %APPDATA%(사용자 프로필) 하위라 기본 ACL 로 이미 사용자 전용 보호됨 →
    // 추가 NuGet(System.IO.FileSystem.AccessControl) 의존을 피하려고 명시 ACL 은 생략한다.
    // 전체 DPAPI/Keychain 암호화는 POST-1.0.
    public static void Harden(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); // 0600
        }
        catch (Exception ex)
        {
            AppLog.Warn(LaunchStage.Auth, "계정 파일 권한 강화 실패(무시): " + ex.Message);
        }
    }
}
