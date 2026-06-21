#!/usr/bin/env bash
# 전체 모드셋 packwiz 추가. 의존 라이브러리는 -y 로 자동 수락.
PW="${PACKWIZ:-packwiz}"
cd "$(dirname "$0")" || exit 1
set -o pipefail   # 파이프 중간 실패(packwiz refresh 등) 마스킹 방지 (review F-1)

# 핵심(모드구성.md) — fabric-api, sodium 은 이미 추가됨
CORE="lithium ferrite-core entityculling immediatelyfast sodium-extra reeses-sodium-options iris \
entitytexturefeatures entity-model-features lambdynamiclights simple-voice-chat sound-physics-remastered \
xaeros-minimap xaeros-world-map enchantment-descriptions clumps animal_feeding_trough rightclickharvest \
modmenu appleskin jade mouse-tweaks controlling zoomify"

# 추가 바닐라+(추가모드_서버스택.md client)
ADD="fallingleaves make_bubbles_pop ambientsounds presence-footsteps rrv inventory-profiles-next \
dynamic-fps screencopy autoreconnectrf natures-compass explorers-compass ultimate_map_atlases \
offershud visible-traders betterf3 not-enough-animations 3dskinlayers chat-heads \
better-clouds bobby recall-coords capes simple-auto-fishing \
visuality lambdabettergrass"

# 서버
SERVER="luckperms warputils styled-chat starter-kit blastproof open-parties-and-claims ledger fastback \
chunky krypton servercore scalablelux spark squaremap"

# 리소스/셰이더팩
RES="fresh-animations fresh-animations-extensions complementary-reimagined \
complementary-unbound bsl-shaders sildurs-vibrant-shaders \
better-leaves default-dark-mode nautilus3d vanilla-connected-glass"

# RPG/던전/창고 (2026-06-21 추가, 근거: docs/research/2026-06-20-multimode-magic-rpg.md)
# 창고=Refined Storage(디지털 저장망) + 던전/구조물/월드젠/몹/NPC/전투편의. 전부 MC 26.1.2 Fabric 라이브 검증.
RPG="refined-storage waystones travelersbackpack lootr \
dungeons-and-taverns moogs-voyager-structures structory structory-towers towns-and-towers explorify \
terralith incendium nullscape \
friends-and-foes illager-invasion mutant-monsters minecraft-comes-alive-reborn edf-remastered \
combatify cut-through"
# 편의성 확장 (2026-06-21, "괜찮은 건 다" — vanilla+/QoL/RPG, MC 26.1.2 Fabric 검증)
CONV="farmers-delight-refabricated more-delight cooking-for-blockheads ecologics wilder-wild fish-of-thieves \
universal-bone-meal trample-no-more stellarity \
veinminer veinminer-client easy-anvils enchanting-infuser grind-enchantments advanced-netherite tool-stats \
held-item-info building-wands armor-statues spyglass-improvements max-health-fix first-person-model boat-item-view \
netherportalfix hardcore-revival double-doors kleeslabs bl4cks-sit client-tweaks chatpatches \
emotecraft villager-names-serilum overflowing-bars invmove \
shulkerboxtooltip easy-shulker-boxes universal-graves stack-to-nearby-chests xp-tome inventoryhudplus simple-copper-pipes \
promenade mes-moogs-end-structures respawnable-pets shoulder-surfing-reloaded no-chat-reports moreculling \
macaws-furniture macaws-doors macaws-bridges macaws-roofs macaws-fences-and-walls macaws-windows macaws-trapdoors \
macaws-paintings diagonal-fences"

ok=0; fail=0; failed=""
add() {
  for m in $1; do
    if "$PW" modrinth add "$m" -y >/tmp/pw_$m.log 2>&1; then
      line=$(grep -E "successfully added" /tmp/pw_$m.log | tail -1)
      echo "OK   $m :: ${line:-added}"
      ok=$((ok+1))
    else
      echo "FAIL $m :: $(tail -2 /tmp/pw_$m.log | tr '\n' ' ')"
      fail=$((fail+1)); failed="$failed $m"
    fi
  done
}

echo "### CORE ###"; add "$CORE"
echo "### ADD ###"; add "$ADD"
echo "### SERVER ###"; add "$SERVER"
echo "### RES ###"; add "$RES"
echo "### RPG ###"; add "$RPG"
echo "### CONV ###"; add "$CONV"

# side 교정: Modrinth env 선언 기준으로 both 오감지를 server/client 로 좁힘 (fix-sides.py).
# 멱등 + 명시 side 는 미수정. RPG/CONV 신규분만 대상(원본 큐레이션 보호).
echo "### fix sides ###"
PY=$(command -v python || command -v python3) || { echo "ERROR: python 인터프리터 없음"; exit 1; }
"$PY" fix-sides.py $RPG $CONV   # stderr 노출 (SyntaxError/ImportError 은폐 방지, review F-1)

echo
echo "===== SUMMARY: ok=$ok fail=$fail ====="
echo "FAILED:$failed"
echo "### refresh ###"
"$PW" refresh 2>&1 | tail -3
if [ "$fail" -gt 0 ]; then
  echo "ERROR: 모드 추가 $fail 건 실패 ($failed) — 팩이 불완전합니다. 중단." >&2
  exit 1
fi
