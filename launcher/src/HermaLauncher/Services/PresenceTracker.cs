using System;
using System.Collections.Generic;
using System.Linq;

namespace HermaLauncher.Services;

// 서버 접속 알림(토스트)용 순수 로직 — 폴링마다 ServerStatus 를 받아 "새로 접속한 사람"을 산출한다.
//
// 설계 핵심(왜 인원수 순증을 트리거로 쓰나):
//   ServerStatus.Sample(닉네임)은 best-effort 라 서버가 매 ping 마다 '랜덤 일부'만 채울 수 있다(회전).
//   sample 만 diff 하면 같은 인원인데도 이름이 들락날락해 거짓 "접속" 알림이 쏟아진다.
//   → 알림 트리거는 **인원수(Players)의 순증(net increase)** 으로 두고, 닉네임은 라벨로만 쓴다.
//      (sample 회전은 인원수 불변 → 트리거 안 됨. 신뢰 가능.)
//
// 본인 제외: 내가 게임에 접속하면 내 닉네임/인원수도 오른다 → 본인(selfName)은 인원수·이름 양쪽에서 제외.
// 베이스라인: 첫 폴링(또는 오프라인→온라인 복귀) 직후엔 '이미 접속해 있던 사람'을 알리지 않는다(스팸 방지).
public sealed class PresenceTracker
{
    // 직전 폴링에서 '나를 제외한' 접속자 닉네임 집합(라벨용).
    private HashSet<string> _knownNames = new(StringComparer.OrdinalIgnoreCase);

    // 직전 폴링의 '나를 제외한' 유효 인원수(트리거용). null = 베이스라인 미설정(첫 폴링/오프라인 복귀 직후).
    private int? _lastOthersCount;

    private bool _hasBaseline;

    // 폴링 1회 결과. Names = 새 닉네임(라벨), AnonymousCount = 이름 없이 인원수만 늘어난 수.
    public readonly record struct JoinResult(IReadOnlyList<string> Names, int AnonymousCount)
    {
        public static JoinResult None => new(Array.Empty<string>(), 0);

        // 알릴 게 있나(이름 1개라도, 또는 익명 1명이라도).
        public bool HasJoin => Names.Count > 0 || AnonymousCount > 0;
    }

    // 서버가 응답한 status 로 갱신하고 '이번에 새로 들어온 사람'을 반환한다.
    //   sample        : status.players.sample[].name (없으면 빈 리스트)
    //   online        : status.players.online (없으면 null — 이 경우 트리거 불가 → 항상 None)
    //   selfName      : 내 MC 닉네임(AccountCache.LastUsername) — 본인 제외용. null 이면 제외 안 함.
    //   selfOnlineHint: 내가 지금 게임에 접속 중일 가능성(IsGameRunning). 익명/부분 sample 서버에서 sample 에
    //                   내 닉네임이 없어도 '내 슬롯'을 인원수에서 빼, 자기 접속을 친구 접속으로 오인하지 않게.
    public JoinResult Update(IReadOnlyList<string>? sample, int? online, string? selfName, bool selfOnlineHint = false)
    {
        var raw = new HashSet<string>(
            (sample ?? Array.Empty<string>()).Where(n => !string.IsNullOrWhiteSpace(n)),
            StringComparer.OrdinalIgnoreCase);

        var selfOnline = selfOnlineHint || (selfName is not null && raw.Contains(selfName));

        // 나를 제외한 이름 집합(라벨용).
        var others = new HashSet<string>(raw, StringComparer.OrdinalIgnoreCase);
        if (selfName is not null) others.Remove(selfName);

        // 나를 제외한 유효 인원수(트리거용). online 이 없으면 트리거 불가.
        int? othersCount = online is int c ? Math.Max(0, c - (selfOnline ? 1 : 0)) : null;

        // 베이스라인 미설정 → 현재 상태만 기록하고 알리지 않음(이미 있던 사람은 스킵).
        if (!_hasBaseline)
        {
            _knownNames = others;
            _lastOthersCount = othersCount;
            _hasBaseline = true;
            return JoinResult.None;
        }

        // 인원수 순증이 없으면(같거나 줄었으면) 알릴 접속 없음 — sample 회전 거짓양성 차단.
        var delta = (othersCount is int now && _lastOthersCount is int prev) ? now - prev : 0;
        if (delta <= 0)
        {
            _knownNames = others;
            _lastOthersCount = othersCount;
            return JoinResult.None;
        }

        // 순증분 delta 만큼이 새 접속 — 그중 이름을 알 수 있는 사람을 라벨로(최대 delta 명).
        var newNames = others.Where(n => !_knownNames.Contains(n)).Take(delta).ToList();
        var anonymous = Math.Max(0, delta - newNames.Count);

        _knownNames = others;
        _lastOthersCount = othersCount;
        return new JoinResult(newNames, anonymous);
    }

    // 서버 오프라인 등으로 상태를 알 수 없을 때 — 베이스라인을 비워 다음 온라인 복귀 시 스팸 없이 재기준.
    public void Reset()
    {
        _hasBaseline = false;
        _knownNames.Clear();
        _lastOthersCount = null;
    }

    // 접속 결과 → 사용자용 한국어 토스트 본문. 알릴 게 없으면 null.
    public static string? FormatJoinMessage(JoinResult r)
    {
        if (!r.HasJoin)
            return null;

        var total = r.Names.Count + r.AnonymousCount;

        if (r.Names.Count == 0)
            return total == 1 ? "누군가 서버에 접속했어요" : $"{total}명이 서버에 접속했어요";

        if (total == 1)
            return $"{r.Names[0]} 님이 서버에 접속했어요";

        return $"{r.Names[0]} 외 {total - 1}명이 서버에 접속했어요";
    }
}
