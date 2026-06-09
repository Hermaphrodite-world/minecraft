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
```
- **라이브 호스팅(Pages)**: https://hermaphrodite-world.github.io/minecraft/pack.toml ✅
- push → `modpack-pages.yml` 가 자동 재배포 (런처·서버는 다음 동기화 시 변경분 반영).
- 재현 빌드: `PACKWIZ=packwiz bash modpack/build-pack.sh`

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

> **🎉 end-to-end 실증 완료** — 로컬 26.1.2 서버 + 런처로 실제 검증: 클릭 한 번 → 오프라인 인증 → Java 25 설치 → 모드 동기화 → Fabric → quickPlayMultiplayer 자동 접속 → **월드 스폰**. 공식 런처·MS 로그인 없음. Xaero Minimap·Simple Voice Chat·Jade·Sodium 등 인게임 작동 확인.

| 영역 | 상태 |
|------|------|
| packwiz 모드팩 (26.1.2, 64 모드 + side 분류) | ✅ 완료 — Pages 라이브 + e2e 동기화 검증(실 모드 40개) |
| 런처 풀 파이프라인 (UI·인증·Java·packwiz·Fabric·실행·자동접속) | ✅ **실 게임 실증** — 26.1.2 월드 접속 확인 (net10.0, 빌드 0/0) |
| 오프라인 로그인 (친구 서버) | ✅ **실증** — 닉네임만, MS 로그인 0 |
| 온라인 로그인 (정품 계정) | ✅ **시스템 브라우저**(요즘 공식 런처 방식, 크로스플랫폼) — [셋업 가이드](docs/online-login-setup.md). Azure 앱 1개(메인테이너) 공유 |
| 자동 접속 (quickPlayMultiplayer) | ✅ **실증** — MC 26.1 구형 --server 제거 대응 |
| Velopack 자체 업데이트 | ✅ 구현 완료 |
| Windows 배포 (미서명 단일 exe) | ✅ 완료 — self-contained 96MB (결정 D: 미서명) |
| 서버 스택 (Fabric 26.1.2 + 40 모드 + Java 25) | ✅ **실증** — Done(2.3s), Blastproof·LuckPerms·SVC 로드, 포트 바인딩 |
| CI (GitHub Actions) | ✅ Launcher Build 통과 + Modpack Pages 배포 성공 |
| macOS 빌드/공증 | 🕓 최종 단계 보류 (결정 C) |
| macOS 빌드 / Apple 공증 | 🕓 **최종 단계 보류** (결정 C) — CI에 job 골격 준비됨 |

> 미검증(외부 게이트): 실 MS 로그인(WebView2/MS 계정), 실 게임 실행/서버 접속(Java 25 서버·온라인 인증)은 사용자 PC에서 검증. macOS 공증은 최종 단계.

## 라이선스
[MIT](LICENSE) (런처 소스·스크립트·문서). 서드파티 모드는 각자 라이선스를 따르며 바이너리는 미포함(packwiz 메타데이터만).
