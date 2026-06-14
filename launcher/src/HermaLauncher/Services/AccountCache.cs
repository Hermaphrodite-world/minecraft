using System;
using System.IO;

namespace HermaLauncher.Services;

// P3-1: 로그인한 계정 표시명(마인크래프트 닉네임)을 별도 파일에 캐시한다.
// accounts.json(XboxAuthNet 토큰 캐시)에서 표시명을 직접 읽기는 까다로워, 로그인 성공 시
// 닉네임만 평문으로 따로 저장해 시작 화면/설정에서 "○○님으로 로그인됨"을 보여준다.
// 토큰이 아니므로 평문 저장 무방. 로그아웃 = accounts.json + 이 파일 삭제.
// 동시 인증/로그아웃 경로 race 방지를 위해 모든 파일 접근을 단일 lock 으로 직렬화(Codex LOW-6).
public static class AccountCache
{
    private static readonly object _gate = new();

    // 마지막 로그인 표시명. 없으면 null.
    public static string? LastUsername()
    {
        lock (_gate)
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
    }

    // 로그인 성공 후 표시명 저장(best-effort, 원자적 교체).
    public static void Remember(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return;
        lock (_gate)
        {
            try
            {
                var tmp = AppPaths.LastAccountFile + ".tmp";
                File.WriteAllText(tmp, username.Trim());
                File.Move(tmp, AppPaths.LastAccountFile, overwrite: true);
            }
            catch (Exception ex) { AppLog.Warn(LaunchStage.Auth, "계정 표시명 저장 실패(무시): " + ex.Message); }
        }
    }

    // 로그아웃 — 토큰 캐시(accounts.json) + 표시명 파일 삭제.
    // 반환값 = 토큰 캐시가 실제로 사라졌는지. false 면 토큰이 남아 다음 Play 가 silent-login 할 수 있으므로
    // 호출자는 UI 를 '로그인됨' 으로 유지하고 사용자에게 재시도를 안내해야 한다(Codex HIGH-1).
    public static bool Clear()
    {
        lock (_gate)
        {
            foreach (var path in new[] { AppPaths.AccountsJson, AppPaths.LastAccountFile })
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (Exception ex) { AppLog.Warn(LaunchStage.Auth, $"계정 캐시 삭제 실패({Path.GetFileName(path)}): " + ex.Message); }
            }
            var tokenGone = !File.Exists(AppPaths.AccountsJson); // 토큰 잔존 여부가 로그아웃 성패의 핵심
            if (tokenGone)
                AppLog.Info(LaunchStage.Auth, "로그아웃: 계정 캐시 삭제됨");
            else
                AppLog.Warn(LaunchStage.Auth, "로그아웃 실패: 토큰 캐시(accounts.json) 가 남아있음");
            return tokenGone;
        }
    }
}
