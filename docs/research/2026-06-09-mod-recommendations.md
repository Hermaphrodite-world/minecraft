<!-- 생성: 2026-06-09 MC 26.1.2 Fabric 친구서버 추가 모드/팩 리서치 (6 카테고리 병렬 + Modrinth 검증, 후보 59) -->

# MC 26.1.2 Fabric 친구서버 추가 모드/팩 최종 추천 리포트

## 3줄 요약
- 현재 팩은 **QoL·최적화·핵심 비주얼이 이미 포화** 상태이며, 진짜로 비어 있는 영역은 **쉐이더 다양성**, **블록/UI 리소스팩**, **멀티플레이 소셜·편의(TPA·무덤·디스코드)**, **실제 컨텐츠(구조물·월드젠)** 네 곳이다.
- 이 리포트는 리서치 JSON 중 **Modrinth 26.1.2 빌드가 실측 확인된(verified-yes) 항목만** 강력추천/선택으로 올리고, likely/no 는 정직하게 "미확인" 또는 "26.1.x 미지원"으로 분리했다.
- 컨텐츠·월드젠 모드는 **신규 청크에만 적용 + 서버 전원 설치/사전합의 필요**라 별도 경고를 달았다. 억지로 채우지 않고, 포화 영역은 "추가 불필요"라고 명시한다.

## 가장 강력한 Top 5 픽 (카테고리 불문)
1. ⭐ **Complementary Unbound** (`complementary-unbound`) — 이미 쓰는 Reimagined 의 자매 팩. 토글 한 번으로 "바닐라 충실 vs 시네마틱" 두 노선 확보. Iris 만으로 동작, 추가 비용 0.
2. ⭐ **Universal Graves** (`universal-graves`) — 사망 시 아이템 무덤 보관. 캐주얼 친구서버 안티-아이템로스의 핵심. 서버사이드라 친구들 클라 설치 불필요.
3. ⭐ **SimpleTPA** (`simpletpa+`) 또는 **Teleport Commands** — 서로 순간이동(/tpa). 소규모 친구서버의 가장 큰 갭. 초경량 서버사이드.
4. ⭐ **Structory** (`structory`) — 100% 바닐라 블록 탐험 구조물. 새 시스템 없이 "갈 곳"만 늘리는 '바닐라를 즐겁게'의 정수.
5. ⭐ **Default Dark Mode** (`default-dark-mode`) — 바닐라 레이아웃 유지 다크 GUI. "더 나은 UI" 요구에 정확히 부합, 야간 플레이 눈 편함. 다운로드 25만+.

---

## 쉐이더팩
**갭: 충분함.** 현재 Complementary Reimagined 1개뿐 — 티어/미적 다양성 확보 여지 큼. 전부 Iris 호환(이미 설치됨), OptiFine 전용 없음. 쉐이더는 리소스라 모드 충돌 위험 거의 없고, conflictRisk 는 대부분 "GPU 부하" 수준. **동시에 하나만 활성화**되므로 여러 개 받아두고 토글해도 무방하다.

- ⭐ **Complementary Unbound** (`complementary-unbound`) — Reimagined 의 시네마틱 자매 분기(강한 라이팅/볼류메트릭). 같은 제작자라 일관성 + "바닐라 vs 시네마틱" 토글 다양성. 설정으로 밸런스~고사양 커버. 의존성 Iris(설치됨). 충돌 없음. **검증: 확인됨** (r5.8.1 등 26.1/26.1.1/26.1.2 포함, loaders=iris/optifine).
- ⭐ **BSL Shaders** (`bsl-shaders`) — 가장 친숙한 올라운드 입문 표준. 따뜻한 색감, 풍부한 설정으로 누구에게나 권하기 안전. Reimagined 와 색조가 달라 다양성 기여. 의존성 Iris. 충돌 없음(기본 중간 부하). **검증: 확인됨** (v10.1.3/v10.1.2 만 26.1.x 포함 — v10.1.1 이하는 미포함 주의).
- ⭐ **Sildur's Vibrant Shaders** (`sildurs-vibrant-shaders`) — Lite/Medium/High/Extreme 다중 변형 제공, 한 팩으로 여러 티어 커버. **저사양 친구도 Lite 로 쉐이더 체감 가능** — 사양 제각각인 친구서버에 가성비 최상. 의존성 Iris. **검증: 확인됨** (1.56 전 변형 26.1.x 포함).
- 🔹 **Photon Shader** (`photon-shader`) — 고품질 리얼리스틱(대기산란/물반사/색 그레이딩). "사진같은" 고사양 티어 대표. 의존성 Iris. **검증: 확인됨** (v1.3b 등). ⚠️ 프레임 부담 큼 — 저사양 친구 비권장.
- 🔹 **Solas Shader** (`solas-shader`) — Photon 제작자(SixthSurge)의 밸런스형. 화려함보다 "보기 편한 안정감". 리얼리스틱과 경량 사이 중간 티어. 의존성 Iris. **검증: 확인됨** (V3.6 — V3.5 이하 미포함).
- 🔹 **Bliss Shader** (`bliss-shader`) — 따뜻/몽환 색감 + 강한 볼류메트릭. 판타지·감성 룩 다양성. 의존성 Iris. **검증: 확인됨** (2.1.2 — 2.1.1 이하 미포함). 중상 부하.
- 🔹 **MakeUp - Ultra Fast Shaders** (`makeup-ultra-fast-shaders`) — 초경량 지향(물반사/부드러운 그림자). Sildur's Lite 와 함께 저사양 보강. 의존성 Iris. **검증: 확인됨** (9.5b 등).
- 🔹 **Miniature Shader** (`miniature-shader`) — 틸트시프트로 세상을 미니어처처럼. 스크린샷/포토모드용 별미. 의존성 Iris. **검증: 확인됨** (2.18.11 — 2.18.10 이하 미포함). ⚠️ 흐림 효과로 일반 플레이 가독성↓ — 상시용 아님.
- ❌ **Rethinking Voxels** (`rethinking-voxels`) — **26.1.x 미지원**(최신 r0.1-beta9 가 1.21.10 까지). 복셀 컬러드 라이팅 최상위 티어였으나 현재 설치 불가. Iris 신버전 안정화 후 재확인 목록.

---

## 리소스팩
**갭: 일부 있음.** 현재 Fresh Animations(엔티티 애니)만 깔려 블록/아이템·UI·CTM 영역이 빔. 아래는 EMF/ETF/Fresh Animations 와 영역이 겹치지 않아 무충돌. **단, audio 영역은 Sound Physics Remastered + Presence Footsteps + AmbientSounds 로 이미 포화 — 사운드팩은 취향 선택일 뿐 불필요에 가깝다.**

- ⭐ **Better Leaves** (`better-leaves`) — 나뭇잎을 풍성한 cross-model 텍스처로 교체. 바닐라 친화 비주얼 향상, Reimagined 궁합 좋음. CTM 모드 불필요(바닐라 cross-model). 의존성 없음. **검증: 확인됨** (9.5, 26.1/26.1.1/26.1.2). 이미 깔린 Falling Leaves(파티클 모드)와 기능 축 다름 — 보완 관계.
- ⭐ **Default Dark Mode** (`default-dark-mode`) — 바닐라 톤 유지 다크 GUI 전반. "더 나은 UI" 요구 정확 충족, 야간 편함, 다운로드 25만+. 의존성 없음. **검증: 확인됨**. ⚠️ GUI 팩은 1개만 — 아래 Unique Dark/GUI Improvements 와 택일.
- ⭐ **Nautilus 3D** (`nautilus3d`) — 장식 블록(레일/사다리/철창/꽃 등) 바닐라풍 3D 디테일 + 최적화/버그픽스. "3D 모델 디테일" 직결, 빌딩 풍성. EMF/ETF 와 영역 다름. 의존성 없음. **검증: 확인됨** (V2.6.1, game_versions 정확히 26.1/26.1.1/26.1.2). ⚠️ 3D 블록 팩은 1개 권장(3D Default 와 택일).
- ⭐ **Vanilla Connected Glass** (`vanilla-connected-glass`) — 유리 테두리 제거(바닐라 cullface 방식, **Continuity/OptiFine 불필요**). 현재 CTM 빈 영역을 추가 모드 없이 메움, 빌딩 미관↑. 의존성 없음. **검증: 확인됨** (0.9). ⚠️ Borderless Glass 와 택일.
- 🔹 **Unique Dark** (`unique-dark`) — 다크 GUI + 외곽선/하이라이트로 좀 더 스타일라이즈. Default Dark Mode 대안. 의존성 없음. **검증: 확인됨** (2.6). Default Dark Mode 와 택일.
- 🔹 **Borderless Glass** (`borderless-glass`) — 색유리까지 테두리 제거. Vanilla Connected Glass 대안. 의존성 없음. **검증: 확인됨** (1.0.1-mc26.1). Vanilla Connected Glass 와 택일.
- 🔹 **3D Default** (`3d-default`) — Nautilus 보다 넓은 범위의 바닐라 충실 3D 모델. 의존성 없음. **검증: 확인됨** (1.14.0). Nautilus 3D 와 택일, 범위 넓어 저사양 FPS 영향 약간 큼.
- 🔹 **Vocal Villagers** (`vvi`) — 주민 사운드 43종 추가(바닐라 친화, 가벼움). 마을 몰입감↑, EMF/ETF/Fresh Animations 무충돌. 의존성 없음. **검증: 확인됨** (1.3). 오디오는 이미 포화이나 "주민 보이스"라 영역 달라 직접 충돌은 없음 — 취향.
- 🔹 **GUI Improvements** (`gui-improvements`) — 외곽선만 더하는 미니멀 GUI(바닐라 색감 유지). 다크모드 부담 시 최경량 대안. 의존성 없음. **검증: 미확인(설치 전 Modrinth 확인 필요)** — 1.2.7 이 26.1 만 명시, 26.1.1/26.1.2 미포함. 포맷 변화 없으면 동작 가능성 높으나 미확정. 다른 GUI 팩과 택일.
- ⚠️ **Enhanced Audio** (`enhanced-audio`) — 바닐라 사운드 다수를 현실적으로 교체하는 종합 사운드팩. **검증: 확인됨**(r7)이나, **이미 Sound Physics Remastered/Presence Footsteps/AmbientSounds 로 포화** — 동시 사용 시 발소리/환경음 중첩으로 과해짐. "가벼운 바닐라+" 기준에선 선택적/비권장.

---

## QoL·편의
**갭: 좁음 (거의 포화).** 이미 Jade/AppleSkin/IPN/Mouse Tweaks/Controlling/REI(RRV)/Zoomify/맵/나침반/RightClickHarvest 등 핵심 QoL 다수 보유. 인기 QoL 상당수(Trinkets·Carry On·Chest Tracker·Jade Addons·Equipment Compare·Legendary Tooltips·Item Borders·What's That Slot)는 **아직 26.1.x 미업데이트**라 후보 자체가 적다. 아래는 26.1.x Fabric 빌드 실측 확인된 가벼운 것만. **전반적으로 "급하지 않음 — 취향대로 골라 소수만".**

- 🔹 **Cherished Worlds** (`cherished-worlds`) — 월드 선택 화면 즐겨찾기/고정 + 실수 삭제 방지. 클라 전용, 게임플레이 변경 0. 의존성 없음. 충돌 없음. **검증: 확인됨** (16.0.0+26.1.2).
- 🔹 **Spyglass Improvements** (`spyglass-improvements`) — 망원경 줌 스크롤 조절 + 오버레이 완화. 프롬프트 갭 예시 "망원경 개선" 직접 충족. Zoomify(키 줌)와 대상 달라 기능 중복 아님. 의존성 없음. **검증: 확인됨** (1.5.13+mc26.1). 줌 키 겹치면 Controlling 으로 재바인드.
- 🔹 **Status Effect Bars** (`status-effect-bars`) — 포션/상태효과에 남은 시간 바 표시. 클라 전용 정보 QoL, AppleSkin/BetterF3 와 표시 영역 다름. 의존성 없음(Cloth Config 이미 보유). **검증: 확인됨**.
- 🔹 **Smooth Swapping** (`smooth-swapping`) — 인벤토리 아이템 이동 부드러운 애니메이션. 순수 비주얼 폴리시, 매우 가벼움. 의존성 없음. **검증: 확인됨**. IPN 과 드물게 미세 글리치 가능하나 기능 충돌 아님(광범위 공존).
- 🔹 **Inventory Totem** (`inventory-totem`) — 불사 토템이 인벤토리 어디서든 발동. 죽음 방지 편의. 의존성 없음. **검증: 확인됨** (26.1.2-3.4). ⚠️ 일부에겐 "난이도 하향" 호불호 — 서버 취향.
- 🔹 **Reacharound** (`reacharound`) — 허공 보며 발 아래/뒤 블록 자동 배치(다리/기둥 편의). 의존성 없음. **검증: 확인됨**. ⚠️ 의도치 않은 배치 싫어하는 빌더 있음 — 기본 토글/키 확인 권장.
- 🔹 **Raised** (`raised`) — 핫바/HUD 높이 조절 + 일부 깨진 핫바 텍스처 수정. 의존성 없음. **검증: 확인됨**. 다른 HUD 모드와 좌표 겹치면 미세 정렬만.
- 🔹 **Status Effect Bars / Smooth Swapping / Cherished Worlds** 정도가 "있으면 좋은" 무해 픽이고, 나머지(Inventory Totem·Reacharound)는 서버 취향 확인 후.

---

## 비주얼
**갭: 작지만 명확.** FO 베이스라 핵심 비주얼(엔티티 애니/조명/구름/잎/거품)은 포화. 빈 곳은 추가 디테일 파티클, 폴리시된 날씨, CTM, 자잘한 코스메틱. 전부 26.1.2/Fabric 실측 확인, 대형 오버홀 없음.

- ⭐ **Visuality** (`visuality`) — 픽업/화살 명중/모루/잠자기/보트 물보라 등 자잘한 디테일 파티클. 순수 바닐라+ 폴리시, 클라 전용. 의존성 Fabric API(설치됨). **검증: 확인됨** (0.7.13+26.1). 기존 Make Bubbles Pop 과 영역 다름.
- ⭐ **LambdaBetterGrass** (`lambdabettergrass`) — 잔디/눈/모래 옆면이 윗면과 부드럽게 이어짐(better-grass). LambDynamicLights 와 같은 제작자라 안정적, Sodium 호환. 의존성 없음(Fabric API). **검증: 확인됨** (2.7.2+26.1.1). 성능 영향 미미.
- 🔹 **WeatherRefind** (`weatherrefind`) — 바닐라 비/눈 파티클을 폴리시(잡음↓). "바닐라 그대로 다듬기"라 Particle Rain 보다 안전한 날씨 선택. 클라 전용. 의존성 없음. **검증: 확인됨** (1.4). Particle Rain 과 택일.
- 🔹 **Particle Rain** (`particle-rain`) — 바이옴별 강수 비주얼 재구현(안개/소리 어우러진 몰입형). 날씨 폴리시 직격. 클라 전용. 의존성 없음(Fabric API). **검증: 확인됨** (v4-beta.10+26.1). WeatherRefind 와 택일, 쉐이더 자체 날씨와 겹칠 수 있어 한쪽 조정.
- 🔹 **Particle Effects** (`particle-effects`) — 모닥불 연기/횃불 스파크/용암 ember 등 환경 파티클. 조명·불 연출 디테일↑. 클라 전용. 의존성 없음(Fabric API). **검증: 확인됨** (1.4.0+26.1). Visuality 와 대체로 상보적, 둘 다 켜면 저사양 약간 부담.
- 🔹 **Ears** (`ears`) — 스킨에 꼬리/귀/날개/뿔 부속 렌더(코스메틱). 3D Skin Layers 와 영역 달라 상보적. 안 쓰면 시각 변화 0(부담 0). 클라 전용. 의존성 없음. **검증: 확인됨** (1.4.7_01+fabric-26.1).
- 🔹 **Show Me Your Skin!** (`show-me-your-skin`) — 1인칭에서 자기 스킨 second layer 표시/토글. 의존성 없음. **검증: 확인됨** (2.0.3+26.1.2). 3D Skin Layers 와 일부 겹쳐 중복 검토 권장.
- 🔹 **Continuity** (`continuity`) — Sodium 기반 CTM(유리/책장 연결). OptiFine 없이 CTM 쓰는 표준. 의존성 Sodium(설치됨) + **CTM 제공 리소스팩 필요**. **검증: 확인됨** (3.0.1-beta.2, beta). ⚠️ 현재 리소스팩(Fresh Animations)이 CTM 미제공이라 단독으론 시각 변화 없음 + beta 안정성 주의. (Vanilla Connected Glass 리소스팩이면 Continuity 불필요.)
- ⚠️ **Distant Horizons** (`distanthorizons`) — LOD 로 먼 지형 렌더, 광활한 지평선. 임팩트 최대 후보지만 **(1) 성능 부담 큼**(첫 LOD 생성 CPU/디스크), **(2) Iris(Complementary)와 특정 버전 조합에서만 정상** — 안 맞으면 원경 깨짐, **(3) beta 빌드**. 의존성 Sodium/Iris 호환 버전 매칭 필수. **검증: 확인됨** (3.0.3-b-26.1.2). "가볍게" 테마와 가장 거리 멀어 **선택 옵션으로만**.

---

## 컨텐츠 (바닐라+)
**갭: 중간.** QoL/비주얼은 포화지만 "실제 컨텐츠(구조물/월드젠)"는 거의 비어 있어 바닐라+ 추가 여지 큼. **단 모든 컨텐츠 모드는 서버 전원 설치 + 사전합의 필요**, 특히 월드젠은 **신규 청크에만 적용**(기존 월드는 청크 경계 seam) — **새 월드 시작 또는 미탐사 지역 도입 강력 권고**. 구조물 모드는 **총 2~3개 이내**로 제한해 과밀 방지.

> ⚠️ 참고: YUNG's 전 라인업·Explorations·When Dungeons Arise·Naturalist·The Graveyard 등 인기 컨텐츠 모드는 **2026-06 현재 1.21.x 머물러 26.1.x 미지원**. 아래는 26.1.x 빌드 실재하는 가벼운 폴리시 위주.

### 구조물 (탐험 거리)
- ⭐ **Structory** (`structory`) — 100% 바닐라 블록 소규모 탐험 구조물(폐허/야영지/등대). 새 블록/아이템/엔티티 0, 분위기·탐험 보상만. 테마 정합 최상. standalone(의존성 없음). **검증: 확인됨** (1.3.16, 26.1/26.1.1/26.1.2). 서버 전원 설치, 신규 청크 한정.
- 🔹 **Structory: Towers** (`structory-towers`) — Structory 확장(탑/요새 + 'Channeler' 미니보스 컨셉). 같은 제작자 일관 톤. 의존성: Structory 권장(단독도 가능). **검증: 확인됨** (1.0.16). ⚠️ 미니보스/커스텀 전리품으로 순수 바닐라보다 한 발 나아감 — 취향 확인.
- 🔹 **Towns and Towers** (`towns-and-towers`) — 바닐라 마을/전초기지를 바이옴별로 증강(대체 아님). 탐험·교역 재미↑. 의존성: **Cristel Lib 필수**(`cristel-lib`). **검증: 확인됨** (1.13.11). 신규 청크 한정. Visible Traders(설치됨)와 무충돌.
- 🔹 **Moog's Voyager Structures** (`moogs-voyager-structures`) — 바닐라풍 130+ 탐험 구조물. 의존성 없음(자체 라이브러리). **검증: 확인됨** (5.0.11). ⚠️ 구조물 밀도 높아 붐빌 수 있음 — config 빈도 조절, Structory/T&T 와 셋 중 1~2개만.
- 🔹 **Dungeons and Taverns** (`dungeons-and-taverns`) — 바닐라풍 던전/선술집/보스 구조물(데이터팩 기반, 바닐라 전리품). standalone. **검증: 확인됨** (5.2.0+mod). 다른 던전 모드와 과밀 주의.
- 🔹 **Repurposed Structures** (`repurposed-structures`) — 바닐라 구조물의 바이옴별 변형(정글/사막 요새 등). 완전 새 구조물보다 더 바닐라 친화. **검증: 확인됨** (5.0.11). 의존성 없음으로 확인되나 일부 버전 라이브러리 요구 가능 — 설치 시 Modrinth 의존성 탭 확인 권장. 과밀 주의.

### 월드젠 (풍경 — 셋 중 하나만, 사전합의 필수)
- ⭐ **Terralith** (`terralith`) — 바닐라 noise 위 95+ 바이옴·극적 지형. 새 블록 거의 없는 월드젠 폴리시. 의존성 없음(Fabric API). **검증: 확인됨** (2.6.2). ⚠️ **월드젠 변경 — seam 발생, pregen(Chunky/ServerCore 설치됨) 권장**. Tectonic/Nullscape/Incendium 호환 설계.
- 🔹 **Tectonic** (`tectonic`) — 지형 스케일/다양성 극대화(큰 산·깊은 협곡). Terralith 와 공식 호환. 의존성 없음. **검증: 확인됨** (3.0.22-fabric-26.1). ⚠️ 둘 다 켜면 지형 매우 극단적 — 취향, seam/pregen.
- 🔹 **Geophilic** (`geophilic`) — 오버월드 바이옴 **미세** 폴리시(새 바이옴/블록 0). Terralith/Tectonic 부담 시 **가장 보수적 초경량 대안**. 의존성 없음. **검증: 확인됨** (3.5). 셋(Geophilic/Terralith/Tectonic) 중 하나만.
- 🔹 **Nullscape** (`nullscape`) — 엔드 외곽섬을 적막/광활하게 재구성(바닐라 블록만). 엔드 미방문이면 부작용 거의 0, 부하 낮음. standalone. **검증: 확인됨** (1.2.19). 엔드 신규 청크 한정.
- 🔹 **Incendium** (`incendium`) — 네더 새 바이옴/구조물 + 소량 적/보스. 네더 탐험 풍성. 의존성 없음(Fabric API). **검증: 확인됨** (5.4.12). ⚠️ 새 적/보스로 순수 바닐라보다 한 발 — 취향. 네더 신규 청크 한정(메인 월드 부하 영향 적음).

### 환경 디테일·계절
- 🔹 **Cave Dust** (`cave-dust`) — 동굴 천장 미세 먼지 입자(순수 앰비언트, 게임플레이 변화 0). 동굴 몰입↑, 서버 부하 0(클라 파티클). **검증: 확인됨** (3.1.0, 26.1.2). 일부 빌드가 Resourceful Lib/Forge Config API Port 요구 가능 — 후자는 이미 설치됨, 설치 시 의존성 확인.
- ⚠️ **Serene Seasons** (`serene-seasons`) — 봄/여름/가을/겨울 사이클(잎 색·작물 성장·온도 변화). 계절감으로 분위기 크게 바꿈, Falling Leaves 와 잘 어울림. **검증: 확인됨** (26.1.2.0.3). ⚠️ **게임플레이 영향 — 사전합의 필수**: 겨울 농사 페널티로 바닐라 농사 경험 변화, 자동 농장 기대치 어긋날 수 있음. 서버 전원 설치 + 동의 필요. config 로 작물 페널티 비활성화 가능. "편의"보다 "도전" 쪽이라 테마 경계선.

---

## 멀티플레이·서버
**갭: 충분함.** 소셜/편의(TPA, 무덤, 백팩, 잠수, 디스코드, 잠자기 투표)가 기존 모드에 거의 비어 좋은 후보 많음. **단 homes/warps 는 이미 WarpUtils 가 부분 점유 — Essential Commands 류는 명령 중복 주의.** 대부분 서버사이드라 친구들 클라 설치 불필요.

- ⭐ **Universal Graves** (`universal-graves`) — 사망 시 보호 무덤(시간제한/사망나침반/홀로그램/클릭 UI). 안티-아이템로스 핵심. Polymer 기반이라 바닐라 클라에서도 보임. 의존성: Polymer(번들/자동)+Fabric API. **검증: 확인됨** (3.11.1+26.1.2, Patbox). 충돌 낮음(Recall Coords 와 별개). 다른 Polymer 모드와 버전 정합만 확인.
- ⭐ **SimpleTPA** (`simpletpa+`) — /tpa·/tpaccept·/tpdeny 초경량 서버사이드(클라 불필요). 소규모 서버 핵심 갭. 의존성 없음(Fabric API). **검증: 확인됨** (26.1.2 v1, 2026-05-13). ⚠️ WarpUtils 가 자체 tpa 제공 시 명령 중복 — 둘 중 하나만 활성화.
- 🔹 **Teleport Commands** (`teleport-commands`) — /tpa·/tpahere 등 + /back 일부 포함. SimpleTPA 대안(통합형 선호 시). 의존성 Fabric API. **검증: 확인됨** (1.3.4). SimpleTPA 와 택일, WarpUtils 범위 확인 후.
- 🔹 **Server Backpacks!** (`serverbacksnow`) — 서버사이드 백팩(9/18/27 슬롯 + 엔더). Polymer 기반 바닐라 클라 표시. 의존성 Polymer(번들)+Fabric API. **검증: 확인됨** (1.5.7+26.1). IPN 과 레이어 달라 충돌 적으나 백팩 내부 정렬 테스트 권장.
- 🔹 **AfkPlus** (`afkplus`) — /afk + AFK 시간/사유 표시/탭리스트(서버사이드). 지침의 "잠수" 갭 직접 충족. Text Placeholder API(설치됨)와 연동. 의존성 Fabric API. **검증: 확인됨** (v1.7.14-mc26.1.2).
- 🔹 **Death Finder** (`death-finder`) — 사망 메시지에 좌표/차원/거리 추가, OP 클릭 텔레포트. "죽음 좌표 공유" 갭. 의존성 Fabric API. **검증: 확인됨** (v26.1.1 빌드, 26.1.x 태그). ⚠️ Recall Coords(설치됨)와 일부 겹침 — 차별점은 채팅 공유+OP TP. client-side 측면 있어 플레이어별 설치 권장.
- 🔹 **Mc2Discord** (`mc2discord`) — MC↔디스코드 채팅/이벤트 연동(정식 릴리스, beta 아님). 안정성 우선 디스코드 연동. 의존성 Fabric API + **디스코드 봇 토큰 설정(서버 config)**. **검증: 확인됨** (4.2.7, featured). ⚠️ 아래 Discord-MC-Chat 와 **택일(동시 설치 금지)**.
- 🔹 **Sleep Poll** (`sleeppoll`) — 침대 누우면 자동 투표(YES/NO 클릭으로 밤 스킵 결정). 소규모 서버 재미+편의. 의존성 Fabric API. **검증: 미확인(설치 전 Modrinth 확인 필요)** — 3.0.0-26.1 빌드가 26.1 만 명시, 26.1.1/26.1.2 태그 없음(호환 가능성 높으나 26.1.2 실행 테스트 권장). 바닐라 sleep 게임룰과 동시 사용 시 조정.
- ⚠️ **Discord-MC-Chat** (`discord-mc-chat`) — 디스코드↔MC 양방향 브리지(사망/접속/업적). **검증: 확인됨**이나 **3.0.0-beta.1** — beta 안정성 주의. Mc2Discord 와 택일이며, 안정성 우선이면 Mc2Discord 권장.
- ⚠️ **Essential Commands** (`essential-commands`) — /tpa·/home·/sethome·/warp·/spawn·/back 통합 + LuckPerms(설치됨) 연동. 통합 essentials 원하면 강력하나 **중복 리스크 높음**: WarpUtils(워프)·Recall Coords·SimpleTPA/Teleport Commands(tpa)와 겹침. 도입 시 WarpUtils 제거 검토 또는 본 모드 제외 — 택일. **검증: 미확인(설치 전 Modrinth 확인 필요)** — 0.39.0-mc26.1.1 빌드(26.1.1), 26.1.2 태그 없어 실행 테스트 권장.
- ⚠️ **EasyAuth** (`easyauth`) — 오프라인/크랙 서버 인증(미인증 차단/비번 로그인). **온라인(정품 전용) 서버면 불필요/제외**, 오프라인 운영 시에만 유용. **검증: 확인됨**이나 **3.4.3-SNAPSHOT** — 스냅샷 안정성 검증 후 도입.

---

## 추가 시 주의 (공통)

- **packwiz add 방법**: 모드팩 디렉토리에서 `packwiz modrinth add <slug>` (예: `packwiz modrinth add complementary-unbound`)로 추가하면 `.pw.toml` 메타가 생성되고 인덱스가 갱신된다. 추가 후 `packwiz refresh` 로 인덱스 정리.
- **side(클라/서버) 분류 반드시 확인 후 추가**:
  - **클라 전용**(서버에 넣지 말 것 — 각자 설치): 쉐이더팩 전부, 리소스팩 전부, Visuality/LambdaBetterGrass/Particle Rain/WeatherRefind/Particle Effects/Ears/Show Me Your Skin/Continuity/Distant Horizons, Cherished Worlds/Spyglass Improvements/Status Effect Bars/Smooth Swapping/Raised, Cave Dust.
  - **서버사이드(서버 필수, 멤버는 미설치 가능)**: Universal Graves, SimpleTPA, Teleport Commands, Server Backpacks!, AfkPlus, Mc2Discord/Discord-MC-Chat, Sleep Poll, Essential Commands, EasyAuth.
  - **양쪽 설치 필요(both)**: 구조물·월드젠 컨텐츠 모드 전부(Structory/Towns and Towers/MVS/Dungeons and Taverns/Repurposed Structures/Terralith/Tectonic/Geophilic/Nullscape/Incendium), Serene Seasons. **미설치 시 구조물 미생성/동기화 깨짐** → 서버 전원 설치 필수.
  - **확인 권장**: Inventory Totem·Reacharound(클라 측), Death Finder(client-side 측면 — 플레이어별 설치 권장).
- **사전 친구 합의 필요한 컨텐츠 모드 경고**:
  - **월드젠(Terralith/Tectonic/Geophilic/Nullscape/Incendium)**: 신규 청크에만 적용 → 기존 월드는 청크 경계 seam. **새 월드 시작 또는 미탐사 지역에서만 도입**, pregen(Chunky/ServerCore) 권장. 오버월드 월드젠 3종은 **택일**.
  - **Serene Seasons**: 작물 성장 페널티로 농사 경험 변화 → **전원 동의 + 전원 설치 필수**(config 로 페널티 off 가능).
  - **구조물 모드**: 총 2~3개 이내로 제한(과밀 방지), 신규 청크 한정이라 도입 시점 친구들에게 공지.

## 정직한 결론 (포화 영역 / 추천 우선순위)
- **추천 적음·불필요**: **QoL·편의**(이미 포화 — 무해한 소수만), **오디오 리소스팩**(Sound Physics+Presence Footsteps+AmbientSounds 로 포화 — Enhanced Audio 비권장, Vocal Villagers 만 취향), **핵심 비주얼**(엔티티/조명/구름/잎/거품 포화).
- **실질 추가 가치 큰 영역(우선순위 순)**: ① 멀티플레이 소셜·편의(TPA·무덤·디스코드 — 갭 가장 큼) → ② 쉐이더 다양성(토글용, 비용 0) → ③ 컨텐츠 구조물(Structory 류, 사전합의) → ④ 리소스팩 블록/UI(Better Leaves·Default Dark Mode·3D·유리).
- **억지로 채우지 않음**: 월드젠은 "있으면 멋지지만" 서버 운영 부담·seam·합의 필요라 **선택 옵션**으로만 두었고, Distant Horizons·Serene Seasons·Essential Commands·EasyAuth 는 부하/중복/운영방식 이슈로 ⚠️ 처리했다.
