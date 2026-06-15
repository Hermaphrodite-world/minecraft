using System;
using System.Collections.Generic;

namespace HermaLauncher.Services;

// quickPlay 자동 접속 host 결정 정책(순수 함수 — 단위 테스트 가능). 실제 네트워크 probe 는 호출자가 수행하고
// 그 결과(localServerUp)와 사용자 설정(overrideHost)만 받아 "어느 host 를 쓸지"를 결정한다.
//
// 우선순위(설계 근거):
//   (0) UserOverride — 설정의 '서버 주소 직접 입력'. 같은 집/네트워크의 다른 PC 에서 서버를 켠 경우
//       (NAT 헤어핀 미지원 → 공개 IP·localhost 둘 다 실패) LAN IP 를 직접 지정하는 탈출구. 명시 선택이라 최우선.
//   (1) Local(127.0.0.1) — 서버를 켠 바로 그 PC. NAT 헤어핀 미지원 시 자기 공개 IP 로 자기 서버에 못 들어감(P1-10).
//   (2) Public — 일반 친구 PC. 위 둘 다 아니면 공개 ServerIp.
public static class ServerHostResolver
{
    public enum Source
    {
        UserOverride,
        Local,
        Public,
    }

    // overrideHost 가 있으면 항상 UserOverride(로컬 probe 불필요). 없으면 localServerUp 으로 Local/Public.
    public static Source Decide(string? overrideHost, bool localServerUp)
    {
        if (!string.IsNullOrWhiteSpace(overrideHost)) return Source.UserOverride;
        if (localServerUp) return Source.Local;
        return Source.Public;
    }

    // 상태 pill 이 "응답하는 host" 를 찾기 위해 ping 할 순서(launch 의 ResolveServerHostAsync 와 동일 우선순위).
    //   override 있으면 그것만(사용자 명시 선택). 없으면 로컬(서버 켠 본인 PC) → 공개 IP.
    //   이로써 pill 이 launch 해석과 drift 하지 않는다(같은 PC 호스트가 '오프라인'으로 오표시되던 문제 해소).
    public static IReadOnlyList<string> StatusProbeOrder(string? overrideHost, string publicHost)
    {
        var ov = Normalize(overrideHost);
        if (ov is not null) return new[] { ov };
        return new[] { LoopbackHost, publicHost };
    }

    public const string LoopbackHost = "127.0.0.1";

    // 사용자 입력 host 정규화: 앞뒤 공백 제거 + 흔한 실수(scheme/슬래시/경로) 제거.
    // "192.168.0.5 ", "tcp://192.168.0.5", "192.168.0.5/" → "192.168.0.5". 빈/공백 → null.
    // 포트는 LauncherConfig.ServerPort 를 쓰므로 host 만 남긴다(끝의 ":포트"는 떼지 않음 — IPv6/오입력 위험 회피, 그대로 둠).
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim();
        var scheme = s.IndexOf("://", StringComparison.Ordinal);
        if (scheme >= 0) s = s[(scheme + 3)..];
        s = s.TrimEnd('/');
        s = s.Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
