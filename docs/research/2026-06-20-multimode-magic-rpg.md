<!-- 생성: 2026-06-20 야생/마법/RPG 멀티모드 아키텍처 + 월드 마이그레이션 + 마법·RPG 모드 Modrinth 라이브 실측 -->

# 야생/마법/RPG 멀티모드 + 마이그레이션 + 마법·RPG 모드 리서치

- **작성일:** 2026-06-20 (KST)
- **현재 스택:** Fabric / MC 26.1.2 / Java 25 / packwiz / 커스텀 런처(HermaLauncher) / 단일 서버 / online-mode=false
- **검증 방식:** Modrinth REST API 라이브 조회(부모 컨텍스트 직접 실행, 대조군 sodium@fabric/26.1.2=9로 쿼리 정상 확인). 멀티에이전트 워크플로 아키텍처/마이그레이션 스트림 + 부모 직접 모드 실측.

---

## 0. 결론 3줄

1. **여러 모드(야생/마법/RPG)는 "서버 인스턴스 여러 개"로만 분리 가능**(단일 Fabric 서버는 모드셋이 JVM 전역이라 월드별 모드 분리 불가). 단, **Velocity 프록시는 쓰지 말고 "런처 모드 선택기"(아키텍처 B)**로 — 마법/RPG가 구버전(1.21.1)을 강제하는 순간 프록시는 기술적으로 불가능.
2. **마법/RPG 유명 모드는 26.1.2에 사실상 없음**(전부 1.21.1에서 멈춤). → 야생=26.1.2 Fabric 유지, **마법/RPG=1.21.1 (NeoForge 권장)** 별도 팩+서버.
3. **단 하나의 핵심 결정 = "런처에 NeoForge 지원을 추가할 것인가."** 추가하면 Ars Nouveau·Iron's Spells·Epic Fight·L_Ender's Cataclysm 등 풍부한 컨텐츠 해금. Fabric만 고수하면 Spell Engine 생태계 위주의 더 작은 셋.

---

## 1. 멀티모드 아키텍처 (질문 1)

### 단일 서버로는 불가
Fabric 모드는 서버 프로세스 시작 시 JVM에 **전역 로드**된다. 한 서버 = 한 모드셋. multiworld 모드(Multiworld/IsaiahMC, 최신 1.21.11·26.1.2 빌드 없음)도 "같은 모드셋, 다른 맵"만 가능 → **모드셋이 다른 3월드 요구엔 부족**. ⇒ 서버 인스턴스를 나누는 건 필수.

### 프록시(A) vs 런처 선택기(B)

| | A) Velocity 프록시 | B) 런처 모드 선택기 ★권장 |
|---|---|---|
| 전환 | 인게임 `/server` | 런처에서 택1 후 실행 |
| 모드셋 | **클라가 3모드 합집합 상시 보유** | **선택 모드만 다운로드** |
| MC버전 혼합 | **불가**(전 백엔드 동일 버전 필수) | **자유**(야생26.1.2 / 마법1.21.1) |
| 보안 | online-mode=false+forwarding 위험 | 프록시 없음, 화이트리스트 그대로 |
| 기존 런처 | 별도 프록시 구축 | **packwiz+단일서버 구조의 자연 확장** |

**B가 탈락 불가인 이유:** 마법/RPG가 1.21.1을 강제 → 야생(26.1.2)과 MC 버전이 다름 → **Velocity/BungeeCord는 서로 다른 프로토콜(MC버전) 백엔드를 못 묶음**. ViaVersion도 모드 커스텀 패킷/레지스트리는 번역 못 해 cross-version 모드 환경엔 실용 불가. ⇒ **A는 기술적으로 불가능, B만 남음.**

### B 구현 형태
1. **런처 UI에 모드 선택기**(야생/마법/RPG 카드 3개) — 선택값에 따라 `pack.toml` URL + `ServerIp:Port` + (MC버전/로더) 스위치.
2. **modpack 분리** — `pack-wild`/`pack-magic`/`pack-rpg` (별도 레포 또는 브랜치/디렉토리, GitHub Pages 경로 3개).
3. **서버 인스턴스 3개** — 각자 버전/로더/모드셋. **온디맨드 기동** 권장(상시 3대 = 10~18GB 낭비; 지인 10명은 1~2대로 충분, lazymc로 자동 wake/sleep 또는 수동 start).
4. **런처 멀티버전 launch 필요**(마법/RPG=1.21.1) + NeoForge 채택 시 **CmlLib NeoForge installer 연동 추가**(현 계획서는 NeoForge 제외 — 별도 결정 항목).

> ⚠️ scope 주의: 자동접속 주소 해석을 **모든 consumer(quickPlay 인자/servers.dat 등록/상태 pill)가 단일 SoT**로 받게 해야 함(프로필 전환 시 한 consumer만 다른 주소면 "직접입력은 되는데 서버목록 클릭 안 됨" silent divergence 재발 — 메모리의 LAN NAT hairpin 교훈).

---

## 2. 야생월드 마이그레이션 (질문 2) — 가능

| 도구 | 26.1.2 지원 | 용도 |
|---|---|---|
| **MCA Selector** v2.8 | ✅ (v2.7=26.1, v2.8=26.2 매핑) | 주거지 청크만 KEEP, 나머지 삭제→**현 시드/월드젠으로 재생성** |
| **Litematica** (이미 팩 포함) | ✅ client | 빌드 `.litematic` 저장→새 월드 재배치 |
| **WorldEdit (Fabric)** 7.4.3 | ✅ (Modrinth, MC 26.1–26.1.2) | `//copy`·`//schem`·`//paste` 대량 빌드 재배치 |
| FAWE(FastAsyncWorldEdit) | ⚠️ Fabric+26.1.2 미확인 | Bukkit 중심 — Fabric엔 정식 WorldEdit로 충분 |

**시나리오:**
- **A. 시드/월드젠 유지, 야생만 리셋** → MCA Selector로 주거지+외곽 buffer 청크 KEEP, 나머지 삭제. (작업 최소, seam 주의)
- **B. 새 시드+새 월드젠(Terralith 등)** → Litematica/WorldEdit로 핵심 빌드 떠서 신규 월드 재배치. (가장 깔끔)
- **C. 시너지** — 기존 월드에 Terralith 도입 시: 월드젠 모드는 기존 청크엔 적용 안 됨 → MCA Selector로 미탐사 청크 삭제→재생성하면 **트리밍과 새 월드젠 도입이 한 번에**.

**주의:** ① 실행 전 **월드 폴더 전체 백업 + region 1개 dry-run**(26.x 새 포맷 보험) ② 마법/RPG 모드는 어차피 **신규 월드가 정답**(월드젠/구조물 깨끗이 생성) → 마이그레이션은 **야생 모드 1개에만 해당** ③ **26.1.2 월드를 1.21.1로 다운그레이드 불가**.

---

## 3. 마법·RPG 모드 실측 (질문 3)

> 숫자 = Modrinth 빌드 개수 (2026-06-20 라이브). F=Fabric, N=NeoForge. **거의 전부 `both`(클라+서버 양쪽 필요)** — 런처가 클라 팩 동기화하므로 OK.

### 3-1. 마법 모드

| 모드 | slug | 26.1.2 F/N | 1.21.1 F/N | 역할 |
|---|---|---|---|---|
| **Ars Nouveau** | `ars-nouveau` | 0/0 | **0/26** | 주문 제작·마법 자동화 (마법계 대표) |
| **Iron's Spells 'n Spellbooks** | `irons-spells-n-spellbooks` | 0/0 | **0/22** | 액션 주문 전투 |
| **Occultism** | `occultism` | **0/47** ✅ | 0/124 | 소환·의식·차원 마법 (**26.1.2 NeoForge 있음!**) |
| **Forbidden & Arcanus** | `forbidden-arcanus` | **0/7** ✅ | 0/16 | 마법 블록·모험 (26.1.2 NeoForge 있음) |
| Mahou Tsukai | `mahou-tsukai` | 0/0 | 0/8 | 화려한 애니풍 주문 |
| Eidolon: Repraised | `eidolonrepraised` | 0/0 | 0/5 | 오컬트·강령술 |
| Malum | `malum` | 0/0 | 0/6 | 영혼/비전 마법 |
| Hexerei | `hexerei` | 0/0 | 0/4 | 마녀/약초학 |
| Psi | `psi` | 0/0 | 0/4 | 주문 "프로그래밍" |
| Ars Elemancy | `ars-elemancy` | 0/0 | 0/1 | Ars Nouveau 애드온 |
| **Spell Engine** | `spell-engine` | 0/0 | **73/29** | 액션 RPG 주문 (**Fabric 마법 대표**) |
| Spell Power | `spell-power` | 0/0 | 25/7 | Spell Engine 스케일링 동반 |
| **Spectrum** | `spectrum` | 0/0 | **12/11** | 대형 진행형 마법(잉크/색채) |
| Easy Magic | `easy-magic` | 1/1 | 4/4 | 마법부여대 QoL (컨텐츠 아님) |
| Botania | `botania` | 0/0 | 0/0 (1.20.1 max) | 꽃 기반 마법 — **너무 구버전** |

**판정:**
- **NeoForge 1.21.1 = 가장 풍부**: Ars Nouveau + Iron's Spells + Occultism + Mahou Tsukai + Eidolon + Forbidden&Arcanus + Psi.
- **Fabric 1.21.1 = 더 작지만 응집**: Spell Engine + Spell Power + Spectrum (+ Paladins&Priests). "깊은 마법 시스템"보다 "액션 주문 전투" 색.
- **26.1.2에서도 NeoForge면 소규모 마법 가능**: Occultism(47) + Forbidden&Arcanus(7) + Easy Magic. 단 Ars Nouveau/Iron's Spells는 없음.

### 3-2. RPG 모드 (축별)

| 축 | 모드 | slug | 26.1.2 F/N | 1.21.1 F/N |
|---|---|---|---|---|
| 클래스/종족 | **Origins** | `origins` | 0/0 | **9/0** (Fabric) |
| 레벨/스탯 | **Levelz** | `levelz` | 0/0 | **11/0** (Fabric) |
| 스킬트리 | More RPG Classes-Skill Tree | `more-rpg-classes-skill-tree` | 0/0 | 12/8 |
| 스킬트리 | Arcwise Puffish Skill Tree | `arcwise-puffish-skill-tree` | 0/0 | 5/4 |
| MMO 진행 | **Project MMO** | `project-mmo` | 0/0 | **0/31** (NeoForge) |
| 클래스 주문 | Paladins & Priests | `paladins-and-priests` | 0/0 | 38/6 |
| 전투 개편 | **Better Combat** | `better-combat` | 0/0 | 20/18 |
| 전투 | Combat Roll(회피) | `combat-roll` | 0/0 | 7/7 |
| 전투 | Combatify | `combatify` | 1/0 ✅ | 13/5 |
| 전투(애니) | **Epic Fight** | `epic-fight` | 0/0 | **0/33** (NeoForge) |
| 장비/광물 | Mythic Metals | `mythicmetals` | 0/0 | 17/0 (Fabric) |
| 전리품 | **Artifacts** | `artifacts` | 1/1 ✅ | 18/17 |
| 장신구 슬롯 | Trinkets | `trinkets` | 0/0 | 1/0 |
| 무기 | Simply Swords | `simply-swords` | 0/0 | 8/8 |
| 보스/던전 | **L_Ender's Cataclysm** | `l_enders-cataclysm` | 0/0 | **0/43** (NeoForge) |
| 보스 | Bosses of Mass Destruction | `bosses-of-mass-destruction` | 0/0 | 1/0 (Fabric) |
| 미니보스 | Mowzie's Mobs | `mowzies-mobs` | 0/0 | 0/6 |
| 몹 | Mutant Monsters | `mutant-monsters` | 2/2 ✅ | 2/2 |
| 몹 | Born in Chaos | `borninchaos` | 0/0 | 0/3 |
| 던전 | When Dungeons Arise | `when-dungeons-arise` | 0/0 | 3/4 |
| 던전 | Dungeons and Taverns | `dungeons-and-taverns` | 1/1 ✅ | 6/6 |
| 이동 | Waystones | `waystones` | 4/4 ✅ | 31/30 |

**퀘스트북(FTB Quests/Heracles)은 Modrinth Fabric에 깔끔히 없음** — 주로 CurseForge. 퀘스트 진행이 필수면 CurseForge 또는 KubeJS/데이터팩 경로 별도 검토.

**판정:**
- **NeoForge 1.21.1 = 깊은 RPG**: Epic Fight(전투 애니) + Project MMO(스킬 진행) + L_Ender's Cataclysm(보스+던전) + Better Combat + Artifacts + Waystones.
- **Fabric 1.21.1 = 가볍고 모듈식**: Origins(종족) + Levelz(레벨) + Spell Engine(클래스 주문) + Better Combat + 스킬트리 + Mythic Metals + Artifacts.
- **26.1.2 RPG도 소규모 가능**: Waystones + Artifacts + Mutant Monsters + Dungeons and Taverns + Combatify (Fabric/NeoForge 둘 다). 단 Origins/Epic Fight/Cataclysm은 없음.

---

## 4. 권장 구성

### 핵심 결정: NeoForge를 런처에 추가? (Codex 교차검증 권고 — 모드 선정 종속)

| 선택 | 마법 모드 | RPG 모드 | 런처 작업 |
|---|---|---|---|
| **NeoForge 추가** ★풍부 | 1.21.1 NeoForge: Ars Nouveau+Iron's Spells+Occultism+Mahou Tsukai+Forbidden&Arcanus | 1.21.1 NeoForge: Epic Fight+Project MMO+L_Ender's Cataclysm+Better Combat+Waystones | CmlLib NeoForge installer 연동 + 멀티버전 |
| **Fabric 고수** | 1.21.1 Fabric: Spell Engine+Spell Power+Spectrum+Paladins&Priests | 1.21.1 Fabric: Origins+Levelz+Spell Engine+Better Combat+스킬트리+Mythic Metals | 멀티버전만(로더 단일) |
| **최신 고수(26.1.2)** | 빈약: Occultism(NeoForge)+Forbidden&Arcanus만 | 빈약: Waystones+Artifacts+Mutant Monsters+Dungeons&Taverns | 단일버전(NeoForge면 로더 추가) |

**가장 효율적 조합:** 야생=26.1.2 Fabric(현행 유지) + **마법·RPG = 1.21.1 NeoForge** 2팩/2서버. 이러면 런처가 추가할 건 **딱 (a) 1.21.1 버전 (b) NeoForge 로더** 둘뿐이고, 마법·RPG 양쪽 모두 최고 생태계 확보. (마법·RPG가 같은 1.21.1 NeoForge 베이스를 공유하므로 운영도 단순.)

### 버전 앵커 주의
1.21.1이 "현재 컨텐츠 최다 앵커"지만, 일부 전투/스킬트리 모드는 이미 1.21.10/1.21.11로 이동. 그러나 **대형 앵커(Ars Nouveau·Iron's Spells·Epic Fight·L_Ender's Cataclysm·Origins)가 1.21.1에서 멈춤** → 컨텐츠 팩은 **1.21.1 고정**이 정답. 26.x 포팅 여부는 도입 시점 재확인.

---

## 5. Sources (라이브 검증)
- Modrinth API: `GET /v2/project/{slug}/version?loaders=[...]&game_versions=[...]` + `/v2/project/{slug}` + `/v2/search` (대조군 sodium@fabric/26.1.2=9로 쿼리 정상 확인)
- [MCA Selector Releases (v2.7=26.1, v2.8=26.2)](https://github.com/Querz/mcaselector/releases)
- [WorldEdit 7.4.3 — Modrinth (Fabric, MC 26.1–26.1.2)](https://modrinth.com/plugin/worldedit/version/7.4.3)
- [Velocity 서버 호환성](https://docs.papermc.io/velocity/server-compatibility/) · [Multiworld(IsaiahMC)](https://modrinth.com/mod/multiworld)
- 미검증/시점의존: 26.1.2용 마법·RPG 신규 포팅 출현(현재 0), lazymc 정확 빌드 — 도입 시점 재확인.

---

## 6. 최종 결정 & 적용 결과 (2026-06-21)

**결정:** 단일 월드 통합 / **Fabric 26.1.2 유지**(구리 골렘 등 26.x 빌드·기존 인프라 보존) / RPG·던전 위주 + 편의성 최대 / 마법은 라이트. NeoForge 미채택(Blastproof·Ledger·Litematica 등 Fabric 인프라가 NeoForge 26.1.2에 부재 + 이점이 거의 마법뿐). 구리 골렘 자동분류기는 **Refined Storage(디지털 저장망)**로 대체(오히려 강화).

> 핵심 함정 재확인: 구리 골렘은 1.21.9+ 기능, 큰 RPG 모드는 1.21.1 상한 → 공존 버전 없음. 구리 골렘·빌드 보존 우선 → 26.1.2 잔류가 정답.

**적용된 신규 모드 (packwiz, 20개 + 의존성 9개, 전부 MC 26.1.2 Fabric 라이브 검증):**

| 그룹 | 모드 | side |
|---|---|---|
| 창고(★) | Refined Storage | both |
| 편의 | Waystones, Traveler's Backpack, Lootr(플레이어별 전리품) | both |
| 던전/구조물 | Dungeons and Taverns, Moog's Voyager Structures, Structory(+Towers), Towns and Towers, Explorify | **server** |
| 월드젠 | Terralith, Incendium, Nullscape | **server** |
| 몹/보스/NPC | Friends&Foes, Illager Invasion, Mutant Monsters, MCA Reborn, EDF Remastered | 몹3=both / EDF=server |
| 전투 편의 | Combatify(both), Cut Through(client) | both/client |
| 의존성(auto) | fabric-api(기존), cristel-lib(server), balm, puzzles-lib, resourceful-lib, atlas-core, shogi, defaulted(server), lithostitched(server), moogs-structure-lib(server) | 혼합 |

**side 감사:** packwiz 가 월드젠/구조물 11개를 `both`로 오감지 → `client_side=optional/server_side=required`(서버권위) 기준 **`server`로 교정**(클라 미다운=가벼움 + lithostitched[client 미지원] 크래시 방지). 신규 중 클라 다운로드는 15개(both 14 + client 1)뿐, 서버전용 14개.

**검증 범위:** Modrinth 가용성/의존성/side 정합 + `packwiz refresh` 인덱스 정합까지. **런타임 미검증**(실제 서버 기동·모드 로드·월드 생성 스모크 미실행) — 배포 전 실기동 1회 필요.

**남은 수동 단계:** (1) 서버 박스 `-s server` 동기화 (2) **기존 월드에 MCA Selector로 미탐사 청크 트리밍**(안 하면 신규 바이옴/구조물/던전이 기존 청크에 안 생성) (3) Refined Storage·Lootr 등 서버 config 점검 (4) 실기동 스모크.

### 6.1 편의성 확장 배치 (2026-06-21, "괜찮은 건 다")

26.1.2 Fabric 카테고리 전수 스윕(adventure/mobs/food/equipment/game-mechanics/storage/transportation/decoration/utility) 후, 기존과 중복·충돌·리소스팩의존·밸런스합의필요(serene-seasons/frostiful/spice-of-fabric 등)를 제외하고 **품질 vanilla+/QoL/RPG 57개 추가** (108→171, +의존성 6).

- **음식/농사**: Farmer's Delight Refabricated(+More Delight, Cooking for Blockheads), Ecologics, Wilder Wild, Fish of Thieves, Universal Bone Meal, Trample No More, Stellarity
- **장비/채광/마법부여**: VeinMiner(+hotkey), Easy Anvils, Enchanting Infuser, Grind Enchantments, Advanced Netherite, Tool Stats, Held Item Info, Building Wands, Armor Statues, Spyglass Improvements, Max Health Fix, First-person Model, Boat Item View
- **게임 편의**: NetherPortalFix, Hardcore Revival(다운 부활), Double Doors, Sparse Structures(구조물 과밀 방지), KleeSlabs, Sit, Client Tweaks, Chat Patches, Emotecraft, Villager Names, Overflowing Bars, InvMove
- **보관/인벤**: Shulker Box Tooltip, Easy Shulker Boxes, Universal Graves(무덤), Stack to Nearby Chests, XP Tome, InventoryHUD+, Simple Copper Pipes
- **컨텐츠/몹**: Promenade, MES Moog's End Structures, Respawnable Pets, Shoulder Surfing Reloaded(RPG 시점), No Chat Reports, More Culling
- **장식**: Macaw's(Furniture/Doors/Bridges/Roofs/Fences&Walls/Windows/Trapdoors/Paintings), Diagonal Fences

**side 자동 교정**: `fix-sides.py`(Modrinth env 기준, both 오감지만 server/client 로 좁힘, 명시값 미수정·멱등) 신설 + build-pack.sh 연결. 최종 전체 171개 = both 82 / server 37 / client 52(클라 미다운로드 37개 → 가벼운 클라).

**⚠️ 런타임 미검증 강화**: 171개 규모는 모드 간 런타임 충돌 가능성이 실재 — packwiz 정합/side 까지만 검증됨. **배포 전 실서버+클라 기동 스모크 필수**(이 환경에선 MC 미기동).
