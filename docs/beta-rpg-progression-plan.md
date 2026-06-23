# beta(26.1.2 Fabric) 진행형 RPG 승격 — 이식 계획

> ⚠️ **SUPERSEDED (2026-06-23) — 미채택**: 이 계획은 실행 중 폐기됨. `packwiz curseforge add` 실측 검증 결과 26.1.2 Fabric 은 스킬 **프레임워크**(Puffish Skills/Attributes)만 네이티브이고 **기성 스킬트리·레벨드몹(Dynamic Difficulty)·매직은 1.21.x 천장**(26.1.2 네이티브 빌드 부재). 사용자 결정: **beta 는 RPG-lite(콘텐츠 확장) 유지, 진짜 RPG 는 rpg(1.21.1 NeoForge) 트랙**. 근거/상세: [research §11](research/2026-06-22-2309-rpg-track-status-codex-pair.md). 아래 본문은 참고용 보존(실행 안 함).

> 작성: 2026-06-22 23:48 KST · 근거: [docs/research/2026-06-22-2309-rpg-track-status-codex-pair.md](research/2026-06-22-2309-rpg-track-status-codex-pair.md) §10 + CurseForge 레퍼런스 팩(new-age-adventures / dreamcraft-dreamcore) 모드 마이닝
> 성격: **계획(plan)** — 실제 모드팩 변경 전 단계. 모드 추가는 사용자 승인 후 실행.

## 1. 목표 & 범위

beta 트랙(`modpack/`, MC 26.1.2 Fabric)을 "콘텐츠 확장(RPG-lite)"에서 **진행형 RPG**(스킬·레벨링·전투·경제 루프)로 승격한다. **NeoForge 1.21.1 다운그레이드 없이** — 구리골렘·Litematica·26.1.2 월드·기존 인프라 보존.

**범위 IN**: 스킬트리, 속성, 레벨드 몹, 파티/공유XP, 엘리트 몹, 무기 숙련, RPG 루트, NPC/퀘스트, 경제, 트링켓.

**범위 OUT (불가능 — 의도적 제외)**: **스펠캐스팅 매직**. Spell Engine·Wizards(RPG Series)·Ars Nouveau·Iron's Spells 모두 **26.1.2 미지원(천장 1.21.1)**. 매직이 핵심이면 그건 rpg(1.21.1) 트랙의 영역이지 beta 가 아니다. (근거: §10 매직 조사)

## 2. 결정 필요 (사용자 영역 — BLOCKING)

| # | 결정 | 선택지 | 권장 |
|---|---|---|---|
| D1 | **타깃 브랜치** | (a) `origin/beta` 직접 (b) `feat/rpg-dungeon-pack`(beta 포함 + 3채널 런처) (c) 신규 `feat/beta-rpg` 분기 | **(b)** — 채널 런처가 이미 있어 테스트 배포(v1.3.0-beta.x) 흐름에 바로 올라탐 |
| D2 | **범위 확정** | 진행형 RPG(매직 제외) 동의 여부 | 동의 시 진행 |
| D3 | **티어 깊이** | T1만(코어 루프) / T1+T2(전투까지) / 전체(T1~T3) | T1 먼저 검증 후 확장 |

> D1 은 §리서치 6-A(브랜치 분산)와 직결 — beta 산출물이 어느 브랜치에 쌓일지 결정해야 코히런트 릴리스가 됨.

## 3. Pre-flight 검증 게이트 (모드 추가 전 — BLOCKING, 함정 방어)

각 후보 모드에 대해 **추가 전** 다음을 통과해야 함:

1. **로더+버전 동시 필터** — Modrinth/CurseForge 에서 `loaders=["fabric"]` **AND** `game_versions=["26.1.2"]` 둘 다로 쿼리해 실제 빌드 존재 확인. (근거: [modrinth-loader-filter-false-availability](solutions/modrinth-loader-filter-false-availability.md) — `game_versions`만 보면 NeoForge 빌드가 잡혀 거짓 가용). new-age-adventures(26.1.2 Fabric) 포함이 1차 증거지만, 정확 슬러그+빌드는 add 시점 재확인.
2. **side 분류** — 싱글플레이=통합 서버라 server 몫도 클라에 필요. 라이브러리/월드젠/구조물=`both`, 클라 전용 렌더만 `client`. **`side="server"` 금지**. (근거: [modpack-packwiz-side-singleplayer-needs-server-mods](solutions/modpack-packwiz-side-singleplayer-needs-server-mods.md))
3. **블랙리스트** — **Sparse Structures 추가 금지**(beta 가 MES structure_set 레지스트리 NPE 크래시로 제거함, `3346fc7`).
4. **의존성 동반** — 아래 deps 표 참조. 의존 누락 = Fabric "Incompatible/Missing mods" 크래시(`97933f7` 전례).

## 4. 이식 후보 (티어별) — beta 170 미보유분, 26.1.2 Fabric 실증(new-age/dreamcraft)

### Tier 1 — 코어 진행 루프 (최소 RPG 성립)
| 모드 | 역할 | 의존 | side(예상) |
|---|---|---|---|
| **Puffish's Skills** (`puffish_skills`) | ★ 스킬 트리 엔진 | — | both |
| **Default Skill Trees** | Puffish용 기성 스킬트리(또는 자작 config) | Puffish Skills | both/datapack |
| **Puffish's Attributes** | 속성 시스템 | — | both |
| **Dynamic Difficulty** | 스폰 거리 기반 몹 스케일링(=레벨드 몹) | — | both |
| **Party Link** | 그룹 + XP 공유 | — | both |

### Tier 2 — 전투/몹/루트
| 모드 | 역할 | 의존 | side |
|---|---|---|---|
| **Mob Champions** | 엘리트/챔피언 몹 | — | both |
| **YDM's Weapon Master** | 무기 숙련 레벨링 | — | both |
| **DarkLoot** | RPG 몹 루트/헤드 | — | both |
| **Provi's Health Bars** | 몹 체력바 | — | client |
| **CombatEdit** | 전투 튜닝 | — | both |

### Tier 3 — 장비/NPC/경제
| 모드 | 역할 | 의존 | side |
|---|---|---|---|
| **Trinkets Updated** + **Artifacts** | 트링켓 슬롯 + 패시브 아이템 | Artifacts→Trinkets | both |
| **Easy NPC** (+Core/Config UI) | 커스텀 NPC/퀘스트 | GeckoLib | both |
| **EasyEconomy** | 화폐/경제 | — | both(server 권위) |
| **Goblin Traders** | 트레이더 NPC | — | both |

> beta 기보유로 중복인 것(추가 불요): dungeons-and-taverns, explorify, illager-invasion, lootr, mutant-monsters, waystones, towns-and-towers, terralith, structory, open-parties-and-claims(클레임). Grim Kingdoms/Epic Structures 등 추가 구조물은 중복 검토 후 선택.

## 5. Config 통합 — 실질 작업의 핵심

스킬트리는 **모드만 추가해선 빈 껍데기**다. Puffish Skills 는 스킬 정의(config/datapack)가 있어야 작동 → **"Default Skill Trees" 채택 또는 자작 트리 정의**가 필요. 이는 rpg 트랙의 `herma-rpg-tweaks` 데이터팩에 대응하는 **beta 판 통합 레이어** 작업(effort: **L**). 밸런스(스킬 비용/속성 스케일/난이도 곡선)도 config. → beta 용 `herma-rpg-lite-tweaks`(가칭) 글로벌팩/데이터팩 신설 검토.

## 6. 번역 (한국어)

신규 모드의 ko_kr.json 을 `translations/herma-ko` 보충 리소스팩에 추가(기존 워크플로 `translations/tools/`). 우선순위: Tier 1 스킬/속성 UI(플레이어 직접 노출) → Tier 2/3. effort: **M**.

## 7. 검증 (PASS 선언 전 — runtime e2e 필수)

1. **헤드리스 서버 부팅 스모크** — `server/`(또는 beta 동기화)로 Java 25 부팅, `Done` 로그 + 크래시 0 확인. (Sparse Structures NPE 류 런타임 발현 검출 — 정적 검사 미검출)
2. **클라 싱글플레이 실행** — 런처 beta 채널로 동기화 → 싱글플레이 진입 → 스킬트리 UI 열림 + 몹 스케일링 + 트링켓 슬롯 작동 육안 확인. (Incompatible mods 류는 클라에서만 발현)
3. **side 비대칭 재확인** — server 부팅 통과 ≠ 클라 통과. 양쪽 경로 모두.
4. 미검증 영역 명시(예: 멀티 파티 XP 공유 실접속 미검증 등).

## 8. 리스크 & 함정

- **R1 스킬트리 config 공수 과소평가** — 모드 추가는 S, 의미 있는 스킬트리/밸런스는 L. 빈 트리로 "완료" 선언 금지.
- **R2 26.1.2 빌드 부재 가능성** — new-age 포함이 강한 증거이나 일부 모드는 26.1.x 패치마다 빌드 갱신 지연 가능 → §3.1 게이트로 add 시점 재확인.
- **R3 모드 충돌** — 속성/전투 모드(Puffish Attributes × CombatEdit × Dynamic Difficulty)가 데미지 계산 중첩 가능 → 통합 테스트.
- **R4 170→190+ 모드 부하** — beta 가 이미 170. 추가 시 메모리/부팅 시간↑(런처 RAM 권장값 재검토).
- **R5 브랜치 분산(6-A)** — D1 미결 시 산출물이 또 다른 브랜치에 고립.

## 9. 실행 순서 (effort: XS~L, 시간 환산 없음)

| Phase | 작업 | effort | 게이트 |
|---|---|---|---|
| P0 | D1~D3 결정 + 타깃 브랜치 체크아웃 | XS | 사용자 |
| P1 | Tier 1 모드 §3 게이트 통과 후 packwiz add + side + refresh | S | 빌드 확인 |
| P2 | Puffish 스킬트리 config(Default Skill Trees 또는 자작) + 밸런스 | **L** | — |
| P3 | 서버 부팅 + 클라 싱글 스모크(§7) | M | **BLOCKING** |
| P4 | Tier 2/3 단계 추가(각각 P1~P3 반복) | M~L | 티어별 스모크 |
| P5 | 한국어 번역 보충 | M | — |
| P6 | 채널 런처로 테스트 배포(v1.3.0-beta.x 흐름) | S | D1=(b) 전제 |

## 10. 정의된 "완료" (evidence 필수)

- packwiz refresh 성공 + index hash 갱신
- 헤드리스 서버 `Done` 로그(크래시 0) + 클라 싱글 진입 스크린샷
- 스킬트리 UI 실작동(빈 트리 아님) + 몹 스케일링 + 트링켓 슬롯 육안
- 미검증 영역(멀티 실접속 등) 명시
