# macOS 설치/실행 가이드 (친구용)

> Herma Launcher 는 Windows·macOS 둘 다 동작합니다. 이 문서는 **Mac 사용자**가 받은 앱을 여는 방법입니다.
> 로그인은 **오프라인(닉네임만)** 로 바로 가능하고, 온라인(정품 계정) 로그인도 같은 앱에서 됩니다.

## 받는 것
`HermaLauncher-macos-arm64.zip` (Apple Silicon — M1/M2/M3/M4 Mac, **macOS 12 Monterey 이상**).

> Intel Mac(2020 이전)이면 메인테이너에게 알려주세요 — `osx-x64` 빌드를 추가합니다.

## 처음 1회 — 우클릭으로 열기 (중요)

이 앱은 **미공증**(Apple Developer 미등록 — 지인용이라 인증 생략)이라, 그냥 더블클릭하면
macOS 가 "확인되지 않은 개발자"라며 막습니다. **처음 한 번만** 아래대로 열면 이후엔 더블클릭으로 실행됩니다.

1. `HermaLauncher-macos-arm64.zip` 더블클릭 → 압축 풀기 → `HermaLauncher.app` 생성.
2. `HermaLauncher.app` 을 **우클릭(또는 Control+클릭) → "열기"**.
3. 경고창의 **"열기"** 다시 클릭. → 실행됩니다(이후부턴 더블클릭 OK).

### 그래도 "손상되어 열 수 없습니다"가 뜨면
다운로드 격리(quarantine) 속성 때문입니다. **터미널**(응용프로그램 → 유틸리티 → 터미널)에서:
```bash
xattr -cr ~/Downloads/HermaLauncher.app    # 경로는 실제 위치로
```
실행 후 다시 우클릭 → 열기.

> macOS 15(Sequoia)+ 는 우클릭→열기 대신 **시스템 설정 → 개인정보 보호 및 보안 → 아래로 스크롤 → "확인 없이 열기"** 버튼이 나올 수 있습니다.

## 사용
1. 앱이 열리면 **닉네임** 입력.
2. **오프라인 모드 체크** (친구 서버는 online-mode=false 기본) → **Play**.
3. 자동으로 Java 25 설치 → 모드 동기화 → Fabric → 서버 자동 접속.

> 온라인(정품 계정)으로 쓰려면 "오프라인 모드" 해제 → 기본 브라우저로 MS 로그인.
> (단 Azure 앱 Mojang 승인 완료 후 가능 — 그 전엔 오프라인으로 플레이)

## 왜 "확인되지 않은 개발자"인가?
- Windows 미서명 exe 의 "Windows의 PC 보호" 경고와 같은 것입니다(지인용 무료 배포).
- 공증(notarization, 경고조차 없이 실행)은 Apple Developer 연 $99 가입 후 가능 — 최종 단계로 보류.
- 코드는 동일하니 공증만 추가하면 경고 없이 열립니다.
