# herma-rpg-tweaks — ARPG 통합 레이어 설계 (SoT)

> Hermaphrodite RPG (1.21.1 NeoForge) 를 "하드코어 RPG + 마법 적절히 섞기" 로 응집시키는 통합 설계.
> 모드를 수정/포팅하지 않고, **우리가 저작하는 KubeJS + PMMO config + 데이터(loot/recipe/tag)** 로 위에서 엮는다.
> 라이선스/컴파일/포팅 문제 없음(우리 콘텐츠가 모드를 *참조*만 함).

## 0. 목표 게임플레이 루프 (사용자 비전)

```
파티 co-op → 파밍/퀘스트 → 던전 공략 → 레이드 보스 (다같이 준비)
   → 보스 처치 → 루트(affix 장비 + 주문서 + 재료) → 스펙업(스킬 레벨 + 장비)
   → 더 강한 보스 해금 → 반복
```

톤 = **하드코어 RPG(A)**: 스킬 게이팅 강함, 성장 느림, 마법은 RPG 진행의 *일부*(주축이 아닌 보조 축으로 섞임).

## 1. 모드 큐레이션 (역할 고정)

| 역할 | 모드 | 통합 방식 |
|---|---|---|
| **주축 마법** | Iron's Spells & Spellbooks | PMMO **Magic** 스킬로 게이팅. 주문서=루트, 지팡이/로브=루트 → 스펙업 직결 |
| **보조 마법(유틸)** | Occultism | 소환/의식/차원저장. 느슨한 게이팅. 주문계와 안 겹침 |
| **사이드 마법(선택)** | Ars Nouveau | 게이트 없이 후반 탐구 콘텐츠. 주축 아님 |
| **플레이버(옵션)** | Eidolon, Forbidden Arcanus | 깊은 통합 X. 유지(컷은 사용자 결정) |
| **근접 전투** | Better Combat, Combat Roll, Simply Swords | PMMO **Combat** 게이팅. 고유무기=루트 |
| **스킬 엔진** | Project MMO (PMMO) | 전 진행의 중심축 (아래 §2) |
| **ARPG 루트** | Apotheosis(+Apothic Attributes, AttributeFix) | affix/등급 랜덤 장비 = "스펙업" 핵심 |
| **레이드 보스** | L_Ender's Cataclysm, Mowzie's Mobs, Mutant Monsters, Gateways to Eternity | 보스 트레드밀 (아래 §3) |
| **파티 루트** | Lootr | 플레이어별 인스턴스 전리품(분배 다툼 0) |
| **던전** | When Dungeons Arise, Dungeons&Taverns, YUNG's, Towns&Towers, Structory, Moog's | loot 주입 대상 (아래 §4) |
| **저장/유틸** | Refined Storage, Sophisticated, Waystones, JEI/JER, Xaero's | 루프 지원 |

## 2. 스킬 축 + 게이팅 정책 (PMMO — 하드코어 핵심)

### 2.1 사용할 스킬 (PMMO)
- **Combat** — 근접 피해/처치 (Better Combat/Simply Swords)
- **Magic** — 주문 시전 (Iron's Spells) ★ 마법 통합의 축
- **Archery** — 원거리
- **Endurance** — 최대 체력(피격/생존)
- **Defense** — 방어구 착용
- **Mining / Woodcutting / Excavation / Farming / Building** — 파밍 축(자원 채집)
- **Smithing / Crafting** — 장비 제작
- (보조) **Sorcery/Alchemy** — Occultism 의식 등

### 2.2 XP 획득원
- PMMO 내장: 몹 처치→Combat/Slayer, 채굴→Mining, 농사→Farming, 피격→Endurance 등 (config `xp_values`)
- **커스텀(KubeJS 필요)**: Iron's Spells **주문 시전 시 Magic XP** 부여 (PMMO가 Iron's 캐스팅을 기본 추적 안 하면 KubeJS 이벤트 훅으로 보강)

### 2.3 게이팅 (하드코어 — `req` 정책)
PMMO `items`/`req` config 로 "사용/착용/들기" 요구 레벨 지정:
- **Iron's Spells (티어드)**: 주문서/지팡이/학파 장비 → REQ Magic
  - 견습(apprentice) 주문: Magic 5
  - 마법사(mage) 주문: Magic 20
  - 대마법사(archmage) 주문: Magic 40
  - 명인(master) 주문: Magic 60
- **Simply Swords 고유무기**: REQ Combat (티어별 15/30/45), 마법무기는 +Magic
- **방어구 티어**: REQ Defense/Endurance (가죽=0, 철=10, 다이아=25, 네더라이트=40, 보스장비=상위)
- **Apotheosis 등급 장비**: 기본 아이템 REQ는 위 규칙 따름 + affix 강도는 등급 드랍률로 조절(§4)
- **Occultism 상위 의식**: REQ Magic (느슨, 20/40)

> 하이브리드 완화(친구 서버): 채집/이동/기본템은 게이트 느슨, **전투/마법/보스장비만 강한 게이트**. 저사양·신규 친구가 "갈린다" 느낌 최소화.

## 3. 보스 트레드밀 (티어 그래프)

| 티어 | 권장 레벨 | 보스 | 접근 게이트 | 드랍(루트) |
|---|---|---|---|---|
| **T1 입문** | Magic/Combat 10~20 | Mutant Monsters, Mowzie's(Ferrous Wroughtnaut), Cataclysm 초반, Gateways T1 | 없음(야생 조우/소환) | uncommon affix 장비, 견습 주문서, T1 재료 |
| **T2 중반** | 25~40 | Cataclysm(Netherite Monstrosity/Ignis), Mowzie's(Frostmaw/Umvuthi), Gateways T2 | **T1 보스 재료**로 소환템/입장 제작 | rare affix 장비, 마법사 주문서, T2 재료 |
| **T3 레이드** | 45~60 | Cataclysm 상위(The Leviathan/Maledictus/Harbinger), Gateways T3 웨이브 | **T2 장비/재료** 필요 | epic/mythic affix 장비, 대마법사/명인 주문서 |

- **게이트 구현**: KubeJS — 보스 소환 아이템/입장템을 *직전 티어 보스 드랍*으로 제작(crafting). "보스 A 잡아야 보스 B 소환 가능" = 트레드밀 성립.
- **Gateways to Eternity**: 보스 루트로 "게이트웨이" 제작 → 설치 → 파티가 웨이브+보스 처치 → 보상. **"다같이 준비해서 잡는 레이드"** 그 자체.
- **Cataclysm**: 자체 보스별 고유 장비 + 느슨한 진행 보유 → T1~T3 골격으로 활용.

## 4. 루트 정책 (Apotheosis + 던전/보스 주입)

- **Apotheosis**: affix(랜덤 수식어) + 등급(common→mythic) + 보석 소켓. 장비 드랍을 ARPG화.
  - 등급 드랍률을 티어/소스별 조절(config): 일반 몹=낮음, 던전 상자=중간, 보스=높음(상위 등급 가중)
- **던전 상자 loot 주입(KubeJS loot 이벤트 또는 데이터팩)**: WDA/Dungeons&Taverns/YUNG's 상자에 → Iron's 주문서 + Apotheosis affix 장비 + 티어 재료
- **보스 드랍**: Cataclysm/Mowzie's/Gateways 보스 → 상위 등급 affix 장비 + 상위 주문서 + 다음 티어 게이트 재료
- **Lootr**: 던전 상자 = 플레이어별 인스턴스(파티원 각자 자기 루트) → 분배 다툼 0
- **JER**: 몹 드랍/던전 루트/월드젠 정보 인게임 표시 → 플레이어가 루프 파악

## 5. 파티 / 하드코어 요소

- **파티**: Lootr(개별 루트) + PMMO 파티 XP 공유 + co-op 보스(Cataclysm 다인 튜닝)
- **하드코어**: 강한 전투/마법 게이팅 + 느린 성장 곡선(config) + 높은 몹 난이도(Cataclysm/Mowzie's 기본 어려움)
- (검토) **Hardcore Revival** 추가 — 다운된 친구 부활(영구사망 대신) → 파티 하드코어에 적합. *현재 RPG팩 미포함, 추가 검토*

## 6. 구현 구성 (herma-rpg-tweaks = 우리 콘텐츠)

`modpack-rpg/` 에 다음을 저작(packwiz가 인스턴스로 동기화):
- **`kubejs/`** (KubeJS 모드 필요 — 추가 예정): `server_scripts`(loot 주입·보스게이트 레시피·이벤트), `startup_scripts`(아이템 태그)
  - 예: `server_scripts/herma_xp.js`(Iron's 주문 시전→Magic XP), `herma_loot.js`(던전/보스 loot 주입), `herma_boss_gate.js`(보스 소환템 레시피)
- **`config/pmmo/`** (PMMO 스킬/게이팅 JSON): `items_*.json`(req), `xp_values`, 스킬 곡선
- **`config/`** (모드별 밸런스): Apotheosis 등급률, Cataclysm/Mowzie's 난이도, Iron's 마나/주문력
- 전부 **우리 데이터** — 모드 코드 무수정, 라이선스 무관

## 7. 구현 단계 (Phased)

1. **Phase 1 — 스킬 축**: KubeJS 추가 + PMMO 스킬 정의 + Iron's 주문 시전 XP 훅 + 핵심 게이트(주문 티어·무기·방어구) 대표 매핑
2. **Phase 2 — 루트**: Apotheosis 등급률 config + 던전/보스 loot 주입(주문서+affix 장비) + Lootr 확인
3. **Phase 3 — 보스 트레드밀**: 보스 티어 게이트 레시피(KubeJS) + Gateways 게이트웨이 제작 연결
4. **Phase 4 — 밸런스/하드코어**: 성장 곡선·몹 난이도·마나 비용 조정 + (옵션)Hardcore Revival
5. **Phase 5 — 폴리시**: 신규 모드 한국어 번역 보강, 쉐이더 기본 OFF, 퀘스트(Easy NPC/Pumpkillager's) 입문 가이드

각 Phase 후 **런타임 검증**(싱글 + 서버 부팅 + 클라 실행) 필수.

## 8. 검증 계획

- 정적: packwiz 정합 + dep 폐쇄 + fix-sides(0 server) — 각 모드 추가 시
- 런타임(BLOCKING for "완료"):
  - 서버 부팅(Java 21 NeoForge --installServer, "Done" 도달) — 월드젠/both-side 모드 충돌
  - 클라 실행(RPG 채널) — 클라 모드(미니맵/쉐이더/리소스팩) + 실제 게이팅/루트 동작
  - 스킬 게이팅 실증(레벨 부족 시 사용 차단되나) + loot 드랍 확인 + 보스 게이트 동작

## 9. 리스크 / 오픈 질문

- **PMMO 게이팅 양**: 88모드 아이템 매핑은 방대 → Phase별·핵심 아이템 우선(전수 아님, 점진).
- **Iron's↔PMMO XP 연동**: PMMO가 Iron's 캐스팅을 기본 추적하는지 미확인 → KubeJS 훅 필요 여부 런타임 확인.
- **Apotheosis 밸런스**: affix 강도가 보스 난이도와 안 맞으면 파워커브 붕괴 → 등급률·affix 풀 튜닝 반복.
- **하드코어 강도**: 친구 서버라 과한 게이팅은 역효과 → 전투/마법만 강게이트, 채집/이동 느슨(§2.3).
- **오픈 결정**: (a) Eidolon/Forbidden 유지 vs 컷 (b) Hardcore Revival 추가 여부 (c) 성장 속도(느림/보통).

## 10. 진행 상태 (2026-06-22)

### 토대 (완료)
- ARPG/보강 모드 추가(0e78a7f, e3915eb): Apotheosis 생태계+Gateways+Lootr / JEI·JER·Xaero's·최적화·Fresh Animations·쉐이더 / KubeJS+Rhino / Hardcore Revival / GlobalPacks. dep 폐쇄 OK, fix-sides 0-server.

### Phase 1 — 스킬 축 (config 작성 완료, 런타임 검증 대기)
- **검증된 메커니즘**(PMMO/Iron's jar 실측): (a) **Magic XP = `#pmmo:magic` DEAL_DAMAGE → magic 100** → Iron's 공격 주문이 Magic 경험치 자동 부여(KubeJS 훅 불필요). (b) **게이팅 = `requirement_enabled` 마스터 스위치** — easy 프리셋 전부 OFF → 하드코어로 WEAR/WEAPON/USE ON(명시 req 있는 아이템만 게이트 = surgical). (c) PMMO에 **Iron's 방어구 46종 WEAR req 내장**.
- **작성물**(`globalpacks/datapacks/herma-rpg-tweaks/`, GlobalPacks가 전 월드 글로벌 로드):
  - `data/pmmo/config/server.json` — 하드코어 게이팅 ON + Magic XP(마법데미지) + 파티보너스 1.5 + 성장 normal(per_level 1.0) + 사망손실 0(친구서버). 채집/이동(BREAK/PLACE/TOOL/KILL)은 OFF=느슨(결정3).
  - `data/irons_spellbooks/pmmo/items/*.json`(22) — 주문책/지팡이 **Magic 티어 게이트**(T1 wimpy/iron=5, T2 gold/diamond/ice=25, T3 netherite/blood=45, T4 necronomicon/staff_of_the_nines=60). USE+WEAPON req.
- **정적 검증 통과**: PMMO 포맷(jar) + Iron's 아이템 ID(jar) + JSON 유효 + packwiz 인덱싱(24파일).
- **⚠️ 런타임 검증 대기(BLOCKING "동작" 단정 전)**:
  1. GlobalPacks가 datapack을 실제 로드 + PMMO가 server.json/item config 적용하나(데이터팩 reload 로그)
  2. 게이팅 인게임 강제(레벨 부족 시 주문책 사용 차단)
  3. 공격 주문 시전 → Magic XP 누적 확인
  4. 밸런스(티어 레벨이 보스 진행과 맞나)
  → 서버 부팅(Java 21) + 클라 실행으로 확인 필요.

### Phase 2 — 루트(던전→주문서) (config 작성 완료, 런타임 검증 대기)
- **검증된 메커니즘**(Iron's jar 실측): Iron's GLM 타입 `irons_spellbooks:append_loot` + `RandomizeSpellFunction`으로 구조물 chest에 랜덤 주문서 주입. Iron's는 이미 **바닐라 + YUNG's(betterdungeons) + Structory + trial chambers** 커버. **modded 미커버 = When Dungeons Arise / Dungeons&Taverns / Cataclysm**.
- **작성물**(herma-rpg-tweaks datapack):
  - `data/herma_rpg/loot_modifiers/chest_loot/modded_treasure_scrolls.json` — WDA/Cataclysm **treasure 40종** → Iron's `additional_treasure_loot`(상위 주문서)
  - `.../modded_generic_scrolls.json` — WDA/D&T **일반/도서관/대장간 48종** → `additional_generic_loot`(일반 주문서)
  - `data/neoforge/loot_modifiers/global_loot_modifiers.json` — 등록(Iron's 것과 머지, replace:false)
  - Iron's GLM 타입·주문서 loot table **재사용**(새 주문서 테이블 저작 불필요) → "던전/보스 treasure = 랜덤 주문서" 루프
- **정적 검증**: GLM 포맷(Iron's 실측) + loot table ID(modded jar 실측) + JSON 유효.
- **⚠️ 런타임 대기**: GLM 적용(던전 상자에 주문서 뜨나) + 밸런스(드랍률).
- **🟡 Apotheosis affix 커버리지**: 기본값이 chest loot 광범위 처리 — modded 던전 affix 장비 드랍 + 등급률은 **런타임 확인 후 튜닝**(별도). Lootr는 자동(상자 인스턴스화).

### 다음 (Phase 1·2 마무리 + Phase 3)
- Phase 1: Simply Swords 고유무기 Combat 게이트 / Occultism 느슨 게이트
- Phase 2: Apotheosis 등급률 튜닝(런타임 후) / Cataclysm 보스 entity_drops에 주문서·재료
- Phase 3: 보스 트레드밀 게이트(KubeJS 레시피 — 보스 A 재료로 B 소환) + Gateways 연결
