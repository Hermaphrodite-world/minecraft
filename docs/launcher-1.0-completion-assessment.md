# 런처 1.0 완성도 평가 (객관·비관적 다관점)

> ⚠️ **이 문서는 v0.1.x 하드닝 시점(de7d6b1)의 스냅샷이다.** 이후 정식 v1.0.0~**v1.0.2** 배포 완료(신뢰·회복 기능군 추가, Win+macOS 공증 자동 업데이트). 현행 기능/검증 SoT 는 [launcher-v1.0-feature-plan.md](launcher-v1.0-feature-plan.md). 아래 본문은 당시 판정 기록으로 보존.
>
> 작성 기준: main @ `de7d6b1` (P3+P4 머지 후). build 0/0, test 16/16.
> 목적: "1.0 완성되었는가"를 낙관 배제하고 여러 관점에서 정직하게 판정.

## 한 줄 결론

**코드 1.0 완성 + 하드닝 완료 + 이미 출시 중(v0.1.10, Win+macOS공증).** 남은 것은 (a) 선택적 푸터 URL(코스메틱), (b) **P3 신규 UI 육안 스모크 1회**, (c) P3/P4 반영 신규 릴리스 컷. 코드 갭은 없음.

---

## 관점 1 — 코드 완성도 (UltraPlan P0~P4)

| 단계 | 내용 | 상태 |
|---|---|---|
| P0 | 로깅 기반(AppLog, 로테이션, 게임로그 캡처) | ✅ |
| P1 | blocker(silent auth, session refresh, single-instance, disk, server ping, crash 진단, 공식설치 문구, SecureFile) | ✅ |
| P2 | 보안/재현(packwiz pin+SHA, Fabric pin, ACL, 버전주입, global.json, OCE, semver gate) | ✅ |
| P3 | UX(RAM 자동, 설정화면, 계정/로그아웃, 푸터정리, 버전표시, 오류 로그) | ✅ |
| P4 | 품질(16 단위테스트, config-drift CI 게이트, 라이선스, dead const) | ✅ |

각 단계 Codex 페어 리뷰 반영. **판정: 1.0 스코프 코드 완성.**

## 관점 2 — 런타임 검증 (가장 비관적으로 봐야 할 지점)

- **증명됨**: 리소스팩 적용(오프라인 MC 하니스 실측), build 0/0, test 16/16, **사용자 실기 v0.1.10 7팩 로드 확인**(코어 런치 실동작 증거).
- **이번 세션 미증명**: auth→install→packwiz→launch→server 전체 e2e 를 실 계정/실 서버로 재실행하지 않음(통합 API 시그니처 검증 + 과거 실기 동작에 의존).
- **P3 신규 UI 미검증**: 설정화면·계정표시·RAM 슬라이더·푸터 숨김은 **컴파일 바인딩 검증만**(Avalonia 컴파일 바인딩이 타입/경로 오류는 차단). **육안 레이아웃 검증 안 함** → § UI Breakage Definition 상 "정상" 단정 불가.

**판정: 코어 런치=실기 증명, P3 UI=빌드검증만(육안 스모크 1회 필요).**

## 관점 3 — 외부 입력(EXT) — 객관 확인(`gh secret list`)

| 항목 | 상태 | 근거 |
|---|---|---|
| Azure client ID + MS 승인 | ✅ | `HERMA_AZURE_CLIENT_ID` set(06-12), v0.1.x 릴리스 정상(bake 통과) |
| 실 서버 주소 | ✅ | `HERMA_SERVER_IP` set(06-10) |
| Apple Developer 공증 | ✅ | Apple secret 8종 set(06-12), v0.1.10 자산에 `osx-Setup.pkg`(공증 .pkg) |
| 푸터 URL(디스코드/가이드/웹) | ❌ 선택 | secret 미등록 → 버튼 graceful 숨김(P3-6). 코스메틱. |

**판정: 필수 EXT 전부 완료. 남은 건 선택적 푸터 URL뿐.**

## 관점 4 — 플랫폼/배포

- **Windows**: 빌드+실기 증명, Velopack 자동업데이트 10릴리스 검증(v0.1.5→v0.1.10).
- **macOS**: 빌드+공증 .pkg 출시, DockName 크래시는 실기에서 발견·수정됨(macOS 실행 이력 존재).
- **Linux**: 미지원(문서 명시).
- **신규 CI dotnet test 게이트**: 아직 실 릴리스에서 미실행(다음 릴리스가 첫 실행). 로컬 Release 16/16 통과라 저위험.

**판정: Win 프로덕션 준비. macOS 공증 출시 준비.**

## 종합 비관 판정 — 남은 실제 작업

1. **P3 신규 UI 육안 스모크 1회** (설정 진입→계정/RAM 슬라이더/로그폴더→저장→복귀; 오류 시 '로그 보기'). — 빌드는 통과, 시각만 미확인.
2. **푸터 URL**(선택): 디스코드/가이드/웹 secret 채우면 버튼 표시.
3. **v0.1.11 릴리스 컷**: P3/P4 를 사용자에게 배포(자동업데이트). 모든 필수 secret set 상태라 CI green 예상.
4. **(권장) 신규 릴리스 후 실기 1회**: 자동업데이트 적용 + PLAY 전체 흐름 + 설정화면 육안.

> **결정 필요(아웃바운드)**: 신규 릴리스는 전 사용자 자동업데이트를 트리거. P3 UI 육안 스모크 없이 바로 컷할지(빌드+테스트 검증 신뢰), 스모크 후 컷할지는 사용자 판단.

## Codex 종합 적대적 리뷰 (결과 통합)

전체 런처를 6개 차원(런치흐름/동시성/보안/CI/UX/테스트)에서 적대적·비관적으로 리뷰. **ship-blocker 1건** + RISK 다수. 1.0 관련 항목은 P5 에서 즉시 반영(아래), 구조개선/EXT 는 POST-1.0 로 분류.

### P5 에서 수정 완료 (build 0/0, test 23/23)

| 출처 | 이슈 | 수정 |
|---|---|---|
| **S1 ship-blocker** | 진행 중 창 닫기가 취소토큰 미연결 → packwiz/java 고아 프로세스 | `Window.Closing` → busy 시 `CancelOngoing()`(=_cts 취소 → ct.Register 동기 kill) |
| Launch-R1 | Fabric/게임 설치 실패가 generic "알 수 없는 오류"로 | EnsureJavaAsync 를 `LaunchStageException(Java)` 로 래핑(네트워크/디스크 안내) |
| UX-R1 | 설정 저장 실패해도 "저장했어요" 표시 | `LauncherSettings.Save()` bool 반환 + 실패 메시지 + 설정화면 유지 |
| UX-R2 | 크래시 안내가 launcher log 만 염(게임 로그 아님) | 오류 버튼 '로그 열기' → 로그 **폴더**(launcher-*.log + game-*.log) |
| CI-R3 | 서명 release 에 .pkg 없으면 경고 후 업로드 | .pkg 없음 → `exit 1`(깨진 릴리스 차단) |
| Test-R1 | 런치 orchestration 무테스트 | `IPackwizService` 추출 + 4 테스트(순서/단축회로/취소/단계오류) |
| Test-R3 | Save 실패 UX 무테스트 | path 주입 + 3 테스트(round-trip/쓰기실패 false/누락 기본값) |

> CI-R2(서명 secret 누락 fail-fast)는 이미 구현돼 있음(launcher-build.yml MISSING 체크). Codex 가 해당 라인 미관측.

### POST-1.0 / EXT 로 분류 (1.0 차단 아님 — 근거 명시)

| 출처 | 이슈 | 분류 사유 |
|---|---|---|
| Concurrency-R1 | `_fabricVersionId` mutable state | IsBusy 게이트로 single-flight 보장 성립. launch-context 리팩토링은 구조개선(POST-1.0) |
| Concurrency-R2 | Progress 이중 dispatch stale | 종료 직후 희귀 race. attempt-id 도입은 POST-1.0 |
| Security-R1 | Windows 토큰 DPAPI 미적용 | 의도적 deferral. %APPDATA% 기본 ACL = 동일 사용자 보호. 전체 암호화는 POST-1.0(기존 결정) |
| Launch-R2 | 긴 설치 후 토큰 만료 deep 처리 | best-effort revalidate 로 대부분 커버. 만료 시 MC 가 자체 안내. POST-1.0 |
| Launch-R3 | 서버다운 경고가 성공 메시지에 덮임 | 경고는 로그 보존 + MC 가 quickPlay 실패 자체 표시. минор |
| UX-R3 | 상태 메시지 2줄 ellipsis 절단 | '로그 열기'로 전체 확인 가능(완화됨) |
| UX-N1 | 2번째 실행 silent 종료 | activate IPC 는 POST-1.0(중복 실행 방지는 동작) |
| CI-R1 | Windows 미서명(SmartScreen 경고) | **EXT** — Authenticode 인증서 구매(사용자 결정). macOS ad-hoc 과 동일 정책 |
| Test-R2/R4 | packwiz/공식설치 통합테스트 부재 | process/fs 주입 필요. POST-1.0 |

### Codex 정직 판정 + 본 평가의 종합

Codex 1차 판정은 "No(ship-blocker S1 등 미해결)"였으나, **그 ship-blocker(S1) 및 지적된 1.0 RISK 를 P5 에서 전부 반영**했다. 잔여는 모두 POST-1.0 구조개선 또는 EXT(인증서 구매) 로, 코드 1.0 차단 요소는 없다.

**최종 종합 판정**: 런처는 **1.0 코드 완성 + 적대적 리뷰 반영 완료**. 실 출시 전 남은 것은 (1) **P3 신규 UI 육안 스모크 1회**(제가 GUI 실행 불가 — 사용자 영역), (2) v0.1.11 릴리스 컷(아웃바운드 결정), (3) 선택적 푸터 URL. 비관적으로 보아도 **코드/CI/테스트 측 1.0 차단 요소 0건**.
