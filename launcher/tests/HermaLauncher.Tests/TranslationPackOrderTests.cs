using System.Collections.Generic;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 한국어 번역팩을 항상 "vanilla" 바로 다음(게임 내 '선택됨' 맨 아래 = lowest priority)으로 고정.
// 사용자 요구("한국어 팩이 제일 맨 아래") 회귀 방지 + idempotent 보장.
public class TranslationPackOrderTests
{
    private const string Ko = "\"file/herma-korean.zip\"";
    private const string Vanilla = "\"vanilla\"";

    [Fact]
    public void Moves_korean_pack_right_after_vanilla()
    {
        var active = new List<string> { Vanilla, "\"file/a.zip\"", Ko };
        var changed = false;
        ClientDefaults.EnsureTranslationPackAtBottom(active, ref changed);
        Assert.True(changed);
        Assert.Equal(1, active.IndexOf(Ko)); // vanilla(0) 바로 다음
    }

    [Fact]
    public void Idempotent_when_already_in_place()
    {
        var active = new List<string> { Vanilla, Ko, "\"file/a.zip\"" };
        var changed = false;
        ClientDefaults.EnsureTranslationPackAtBottom(active, ref changed);
        Assert.False(changed); // 이미 제자리 → 쓰기 없음
    }

    [Fact]
    public void Noop_when_korean_absent()
    {
        var active = new List<string> { Vanilla, "\"file/a.zip\"" };
        var changed = false;
        ClientDefaults.EnsureTranslationPackAtBottom(active, ref changed);
        Assert.False(changed); // 사용자가 껐거나 폴더에 없음 → 존중
    }

    [Fact]
    public void Korean_at_front_when_no_vanilla()
    {
        var active = new List<string> { "\"file/a.zip\"", Ko };
        var changed = false;
        ClientDefaults.EnsureTranslationPackAtBottom(active, ref changed);
        Assert.True(changed);
        Assert.Equal(0, active.IndexOf(Ko));
    }

    // ── apply-once 예외: 번역팩이 폴더에 있으면 active 에 없어도 재추가(채널 공유 인스턴스 함정 우회) ──

    [Fact]
    public void Activate_adds_korean_when_present_but_absent_from_active()
    {
        // 채널 전환으로 MC 가 options 에서 herma-korean 을 뺀 상태(active 에 없음)인데
        // resourcepacks/ 엔 파일이 다시 존재 → 재추가해야 한다(영구 미로드 회귀 방지).
        var present = new List<string> { "herma-korean.zip", "shader-x.zip" };
        var active = new List<string> { Vanilla, "\"file/shader-x.zip\"" };
        var changed = false;
        ClientDefaults.EnsureTranslationPackActive(present, active, ref changed);
        Assert.True(changed);
        Assert.Contains(Ko, active);
    }

    [Fact]
    public void Activate_noop_when_already_active()
    {
        var present = new List<string> { "herma-korean.zip" };
        var active = new List<string> { Vanilla, Ko };
        var changed = false;
        ClientDefaults.EnsureTranslationPackActive(present, active, ref changed);
        Assert.False(changed); // 이미 활성 → 중복 추가 안 함
    }

    [Fact]
    public void Activate_noop_when_pack_file_absent_from_folder()
    {
        // 폴더에 번역팩 zip 자체가 없으면(미동기화 등) 추가하지 않는다.
        var present = new List<string> { "shader-x.zip" };
        var active = new List<string> { Vanilla };
        var changed = false;
        ClientDefaults.EnsureTranslationPackActive(present, active, ref changed);
        Assert.False(changed);
        Assert.DoesNotContain(Ko, active);
    }
}
