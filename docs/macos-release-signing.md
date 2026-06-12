# macOS 코드 서명 + 공증 설정 (메인테이너용)

> 이 문서는 **릴리스를 발행하는 사람**이 Herma Launcher 의 macOS 자동 업데이트를 켜기 위한 1회 설정 가이드입니다.
> 친구용 설치 안내는 [docs/macos-setup.md](macos-setup.md) 를 보세요.

## 무엇을, 왜

macOS 의 Velopack 자동 업데이트는 **코드 서명 + 공증(notarization)** 이 사실상 필수입니다.
Gatekeeper 가 미서명 `.app` 의 in-place 교체를 막고, 미공증 앱은 친구가 "우클릭→열기"를 해야 합니다.

`vpk pack` (osx) 는 한 번에 세 가지를 만듭니다:

- `.app` 번들 → **Developer ID Application** 인증서로 codesign
- `.pkg` 설치 파일 → **Developer ID Installer** 인증서로 productsign
- portable `.zip`

그래서 인증서가 **2개** 필요합니다(둘 다 **하나의 개인키**로 발급 가능). 공증은 **앱 암호(app-specific password)** 로 합니다.
vpk 가 .NET 용 hardened-runtime entitlements 를 자동 적용하므로 entitlements 파일은 만들 필요 없습니다.

설정이 끝나면 GitHub Secrets 7개를 등록하고, 릴리스를 발행하면 `.github/workflows/launcher-build.yml` 의 macos 잡이
자동으로 서명·공증·업로드합니다. Secrets 가 없으면 기존 ad-hoc 배포로 graceful 하게 fallback 합니다.

---

## Part A — Developer ID 인증서 2개 만들기 (Windows, git-bash + openssl)

Mac 없이 openssl 로 개인키·CSR 을 만들고, Apple 포털에서 인증서를 발급받아 `.p12` 로 조립합니다.
(git-bash 에 openssl 이 포함돼 있습니다. 작업 디렉토리는 임의의 빈 폴더에서.)

```bash
# A-1. 개인키 2개 — Apple 은 한 CSR 으로 인증서 1개만 발급(재사용 거부)하므로 인증서별 별도 키·CSR 이 필요.
#      (같은 키로 CSR 을 다시 만들어도 RSA 서명이 결정적이라 바이트가 동일 → Apple 이 또 거부. 키부터 따로.)
openssl genrsa -out herma_dev_id.key 2048       # Developer ID Application 용
openssl genrsa -out herma_installer.key 2048    # Developer ID Installer 용

# A-2. CSR 2개 (각 키로 1개씩)
#      ※ Git Bash(MSYS) 는 -subj 의 맨 앞 '/' 를 Windows 경로로 변환(C:/Program Files/Git/...)해
#        "subject name is ... not in that format" 에러를 낸다. MSYS_NO_PATHCONV=1 로 변환을 끈다.
MSYS_NO_PATHCONV=1 openssl req -new -key herma_dev_id.key -out herma_dev_id.csr \
  -subj "/emailAddress=oharapass@gmail.com/CN=Herma Launcher App/C=KR"
MSYS_NO_PATHCONV=1 openssl req -new -key herma_installer.key -out herma_installer.csr \
  -subj "/emailAddress=oharapass@gmail.com/CN=Herma Launcher Installer/C=KR"
```

**Apple Developer 포털에서 인증서 2개 발급** (https://developer.apple.com/account → Certificates):

1. `+` → **Developer ID Application** → (G2 Sub-CA 기본) → `herma_dev_id.csr` 업로드 → `developerID_application.cer` 다운로드
2. `+` → **Developer ID Installer** → `herma_installer.csr`(다른 CSR) 업로드 → `developerID_installer.cer` 다운로드

> 같은 CSR 을 두 번 올리면 "An attribute specified more than once / already been used" 에러가 납니다 — 반드시 별도 CSR.

> Developer ID 인증서 발급은 **Account Holder** 권한이 필요합니다(계정 소유자 본인이면 OK).

```bash
# A-3. .cer(DER) → PEM 변환
openssl x509 -inform DER -in developerID_application.cer -out app.pem
openssl x509 -inform DER -in developerID_installer.cer  -out installer.pem

# A-4. 식별자 문자열 확인 — 출력된 CN 에서 Secret 값을 만든다.
openssl x509 -in app.pem       -noout -subject
#   예) subject= ... CN=Developer ID Application: TaeGyum Kim (ABCDE12345) ...
openssl x509 -in installer.pem -noout -subject
#   예) subject= ... CN=Developer ID Installer: TaeGyum Kim (ABCDE12345) ...
#
#   ※ Velopack 은 식별자 인자에 **(TEAMID) 괄호 부분을 빼라**고 권고한다(Velopack signing docs).
#      → Secret 값은 괄호 앞까지: "Developer ID Application: TaeGyum Kim"
#                                  "Developer ID Installer: TaeGyum Kim"
#      Application·Installer 는 접두어가 달라 괄호 없이도 유일하게 매칭된다.
#      (정확한 매칭 문자열은 첫 릴리스 CI 로그의 `security find-identity` 출력으로 확정 — workflow 가 자동 출력)

# A-5. Apple 중간 인증서(체인 완성용 — 러너에 없을 때 대비, 권장)
curl -fL -o DeveloperIDG2CA.cer https://www.apple.com/certificateauthority/DeveloperIDG2CA.cer
openssl x509 -inform DER -in DeveloperIDG2CA.cer -out intermediate.pem

# A-6. .p12 2개 조립 (각 키 + leaf + 중간 인증서). 두 .p12 비밀번호는 동일하게(= APPLE_CERT_PASSWORD 하나로 공유).
#      먼저 기본(non-legacy). openssl 3.x 기본은 AES-256-CBC/PBKDF2.
openssl pkcs12 -export -out herma_app.p12 \
  -inkey herma_dev_id.key -in app.pem -certfile intermediate.pem \
  -name "Herma Dev ID App" -passout pass:CHANGE_THIS_P12_PASSWORD
openssl pkcs12 -export -out herma_installer.p12 \
  -inkey herma_installer.key -in installer.pem -certfile intermediate.pem \
  -name "Herma Dev ID Installer" -passout pass:CHANGE_THIS_P12_PASSWORD
#      → 첫 릴리스에서 CI 의 `security import` 가 실패하면(구 macOS keychain 이 AES p12 거부),
#        두 .p12 모두 -legacy(RC2/3DES 계열) 플래그를 추가해 재생성하고 Secret 을 갱신.

# A-7. base64 인코딩 → APPLE_CERT_P12_BASE64 / APPLE_INSTALL_CERT_P12_BASE64 값
base64 -w0 herma_app.p12       > herma_app.p12.b64
base64 -w0 herma_installer.p12 > herma_installer.p12.b64
#  (PowerShell 대안: [Convert]::ToBase64String([IO.File]::ReadAllBytes("herma_app.p12")) | Set-Content herma_app.p12.b64 -NoNewline)
```

> `CHANGE_THIS_P12_PASSWORD` 는 임의의 비밀번호로 바꾸세요(두 .p12 동일 = `APPLE_CERT_PASSWORD` Secret 값).
> `*.key` / `*.p12` 는 **절대 커밋하지 말 것**. Secret 등록 후 로컬 파일은 안전하게 보관/삭제.

---

## Part B — 공증용 앱 암호(app-specific password)

1. **Team ID 확인**: https://developer.apple.com/account → Membership details → **Team ID** (10자, 예: `ABCDE12345`).
   - A-4 에서 본 CN 의 괄호 안 값과 동일합니다.
2. **앱 암호 생성**: https://account.apple.com → 로그인 및 보안 → **앱 암호** → `+` → 이름 `herma-notary`
   → 생성된 `xxxx-xxxx-xxxx-xxxx` 복사 (= `APPLE_APP_PASSWORD`).
3. `APPLE_ID` = Apple Developer 로그인 이메일 (`oharapass@gmail.com`).

> 앱 암호 방식은 App Store Connect API 키(.p8)보다 단계가 적어 권장합니다. notarytool 이 공식 지원합니다.

---

## Part C — GitHub Secrets 등록 (8개)

레포 → **Settings → Secrets and variables → Actions → New repository secret** 로 아래를 등록:

| Secret | 값 | 용도 |
|---|---|---|
| `APPLE_CERT_P12_BASE64` | `herma_app.p12.b64` 파일 내용 | Application 인증서+키 (.app) |
| `APPLE_INSTALL_CERT_P12_BASE64` | `herma_installer.p12.b64` 파일 내용 | Installer 인증서+키 (.pkg) |
| `APPLE_CERT_PASSWORD` | A-6 의 `.p12` 비밀번호 (두 .p12 공유) | .p12 복호화 |
| `APPLE_SIGN_IDENTITY` | `Developer ID Application: TaeGyum Kim` (A-4 CN, 괄호 TEAMID 제외) | .app codesign |
| `APPLE_INSTALL_SIGN_IDENTITY` | `Developer ID Installer: TaeGyum Kim` (A-4 CN, 괄호 제외) | .pkg productsign |
| `APPLE_ID` | Apple Developer 이메일 | 공증 |
| `APPLE_TEAM_ID` | Team ID (10자) | 공증 |
| `APPLE_APP_PASSWORD` | 앱 암호 `xxxx-xxxx-xxxx-xxxx` | 공증 |

게이트 동작:

- `APPLE_CERT_P12_BASE64` + `APPLE_SIGN_IDENTITY` 둘 다 있어야 macOS Velopack 경로가 켜짐(`signcheck`).
- 그 경로가 켜진 release 에서는 **8개 secret 전부 필수** — `APPLE_INSTALL_CERT_P12_BASE64` / `APPLE_INSTALL_SIGN_IDENTITY` / 공증 3종(`APPLE_ID`/`APPLE_TEAM_ID`/`APPLE_APP_PASSWORD`) 중 하나라도 빠지면 잡이 **즉시 실패**(silent 하게 서명만 업로드하지 않음). 8개를 한 번에 등록하세요.
- 등록 후 CI 가 자동으로 (a) keychain 에 Application·Installer 두 identity 존재 확인, (b) 산출 `.pkg` 의 `pkgutil --check-signature` + `stapler validate` 를 fail-fast 검증한다.

기존 `HERMA_SERVER_IP`, `HERMA_AZURE_CLIENT_ID` 는 그대로 유지.

---

## Part D — 검증 (첫 서명 릴리스 — 현재 유일한 미검증 영역)

> 지금까지 Windows 자동 업데이트는 실설치 e2e 실증 완료, macOS 서명/공증 flow 만 미검증입니다.
> 첫 서명 릴리스에서 아래를 반드시 확인하세요.

1. 릴리스 발행(태그 `vX.Y.Z`) → Actions 의 **Launcher Build → macos** 잡 확인.
2. `서명 자산 존재 확인` 스텝이 `enabled=true` 인지.
3. `Velopack 패키징 + 업로드` 스텝 로그에서:
   - `notarytool store-credentials` 성공 + `공증 활성` 출력
   - `vpk pack` 이 codesign/productsign + notarytool submit `status: Accepted` + staple 까지 통과
4. 릴리스 자산에 osx 채널 산출물(`*-osx-*`, `RELEASES-osx`, `.pkg`/portable `.zip`)이 올라왔는지.
5. (가능하면) 친구 Mac 에서 `.pkg` 더블클릭 → **경고 없이** 설치/실행 → 다음 릴리스에서 자동 업데이트 동작.

### 트러블슈팅

- **`security import` 실패** → `.p12` 를 `-legacy` 로 다시 만들기(A-6).
- **공증 `credentials not found`** → `store-credentials` 에 `--keychain` 누락(이미 workflow 에 반영됨) / Team ID·앱 암호 오타.
- **공증 `Invalid` (status)** → hardened runtime/서명 누락. vpk 가 자동 처리하나, 실패 시 `--signEntitlements` 로 .NET entitlements(`com.apple.security.cs.allow-jit`, `disable-library-validation`) 명시 검토.
- **`.pkg` Gatekeeper 거부** → Installer 인증서 미사용. `APPLE_INSTALL_SIGN_IDENTITY` 등록 확인.

---

## 참고

- `launcher/build-mac-app.sh` 의 ad-hoc 서명 zip 은 인증서 도입 전의 interim 경로입니다.
  공증 flow 가 검증되면, 릴리스에 ad-hoc zip 과 Velopack 산출물이 **둘 다** 붙어 친구가 혼동할 수 있으니
  ad-hoc 첨부를 내릴지(`Attach to release` 스텝) 결정하세요. (현재는 안전망으로 유지)
- 채널: 앱은 `UpdateManager(source, null, null)` 로 OS 기본 채널을 조회하고, Velopack 의 macOS 기본 채널은 `osx`
  이므로 CI 의 `--channel osx` 와 일치합니다(코드 수정 불필요).
- 인증서 갱신: Developer ID 인증서는 유효기간이 있습니다(보통 5년). 만료 전 재발급 후 Secret 갱신.
