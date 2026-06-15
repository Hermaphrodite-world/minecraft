using System;

namespace HermaLauncher.Services;

// 게임 크래시/실패 시 흩어진 로그 대신 '한 가지 한국어 액션'을 제시하기 위한 분류기(순수 함수 — 단위 테스트 가능).
// 게임 로그(game-*.log) 본문에서 알려진 시그니처를 찾아 비개발자가 바로 행동할 수 있는 안내를 돌려준다.
// 매칭이 없으면 null → 호출자는 일반 안내로 폴백한다(억지 추측 금지 — false positive 회피).
public static class FailureDiagnosis
{
    public readonly record struct Hint(string Title, string Action);

    // (시그니처[소문자], 안내) 우선순위 순. 더 구체적/강한 신호를 위에 둔다.
    // 모두 case-insensitive 부분일치. 실제 마인크래프트/Fabric 로그·디스커넥트 메시지 기준.
    private static readonly (string[] Needles, Hint Hint)[] Rules =
    {
        // 메모리 부족(OOM) — 크래시 로그의 예외.
        (new[] { "outofmemoryerror", "out of memory", "java heap space" },
            new Hint("메모리가 부족해서 종료됐어요.", "설정 화면에서 메모리(RAM)를 올린 뒤 다시 시도해 주세요.")),
        // 화이트리스트 미등록 — 서버 접속 거부.
        (new[] { "not white-listed", "not whitelisted", "you are not white" },
            new Hint("서버 화이트리스트에 등록되어 있지 않아요.", "디스코드에 마인크래프트 닉네임을 보내 등록을 요청해 주세요.")),
        // 세션/인증 만료.
        (new[] { "invalid session", "failed to verify username", "unverified_username", "multiplayer.disconnect.unverified" },
            new Hint("로그인 세션이 만료됐어요.", "설정에서 로그아웃 후 다시 플레이하면 재로그인됩니다.")),
        // 모드 불일치/누락 — 서버-클라 모드셋 차이.
        (new[] { "requires the following mod", "missing mods", "incompatible mod", "mismatched mod", "mod rejections", "needs the following mod" },
            new Hint("서버와 모드 구성이 맞지 않아요.", "다시 시도하면 모드가 자동으로 다시 동기화돼요. 계속되면 런처를 최신으로 업데이트하거나 디스코드로 알려 주세요.")),
        // 서버 접속 실패(네트워크).
        (new[] { "connection refused", "connection timed out", "annotatedconnectexception", "unknownhostexception", "failed to connect", "no further information" },
            new Hint("서버에 연결하지 못했어요.", "잠시 후 다시 시도하고, 같은 집/네트워크에서 서버를 켰다면 설정의 '서버 주소 직접 입력'에 서버 PC의 IP를 넣어 주세요.")),
    };

    // logText: game-*.log 본문(없으면 null). 매칭 시 Hint, 없으면 null.
    public static Hint? Classify(string? logText)
    {
        if (string.IsNullOrEmpty(logText)) return null;
        var lower = logText.ToLowerInvariant();
        foreach (var (needles, hint) in Rules)
        {
            foreach (var n in needles)
            {
                if (lower.Contains(n, StringComparison.Ordinal))
                    return hint;
            }
        }
        return null;
    }
}
