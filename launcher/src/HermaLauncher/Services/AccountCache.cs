using System;
using System.IO;

namespace HermaLauncher.Services;

// P3-1: 로그인한 계정 표시명(마인크래프트 닉네임)을 별도 파일에 캐시한다.
// accounts.json(XboxAuthNet 토큰 캐시)에서 표시명을 직접 읽기는 까다로워, 로그인 성공 시
// 닉네임만 평문으로 따로 저장해 시작 화면/설정에서 "○○님으로 로그인됨"을 보여준다.
// 토큰이 아니므로 평문 저장 무방. 로그아웃 = accounts.json + 이 파일 삭제.
public static class AccountCache
{
    // 마지막 로그인 표시명. 없으면 null.
    public static string? LastUsername()
    {
        try
        {
            if (File.Exists(AppPaths.LastAccountFile))
            {
                var name = File.ReadAllText(AppPaths.LastAccountFile).Trim();
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
        }
        catch (Exception ex) { AppLog.Warn(LaunchStage.Auth, "계정 표시명 읽기 실패(무시): " + ex.Message); }
        return null;
    }

    // 로그인 성공 후 표시명 저장(best-effort).
    public static void Remember(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return;
        try { File.WriteAllText(AppPaths.LastAccountFile, username.Trim()); }
        catch (Exception ex) { AppLog.Warn(LaunchStage.Auth, "계정 표시명 저장 실패(무시): " + ex.Message); }
    }

    // 로그아웃 — 토큰 캐시(accounts.json) + 표시명 파일 삭제. 다음 Play 시 브라우저 재로그인.
    public static void Clear()
    {
        foreach (var path in new[] { AppPaths.AccountsJson, AppPaths.LastAccountFile })
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { AppLog.Warn(LaunchStage.Auth, $"계정 캐시 삭제 실패({Path.GetFileName(path)}): " + ex.Message); }
        }
        AppLog.Info(LaunchStage.Auth, "로그아웃: 계정 캐시 삭제됨");
    }
}
