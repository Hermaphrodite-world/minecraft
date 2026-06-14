# 서드파티 고지 (Third-Party Notices)

HermaLauncher 는 아래 오픈소스/외부 구성요소를 사용합니다. 각 라이선스는 해당 프로젝트를 따릅니다.

## 런처 의존성 (NuGet)

| 구성요소 | 용도 | 라이선스 |
|---|---|---|
| [Avalonia](https://github.com/AvaloniaUI/Avalonia) (11.3.0) | 크로스플랫폼 UI 프레임워크 | MIT |
| Avalonia.Fonts.Inter ([Inter](https://github.com/rsms/inter)) | UI 폰트 | SIL Open Font License 1.1 |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) (8.4.0) | MVVM (ObservableProperty/RelayCommand) | MIT |
| [CmlLib.Core](https://github.com/CmlLib/CmlLib.Core) (4.0.6) + Auth.Microsoft | Minecraft 설치·실행·인증 | MIT |
| [XboxAuthNet.Game.Msal](https://github.com/CmlLib/XboxAuthNet.Game) | MS/Xbox 인증(MSAL) | MIT |
| [Velopack](https://github.com/velopack/velopack) (1.2.0) | 자동 업데이트/설치 | MIT |
| [Microsoft.Identity.Client (MSAL)](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) | OAuth | MIT |

## 외부 도구/런타임 (런타임 다운로드)

| 구성요소 | 용도 | 라이선스/출처 |
|---|---|---|
| [packwiz-installer-bootstrap](https://github.com/packwiz/packwiz-installer-bootstrap) | 모드팩 동기화 | MIT (특정 버전+SHA-256 핀 — P2-1) |
| [Fabric Loader](https://fabricmc.net/) | 모드 로더 | Apache 2.0 (버전 핀 — P2-2) |
| Minecraft: Java Edition / Java 런타임 | 게임 본체·JRE | Mojang/Microsoft EULA — 정품 인증 필요 |

## 모드팩 콘텐츠
- `modpack/` 의 모드·리소스팩·쉐이더팩은 각 저작자/라이선스를 따릅니다(Modrinth 출처, `modpack/index.toml` 참조).
- 한국어 번역 보충팩(`herma-korean`)은 본 프로젝트 산출물입니다.

## 플랫폼 지원
- **지원**: Windows (x64), macOS (Apple Silicon / arm64).
- **미지원**: Linux — 빌드/배포 파이프라인 없음(코드는 크로스플랫폼이나 1.0 배포 대상 아님).

> 본 고지는 1.0 출시 기준. 의존성 버전 변경 시 갱신.
