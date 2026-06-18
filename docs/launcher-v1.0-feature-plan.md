# HermaLauncher v1.0.0 기능 계획 + 진행 상태

> 작성: 2026-06-15 · 근거: `/analyze` + 검증 워크플로(4 lens 31 후보) + 카톡 접속 이슈
> 원칙: 이 런처의 1.0 은 기능 breadth 가 아니라 **신뢰감·회복성·운영자 소통** 3축. friends-only / one-click / single-server 스코프 고정.
> 시간 환산 금지(사이즈 XS~XL만). UI 변경 항목은 **육안 스모크 사용자 영역**(빌드 컴파일 바인딩만 검증됨).

---

## A. 접속 이슈 (서버 호스트가 별도 PC에서 플레이) — ✅ 구현 완료

### 상황 / Root Cause

- 이재석(서버 운영자): 서버는 **맥**(LAN `192.168.219.102`)에서 돌리고, 마크는 **같은 집의 다른 Windows PC**에서 한다.
- 기존 런처 접속 해석은 두 경우만 처리: (1) `127.0.0.1`(서버를 켠 바로 그 PC) → (2) 공개 `ServerIp`(일반 친구).
- 이재석의 Windows PC는 (1) localhost probe 실패(로컬에 서버 없음) → (2) 공개 IP 폴백 → **NAT 헤어핀 미지원**으로 같은 LAN에서 공개 IP 접속 실패.
- 그는 실제로 **LAN IP `192.168.219.102` 직접 입력**으로만 접속 가능. 런처엔 이 "같은 LAN, 다른 PC" 경로가 없었다.

### 수정 내용

1. **`LauncherSettings.ServerHostOverride`** (신규 필드, settings.json 영속) — '서버 주소 직접 입력'.
2. **`ServerHostResolver`** (신규 순수 클래스, 단위 테스트) — 우선순위 결정: **override → localhost 감지 → 공개 IP**. + `Normalize`(공백/scheme/슬래시 정리).
3. **`ResolveServerHostAsync` 재작성** ([CmlLibServices.cs](launcher/src/HermaLauncher/Services/CmlLibServices.cs)) — override 있으면 최우선 사용(ping 실패해도 명시 선택 존중, 경고만). override 없으면 기존 동작 유지.
4. **사전 감지 + 안내** — 공개 IP가 응답 안 하면 경고 메시지에 *"같은 집·네트워크에서 서버를 켰다면 설정의 '서버 주소 직접 입력'에 서버 PC의 IP를 넣어 주세요"* 추가 (운영자가 원한 "접속 안 되는 걸 감지 + 안내").
5. **설정 UI** ([MainWindow.axaml](launcher/src/HermaLauncher/Views/MainWindow.axaml)) — "서버 주소 직접 입력(고급)" 텍스트박스 + VM 배선(`SaveSettings`가 RAM·서버주소 두 필드 한 객체로 저장 → 한쪽이 다른 쪽을 지우지 않음).

**이재석 해결법**: 런처 → 설정 → '서버 주소 직접 입력'에 `192.168.219.102` 입력 → 저장. 이후 PLAY가 그 IP로 자동 접속.
**검증**: build 0/0, 단위테스트(resolver 11 + settings 3) 통과. **육안 스모크 필요**: 설정 화면에 필드 표시/입력/저장 1회.

---

## B. v1.0 신뢰·회복 묶음 — 구현 상태별

### ✅ 이번 세션 구현 완료 (build 0/0, test 47/47)

| 기능 | 상태 | 내용 |
|---|---|---|
| **서버 주소 직접 입력** | ✅ + 🔬스모크 | 위 A. |
| **원클릭 진단 ZIP 번들** | ✅ + 🔬스모크 | [DiagnosticsBundle.cs](launcher/src/HermaLauncher/Services/DiagnosticsBundle.cs) — `herma-진단-{시각}.zip`(launcher/game 로그 최신 6개 + system-info.txt) 생성 후 폴더 열기. 설정에 '진단 파일 만들기' 버튼. 크래시 메시지가 이 버튼을 안내(약속-구현 갭 해소). |
| **스마트 실패 진단(크래시)** | ✅ + 🔬스모크 | [FailureDiagnosis.cs](launcher/src/HermaLauncher/Services/FailureDiagnosis.cs) — game-*.log를 읽어 OOM/화이트리스트/세션만료/모드불일치/연결실패를 한 가지 한국어 액션으로 분류. exit≠0 크래시 분기에서 StatusMessage로 안내. `AppLog.LatestGameLogPath()` 추가. |
| **전용 한국어 로그인 오류(XSTS)** | ✅ + 테스트 | [XboxLoginError.cs](launcher/src/HermaLauncher/Services/XboxLoginError.cs) — XErr(미성년 2148916238 / 지역 235 / Xbox프로필부재 233 / 성인인증 236·237 / 밴 227 / 보호자 229) → 한국어 안내. **XboxAuthNet 예외 계약을 어셈블리 리플렉션으로 검증**(XErr 출처 = `XboxAuthException` Error/ErrorMessage/Redirect) 후 매핑. [CmlLibServices.cs](launcher/src/HermaLauncher/Services/CmlLibServices.cs) catch 에 `ExtractXErr`(AggregateException/InnerException 트리 스캔) 배선. 매핑·추출 단위테스트 14. |
| **2번째 실행 시 기존 창 활성화** | ✅(Win) + 🔬스모크 | [SingleInstanceSignal.cs](launcher/src/HermaLauncher/Services/SingleInstanceSignal.cs) — named EventWaitHandle 신호(**Windows 전용**, 비-Windows no-op·기존 silent-exit 유지). [Program.cs](launcher/src/HermaLauncher/Program.cs) 2번째 인스턴스가 신호 → 1번째가 `App.ActivateMainWindow`(복원+Activate+Topmost 토글). **IPC 메커니즘은 단위테스트로 검증**(같은 프로세스 신호→콜백). **창 전면화 시각 동작은 런타임 스모크 필요**. 전 경로 try/catch fail-safe(최악=기존 동작). |

> 🔬스모크 = 코드/바인딩은 빌드 검증됨, **시각 레이아웃/포커스는 사용자가 런처 실행해 1회 육안 확인 필요**(§ UI Breakage / 메모리 선호 정합).

### ⏳ 남은 HIGH (다음 작업 후보)

| 기능 | 크기 | 비고 / 선행 조건 |
|---|---|---|
| **정직한 다단계 진행(단계 N/총)** | S | **보류 결정**: 실제 emit 되는 stage 가 Update→Auth→Java→Packwiz→Launch(약 5개)뿐이고 SessionRefresh 무음·Fabric 은 Java 에 흡수돼, 고정 "N/7" 은 오라벨 위험. 현재 메시지도 이미 한국어 친화적이라 ROI 낮음 → 실 런타임 stage 시퀀스 확정 후 진행 권장. |

### ✅ MEDIUM — 구현 완료 (build 0/0, test 76/76)

| 기능 | 상태 | 내용 |
|---|---|---|
| **전송 단계 자동 재시도+백오프** | ✅ + 테스트 | [RetryPolicy.cs](launcher/src/HermaLauncher/Services/RetryPolicy.cs) — 지수 백오프(순수 util). [PackwizService.cs](launcher/src/HermaLauncher/Services/PackwizService.cs) bootstrap 다운로드에 배선: 5xx/연결끊김=재시도(최대 3회), 4xx/무결성 실패/취소=즉시 실패. 단위테스트 5(성공/재시도/구조적즉시실패/소진/취소). |
| **메인 화면 라이브 서버 상태 pill** | ✅(파싱 테스트) + 🔬스모크 | [ServerStatus.cs](launcher/src/HermaLauncher/Services/ServerStatus.cs) status JSON 파싱(players.online/max + MOTD 문자열·컴포넌트, 테스트 8) + [ServerPing.QueryStatusAsync](launcher/src/HermaLauncher/Services/ServerPing.cs)(전체 JSON read, 기존 IsServerUpAsync launch 경로는 불변). VM 30초 타이머(디자이너/테스트 미가동)로 "🟢 온라인 · N/M명 / 🔴 오프라인" 칩 표시. **실 ping 결과·칩 렌더는 런타임 스모크 필요.** |
| **첫 실행 환영 1화면** | ✅ + 🔬스모크 | [MainWindowViewModel](launcher/src/HermaLauncher/ViewModels/MainWindowViewModel.cs) AppView.Welcome + `LauncherSettings.HasSeenWelcome`(첫 실행 1회). '시작하기'→저장 후 Main. **SaveSettings 를 load-modify-save 로 리팩터**(HasSeenWelcome 등 VM 미추적 필드 보존 — 회귀 테스트 2). 화면 렌더 스모크 필요. |
| **공지/패치노트 패널 + 점검 배너** | ✅(파싱 테스트) + 🔬스모크 | [NewsFeed.cs](launcher/src/HermaLauncher/Services/NewsFeed.cs)(파싱 테스트 10) + [NewsService.cs](launcher/src/HermaLauncher/Services/NewsService.cs)(원격 fetch best-effort) + `LauncherConfig.NewsUrl`(env `HERMA_NEWS_URL`). **기본 OFF(미설정 시 graceful 숨김 — 푸터 링크 패턴)** → 운영자가 GitHub Pages 에 `news.json` 올리고 env 주입 시 활성. 메인 헤더에 점검(빨강)·공지(시안) 배너. 실 fetch·렌더 스모크 필요. |

### ⏳ MEDIUM — 남음 (신중 진행 — 런타임 검증 필요)

- **Windows 토큰 DPAPI 암호화** (M) — **보류**: 토큰 캐시(accounts.json)는 XboxAuthNet 이 직접 평문 JSON 으로 읽고 쓴다. 파일을 암호화하면 XboxAuthNet read 가 깨지므로 read 전 복호/ write 후 암호화 wrap 이 필요한데, **실패 시 로그인 자체가 깨진다(전 사용자 재로그인). 런타임 인증 검증 불가 상태에서 blind ship 금지.** %APPDATA% ACL 이 이미 동일 사용자 보호를 제공(기존 결정). 실 인증 검증 가능 시 진행.
- **실패-제안 모드팩 복구** (M) — **보류**: mods/ + 마커 삭제로 재동기화 유도하나, **packwiz-installer 의 설치 manifest 를 함께 비워야 실제 재다운로드가 일어나는지(아니면 "이미 설치됨"으로 no-op)** 를 런타임으로 확인해야 함(다운로드-존재판정 stub masking 위험). manifest 위치/동작 실측 후 진행.
- **손상 파일 자가복구 surface** (S) — ServerList 가 이미 손상 시 .bak 백업 후 재작성으로 auto-recover 중 → 수동 복원 surface 는 한계효용 낮음. 보류.
- **공지 unread 배지 / 본문 상세** (S) — 현재는 최신 공지 제목 배너만. `LastSeenNewsId` 비교 unread 표시 + 본문 detail 은 후속.

### LOW

모드 무결성 재검증 · 자가 점검 화면 · 모드 변경 내역 · 계정 전환 · 업데이트 후 1줄 알림 · SmartScreen 클릭스루 안내.

### User Decision (운영자 product/cost 판단)

- **서버 오프라인 시 last-known-good 진입** — fail-open(모드 어긋나 튕길 수 있음) vs fail-closed. 서버 online-mode 정책 의존.
- **macOS Keychain 암호화** — 이미 0600+샌드박스 밖이라 DPAPI 대비 ROI 낮음.
- **Linux 빌드** — AppPaths에 XDG 분기는 있으나 build yml job·Velopack Linux 검증 필요 + 리눅스 친구 존재 여부.

### 의도적 제외 (out of scope)

상시 노출 '캐시 삭제' 버튼 · 독립 긴급 broadcast(공지로 흡수) · CmlLibServices 파일 분할(리팩터).

> ~~게임 중 트레이/토스트 알림~~ → **v1.2.0 에서 구현**(트레이로 숨기기 + 친구 접속 시 Win/macOS 네이티브 토스트, 게임 중 트레이 상주). 트레이/토스트 런타임은 GUI 필요라 실기기 육안 스모크 권장.

---

## 변경 파일 요약 (이번 세션)

신규(서비스): `ServerHostResolver.cs`, `FailureDiagnosis.cs`, `DiagnosticsBundle.cs`, `XboxLoginError.cs`, `SingleInstanceSignal.cs`, `RetryPolicy.cs`, `ServerStatus.cs`, `NewsFeed.cs`, `NewsService.cs`.
신규(테스트): `ServerHostResolverTests`, `FailureDiagnosisTests`, `DiagnosticsBundleTests`, `XboxLoginErrorTests`, `SingleInstanceSignalTests`, `RetryPolicyTests`, `ServerStatusTests`, `NewsFeedTests`.
수정: `Services/LauncherSettings.cs`(+ServerHostOverride), `Services/AppLog.cs`(+LatestGameLogPath), `Services/CmlLibServices.cs`(ResolveServerHostAsync + XSTS catch), `Services/PackwizService.cs`(bootstrap 다운로드 retry), `Services/ServerPing.cs`(+QueryStatusAsync), `ViewModels/MainWindowViewModel.cs`(서버주소·진단·크래시힌트·상태 pill 타이머), `Views/MainWindow.axaml`(서버주소 필드·진단 버튼·상태 칩), `Program.cs`(활성화 신호), `App.axaml.cs`(ActivateMainWindow), `LauncherSettingsSaveTests.cs`(+3).

**검증**: `dotnet build` 경고 0/오류 0, `dotnet test` **88/88 통과**. XboxAuthNet XSTS 예외 계약은 어셈블리 리플렉션으로 사전 검증.
**미검증(사용자 영역, 런타임 스모크 필요)**: ① 설정 화면 신규 UI 육안(서버주소 필드·진단 버튼) ② 실제 LAN에서 override 접속 e2e ③ 실제 크래시 로그 분류기 정확도 ④ 2번째 실행 시 창이 실제로 전면화되는지(IPC 신호 전달은 테스트 검증됨, 창 포커스 시각 동작은 미검증) ⑤ 실제 XSTS 거부 계정으로 한국어 메시지 표출.
