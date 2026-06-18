using System;
using System.Collections.Generic;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 접속 알림 로직 — 인원수 순증을 트리거로, sample(닉네임)은 라벨로. 베이스라인/본인 제외/오프라인 리셋/
// sample 회전 거짓양성 차단/익명 카운트/메시지 포맷.
public class PresenceTrackerTests
{
    private static List<string> Names(params string[] n) => new(n);

    [Fact]
    public void First_update_sets_baseline_and_does_not_notify()
    {
        var t = new PresenceTracker();
        // 시작 시 이미 2명 접속 중 — 알리지 않는다(스팸 방지).
        var r = t.Update(Names("alice", "bob"), 2, selfName: null);
        Assert.False(r.HasJoin);
    }

    [Fact]
    public void Detects_named_join_after_baseline()
    {
        var t = new PresenceTracker();
        t.Update(Names("alice"), 1, null);                 // 베이스라인
        var r = t.Update(Names("alice", "yeonwoo"), 2, null);
        Assert.True(r.HasJoin);
        Assert.Equal(new[] { "yeonwoo" }, r.Names);
        Assert.Equal(0, r.AnonymousCount);
        Assert.Equal("yeonwoo 님이 서버에 접속했어요", PresenceTracker.FormatJoinMessage(r));
    }

    [Fact]
    public void Sample_rotation_without_count_change_does_not_notify()
    {
        var t = new PresenceTracker();
        t.Update(Names("alice", "bob"), 2, null);          // 베이스라인
        // 서버가 sample 을 회전: 같은 2명인데 다른 닉네임 노출 → 인원수 불변 → 알림 없음.
        var r = t.Update(Names("carol", "dave"), 2, null);
        Assert.False(r.HasJoin);
    }

    [Fact]
    public void Self_join_is_excluded()
    {
        var t = new PresenceTracker();
        t.Update(Names("alice"), 1, selfName: "me");        // 베이스라인(나는 아직 미접속)
        // 내가 게임에 접속 → 인원수 1→2, sample 에 내 닉네임. 본인 제외라 알림 없음.
        var r = t.Update(Names("alice", "me"), 2, selfName: "me");
        Assert.False(r.HasJoin);
    }

    [Fact]
    public void Self_excluded_but_other_join_still_notifies()
    {
        var t = new PresenceTracker();
        t.Update(Names("me"), 1, selfName: "me");           // 나만 접속(others=0)
        var r = t.Update(Names("me", "yeonwoo"), 2, selfName: "me");
        Assert.True(r.HasJoin);
        Assert.Equal(new[] { "yeonwoo" }, r.Names);
    }

    [Fact]
    public void Count_increase_without_name_yields_anonymous()
    {
        var t = new PresenceTracker();
        t.Update(Names(), 0, null);                         // 베이스라인(빈 서버)
        // 서버가 sample 을 안 채움 — 인원수만 0→1 → 익명 1명.
        var r = t.Update(Names(), 1, null);
        Assert.True(r.HasJoin);
        Assert.Empty(r.Names);
        Assert.Equal(1, r.AnonymousCount);
        Assert.Equal("누군가 서버에 접속했어요", PresenceTracker.FormatJoinMessage(r));
    }

    [Fact]
    public void Multiple_join_mixes_name_and_anonymous_into_count_message()
    {
        var t = new PresenceTracker();
        t.Update(Names("alice"), 1, null);                  // 베이스라인
        // 인원수 1→3(=2명 접속), sample 엔 새 이름 1명만 → "yeonwoo 외 1명".
        var r = t.Update(Names("alice", "yeonwoo"), 3, null);
        Assert.True(r.HasJoin);
        Assert.Equal(new[] { "yeonwoo" }, r.Names);
        Assert.Equal(1, r.AnonymousCount);
        Assert.Equal("yeonwoo 외 1명이 서버에 접속했어요", PresenceTracker.FormatJoinMessage(r));
    }

    [Fact]
    public void Leave_does_not_notify_and_rejoin_after_leave_notifies()
    {
        var t = new PresenceTracker();
        t.Update(Names("alice", "bob"), 2, null);           // 베이스라인
        var leave = t.Update(Names("alice"), 1, null);      // bob 퇴장 → 알림 없음
        Assert.False(leave.HasJoin);
        var rejoin = t.Update(Names("alice", "bob"), 2, null); // 재접속(순증) → 알림
        Assert.True(rejoin.HasJoin);
        Assert.Equal(new[] { "bob" }, rejoin.Names);
    }

    [Fact]
    public void Reset_rebaselines_without_spam_on_reconnect()
    {
        var t = new PresenceTracker();
        t.Update(Names("alice"), 1, null);                  // 온라인
        t.Reset();                                          // 서버 오프라인
        // 복귀 시 이미 2명 있어도 베이스라인 재설정 → 알림 없음.
        var r = t.Update(Names("alice", "bob"), 2, null);
        Assert.False(r.HasJoin);
    }

    [Fact]
    public void Null_online_count_never_notifies()
    {
        var t = new PresenceTracker();
        t.Update(Names("alice"), null, null);               // 인원수 불명
        var r = t.Update(Names("alice", "bob"), null, null);
        Assert.False(r.HasJoin); // 인원수 트리거 불가 → 거짓양성 방지 차원에서 알림 없음.
    }

    [Fact]
    public void Self_online_hint_suppresses_own_join_on_anonymous_server()
    {
        var t = new PresenceTracker();
        // 빈 서버에서 게임 미실행 베이스라인.
        t.Update(Names(), 0, selfName: "me", selfOnlineHint: false);
        // 내가 게임 접속 → 인원수 0→1, sample 은 익명(내 닉네임 없음)이지만 게임 실행 힌트로 내 슬롯 제외 → 알림 없음.
        var r = t.Update(Names(), 1, selfName: "me", selfOnlineHint: true);
        Assert.False(r.HasJoin);
    }

    [Fact]
    public void Friend_join_during_my_game_still_notifies_with_hint()
    {
        var t = new PresenceTracker();
        t.Update(Names(), 0, "me", selfOnlineHint: false);   // 베이스라인(빈 서버)
        t.Update(Names(), 1, "me", selfOnlineHint: true);    // 나만 접속(others=0)
        // 친구가 내 게임 중 접속 → 인원수 2, 내 슬롯 빼면 others 1 → 알림.
        var r = t.Update(Names("yeonwoo"), 2, "me", selfOnlineHint: true);
        Assert.True(r.HasJoin);
        Assert.Equal(new[] { "yeonwoo" }, r.Names);
    }

    [Fact]
    public void Format_returns_null_when_no_join()
    {
        Assert.Null(PresenceTracker.FormatJoinMessage(PresenceTracker.JoinResult.None));
    }
}
