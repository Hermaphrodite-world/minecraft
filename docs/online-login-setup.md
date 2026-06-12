# 온라인 로그인(정품 MS 계정) 셋업 가이드

런처 UI 는 **온라인(정품 MS) 직접 로그인 전용**입니다 (MS 승인 완료, v0.1.5 부터 오프라인/닉네임 UI 제거). 오프라인 경로는 서비스 레이어에 남아 있으나 UI 에 노출하지 않습니다.

| 모드 | 방법 | 필요한 것 | 서버 |
|------|------|-----------|------|
| **온라인 (직접 로그인, 기본·유일 UI)** | **시스템 브라우저** (요즘 공식 런처와 동일) | Azure 앱 client ID **1개**(메인테이너만) | `online-mode=true` |
| 오프라인 (서비스 dormant, UI 미노출) | 닉네임만 | 없음 | `online-mode=false` |

> **"공식 런처처럼 직접 로그인"의 현재 정답 = 시스템 브라우저.** Microsoft가 2023년부터 임베디드 웹뷰를 폐기하고 공식 런처도 기본 브라우저 로그인으로 전환했습니다. 우리 런처도 동일 — 클릭 → 기본 브라우저에서 로그인 → 런처로 자동 복귀(코드 입력 없음). **Windows·macOS 동일**(크로스플랫폼).

> **Azure 앱은 메인테이너 1개만** — 친구는 각자 등록 X. 같은 client ID 하나를 공유하고 각자 자기 MS 계정으로 로그인합니다.

> ⚠️ **코드 서명/공증과는 전혀 다른 것**입니다. "마인크래프트 정품 로그인"용 무료 앱 등록입니다.

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

> 런처는 **온라인 전용**입니다. client ID 가 설정(release bake 또는 `HERMA_AZURE_CLIENT_ID` env)되면 PLAY 시 바로 직접 로그인합니다. 미설정(dev 빌드)이면 온라인 인증이 실패하니 env 로 주입하세요.

## 4단계 — 사용 (시스템 브라우저 직접 로그인)

1. 런처의 **PLAY** → **기본 브라우저가 자동으로 열려** MS 로그인 페이지 표시.
2. 원하는 계정으로 로그인 → 브라우저가 "로그인 완료" → **런처로 자동 복귀**(코드 입력 없음).

### 계정이 다른 게 선택될 때 (질문 사례)
- 브라우저 계정 선택 화면에서 다른 계정이 자동 선택되면 → **"다른 계정 사용"** 클릭 후 원하는 계정 로그인.
- 또는 **시크릿/프라이빗 창**이 기본이 되도록 하거나, 브라우저에서 해당 계정 로그아웃 후 진행.
- 런처는 토큰을 디스크에 저장하지 않아(매 실행 새 로그인) 이전 계정이 끼어들지 않습니다.
- 각 친구는 **자기 PC에서 자기 MS 계정**으로 로그인 → 같은 client ID 하나를 공유(앱은 1개, 계정은 각자).

> **Windows·macOS 동일** — 시스템 브라우저 방식이라 macOS도 별도 작업 없이 같은 코드로 동작합니다(공식 런처도 macOS에서 브라우저 로그인).

## 서버 쪽

온라인 모드로 운영하려면 `server/server.properties` 의 `online-mode=true` 로 (현재 친구용 기본은 `false`). 정품 계정 UUID 기반 화이트리스트 사용.

---

## 미검증 표기 (정직)
- 온라인 device-code 코드는 어셈블리 API로 빌드 검증됨. **실 로그인은 미테스트**(승인된 client ID 부재). 위 절차로 client ID 확보 후 첫 로그인에서 검증 필요.
- 만약 로그인이 scope/authority 에러를 내면 알려주세요 — 그 메시지로 바로 잡습니다.
