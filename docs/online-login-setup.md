# 온라인 로그인(정품 MS 계정) 셋업 가이드

런처는 **2가지 로그인**을 모두 지원합니다.

| 모드 | 방법 | 필요한 것 | 서버 |
|------|------|-----------|------|
| **오프라인** (기본) | 닉네임만 | 없음 | `online-mode=false` |
| **온라인** | MS device-code | 자체 Azure 앱 client ID | `online-mode=true` |

오프라인은 추가 작업이 없습니다(런처에서 "오프라인 모드" 체크 유지 + 닉네임). 아래는 **온라인**(정품 계정·스킨·검증)을 켜는 절차입니다.

> ⚠️ **코드 서명/공증과는 전혀 다른 것**입니다. 이건 "마인크래프트 정품 로그인"용 무료 앱 등록입니다.

---

## 1단계 — Azure 앱 등록 (무료, ~5분)

1. https://portal.azure.com → 상단 검색 **"Microsoft Entra ID"**(구 Azure AD) → 좌측 **앱 등록(App registrations)** → **새 등록(New registration)**.
2. 입력:
   - **이름**: `Herma Launcher` (자유)
   - **지원되는 계정 유형**: **개인 Microsoft 계정만**(Personal Microsoft accounts only)
   - **리디렉션 URI**: 비워두고 등록 → 등록 후 추가
3. **등록** 클릭 → 개요(Overview) 화면의 **애플리케이션(클라이언트) ID** 복사. ← 이게 client ID (비밀 아님, 공유/커밋 가능).
4. 좌측 **인증(Authentication)**:
   - **플랫폼 추가(Add a platform)** → **모바일 및 데스크톱 애플리케이션(Mobile and desktop applications)**
   - 체크: `https://login.microsoftonline.com/common/oauth2/nativeclient`
   - 사용자 지정 URI 추가: `http://localhost`
   - **구성(Configure)**
   - 아래로 스크롤 → **고급 설정 → "공용 클라이언트 흐름 허용(Allow public client flows)"** = **예(Yes)** → **저장(Save)** ★필수

## 2단계 — Microsoft 승인 신청 (Minecraft 서비스 호출 허가)

신규 Azure 앱은 `api.minecraftservices.com` 호출이 기본 차단(403)이라 1회 승인 신청이 필요합니다.

1. 위 client ID로 **로그인 1회 시도**(런처에서 — activity 생성용. 이 시점엔 403 떠도 정상).
2. https://aka.ms/mce-reviewappid (= https://aka.ms/AppRegInfo) 접속 → **client ID 제출**.
3. 승인 + 전파까지 시간이 걸립니다(외부 처리). 승인 전엔 마지막 단계에서 403, 승인 후 정상.

> OSS 런처(Prism/MultiMC)도 이 절차로 승인받았으니 친구용 런처도 통과 가능합니다.

## 3단계 — 런처에 client ID 적용

**방법 A (재빌드 없음, 권장 테스트):** 환경변수
```bat
set HERMA_AZURE_CLIENT_ID=<복사한-client-id>
"d:\...\publish\win-x64\HermaLauncher.exe"
```
**방법 B (배포본에 박기):** `launcher/src/HermaLauncher/LauncherConfig.cs` 의 `AzureClientId` fallback 을 실제 값으로 교체 후 빌드. (client ID는 비밀 아니라 커밋 가능)

## 4단계 — 사용

1. 런처에서 **"오프라인 모드" 체크 해제**.
2. **Play** → 런처가 코드 + `microsoft.com/link` 안내 표시.
3. 브라우저로 link 접속 → 코드 입력 → **원하는 계정 선택**.

### 계정이 다른 게 선택될 때 (질문 사례)
- link 페이지에서 다른 계정이 자동 선택되면 → **"다른 계정 사용"** 클릭 후 원하는 계정 로그인.
- 또는 **시크릿/프라이빗 창**으로 link 를 열어 깨끗한 상태에서 로그인.
- 런처는 토큰을 디스크에 저장하지 않아(매 실행 새 로그인) 이전 계정이 끼어들지 않습니다.
- 각 친구는 **자기 PC에서 자기 MS 계정**으로 로그인 → 같은 client ID 하나를 공유(앱은 1개, 계정은 각자).

## 서버 쪽

온라인 모드로 운영하려면 `server/server.properties` 의 `online-mode=true` 로 (현재 친구용 기본은 `false`). 정품 계정 UUID 기반 화이트리스트 사용.

---

## 미검증 표기 (정직)
- 온라인 device-code 코드는 어셈블리 API로 빌드 검증됨. **실 로그인은 미테스트**(승인된 client ID 부재). 위 절차로 client ID 확보 후 첫 로그인에서 검증 필요.
- 만약 로그인이 scope/authority 에러를 내면 알려주세요 — 그 메시지로 바로 잡습니다.
