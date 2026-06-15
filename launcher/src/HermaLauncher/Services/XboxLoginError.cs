using System.Text.RegularExpressions;

namespace HermaLauncher.Services;

// XSTS(Xbox Live) 로그인 거부의 XErr 코드 → 비개발자용 한국어 안내(순수 함수 — 단위 테스트 가능).
// 기획서 §4.3: 미성년·지역·Xbox 프로필 부재·밴 등을 generic 예외 덤프가 아닌 전용 메시지 경로로 분기.
//
// XErr 출처(어셈블리 리플렉션 검증): XSTS 실패는 `XboxAuthNet.XboxLive.XboxAuthException`
// (StatusCode/Error/ErrorMessage/Redirect) 으로 올라오며, XErr(21489162xx)가 그 문자열 필드/메시지에 실린다.
public static class XboxLoginError
{
    // 알려진 XErr → 한국어 안내. 모르는 코드면 null(호출자가 일반 메시지로 폴백).
    public static string? MessageForXErr(string? xerr) => xerr switch
    {
        "2148916227" => "이 계정은 Xbox에서 정지(밴)된 상태라 로그인할 수 없어요.",
        "2148916229" => "이 계정은 보호자 동의가 필요해요. Microsoft 가족(Family) 설정에서 동의를 완료한 뒤 다시 시도해 주세요.",
        "2148916233" => "이 계정에 Xbox 프로필이 없어요. xbox.com 또는 Xbox 앱에서 프로필을 먼저 만든 뒤 다시 로그인해 주세요.",
        "2148916235" => "이 지역에서는 Xbox Live를 사용할 수 없어 로그인할 수 없어요.",
        "2148916236" or "2148916237" => "이 계정은 성인 인증이 필요해요. Microsoft/Xbox 계정 설정에서 인증을 완료한 뒤 다시 시도해 주세요.",
        "2148916238" => "이 계정은 만 18세 미만이라, 보호자의 Microsoft 가족(Family)에 추가되어야 로그인할 수 있어요.",
        _ => null,
    };

    // 문자열에서 XSTS XErr 코드(21489162xx, 10자리)를 추출. 없으면 null.
    public static string? FindXErr(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var m = Regex.Match(text, "2148916[0-9]{3}");
        return m.Success ? m.Value : null;
    }
}
