# 모드 가이드 — Hermaphrodite World

> **Minecraft 26.1.2 / Fabric** · 80 모드 + 4 쉐이더팩 + 6 리소스팩.
> 대부분은 **자동으로 동작**하니 신경 쓸 필요 없고, 아래 ⭐ 표시된 것들만 **키/명령어로 직접 사용**합니다.
> 모든 키는 **옵션 → 조작(Controls)** 에서 확인·변경할 수 있어요. 설명은 Modrinth 공식 기준.

---

## ⭐ 직접 쓰는 모드 (키/명령어)

| 모드 | 무엇 | 어떻게 |
|------|------|--------|
| **Simple Voice Chat** | 근접 음성채팅 | 기본 **V**(누르고 말하기). 좌하단 아이콘/단축키로 그룹·설정. 서버에서 동작(설치됨) |
| **Xaero's Minimap** | 화면 구석 미니맵 + 웨이포인트 | 웨이포인트 추가/미니맵 토글 단축키(조작 메뉴 참조) |
| **Xaero's World Map** | 전체 지도(탐험한 곳) | 기본 **M** |
| **Zoomify** | 줌(망원경처럼 확대) | 기본 **C**(누르고 있기) |
| **Inventory Profiles Next (IPN)** | 인벤토리 정렬·정리 | 인벤토리 **좌상단 버튼**(정렬/설정). 버튼이 안 보이면 → [복구법](#ipn-버튼이-사라졌을-때) |
| **Reliable Recipe Viewer (RRV)** | 제작법 뷰어(JEI/REI류) | 인벤토리 옆 아이템 목록 → 클릭하면 제작법 |
| **Mouse Tweaks** | 인벤토리 마우스 보조 | 우클릭 드래그 분배, 휠 이동 등 자동 |
| **RightClickHarvest** | 작물 우클릭 수확 | 다 자란 작물 **우클릭** → 수확+재심기 |
| **Recall Coords** | 좌표 저장/소환(사망 위치 포함) | 단축키/명령어로 좌표 저장·이동 |
| **Simple Auto Fishing** 🆕 | 낚시 자동화 | **기본 켜짐** · `/saf toggle` 끄기 · `/saf set <틱>` 딜레이(기본 17) · 낚싯대 들고 **웅크림+공격**으로 모드 전환(내구도 보호 등 4종) |
| **Nature's Compass** | 바이옴 위치 찾기 | 제작 후 우클릭 → 바이옴 검색 |
| **Explorer's Compass** | 구조물 위치 찾기 | 제작 후 우클릭 → 구조물 검색 |
| **Ultimate Map Atlases** | 지도책(지도 모음) | 지도책 제작 → 나침반과 함께 탐험 자동 기록 |
| **OffersHUD** | 주민 거래 목록 HUD | 주민을 **바라보면** 거래 목록 표시 |
| **Visible Traders** | 잠긴 주민 거래 미리보기 | 아직 안 연 거래도 미리 확인 |
| **Screencopy** | 스크린샷 클립보드 복사 | 단축키로 스샷을 바로 복사(디스코드 붙여넣기 편함) |
| **Controlling** | 키 설정 검색 | 조작 메뉴 상단 검색바 |
| **BetterF3** | 깔끔한 F3 디버그 화면 | **F3** |
| **Capes** | 외부 망토 표시 | OptiFine/LabyMod 망토 자동 적용 |
| **Mod Menu** | 모드 목록·설정 | 메인메뉴/ESC → **Mods** |

---

## 🛠 편의 (자동, 설정 선택)

- **AppleSkin** — 음식의 포화도/허기 회복량을 툴팁·HUD로 표시.
- **Enchantment Descriptions** — 인챈트북에 효과 설명 추가.
- **Clumps** — 경험치 오브를 묶어 렉 감소.
- **Not Enough Animations** — 3인칭에서도 1인칭 동작(먹기/활/블록 등) 보임.
- **Auto Reconnect Reforged** — 서버 끊기면 자동 재접속.

## 🎨 비주얼·분위기 (자동)

- **Fresh Animations** (+Extensions) — 몹 애니메이션(트레일러처럼). 리소스팩, **기본 적용됨**. (EMF/ETF 필요 — 설치됨)
- **[EMF] Entity Model Features / [ETF] Entity Texture Features** — Fresh Animations용 커스텀 엔티티 모델·텍스처 지원.
- **Iris Shaders** — 쉐이더 로더. 비디오 설정 > 쉐이더팩에서 켜기(아래 쉐이더 섹션).
- **Better Clouds** — 바닐라풍 예쁜 구름.
- **Falling Leaves** — 나뭇잎 떨어지는 파티클.
- **LambDynamicLights** — 손에 든/바닥의 발광체(횃불 등)가 주변을 밝힘.
- **LambdaBetterGrass** 🆕 — 잔디/눈/모래 옆면이 윗면과 자연스럽게 이어짐.
- **Visuality** 🆕 — 아이템 줍기·모루·잠자기 등 자잘한 디테일 파티클.
- **Make Bubbles Pop** — 물거품이 수면으로 올라가 사실적으로 터짐.
- **3D Skin Layers** — 스킨 겉 레이어를 입체로 렌더.
- **Chat Heads** — 채팅에 말한 사람 머리 아이콘 표시.
- **AmbientSounds** — 바람·물·숲 등 환경 소리.
- **Presence Footsteps** — 블록 종류별 발소리.

## 🔊 음성·사운드

- **Simple Voice Chat** — (위 ⭐ 참조) 근접 음성채팅.
- **Sound Physics Remastered** — 공간감 있는 소리(반향, 벽 너머 차단/흡수).

## ⛏ 게임플레이 (바닐라+)

- **Animal Feeding Trough** — 동물에게 자동으로 먹이를 주는 블록(번식 편의).
- **Starter Kit** — 새 플레이어 첫 접속 시 시작 아이템 지급(서버 설정).

---

## 🖼 쉐이더팩 — 비디오 설정 > 쉐이더팩 (동시 1개만)

> 게임 내 **옵션 → 비디오 설정 → 쉐이더팩**에서 선택. 무거우면 끄거나 가벼운 걸로.

- **Complementary Reimagined** — **기본 적용**. 바닐라 충실 + 고품질·고성능. 무난한 기본값.
- **Complementary Unbound** 🆕 — Reimagined의 시네마틱 분기(강한 라이팅/볼류메트릭).
- **BSL Shaders** 🆕 — 밝고 화사한 올라운드 입문용.
- **Sildur's Vibrant** 🆕 — Lite~Extreme 변형. **저사양이면 Lite**로(2012년부터 모든 사양 지원).

## 🧱 리소스팩 — 옵션 > 리소스팩 (기본 모두 적용됨)

- **Fresh Animations** (+Extensions) — 몹 애니메이션.
- **Better Leaves** 🆕 — 나뭇잎을 풍성하게.
- **Default Dark Mode** 🆕 — 어두운 GUI(야간 눈 편함). 바닐라 밝은 GUI가 좋으면 끄세요.
- **Nautilus 3D** 🆕 — 레일·사다리·철창 등 일부 블록 3D 모델.
- **Vanilla Connected Glass** 🆕 — 유리 블록 테두리 제거(깔끔).

---

## 🌐 서버 모드 (관리자용 — 친구는 신경 안 써도 됨)

- **LuckPerms** — 권한 관리.
- **Open Parties and Claims** — 청크 보호 + 파티(Xaero 지도 연동).
- **Blastproof** — 폭발이 블록을 부수지 않음(크리퍼·TNT 보호).
- **WarpUtils** — `/home`·`/warp` 등 순간이동.
- **Ledger** — 서버 행동 로그(누가 무엇을 했는지 추적).
- **Fast Backups** — Git 기반 증분 월드 백업.
- **Chunky** — 청크 사전 생성(탐험 렉 감소).
- **squaremap** — 웹에서 보는 서버 지도.
- **Styled Chat** — 서버 채팅 스타일링.

## ⚙ 성능·라이브러리 (백그라운드 — 조작 불필요)

- **성능 최적화**: Sodium, Lithium, FerriteCore, Krypton, ImmediatelyFast, ScalableLux, Entity Culling, Dynamic FPS, ServerCore, spark, Bobby, Sodium Extra, Reese's Sodium Options
- **라이브러리**(다른 모드가 사용): Fabric API, Fabric Language Kotlin, Cloth Config, CreativeCore, Forge Config API Port, YACL, libIPN, Collective, JamLib, Searchables, Text Placeholder API, Polymer, Prickle

---

## 부록

### IPN 버튼이 사라졌을 때
인벤토리 좌상단 정렬/설정 버튼이 안 보이면:
1. **Mods**(Mod Menu) → **Inventory Profiles Next** → 설정(톱니) → 버튼 표시 다시 ON, 또는
2. 조작 메뉴에서 IPN 설정 키(기본 **R+C**)로 설정 열기, 또는
3. `config/inventoryprofilesnext/` 폴더 삭제 후 재실행(설정 초기화).

### 모드 추가/제거 (메인테이너)
```bash
cd modpack
packwiz modrinth add <slug> -y && packwiz refresh   # 추가
packwiz remove <slug>                                # 제거 (build-pack.sh 목록에서도 제거)
git add . && git commit -m "modpack: ..." && git push   # → Pages 재배포
```
push 후 친구들이 **런처 재실행(동기화)** 하면 자동 반영됩니다. 서버 모드는 서버에서 `sync-mods.sh` 재실행 필요.

> 전체 추천 후보(미설치 포함)는 [research/2026-06-09-mod-recommendations.md](research/2026-06-09-mod-recommendations.md) 참조.
