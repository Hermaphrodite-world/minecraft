# Herma 한국어 번역 — 공용 용어집 / 규칙 (SoT)

> 39개 모드를 여러 배치(Claude + Codex)로 병렬 번역하므로, **용어 일관성**을 위해 이 파일을 단일 기준으로 삼는다.

## 번역 규칙 (BLOCKING)

1. **키는 절대 번역하지 않는다.** JSON 의 key 는 그대로, **value(영어 문장)만** 한국어로.
2. **플레이스홀더/포맷 코드 원형 보존**: `%s` `%d` `%1$s` `%.1f` `{0}` `{}` `{value}` `\n` `\t` `%%`, Minecraft 색·서식 코드 `§a`~`§r`/`&a`, HTML 류 `<b>...</b>`, 키 토큰 등은 **그대로 유지**하고 위치만 한국어 어순에 맞춘다.
3. **고유명사/브랜드는 영문 유지**: 모드 이름(Sodium, Iris, Jade, Fabric…), 외부 서비스명은 번역하지 않는다. 단 설명문 안에서는 자연스럽게.
4. **마인크래프트 공식 한국어 용어** 우선(아래 표). 표에 없으면 가장 통용되는 표현.
5. **톤**: 게임 UI 다운 간결체. 버튼/토글은 명사형 또는 "~기"("켜기","끄기","초기화"). 설명은 평서체("~합니다" 과용 금지, 간결하게).
6. **빈 문자열/공백만 있는 값**은 그대로 둔다.
7. 의미 불명확하거나 영어 그대로가 관행인 짧은 토큰(예: "FPS", "RGB", "UUID")은 영문 유지.

## 핵심 용어 표 (EN → KO)

| EN | KO | | EN | KO |
|---|---|---|---|---|
| Settings / Config | 설정 | | Durability | 내구도 |
| Options | 옵션 | | Enchantment | 마법부여 |
| Toggle | 토글/켜고 끄기 | | Experience | 경험치 |
| Enable / Disable | 켜기 / 끄기 | | Inventory | 인벤토리 |
| Enabled / Disabled | 켜짐 / 꺼짐 | | Item | 아이템 |
| On / Off | 켜짐 / 꺼짐 | | Block | 블록 |
| Reset | 초기화 | | Recipe | 제작법 |
| Default | 기본값 | | Crafting | 제작 |
| Keybind / Key | 키 설정 / 키 | | Smelting | 제련 |
| Hotkey | 단축키 | | Structure | 구조물 |
| Overlay | 오버레이 | | Biome | 생물 군계 |
| Minimap | 미니맵 | | Dimension | 차원 |
| Map | 지도 | | Waypoint | 경유지 |
| Server | 서버 | | Coordinates | 좌표 |
| World | 세계 | | Player | 플레이어 |
| Backup | 백업 | | Entity | 엔티티 |
| Restore | 복원 | | Mob | 몹 |
| Claim | 영역(점유) | | Villager / Trader | 주민 / 상인 |
| Party | 파티 | | Trade / Offer | 거래 |
| Permission | 권한 | | Tooltip | 툴팁 |
| Render distance | 렌더 거리 | | HUD | HUD |
| Performance | 성능 | | Screenshot | 스크린샷 |
| Brightness | 밝기 | | Clipboard | 클립보드 |
| Shader | 셰이더 | | Sound / Audio | 소리 / 오디오 |
| Particle | 입자 | | Volume | 음량 |
| Animation | 애니메이션 | | Footstep | 발소리 |
| Compass | 나침반 | | Reverb / Echo | 반향 / 메아리 |
| Search | 검색 | | Harvest | 수확 |
| Filter | 필터 | | Ping (marker) | 핑 |
| Category | 분류 | | Cape | 망토 |
| Warning | 경고 | | Skin | 스킨 |

## 산출물 계약

- 입력: `translations/_work/<slug>/<ns>.todo.json` = `{ "key": "english value", ... }` (번역 필요분만)
- 출력: `translations/_work/<slug>/<ns>.ko.json` = `{ "key": "한국어 번역", ... }` — **입력과 키 집합이 정확히 동일**
- 검증: `python translations/tools/verify.py` 가 모드별 커버리지 산출. 목표 100%.
