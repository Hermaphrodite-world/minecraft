# herma-rpg-tweaks 통합 완성 — 구현 계획 (Codex × Claude 페어 검증)

> 작성: 2026-06-23 · 트랙: **rpg** (MC 1.21.1 NeoForge, `modpack-rpg/`) · 설계 SoT: [herma-rpg-tweaks-design.md](herma-rpg-tweaks-design.md)
> 가능성: Claude 실측 + Codex 페어 **수렴 → 진행 가능**(데이터팩/GLM/Gateways 중심). 단 균일 게이팅 *방식*은 Phase 0 spike 로 확정.
> 해결할 문제(사용자 지적): 게임플레이 **통합 호환** — ① 다른 모드 도구가 진행 게이팅을 우회 ② 파밍처가 자기 모드 아이템만 드랍(크로스 없음).

## 1. 가능성 결론 (페어 검증)

| 목표 | verdict | 근거 (Claude 실측 + Codex 검증) |
|---|---|---|
| **② 크로스-모드 루트** | ✅ feasible | GLM `neoforge:loot_table_id` 가 임의 루트테이블 타겟(현재 chest 88 ID: generic 48 + treasure 40). 몹/낚시/구조물/보스 루트테이블 ID 열거해 modifier 추가. ⚠️ wildcard/tag/namespace **bulk 매칭은 미확인** → 명시 ID 열거 방식 전제 |
| **① 균일 게이팅** | ✅ feasible (방식 분기) | **per-item JSON = 확인된 유일 경로**(`data/<modid>/pmmo/items/<item>.json`, O(n)). PMMO tag-요구 / KubeJS bulk = **repo·jar·문서 부재로 확인 불가(uncertain)** → Phase 0 spike 로 결정 |

> ※ 앞 세션에서 Claude 가 "KubeJS 로 일괄 게이팅 가능할 것"이라 한 것은 **미검증 추측**이었고, Codex 페어가 uncertain 으로 교정함. plan 은 bulk 를 전제하지 않는다.

## 2. 아키텍처 (Codex 권고)

- **스킬 매핑**: 근접=`combat`, 마법=`magic`, 활=`archery`, 회피=`agility`/`endurance`, 장신구=`endurance`/`magic`
- **루트 티어 ↔ 진행 연결**: Gateways(trial_1 → trial_2 → raid) + Apotheosis 보스 루트를 GLM loot 단계와 묶음
- **SoT 경로** (기존 herma-rpg-tweaks 확장):
  - PMMO 요구: `data/<modid>/pmmo/items/<item>.json`
  - PMMO 서버 토글: `data/pmmo/config/server.json`
  - GLM 루트: `data/neoforge/loot_modifiers/global_loot_modifiers.json` + `data/herma_rpg/loot_modifiers/<source>/*.json`
  - Gateways: `data/herma_rpg/gateways/gateways/*.json` + `data/herma_rpg/recipe/gate_*.json`
  - (KubeJS 채택 시) `modpack-rpg/kubejs/server_scripts/pmmo_requirements.js` — **단 PMMO binding API 확인 전엔 요구 생성 SoT 로 두지 말 것**

## 3. 단계 (effort = XS/S/M/L/XL, 시간 환산 없음)

| Phase | 작업 | effort | 게이트 |
|---|---|---|---|
| **P0 — 게이팅 방식 SPIKE** | PMMO 가 (a) item tag 기반 requirements 지원? (b) KubeJS server-script 로 bulk 적용 가능? — PMMO 공식 위키/문서 + dev 1케이스 실측. 결과로 P1 방식 결정 | **S** | **BLOCKING (작업량 결정)** |
| **P1 — 균일 스탯 게이팅** | 모든 모드 무기/도구/방어구에 스킬 요구 적용(§2 매핑). bulk 가능 → 태그/KubeJS(O(1)); 불가 → per-item JSON **생성 스크립트**로 대량 생성(O(n)). 대상 우선순위: 무기→방어구→도구→Curios 장신구 | **M(bulk)~XL(per-item)** | P0 결과 |
| **P2 — 크로스-모드 루트** | GLM 확장: chest 외 **몹 드랍/낚시/구조물/보스** 루트테이블 ID 열거 → modifier 추가. 루트 티어를 Gateways/Apotheosis 진행과 연결 | **M** | — |
| **P3 — 밸런스 & 리스크 완화** | 아래 §4 리스크 처리(초반 면제·autovalue 정책·affix/전투 상호작용) | **M** | — |
| **P4 — 검증(실기기, 사용자 영역)** | 헤드리스 서버 부팅 + 클라 싱글 스모크: **다른 모드 도구도 게이팅 걸림 + 크로스 루트 드랍** 육안 확인. 미검증 영역 명시 | **M** | **BLOCKING(PASS 전)** |

## 3.1 P0 SPIKE 결과 (2026-06-23 — 웹 authoritative + Codex 페어)

- **❌ KubeJS bulk(PmmoJS) 경로 = 1.21.1 NeoForge 빌드 없음** — `pmmo-js`(KubeJS PMMO) 최신은 **1.20.1 Forge 전용**(0.5.2, 2026-04). 1.21.x/NeoForge 빌드 부재 → `PmmoJS.settings`/`event.items(...)` bulk API 가 존재하나 **이 버전에선 사용 불가**.
- **✅ PMMO 데이터팩 `isTagFor` 그룹화 = 채택** — 설정 1개 파일이 `isTagFor` **배열에 나열한 모든 아이템**에 동일 `requirements`/`xp_values` 적용. 단 `isTagFor` 는 **아이템 ID 배열**(MC 태그 `#c:swords` 참조 아님) → 아이템 **열거 필요**, 그러나 per-item 수백 파일이 아니라 **티어 그룹 파일 몇 개**로 통합.
- **열거 반자동화**: KubeJS 태그 덤프/`/kubejs` 명령 또는 1회용 스크립트로 카테고리별(검/도끼/활/방어구/도구) 아이템 ID 목록을 추출 → `isTagFor` 배열에 투입.
- requirements 타입(확정): `USE` / `WEAPON` / `WEAR` / `TOOL` / `PLACE` / `BREAK` / `KILL` 등.

> **결정**: 균일 게이팅 = **PMMO 데이터팩 isTagFor 티어 그룹**. effort **L**(per-item XL 회피, 단 열거+티어링 필요). P1 은 이 방식으로 확정.
> Sources: [PMMO items datapack](https://moddedmc.wiki/en/project/pmmo/latest/docs/configuration/datapackconfig/items) · [PMMO gating](https://moddedmc.wiki/en/project/pmmo/latest/docs/core/gating) · [KubeJS PMMO settings](https://moddedmc.wiki/en/project/pmmo-js/latest/docs/serverevents/settings) · [pmmo-js files (1.20.1 only)](https://www.curseforge.com/minecraft/mc-mods/pmmo-js/files/all)
>
> **Codex 페어 재조정**: Codex(훈련지식)는 "태그 불가·per-item만·KubeJS 브릿지 없음"으로 답했고 **`isTagFor` 를 몰랐음**(웹검색 중 timeout). 웹(현행 공식 위키)이 `isTagFor` 배열을 확정 → web-authoritative 채택. **수렴**: 둘 다 (a) 태그-참조 자동 bulk 없음 (b) KubeJS PMMO 브릿지 1.21.1 NeoForge 미가용 (c) 아이템 ID 열거+스크립트 생성이 현실 경로. Codex 추가 옵션: KubeJS `ResourcePackEventJS` 로 startup 시 PMMO JSON 동적 emit(비공식 우회 — 채택 보류, datapack 우선).

## 4. 리스크 (Codex 식별 — 현재 완화 근거 없음)

- **초반 진행 데드락** — 스탯을 못 얻는데 기본 도구가 막히면 시작 불가 → 시작 도구/저티어 아이템 게이팅 면제 정책 필요.
- **PMMO autovalue ↔ 수동 요구 충돌** — autovalue 활성 시 수동 요구와 겹침 → 정책 명시(autovalue off + 수동, 또는 혼용 규칙).
- **대형 GLM condition 성능/유지보수** — 루트 소스마다 ID 수십~수백 → 파일 분리·티어별 구조화.
- **모드 상호작용** — Apotheosis affix 장비·Better Combat·Simply Swords·Combat Roll·Curios 가 게이팅과 어떻게 맞물리는지 런타임 검증.
- **KubeJS bulk 미확인** — P0 미통과 시 게이팅이 XL(per-item 수백). 생성 스크립트로 완화하되 유지보수 부담 잔존.

## 5. 진행 권고

- **P0 spike 부터** — 균일 게이팅의 작업량(M vs XL)이 여기서 갈리므로 plan 의 첫 실행 단위.
- P2(크로스 루트)는 P0 와 독립이라 **병행 착수 가능**(GLM 은 이미 확인됨).
- 모든 모드팩 변경은 **실행 = 사용자 승인 후**(이 문서는 계획). P4 런타임 검증은 사용자 머신 필요(샌드박스 GUI/MC 불가).

## 6. 구현 로그 & 코드리뷰 (2026-06-23 — Codex 페어 + 중간 코드리뷰)

### ✅ 증분 1 — 바닐라 엔드게임 게이팅 (유지, 미커밋)
`data/minecraft/pmmo/items/` 에 4파일: `diamond_sword`(WEAPON combat 15, isTagFor diamond_axe), `netherite_sword`(combat 30, netherite_axe), `diamond_chestplate`(WEAR endurance 15, +helmet/leggings/boots), `netherite_chestplate`(endurance 30, +3).
- 코드리뷰: JSON 유효(37/37) + 포맷 위키 일치 + Codex sanity OK. isTagFor=item-id 배열은 web-authoritative(위키)로 확정(Codex 불확실 → web 우선).
- 잠정: 레벨(15/30)은 Gateways 티어 미연동(밸런스 패스에서 정렬). 도끼는 WEAPON만(채광 TOOL 미게이팅, 의도).

### ↩️ 증분 2 — 크로스-소스 스크롤 루트 (작성 후 **revert**)
낚시+희귀몹(witch/evoker/vindicator/ravager/piglin_brute/wither_skeleton)에 스크롤 주입 시도 → **Codex 코드리뷰가 2건 검출, "merge 금지" → revert**:
- **R1 (likely)**: `irons_spellbooks:append_loot` 가 entity/fishing 루트테이블에 동작하는지 미확인 — chest 전용이면 **silent no-op**. 인게임 테스트(몹킬+낚시) 필수.
- **R2 (high)**: `key: irons_spellbooks:chests/additional_generic_loot` 는 **chest 테이블** → 스크롤 외 아이템도 몹/낚시에 드랍 가능. **스크롤 전용 loot table key 로 교체** 필요.
- → 재추가는 **P4(실기기)에서 R1·R2 확인 후**. (조건 구조 any_of AND random_chance:0.15, 루트테이블 id 표기는 OK 판정)

### ✅ 증분 3 — 모드 장비 포괄 게이팅 (실서버 덤프 기반, 완료 — 사용자 핵심 문제 해결)
**헤드리스 NeoForge 1.21.1 서버를 직접 부팅**(Java 21, win_args.txt, Done 2.3s)해 KubeJS 로 아이템 열거: 전체 **4427 아이템** + 카테고리 태그(`c:swords`=123 모드검, `c:armors`=137). 이를 `isTagFor` 로 그룹화(증분1 바닐라 placeholder 를 대체):
- `iron_sword.json`: floor `WEAPON combat 5`, isTagFor=124 (Simply Swords 등 **모드검 전부** + golden_sword)
- `netherite_sword.json`: top `WEAPON combat 20`, isTagFor=[diamond_sword]
- `iron_chestplate.json`: floor `WEAR endurance 5`, isTagFor=124 (모드 방어구[ars_nouveau 등] + 바닐라 chain/gold/iron)
- `netherite_chestplate.json`: top `WEAR endurance 20`, isTagFor=[diamond/netherite 7조각]
- 스타터(나무/돌검·가죽갑옷·elytra) 면제 → 초반 데드락 회피.
- **코드리뷰**: Codex 4항목(크로스NS OK·밸런스 OK·WEAR OK·중복 위험) + **disjoint 실증**(floor∩top=∅, 어떤 아이템도 2 config 미중복) → 중복 위험 해소.
- → **사용자 핵심 문제(타 모드 무기/방어구가 게이팅 우회) 해결**: 이제 모든 모드 무기/방어구가 일관 게이팅됨.

### ⏳ 남은 것 (P4 / 후속)
- **인게임 효과 검증(P4, 클라 필요)**: isTagFor 크로스-네임스페이스가 실제로 모드검을 막는지 — 헤드리스로는 데이터로드만 가능, 효과(플레이어가 모드검 못 듦)는 클라+플레이 검증. 적용하려면 변경 commit/push(런처 RPG 채널이 remote `rpg` 동기화) 또는 로컬 인스턴스 복사.
- **파워 티어링**(후속): 모드검 전부 floor 5(파워 무관) — 진짜 ARPG 곡선엔 무기 공격력별 티어 필요 → 공격력 덤프 or 사용자 큐레이션(게임디자인 영역).
- **P2 루트**(낚시/몹 스크롤): 이전 revert 사유(R1 append_loot 엔티티 호환·R2 스크롤전용 key) P4 확인 후 재추가.
> Codex 페어 신뢰성: feasibility·spike·증분 코드리뷰 성공 / 설계·일부 호출 플래키(timeout·거부) — 정직 기록. 매 증분 JSON 검증 + Codex 리뷰 병행.
