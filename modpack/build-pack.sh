#!/usr/bin/env bash
# 전체 모드셋 packwiz 추가. 의존 라이브러리는 -y 로 자동 수락.
PW="${PACKWIZ:-packwiz}"
cd "$(dirname "$0")" || exit 1

# 핵심(모드구성.md) — fabric-api, sodium 은 이미 추가됨
CORE="lithium ferrite-core entityculling immediatelyfast sodium-extra reeses-sodium-options iris \
entitytexturefeatures entity-model-features lambdynamiclights simple-voice-chat sound-physics-remastered \
xaeros-minimap xaeros-world-map enchantment-descriptions clumps animal_feeding_trough rightclickharvest \
modmenu appleskin jade mouse-tweaks controlling zoomify"

# 추가 바닐라+(추가모드_서버스택.md client)
ADD="fallingleaves make_bubbles_pop ambientsounds presence-footsteps rrv inventory-profiles-next \
dynamic-fps screencopy autoreconnectrf natures-compass explorers-compass ultimate_map_atlases \
notenoughtooltips offershud visible-traders betterf3 not-enough-animations 3dskinlayers chat-heads \
better-clouds bobby recall-coords capes"

# 서버
SERVER="luckperms warputils styled-chat starter-kit blastproof open-parties-and-claims ledger fastback \
chunky krypton servercore scalablelux spark squaremap"

# 리소스/셰이더팩
RES="fresh-animations fresh-animations-extensions complementary-reimagined"

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

echo
echo "===== SUMMARY: ok=$ok fail=$fail ====="
echo "FAILED:$failed"
echo "### refresh ###"
"$PW" refresh 2>&1 | tail -3
