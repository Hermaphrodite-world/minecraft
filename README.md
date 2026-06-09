# Hermaphrodite World

비개발자 친구도 **클릭 한 번**으로 접속하는 모드 적용 마인크래프트 서버 + 커스텀 런처.

- **로더 / 버전:** Fabric / **Minecraft 26.1.2** (현재 최신, Java 25)
- **단일 진실 공급원:** packwiz 모드팩 — 서버와 런처가 같은 팩을 바라봐 버전 불일치 구조적 방지
- **무재배포 확장:** 모드 추가 = 팩 push, 런처/서버 바이너리 무수정

## 저장소 구성

| 폴더 | 내용 |
|------|------|
| [`launcher/`](launcher/) | Avalonia(.NET 10) 크로스플랫폼 런처 — Windows/macOS(arm64). 실행: 자체 업데이트 → MS 로그인 → Java → packwiz 동기화 → Fabric → ServerIp 자동 접속 |
| [`modpack/`](modpack/) | packwiz 팩 (`pack.toml` + `mods/` + `resourcepacks/` + `shaderpacks/`). 64개 모드 + 의존성, side(client/server/both) 분류 완료 |
| [`server/`](server/) | Fabric 서버 구성 — 기동 스크립트(Aikar flags), `server.properties`(화이트리스트/online-mode), 모드 동기화, 셋업 가이드 |
| [`docs/`](docs/) | 기획서 · 구현계획 · 모드구성 · 서버스택 · 런처 통합 노트 |

## 빠른 시작

### 모드팩 (메인테이너)
```bash
cd modpack
# 모드 추가/수정 후
packwiz refresh && git add . && git commit -m "modpack: ..." && git push
# 호스팅: GitHub Pages → https://<org>.github.io/modpack/pack.toml
```
재현 빌드: `PACKWIZ=packwiz bash modpack/build-pack.sh`

### 런처 (개발 / 배포)
```bash
cd launcher/src/HermaLauncher
dotnet build -c Release          # 0 경고·0 오류 (실 CmlLib+Velopack 통합)
dotnet run                       # UI 기동 (런타임 스모크 통과)
# Windows 미서명 단일 exe 게시:
pwsh launcher/publish-win.ps1    # → publish/win-x64/HermaLauncher.exe (self-contained)
```

### 서버
[`server/setup.md`](server/setup.md) 참조 — Fabric 설치 → EULA → `./sync-mods.sh` → `./start.sh`.

## 구현 상태

| 영역 | 상태 |
|------|------|
| packwiz 모드팩 (26.1.2, 64 모드 + side 분류) | ✅ 완료 (`packwiz refresh` 통과) |
| 런처 골격 (UI · 실행 순서 · 실패 게이트 · packwiz 자동 동기화) | ✅ 빌드 0/0 + 런타임 스모크 통과 (net10.0) |
| 런처 CmlLib 인증/Java/Fabric/실행 통합 | ✅ **구현 완료** — 어셈블리 검증 API. Windows 인증은 CmlLib 기본 OAuth(자체 Azure 앱 불요). 실 게임 런타임은 사용자 PC 검증 |
| Velopack 자체 업데이트 | ✅ **구현 완료** (Program.Main 첫 줄 + GithubSource) |
| Windows 배포 (미서명 단일 exe) | ✅ **완료** — `publish/win-x64/HermaLauncher.exe` 96MB self-contained (결정 D: 미서명) |
| CI (GitHub Actions, Windows) | ✅ 완료 ([.github/workflows/launcher-build.yml](.github/workflows/launcher-build.yml)) |
| 서버 구성 (스크립트·보안·동기화) | ✅ 완료 |
| macOS 빌드 / Apple 공증 | 🕓 **최종 단계 보류** (결정 C) — CI에 job 골격 준비됨 |

> 미검증(외부 게이트): 실 MS 로그인(WebView2/MS 계정), 실 게임 실행/서버 접속(Java 25 서버·온라인 인증)은 사용자 PC에서 검증. macOS 공증은 최종 단계.

## 라이선스
[MIT](LICENSE) (런처 소스·스크립트·문서). 서드파티 모드는 각자 라이선스를 따르며 바이너리는 미포함(packwiz 메타데이터만).
