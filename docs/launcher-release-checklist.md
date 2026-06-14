# HermaLauncher 1.0 릴리스 체크리스트 (EXT 입력)

> 코드·CI·문서는 "값만 넣으면 동작"하도록 완성됨(UltraPlan P1~P4). 본 문서는 **사용자가 직접 넣어야 하는 실값/계정**과 절차다.
> 게이트: 아래 EXT 입력이 채워지지 않으면 release 빌드가 placeholder 출하를 막는다(P1-11). 채우면 자동 동작.

## 1. 온라인 로그인 (Azure 앱 + Microsoft 승인) — 필수

런처는 온라인 전용(MS 정품 인증)이다. 미설정 시 로그인 단계에서 차단된다.

1. **Azure 앱 등록**: Azure Portal → 앱 등록 → 새 등록.
   - 리디렉션 URI: `http://localhost` (public client / loopback).
   - "Live SDK 지원" / public client 허용.
   - 발급된 **Application (client) ID** 복사.
2. **Minecraft(Mojang) 승인**: Azure 앱이 Minecraft 서비스(`api.minecraftservices.com`)를 호출하려면 Microsoft/Mojang 승인이 필요. 미승인이면 **HTTP 403**. 승인 신청 후 완료 확인.
3. **GitHub Secret 등록**: 레포 Settings → Secrets and variables → Actions → `HERMA_AZURE_CLIENT_ID` = 위 client ID.
4. **검증**: release 생성 → CI 의 "Bake Azure client ID" 스텝이 placeholder(`00000000-...`)를 치환. 빌드 실패(placeholder 잔존) 없어야 함. 실행 시 브라우저 MS 로그인 정상.

> 게이트(P1-11): `IsAzureClientConfigured` 는 `Guid.Empty` 의미 비교라 bake collision 없음. placeholder 잔존 시 release CI 실패.

## 2. 실 서버 주소 — 필수

1. 실제 Minecraft 서버 주소(공개 IP 또는 도메인) 확정.
2. **GitHub Secret**: `HERMA_SERVER_IP` = 주소(포트는 기본 25565, 다르면 `HERMA_SERVER_PORT`).
3. **검증**: release CI "Bake server IP" 가 `play.example.com` 치환. PLAY 시 해당 서버로 quickPlay 자동접속. 서버 호스트 본인은 localhost 자동감지(P1-10).

> 서버 주소는 비밀이 아님(바이너리에 포함). 보안은 server.properties 화이트리스트/online-mode/방화벽으로.

## 3. 커뮤니티 링크 (푸터 URL) — 선택(있으면 권장)

값이 없으면 해당 푸터 버튼은 **숨겨짐**(no-op 제거, P3-6).

1. Discord 초대 URL / 가이드 페이지 URL / 웹사이트 URL 확정.
2. **GitHub Secret(또는 LauncherConfig bake)**: `HERMA_DISCORD_URL`, `HERMA_GUIDE_URL`, `HERMA_WEBSITE_URL`.
3. **검증**: 값 채우면 버튼 표시·동작, 빈값이면 숨김.

## 4. macOS 공증 (Apple Developer) — macOS 배포 시 필수

미설정 시 1.0 의 macOS 산출물은 **ad-hoc 서명(미공증)** → 친구 Mac 에서 우클릭→열기(Gatekeeper 경고 통과) 필요. 공증하려면:

1. **Apple Developer Program 가입**($99/년).
2. **Developer ID 인증서 2종**(Application + Installer) 발급 → 각 `.p12` 내보내기(openssl `-legacy` 필수 — `dotnet-selfcontained-macos-app-adhoc-codesign-deep` 참조).
3. **GitHub Secrets 8개**: `APPLE_CERT_P12_BASE64`, `APPLE_INSTALL_CERT_P12_BASE64`, `APPLE_CERT_PASSWORD`, `APPLE_SIGN_IDENTITY`, `APPLE_INSTALL_SIGN_IDENTITY`, `APPLE_ID`, `APPLE_TEAM_ID`, `APPLE_APP_PASSWORD`.
4. **검증**: release 의 macOS job `signcheck` 게이트가 활성 → notarytool 자동 공증 → `.pkg` staple 검증. (첫 공증은 Apple 큐 ~52분 가능 — 락 아님. `docs/macos-release-signing.md`)

> 1.0 결정: **Windows 완전 출시 + macOS ad-hoc(문서화된 우클릭-열기)** 로 출시 가능. 공증 secret 채우면 자동 승격(EXT 게이트).

## 5. MS 승인 상태 기록 — 필수

- §1.2 승인 완료 여부/일자를 본 문서 또는 `docs/online-login-setup.md` 에 기록.
- 미승인 상태로 1.0 출시 시 로그인 불가(HTTP 403) → 출시 차단.

---

## 릴리스 직전 최종 점검 (VERIFY)
- [ ] `gh secret list` 에 §1~4 secret 등록 확인
- [ ] release(tag) 생성 → CI green + 자산(win-Setup.exe, releases.win.json, [macOS 공증 시 .pkg]) 첨부
- [ ] placeholder 출하 0 (CI bake 검증 통과)
- [ ] 실기 1회: Windows 자동업데이트 + PLAY 전체 흐름, (macOS arm64) 실행·창 포커스
- [ ] modpack Pages 라이브(`curl pack.toml`) + packwiz 동기화 정상
