# RPG 모드팩 이니셔티브 — 현황 리서치 (Codex × Claude 교차검증)

> 작성: 2026-06-22 23:09 KST · 방법: 블라인드 4 리서처(Codex 독립 1 + Claude Explore 3) + 부모 컨텍스트 `gh`/`git` 검증
> 트리거: 사용자 "처음부터 다시 코덱스와 리서칭만 전문적으로" — 사전 결론 미주입(blind), read-only, Codex가 1차 참여자.
> 모든 주장은 git/gh/파일 증거 기반. negative assertion 은 2개 이상 명령 교차 또는 confidence 명시.

---

## 0. 핵심 정정 (가장 중요 — 양쪽 1차 결론을 뒤집음)

블라인드 리서처(Codex + Claude launcher-deploy)는 모두 CI `launcher-build.yml` 의 `push: branches:[main]` 만 보고 **"beta/rpg 채널은 배포 경로 없음(shippable 30~40%)"** 으로 판정했다. 부모 컨텍스트에서 `gh release list` 로 교차검증한 결과 이는 **틀렸다**:

- `v1.3.0-beta.1/2/3` 은 **GitHub Pre-release 로 실제 게시됨** (2026-06-21).
- 이 태그들은 `feat/rpg-dungeon-pack` 의 3채널 런처 커밋(`80a51cc`/`32c4bf2`/`05762a5`)을 가리킨다.
- CI 의 `branches:[main]` 은 **push 트리거**만 제한할 뿐, `release: types:[created]` 트리거는 **임의 태그**에서 빌드한다 → maintainer 가 pre-release 를 발행했고 그 빌드가 게시됨.

**결론 정정: 3채널 런처(prod/beta/rpg)는 이미 테스터가 다운로드 가능하다.** 단 "Latest" 가 아닌 "Pre-release" 라 기존 v1.2.0 사용자에게 Velopack 자동 업데이트로 내려가지 않는다(opt-in 테스터용 — 의도와 일치).

| 증거 | 명령 | 결과 |
|---|---|---|
| pre-release 게시 | `gh release list --limit 20` | `v1.3.0-beta.3` Pre-release(06-21), beta.2, beta.1 모두 게시. `v1.2.0` 이 Latest |
| 태그→커밋 | `git log -1 --format='%h %s' v1.3.0-beta.3` | `05762a5 fix(launcher): 번역 보충팩 자동 활성화` (feat/rpg-dungeon-pack tip) |
| CI 트리거 | `launcher-build.yml` | `on: push: branches:[main]; release: types:[created]` — release 는 브랜치 무관 |

---

## 1. 트랙 구조 (Codex ↔ Claude Agreed, certain)

모드팩 트랙은 **2개**, 런처 채널은 **3개**(prod/beta/rpg)다.

| | 정식(prod) | 베타(beta) | RPG |
|---|---|---|---|
| 모드팩 디렉토리 | `modpack/` | `modpack/` (확장) | `modpack-rpg/` |
| MC / 로더 | 26.1.2 / Fabric 0.19.3 | 26.1.2 / Fabric 0.19.3 | **1.21.1 / NeoForge 21.1.234** |
| 모드 수 | 79 (main) | 170 (beta) | 92 |
| 성격 | 야생 친구서버(QoL·최적화·탐험) | **26.1.2 콘텐츠 확장**(던전·구조물·창고·빌딩) | **하드코어 ARPG**(마법·스킬·보스 트레드밀) |
| pack URL | GitHub Pages | beta 브랜치 raw | rpg 브랜치 raw(`modpack-rpg`) |
| AutoConnect | true | false(싱글 테스트) | false(싱글 테스트) |

핵심: **"RPG" 가 두 의미로 쓰인다** — beta 의 "RPG/던전 모드"(`6e59967`)는 *기존 26.1.2 월드에 RPG풍 콘텐츠 추가*(로더 불변), RPG 트랙은 *완전 별도 1.21.1 NeoForge 하드코어 ARPG*. 동일 레이블이라 혼동 가능(Codex Risk 6-E, Claude 동의).

---

## 2. 결정 히스토리 (시간순 — Codex 재구성, Claude git 타임라인 검증)

| 일자(KST) | 사건 | 근거 |
|---|---|---|
| 06-09 | 단일 26.1.2 Fabric 트랙. "갭은 쉐이더/콘텐츠" | `docs/research/2026-06-09-mod-recommendations.md` |
| 06-18 | main `ce2b621`(v1.2.0). **이후 모든 RPG 브랜치의 분기점** | `git merge-base` 4브랜치 = ce2b621 |
| 06-20~21 | 멀티모드 리서치: "마법/RPG 유명 모드는 26.1.2 에 사실상 없음 → 야생=26.1.2 Fabric 유지 + 마법/RPG=별도 1.21.1 팩". Velocity 프록시(MC 버전 혼합)는 기술적 불가 → 런처 채널 선택기만 가능 | `docs/research/2026-06-20-multimode-magic-rpg.md` (`6bfd88a`) |
| 06-21 아침 | RPG 팩 로더 결정: 리서치는 **Fabric 1.21.1 권장**(런처 로더 유지), 그러나 실구현은 **NeoForge 1.21.1** 채택 — 깊은 생태계(PMMO/Ars/Iron's/Cataclysm)가 NeoForge 전용이라 번복 | `docs/research/2026-06-21-1211-rpg-modpack-plan.md` vs 커밋 `7a8c2fd` |
| 06-21 17:33~20:23 | **beta 트랙**(Fabric 26.1.2): +91 모드, 한국어 번역, 베타 채널 런처(`80a51cc`=v1.3.0-beta.1) | `git log origin/main..origin/beta` |
| 06-21 21:13~26 | **RPG 팩 착수**(`feat/rpg-neoforge`): NeoForge 1.21.1 팩 + 의존성/크래시 픽스 | `git log feat/rpg-neoforge` |
| 06-21 21:54~ | 3채널 선택기(`32c4bf2`=beta.2) → 번역 픽스(`05762a5`=beta.3) on `feat/rpg-dungeon-pack` | 태그 위치 |
| 06-22 ~13:23 | **rpg 브랜치 ARPG 심화**(Phase 1~5: PMMO 게이팅·던전 루트·보스 트레드밀·밸런스) + 서버 부팅 검증 | `git log rpg` |

요점: beta 와 rpg 는 "Fabric 시도 실패→NeoForge 피벗" 의 순차가 아니라, **같은 리서치가 제시한 두 방향을 같은 날 4시간 간격으로 동시 착수**한 것. 06-22 의 `modrinth-loader-filter` 솔루션은 트랙 분리의 *사후 보강*(rpg 착수가 16h 빠름).

---

## 3. 각 트랙 현재 상태 (정정된 shippability)

| 트랙 | 상태 | 배포 가능성 | 근거 |
|---|---|---|---|
| **prod (26.1.2 Fabric)** | ACTIVE · 정식 배포 중 | ✅ 완전 | v1.2.0=Latest, Pages 라이브, main push→Pages 자동 배포 |
| **beta (26.1.2 Fabric 확장)** | 단독 브랜치 **정체**(06-21 이후 무활동) · 채널로는 **생존** | ◐ Pre-release 게시됨 | origin/beta 미머지지만 `feat/rpg-dungeon-pack` 에 흡수(ancestor). v1.3.0-beta.3 pre-release 에 beta 채널 포함 |
| **RPG (1.21.1 NeoForge)** | ACTIVE 개발 · Pre-release 로 테스트 가능 · 정식 미배포 | ◐ Pre-release 게시됨 | Phase 1~5 완료, 서버 부팅 검증(`c882dcf`). v1.3.0-beta.3 런처가 rpg 채널 포함, 팩은 rpg 브랜치 raw URL 참조 |
| **feat/rpg-dungeon-pack** | ACTIVE · v1.3.0-beta.3 최신 태그 · main 미머지 | ◐ pre-release 소스 | 3채널 선택기 + NeoForge 설치(`CmlLib.Core.Installer.NeoForge 4.0.0`) + ChannelResolutionTests |

**계보**: 두 lineage 가 분리됨.
- A(Fabric): main → `origin/beta`(8) → `feat/rpg-dungeon-pack`(+2, 3채널)
- B(NeoForge): main → `feat/rpg-neoforge`(3) → `rpg`(+15, Phase 1~5)
- 4브랜치 모두 main 미머지. A·B 는 서로 후손 아님(공통조상=main 분기점).

---

## 4. 로더 가용성 Crux (Codex 정밀 수치 + Claude 동의, certain)

"ARPG/마법 핵심 모드가 Fabric 26.1.2 에 없다" = **CONFIRMED**. 이것이 NeoForge 트랙 분리의 근본 원인.

| 모드 | 26.1.2 Fabric/NeoForge | 1.21.1 Fabric/NeoForge | 비고 |
|---|---|---|---|
| Ars Nouveau | 0 / 0 | 0 / 26 | NeoForge 전용 |
| Iron's Spells | 0 / 0 | 0 / 22 | NeoForge 전용 |
| Project MMO | 0 / 0 | 0 / N | NeoForge 전용(Fabric 은 Levelz 근사) |
| L_Ender's Cataclysm | 0 / 0 | 0 / 43 | NeoForge 전용 |
| Apotheosis / Gateways | 0 / 0 | 0 / N | NeoForge 전용 |

근거: `docs/research/2026-06-20-multimode-magic-rpg.md` 가용성 표 + `modpack-rpg/mods/*.pw.toml` 파일명(`ars_nouveau-1.21.1-*.jar` 등 NeoForge 빌드). beta(+91 모드)는 여전히 Fabric 26.1.2 라 ARPG 코어 **0개** — 추가분은 전부 던전/구조물/창고/빌딩.

---

## 5. 런처·서버·배포 매트릭스 (Agreed, certain)

| 레이어 | prod | beta | rpg |
|---|---|---|---|
| 런처 채널 코드 | main/모든 브랜치 | origin/beta + dungeon-pack | **dungeon-pack 전용**(rpg 브랜치엔 없음) |
| 로더 설치기 | Fabric | Fabric | NeoForge(dungeon-pack 의 `CmlLib.Core.Installer.NeoForge 4.0.0`) |
| 서버 | `server/`(Fabric, Java 25) | (미동기) | `server-rpg/`(NeoForge 1.21.1, 부팅 검증됨) |
| pack 호스팅 | Pages CDN | 브랜치 raw | 브랜치 raw(no CDN) |
| 정식 배포(Latest) | ✅ | ✗ | ✗ |
| Pre-release 배포 | — | ✅ v1.3.0-beta.* | ✅ v1.3.0-beta.* |

`ChannelInfo` 스위치(dungeon-pack): `rpg→(RpgPackTomlUrl, 1.21.1, NeoForge, AutoConnect=false)`, `beta→(BetaPackTomlUrl, 26.1.2, Fabric, false)`, `default→(PackTomlUrl, 26.1.2, Fabric, true)`.

---

## 6. 리스크 / 미해결 (Codex 식별, Claude/부모 검증)

| # | 리스크 | 상태 | confidence |
|---|---|---|---|
| 6-A ★ | **브랜치 분산**: RPG 팩/서버(`rpg`)와 채널 런처(`feat/rpg-dungeon-pack`)가 다른 브랜치 — 한 브랜치에 둘 다 없음. 코히런트 정식 릴리스의 **단일 블로커**. README 도 "병합/조율 필요(메인테이너 결정)" 명시 | 미해결(사용자 영역) | certain |
| 6-B | **RPG 채널 = 싱글 테스트 전용**: `AutoConnect=false` + `ServerIp` 가 전 채널 공유(RPG 전용 서버 IP 없음) → 멀티 접속하려면 추가 작업 | 미구현 | certain |
| 6-C | **RPG 팩 raw URL 의존**: CI 가 `modpack-rpg/` 를 Pages 배포 안 함 → `raw.githubusercontent.com`(캐싱 없음·레이트리밋·공개레포 전제) | 정식 트랙 대비 약함 | certain |
| 6-D | **beta 170모드 안정화 이력**: Sparse Structures NPE(`3346fc7`)·Incompatible mods(`97933f7`) 크래시 제거됨, 추가 안정화 여지 | 부분 해결 | certain |
| 6-E | **"RPG" 레이블 중의성**: beta 의 RPG-lite 콘텐츠 ↔ rpg 의 하드코어 ARPG | 문서/명명 | likely |
| 6-F | **런타임 e2e 미검증**: NeoForge 1.21.1 실설치→런처→게임 실행은 미검증(ChannelResolutionTests 는 라우팅 단위테스트 수준). README 도 "빌드 검증까지, 실기기 미검증" 명시 | 미검증 | certain |

---

## 7. Codex ↔ Claude 교차검증 매트릭스

| 분류 | 항목 |
|---|---|
| **Agreed (4 리서처 일치)** | 트랙 2개 구조 / 로더 crux(ARPG=NeoForge 전용) / 계보 분리·전부 미머지 / 3채널 런처는 dungeon-pack 전용 / RPG 서버 부팅 검증 / 6-A 브랜치 분산이 핵심 블로커 |
| **Codex-only (Claude 미발견, 추가 검증 통과)** | 6-B(RPG 채널 AutoConnect=false + ServerIp 전채널 공유) / 로더 가용성 정밀 수치(0/26 등) / 결정사 Fabric1.21.1→NeoForge1.21.1 sub-pivot |
| **부모 검증 정정** | "beta/rpg 배포 경로 없음" → **틀림**. v1.3.0-beta.* pre-release 실게시(`gh release list`). Codex·Claude 모두 CI push 트리거만 보고 release 트리거·실게시 누락 |
| **Disagree** | 없음(상태 framing 차이만: Codex "beta=PAUSED" ↔ 이전 검증 "parallel-intentional" → "단독 브랜치 정체 + 채널로는 dungeon-pack 에 흡수 생존"으로 통합) |

---

## 8. 미검증 영역 (정직 표기)

- 커밋 메시지의 "132/142 테스트 통과" — 본 리서치에서 `dotnet test` 미재실행.
- NeoForge 1.21.1 실설치→실행 e2e — 미검증(README 도 동일 명시).
- v1.3.0-beta.* pre-release 바이너리의 실제 채널 동작(다운로드→RPG 채널 선택→1.21.1 설치) — 게시는 확인, 런타임 동작은 미검증.
- beta 170모드 풀 런타임 안정성 — 알려진 크래시 2건은 제거됐으나 전수 미검증.

---

## 9. 결론

- **"1.26.1 베타 RPG" = MC 26.1.2 Fabric 라인의 beta 채널 RPG-lite 콘텐츠 확장**(별도 1.21.1 NeoForge 하드코어 ARPG 트랙과 구분). "1.26.1" 은 26.1.2 오기.
- **3채널 런처는 이미 pre-release(v1.3.0-beta.3)로 게시되어 테스터가 받을 수 있다** — 이전 "배포 불가" 결론은 정정됨.
- **정식 릴리스의 단일 블로커 = `rpg`(팩/서버) ↔ `feat/rpg-dungeon-pack`(채널 런처) 브랜치 병합**(6-A). 이것만 해결되면 main→Latest 승격 가능.
- ARPG 코어가 Fabric 26.1.2 에 부재한 것은 사실이며, 그래서 두 트랙이 의도적으로 병존한다(beta=기존 26.1.2 월드 확장, rpg=새 1.21.1 월드).

---

## 10. 정정/보강 (2026-06-22 추가): RPG-on-Fabric-26.1.2 는 가능 — CurseForge 레퍼런스 팩 증거

위 §4·§9 의 "ARPG 코어가 Fabric 26.1.2 에 없다 → RPG 는 NeoForge 1.21.1" 프레이밍은 **부분 정정**한다. 사용자가 제시한 CurseForge 26.1.2 Fabric 모드팩(직접 WebFetch)이 RPG/어드벤처/코지를 26.1.2 Fabric 에서 **이미 출시·운영 중**임을 보여준다:

- **new-age-adventures** (26.1.2 Fabric, 139 deps): 스킬트리·레벨드몹·MMO 파티·NPC·경제 — *코히런트 Fabric RPG 루프*.
- **dreamcraft-dreamcore** (26.1.2 Fabric, 195 deps): Adventure/RPG/Magic Extra Large (챔피언몹·무기숙련·포션/비주얼).
- **honey-bloom-valley** (26.1.2 Fabric): 코지 판타지.

**정밀화**: §4 의 *세부 주장*(Iron's Spells / Ars Nouveau / Apotheosis / PMMO / Cataclysm 이 NeoForge 전용)은 **여전히 참**. 그러나 그 모드들이 RPG 의 유일한 길이 아니다 — Fabric 26.1.2 는 **다른 스택**으로 RPG 를 구성한다: 스킬트리=**Puffish's Skills + Default Skill Trees**, 속성=**Puffish's Attributes**, 레벨드몹=**Dynamic Difficulty**, 파티/공유XP=**Party Link**, NPC/퀘스트=**Easy NPC**, 경제=**EasyEconomy**, RPG 루트=**DarkLoot**, 엘리트몹=**Mob Champions**, 무기숙련=**YDM's Weapon Master**, 트링켓=**Trinkets Updated + Artifacts**.

**전략적 함의**: beta 의 "RPG-lite(콘텐츠 확장)" 트랙을 **NeoForge 다운그레이드 없이 실제 RPG 로 승격**하는 경로가 존재한다(구리골렘·Litematica·26.1.2 월드 보존). 이식 후보 = 위 모드 중 beta 170 미보유분.

**주의(cross-ref)**: new-age 는 **Sparse Structures** 를 포함하지만 beta 는 이걸 MES 레지스트리 NPE 크래시로 제거함(`3346fc7`) → 이식 금지. Artifacts 는 Trinkets Updated 의존.

**검증 경계**: 각 모드 개별 26.1.2 호환 페이지는 미확인 — new-age-adventures(26.1.2 Fabric 확정)에 포함돼 있음이 호환 증거(pack-inclusion). **Spell Engine 은 두 팩 deps(전 페이지) 어디에도 없음** — "Fabric 매직=Spell Engine" 추론은 이 팩들에선 미실증(두 팩의 RPG 는 스킬/근접/콘텐츠형이지 깊은 매직형 아님).

---

## 11. 실행 중 검증 (2026-06-23): 26.1.2 Fabric RPG 생태계가 얇음 — §10 over-claim 정정

§10 의 "RPG-on-Fabric-26.1.2 가능" 은 **스킬 프레임워크 한정으로 정정**한다. feat/rpg-dungeon-pack 에서 `packwiz curseforge add` 로 실제 검증한 결과:

| 모드 | 26.1.2 Fabric 네이티브? | 증거 |
|---|---|---|
| Pufferfish's Skills (프레임워크) | ✅ | `puffish_skills-0.17.4-26.1-fabric` (add 성공) |
| Pufferfish's Attributes | ✅ | `puffish_attributes-0.8.2-26.1-fabric` |
| Default Skill Trees (기성 스킬트리) | ❌ 1.21.9 천장 | packwiz 가 `default_skill_trees-1.1-1.21.9.zip` 로 fallback |
| Dynamic Difficulty (레벨드 몹) | ❌ Fabric 1.21.11 천장 (26.1.2 는 NeoForge 만) | CF 파일목록: `fabric 1.1.1+1.21.11` / `neoforge 1.2.0+26.1.2` |
| 매직(Spell Engine/Wizards) | ❌ 1.21.1 천장 | §10 매직 조사 |

**핵심 교정**: pack-inclusion(new-age 가 26.1.2 로 태깅)은 26.1.2 **네이티브 가용의 증거가 아니다** — CurseForge 모드팩은 파일의 MC 버전을 강제하지 않아, 1.21.x 빌드를 26.1.2 팩에 cross-version 으로 끼워넣을 수 있다(Default Skill Trees 1.21.9, Dynamic Difficulty Fabric 1.21.11 가 그 예). 따라서 §1·§10 의 "RPG/매직이 26.1.2 Fabric 에서 출시·운영 중" 은 **콘텐츠 확장(던전/구조물/창고 — 이건 26.1.2 네이티브 가용)에는 참, 진행 메커니즘(스킬트리 콘텐츠·레벨드몹)·매직에는 거짓**으로 분할된다.

**결론**: 26.1.2 Fabric 은 스킬 **프레임워크**만 네이티브이고 콘텐츠/메커니즘/매직은 1.21.x 천장. → **진짜 RPG(매직+스킬+레벨드몹 풀 생태계)는 1.21.1 에 속한다**. 원 프로젝트의 "RPG=1.21.1" 결정이 사후 정당화됨. beta(26.1.2)는 RPG-lite(콘텐츠 확장) 유지가 합리적. (1.21.1 안에서도 Fabric[Spell Engine+Puffish+Dynamic Difficulty 전부 네이티브] vs NeoForge[Apotheosis/Gateways/PMMO 더 깊음] 선택지가 있으며, 현 rpg 트랙은 NeoForge 채택.)
