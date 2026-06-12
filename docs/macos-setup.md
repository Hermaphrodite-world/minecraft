# macOS 설치/실행 가이드 (친구용)

> Herma Launcher 는 Windows·macOS 둘 다 동작합니다. 이 문서는 **Mac 사용자**가 받은 앱을 여는 방법입니다.
> 로그인은 **오프라인(닉네임만)** 로 바로 가능하고, 온라인(정품 계정) 로그인도 같은 앱에서 됩니다.

## 받는 것
`HermaLauncher-osx-Setup.pkg` (Apple Silicon — M1/M2/M3/M4 Mac, **macOS 12 Monterey 이상**). 설치형.

> 포터블이 필요하면 `HermaLauncher-osx-Portable.zip`(압축 해제 후 `.app` 실행 — 역시 공증됨).
> Intel Mac(2020 이전)이면 메인테이너에게 알려주세요 — `osx-x64` 빌드를 추가합니다.

## 설치 (공증됨 — 경고 없음)

이 앱은 **Developer ID 서명 + Apple 공증**을 받았으므로 그냥 더블클릭하면 됩니다(미공증 앱의 "확인되지 않은 개발자" 경고 없음).

1. `HermaLauncher-osx-Setup.pkg` 더블클릭 → 설치 관리자 실행.
2. 안내대로 진행(설치 위치 `/응용 프로그램` 또는 사용자 폴더 선택 가능) → 설치 후 자동 실행.
3. 이후 새 버전은 앱이 **자동**으로 받아 업데이트합니다.

## 사용
1. 앱이 열리면 **닉네임** 입력.
2. **오프라인 모드 체크** (친구 서버는 online-mode=false 기본) → **Play**.
3. 자동으로 Java 25 설치 → 모드 동기화 → Fabric → 서버 자동 접속.

> 온라인(정품 계정)으로 쓰려면 "오프라인 모드" 해제 → 기본 브라우저로 MS 로그인.
> (단 Azure 앱 Mojang 승인 완료 후 가능 — 그 전엔 오프라인으로 플레이)

## 서명/공증
- 이 앱은 **Developer ID 서명 + Apple 공증(notarization)**을 받았습니다 — Gatekeeper 경고 없이 더블클릭으로 설치/실행됩니다.
- 자동 업데이트도 서명된 패키지로 안전하게 교체됩니다(Windows 와 동일).
