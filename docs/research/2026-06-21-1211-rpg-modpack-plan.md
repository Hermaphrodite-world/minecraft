<!-- 생성: 2026-06-21 — 1.21.1 다운 RPG 모드팩(스킬+NPC퀘스트+마법+던전) Modrinth 실측+적대적audit 후 계획 -->

# 1.21.1 RPG 모드팩 계획 (스킬 + NPC 퀘스트 + 마법 + 던전)

- 작성: 2026-06-21 / 6-카테고리 워크플로 실측 + 적대적 audit (대조군 sodium@fabric/1.21.1 통과, 27 slug 중 조작 0)
- 사용자 확정: 26.1.2 → **1.21.1 다운 OK** (조건: 구리골렘 분류기 대처 가능 — Refined Storage로 충족)
- 원하는 RPG: ★점진적 스킬(시작분기 X) + ★NPC 퀘스트 + 마법 + 던전. 지인 캐주얼.

## 권장: Fabric 1.21.1 (현 런처 로더 유지)

Fabric 1.21.1이 사용자 요구를 **거의 다 커버**하고 런처 로더(Fabric)를 유지함. NeoForge로 가면 마법/보스 명작이 더 붙지만 라이트스킬(Levelz)·런처가 깨짐.

### 확정 모드 목록 (전부 1.21.1 Fabric 실측)

**스킬 (점진형 — Origins식 분기 아님)**
- `levelz` (11 release) — 행동→레벨→스탯. 가볍고 캐주얼 1순위 ★
- `justleveling-fork` (5 release) — 능력치 레벨 + 장비 게이팅 ★
- (선택, 비주얼 스킬트리) `skills`(Pufferfish's, **beta 엔진**) + `attributes`(release) + `arcwise-puffish-skill-tree`(5 release, 1.21.1 정식타겟 — audit: "구버전 위주" 경고는 거짓)

**마법 = Spell Engine 생태계 (클래스 선택→스펠 점진 해금 → 마법이자 "스킬 선택"의 두 번째 축) ★**
- 토대: `spell-engine`(73 release) + `spell-power`(25 release) [+deps: accessories/trinkets/playeranimator/cloth-config/fabric-api]
- 클래스(원하는 만큼): `wizards`(38) · `paladins-and-priests`(38) · `witcher-rpg-class`(37) · `elemental-wizards-rpg`(34) · `forcemaster-rpg-class`(34) · `archers-expansion`(27) · `berserker-rpg-class`(26)
- 독립형 대형 마법(엔진무관): `spectrum`(12 release)

**NPC 퀘스트**
- `easy-npc` (64 release) — NPC + 대화트리 + 행동(아이템/명령) → 퀘스트 NPC 제작 ★
- `daily-quests` (15 release) — 일일 자동 퀘스트(데일리 루프)
- `pumpkillagers-quest` (5 release) — 스토리 NPC 1체(이벤트성)

**던전/구조물/월드젠**
- 던전: `when-dungeons-arise`(3) + `yungs-better-dungeons`(5, +`yungs-api`) + `dungeons-and-taverns`(6)
- 탐험 구조물: `structory`(12) + `structory-towers`(10) + `moogs-voyager-structures`(18) + `towns-and-towers`(3, +`cristel-lib`) + `repurposed-structures-fabric`(16) + `philips-ruins`(6)
- 월드젠: `terralith`(6) ± `tectonic`(24) [+`lithostitched`]

**전투/몹/보스**
- `better-combat`(20 release) ★ + `combat-roll`(7 release) + `simply-swords`(8) + `combatify`(13, 선택)
- 몹/보스: `friends-and-foes`(36) + `mutant-monsters`(2) + `bosses-of-mass-destruction`(1 **beta** — 도입 전 실측 필수)

**창고(구리골렘 대체) + QoL/성능**
- `refined-storage`(27, 10 release) — 검색 터미널 + 자동 입출력 = 구리골렘 분류 대체 ★ (audit: 1:1 아님·캐주얼엔 다소 오버킬 — 더 가볍게는 `toms-storage` 17 release)
- 보조: `storagedrawers`(34) ± `sophisticated-storage-(unofficial-fabric-port)`(13, 포트라 1.21.1이 지원 상한)
- 성능: `sodium` + `lithium` + `ferrite-core` (+`iris` 셰이더 원할 때)
- QoL: `jade` + `appleskin` + `inventory-profiles-next` + `modmenu` + `waystones`(+`balm`) + `universal-graves`(서버사이드)
- 라이브러리: `fabric-api`

### Fabric의 갭 (정직)
1. **보스/던전 끝판 `L_Ender's Cataclysm` = NeoForge 전용** (Fabric 0/Neo 43). Fabric 보스는 Bosses of Mass Destruction(beta 1) + Mutant Monsters로만 메움 — 볼륨 부족.
2. **깊은 마법 명작(Ars Nouveau/Iron's Spells/Occultism/Eidolon/Mahou Tsukai) = NeoForge 전용**. Fabric 마법은 Spell Engine 생태계로 대체(액션 클래스 마법 — 성격 다름). Botania는 1.21.1 자체 없음.
3. **PMMO(행동 레벨링 정석) = NeoForge 전용**. Fabric은 Levelz가 근접하나 더 얕음.
4. **풀 퀘스트북(FTB Quests/Heracles) = CurseForge 전용**(Modrinth 부재). Easy NPC+Daily Quests로 Modrinth-only 닫되, 단계형 스토리 퀘스트라인은 약함.

### NeoForge 1.21.1로 갈 경우 (대안)
추가 획득: Ars Nouveau·Iron's Spells·Occultism(깊은 마법), Epic Fight(소울라이크 전투), **L_Ender's Cataclysm**(던전+보스), PMMO, Sophisticated Storage 공식, AE2.
비용: **Levelz(라이트스킬)·Origins·universal-graves(fabric)·modmenu 등 Fabric전용 상실** + **런처 NeoForge 설치 지원 추가**(CmlLib 지원하나 현 계획서 제외) + 더 무거움. "약간 스킬 + 캐주얼" 의도와 거리.

## 구현에 필요한 나머지 (모드팩 외)

1. **런처 멀티버전/로더** — 현재 RPG는 베타 채널(같은 26.1.2)만 전환. RPG는 **1.21.1**(+Fabric loader 1.21.1 호환 버전)이라, 채널/모드별 **MC버전·로더를 바꾸게** 런처 확장 필요. (`MinecraftVersion`/`FabricLoaderVersion` 상수를 채널별로)
2. **1.21.1 RPG 서버** 별도 기동 (헤드리스 부팅 스모크 — 26.1.2처럼).
3. **빌드 이식** — 26.1.2 → 1.21.1 스키매틱(Litematica/WorldEdit). **26.x 신블록(구리골렘/구리상자/구리 일족)은 1.21.1에 없어 손실** — 단 분류 기능은 Refined Storage로 재구성. ≤1.21.1 블록 빌드만 이식.

## 결정 필요
- **로더**: Fabric(권장 — 스킬+퀘스트+마법(Spell Engine)+던전+창고 충족, 런처 유지) vs NeoForge(깊은 마법·Epic Fight·L_Ender's Cataclysm·PMMO 획득, 라이트스킬/런처 비용)
- 그다음: 모드팩 구성(packwiz) + 런처 멀티버전 + 서버 + 이식 — 단계별 진행
