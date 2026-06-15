# HermaLauncher 1.0 UltraPlan (v2 — Codex 교차검증 R1 반영)

> 현재 v0.1.10 → 1.0 출시 품질. 워크플로 5차원(101건) + Codex 독립탐색 + Codex plan 교차검증 R1(21건) 반영.
> SoT = 본 문서. 현재 라운드/진행은 §진행 로그 참조.

## 0. 범위 분류 + 원칙

| 분류 | 의미 | 처리 |
|---|---|---|
| **CODE** | 코드/CI/문서로 자율 구현 가능 | 전부 구현 |
| **EXT** | 사용자 실값/계정 필요(서버 IP·Azure ID·Apple 계정·커뮤니티 URL) | "값만 넣으면 동작"까지 코드·CI·docs 완성 + `docs/launcher-release-checklist.md` 절차 명시 |
| **VERIFY** | 기존 구현 실동작 확인만 | 명령으로 검증 후 결과 기록 |
| **POST-1.0** | 1.0 불요·고위험 | 본 plan 범위 밖, 목록만 |

**검증 수단**: ① `dotnet build -c Release`(0/0) ② 오프라인 MC 26.1.2 하니스(`_rptest` — MS로그인·서버 우회, 리소스팩/실행 로드 검증) ③ 유닛테스트 프로젝트 ④ Codex 페어 리뷰 ⑤ 릴리스 CI green + 자산 검증.

**순서 원칙**: P0(로깅 foundation) → P1(차단) → P2(보안/재현) → P3(UX) → P4(품질). P1 이후 항목은 P0 로깅 산출물을 소비.

---

## P0 — 로깅 foundation (선행 필수, 다른 항목의 의존성)

### P0-1. 통합 로깅 기반 [CODE/M]
- 현재: `AppPaths.LogDir` + "로그" 버튼 존재하나 **어떤 코드도 파일에 안 씀**. 게임/ packwiz/auth/update 오류가 휘발.
- 변경: 단일 로깅 유틸 — (a) 런처 단계/오류 → `logs/launcher-YYYYMMDD.log`, (b) packwiz stdout/stderr, (c) 게임 Process stdout/stderr(redirect, UTF-8) → `logs/game-YYYYMMDD.log`. "로그" 버튼이 최신 로그/폴더 염. 회전(최근 N개).
- 소비처(이후 의존): P1-3(update 실패), P1-7(late-crash), P1-8(best-effort), P3-4(에러모달).
- 수용: 정상 실행 후 logs/ 에 단계 로그 + 게임 로그 실내용. 오류 재현 시 stderr 포함. `dotnet test` 로 로거 단위 검증.

---

## P1 — 1.0 차단 (must)

### P1-1. 인증: silent refresh + SessionRefresh + 토큰저장 실패 [CODE/M]
- 현재: MSAL `Interactive()` 만(`CmlLibServices.cs:70-84`) → 매번 브라우저. `SessionRefresh`(enum) 미사용 → 토큰만료 시 launch 직전 크래시. `SaveAccounts()` 실패 무음(`:83`) → silent refresh 무효화.
- 변경: (a) `IAuthService` 에 **refresh/validate seam 추가**(`Contracts.cs:22` — 예: `Task<AuthSession> EnsureValidAsync(ct)` 또는 AuthenticateAsync 에 silent-first 분기) → 캐시 silent 시도 후 Interactive fallback. (b) `LaunchOrchestrator` 가 `proc.Start()` 직전 SessionRefresh 단계로 세션 검증/refresh. (c) `SaveAccounts()` 실패를 로그(P0) + "다음 실행 재로그인 필요" 경고.
- 수용: 2회차 실행 브라우저 없음. seam 으로 만료 토큰 주입 테스트 가능. 저장 실패 시 경고 출현.

### P1-2. 인증 실패 메시지 현행 UI 정합 [CODE/XS]
- 현재: `CmlLibServices.cs:55-58` "오프라인 모드를 켜세요"(제거된 UI 지시).
- 변경: 실행 가능한 안내(설정 문제/관리자 문의/공식 런처)로 교체.
- 수용: dev 빌드에서 불가능 지시 없음.

### P1-3. Velopack update 실패 분기 [CODE/M]
- 현재: 체크/다운로드/적용 예외 전부 "건너뜀"(`CmlLibServices.cs:271-286`) → partial/apply 실패 시 broken 진입.
- 변경: 소스부재(정상 skip) / 네트워크 / partial download / apply 실패 구분. apply 실패 = staging 정리 + 복구 + 안내(P0 로그). 소스부재만 graceful.
- 수용: 유형별 분기 + 로그. dev 미설치 graceful skip 유지.

### P1-4. 단일 인스턴스 락 (MVP) [CODE/S]
- 현재: `Program.cs:14-15` 항상 새 창 → packwiz/servers.dat/Velopack 동시쓰기 race.
- 변경(MVP): named Mutex(`Global\`)/lockfile 로 **중복 writer 방지 + 두 번째는 즉시 종료/안내**. (기존 창 활성화 IPC 는 POST-1.0)
- 수용: 두 번째 실행 즉시 종료 + 안내. 동시 race 제거.

### P1-5. 디스크 공간 사전점검 (양 경로) [CODE/M]
- 현재: 설치 전 여유공간 확인 없음 — **커스텀 경로**(`CmlLibServices.cs:125-147`)와 **공식 런처 경로**(`OfficialLauncherInstaller.cs:77`) 둘 다.
- 변경: 예상 용량 vs `DriveInfo.AvailableFreeSpace` → 부족 시 "N GB 필요" 한국어 사전 안내. 두 경로 공통 유틸.
- 수용: 인위적 부족(임계 상향)에서 양 경로 사전 안내.

### P1-6. 프로세스 stdout/stderr UTF-8 인코딩 (cp949) [CODE/XS]
- 현재: `PackwizService.cs:45-46` encoding 미설정 → 한글 mojibake.
- 변경: `StandardOutputEncoding=StandardErrorEncoding=Encoding.UTF8`. 게임 프로세스(P0)도 동일.
- 수용: 한글 경로/메시지 정상.

### P1-7. 늦은 크래시 진단 (P1 승격) [CODE/M, P0 의존]
- 현재: 90초 이후 비정상 종료도 런처 닫혀 원인 소실(`MainWindowViewModel.cs:144-168`).
- 변경: 종료코드 + game 로그(P0) 위치 + crash report(.ips/hs_err) 경로 표시 + 복사/폴더열기/재시도. 조건 완화(늦은 크래시도 진단 보존).
- 수용: 크래시 시 진단정보 + 로그 접근.

### P1-8. best-effort silent failure 가시화 [CODE/S, P0 의존]
- 현재: ServerList/ClientDefaults 실패 무음(`ServerList.cs:74-77`, `ClientDefaults.cs` catch).
- 변경: 실패를 P0 로그 + info-level 안내(차단 아님). (토큰저장 실패는 P1-1 로 분리)
- 수용: 강제 실패 주입 시 로그/안내.

### P1-9. 공식 런처 자동접속 약속 정합 (P1 승격) [CODE/S]
- 현재: 완료화면 "플레이→서버 자동접속"(`MainWindow.axaml:169-171`)이나 프로필 merge 는 자동접속 인자 미주입(`OfficialLauncherInstaller.cs:226-235`).
- 변경(택1 강제): (A) 프로필에 quickPlay 지원 필드 주입(공식 런처가 지원하면) **또는** (B) 완료화면 문구를 "멀티플레이 목록에서 'Hermaphrodite World' 선택"으로 정정 + servers.dat 항목 보강. 둘 중 검증 가능한 쪽 확정.
- 수용: UI 약속 = 실제 동작.

### P1-10. 서버 상태 ping (Play 전) (P1 승격) [CODE/S]
- 현재: 서버 확인 없이 quickPlay(`CmlLibServices.cs:155-173`). localhost probe(`216-232`) TCP-only false-positive.
- 변경(택1 강제): Play 전 MC Server List Ping(handshake+status)으로 up/버전 확인 → down 이면 **경고 후 진행** 또는 **차단** 중 확정(권장: 경고 후 진행 — "서버 응답 없음, 그래도 실행"). localhost probe 도 MC handshake 로 강화(P3-? 통합).
- 수용: down 시 사전 안내. 비-MC 로컬포트 false-positive 제거.

### P1-11. env/config bake 게이트 (P1 승격) [CODE/S + EXT]
- 현재: 서버IP/AzureID/UpdateURL placeholder 가 사용자 실행경로 노출(`LauncherConfig.cs:20,51`). release bake 미검증 시 placeholder 출하 위험.
- 변경(CODE): release CI 가 **placeholder 잔존 시 빌드 실패**(서버IP·AzureID 이미 일부 있음 — 푸터URL/검증 보강). bake 후 `IsAzureClientConfigured`/서버주소 dry-run 실증(secret bake collision 패턴 점검). (EXT: 실값은 secret).
- 수용: placeholder 출하 0(release). bake 후 IsConfigured=true 실증.

---

## P2 — 보안 / 공급망 / 버전 / 재현 (must·should)

### P2-1. packwiz bootstrap jar 핀 [CODE/S]
- 현재: `releases/latest` 무핀 + 캐시 checksum 미검증(`PackwizService.cs:101-121`).
- 변경: 특정 버전 URL + SHA-256 검증(받은/캐시 둘 다).
- 수용: 해시 불일치 거부·재다운로드.

### P2-2. Fabric loader 버전 pin (양 경로) [CODE/S]
- 현재: `FabricLoaderVersion=""` rolling. 설치 호출 2곳(`CmlLibServices.cs:129`, `OfficialLauncherInstaller.cs:82`) 둘 다 버전 미지정.
- 변경: 테스트 완료 loader 버전 명시 + **양 설치 경로** 호출에 반영.
- 수용: 양 경로 고정 버전.

### P2-3. 토큰 캐시 ACL hardening [CODE/S]
- 현재: `accounts.json` 평문 + ACL 없음(`AppPaths.cs:24-25`).
- 변경(MVP): CmlLib `SaveAccounts()` **직후** user-only ACL(Windows: 현재 사용자; Unix: 0600) 적용 + 실패 시 P0 로그 경고. 전체 DPAPI/Keychain 암호화는 POST-1.0(주석 명시).
- 수용: 저장 후 권한 user-only, 실패 시 경고.

### P2-4. 버전 메타데이터 동기화 [CODE/XS]
- 현재: csproj Version/Assembly/File=0.1.0 정적(`HermaLauncher.csproj:18-20`).
- 변경: CI 가 release tag 로 `-p:Version/-p:AssemblyVersion/-p:FileVersion/-p:InformationalVersion` 주입. 비릴리스 dev 기본.
- 수용: 릴리스 바이너리 속성=태그.

### P2-5. lockfile + global.json (P2 승격) [CODE/S]
- 현재: 재현 빌드용 lock 없음(`HermaLauncher.csproj:31`).
- 변경: `global.json`(SDK 핀) + `packages.lock.json` + CI `restore --locked`.
- 수용: `restore --locked`/`build --no-restore` green.

---

## P3 — UX 완성도 (must·should)

### P3-1. 런처 버전 + 로그인 계정 표시 [CODE/S]
- 변경: 헤더/푸터 버전 표기 + 로그인 계정명 + 로그아웃/계정전환(accounts 초기화).
- 수용: 버전·계정 표시, 로그아웃 동작.

### P3-2. 설정/복구 화면 (1.0 필수만) [CODE/M]
- 현재: 설정 "준비 중" 스텁(`MainWindowViewModel.cs:222-227`).
- 변경(1.0 필수만): RAM(자동감지+수동 override), 계정 초기화, 로그 폴더 열기. (캐시 삭제/모드팩 복구 UI 는 POST-1.0)
- 수용: 3항목 동작 + 영속.

### P3-3. RAM 자동 감지 [CODE/S]
- 변경: 호스트 물리 RAM 감지 → 비율(절반, 2~8GB clamp). 설정 override(P3-2).
- 수용: 4GB·16GB 머신 다른 값.

### P3-4. 에러 모달 품질 [CODE/S, P0 의존]
- 현재: 상태텍스트 `MaxLines=2`(`MainWindow.axaml:144-146`) 잘림.
- 변경: 상세 오류 모달(전문 + 복사 + 로그폴더 열기). (late-crash 수집은 P1-7)
- 수용: 긴 오류 전문 확인.

### P3-5. 네트워크 끊김 UX [CODE/M]
- 변경: 단계별 네트워크 실패 구분 한국어 안내(업데이트/인증/packwiz/Fabric) + 재시도.
- 수용: 네트워크 차단 시 단계 안내.

### P3-6. 푸터 URL — 주입경로 + 빈값 숨김 [CODE/S + EXT(값)]
- 현재: Discord/Guide/Website 빈 const, 주입경로 없음, 버튼 no-op(`LauncherConfig.cs:51`).
- 변경(CODE): `HERMA_DISCORD_URL`/`HERMA_GUIDE_URL`/`HERMA_WEBSITE_URL` env override + release bake. **값 없으면 해당 버튼 숨김**(no-op 버튼 제거). SmartScreen/Gatekeeper 대응 안내 docs.
- 수용: 값 채우면 버튼 표시·동작, 빈값이면 숨김.

---

## P4 — 품질 / 유지보수 (should)

### P4-1. 유닛 테스트 프로젝트 [CODE/M]
- 변경: `launcher/tests/HermaLauncher.Tests`(xUnit) — ClientDefaults(파싱/apply-once/stale/순서/인코딩), Nbt(round-trip/truncation/deep), ServerList(host parse/IPv6), Packwiz(인자/인코딩), 로거(P0), 버전/락.
- 수용: `dotnet test` green.

### P4-2. config drift 테스트 [CODE/S]
- 변경: bake 대상/게이트 회귀 테스트(P1-11/P2-4 의 secret bake collision 점검 자동화).
- 수용: bake 후 IsConfigured dry-run 테스트.

### P4-3. CmlLibServices 테스트 seam만 [CODE/S]
- 변경: 테스트/로깅에 필요한 **seam(인터페이스)만** 추출. **전체 파일 분리는 POST-1.0**(기능없는 대형 리팩터 = 1.0 직전 위험).
- 수용: 빌드 0/0, 호출부 무변경.

### P4-4. Linux 미지원 명시 + 라이선스 고지 [CODE/XS]
- 변경: README/런처 Win/macOS 만 명시. 서드파티 고지(CmlLib/Velopack/Avalonia/packwiz).

---

## EXT 게이트 (사용자 입력 — 코드·CI·docs 완성)
`docs/launcher-release-checklist.md` 에 절차 명시.

| 항목 | 입력 | 코드/CI 상태(목표) |
|---|---|---|
| Azure client ID | MS 승인 ID → secret `HERMA_AZURE_CLIENT_ID` | bake + Guid.Empty 게이트 + P1-11 placeholder 실패 |
| 실 서버 IP | 주소 → secret `HERMA_SERVER_IP` | bake + P1-11 검증 |
| 푸터 URL ×3 | Discord/Guide/Website | P3-6 env/bake + 빈값 숨김 |
| MS 승인 상태 | 승인 확인/기록 | online 경로 구현됨 |
| **macOS 공증** | Apple Developer($99) + Developer ID 8 secret | CI macOS job 완비. **1.0 = Windows 완전 + macOS ad-hoc(우클릭 열기, 문서화); notarized macOS 는 secret 채우면 자동 — EXT 게이트** |

## VERIFY (명령으로 검증)
- modpack-pages: `curl -s https://hermaphrodite-world.github.io/minecraft/pack.toml | head` (deploy success 확인됨).
- UpdateRepoUrl: `gh release list` = 릴리스 레포 일치(동작 확인됨, v0.1.5→0.1.10 업데이트 실증).
- CmlLib FabricInstaller v4: `dotnet build` green + 오프라인 하니스 실행 = 동작.
- ~~macOS DockName~~: **삭제 항목** — 코드가 의도적으로 DockName 미주입(공백 크래시 회피, `CmlLibServices.cs:165`). `MacDockName` const 는 dead → P4 에서 제거(cleanup).

## POST-1.0 (1.0 범위 밖)
- 단일인스턴스 기존창 활성화 IPC, 토큰 DPAPI/Keychain 전체암호화, 설정의 캐시삭제/모드팩복구 UI, CmlLibServices 전체 파일분리, Linux 지원.

> **갱신(v1.0.2 시점)**: 위 중 **기존창 활성화 IPC 는 v1.0.0 에서 구현됨**(SingleInstanceSignal, Windows). 잔여 POST-1.0: DPAPI/Keychain 암호화, 모드팩복구 UI, CmlLibServices 파일분리, Linux. 현행 기능/백로그 SoT 는 [launcher-v1.0-feature-plan.md](launcher-v1.0-feature-plan.md).

---

## 진행 로그
- R1: 초안 → Codex 교차검증 R1(21건) 반영(재우선순위/분리/수용기준/seam/macOS재분류). 다음: R2 교차검증 → 0건 수렴 시 구현 착수.
