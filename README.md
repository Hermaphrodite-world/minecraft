# Hermaphrodite World

비개발자 친구도 **클릭 한 번**으로 접속하는 모드 적용 마인크래프트 서버 + 커스텀 런처.

- **로더 / 버전:** Fabric / **Minecraft 26.1.2** (현재 최신, Java 25)
- **단일 진실 공급원:** packwiz 모드팩 — 서버와 런처가 같은 팩을 바라봐 버전 불일치 구조적 방지
- **무재배포 확장:** 모드 추가 = 팩 push, 런처/서버 바이너리 무수정

## 저장소 구성

| 폴더 | 내용 |
|------|------|
| [`launcher/`](launcher/) | Avalonia(.NET 8) 크로스플랫폼 런처 — Windows/macOS(arm64). 실행: 자체 업데이트 → MS 로그인 → Java → packwiz 동기화 → Fabric → ServerIp 자동 접속 |
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

### 런처 (개발)
```bash
cd launcher/src/HermaLauncher
dotnet build -c Release          # 현재 0 경고·0 오류로 빌드됨
dotnet run                       # UI 기동 (CmlLib 통합 전 — docs/launcher-integration-notes.md)
```

### 서버
[`server/setup.md`](server/setup.md) 참조 — Fabric 설치 → EULA → `./sync-mods.sh` → `./start.sh`.

## 구현 상태

| 영역 | 상태 |
|------|------|
| packwiz 모드팩 (26.1.2, 64 모드 + side 분류) | ✅ 완료 (`packwiz refresh` 통과) |
| 런처 골격 (UI · 실행 순서 · 실패 게이트 · packwiz 연동) | ✅ 빌드됨 (net8.0) |
| 런처 CmlLib 인증/Java/실행 통합 | ⏳ 통합 지점 문서화 ([docs/launcher-integration-notes.md](docs/launcher-integration-notes.md)) — Azure 앱 승인(R4) 후 활성화 |
| Velopack 자체 업데이트 | ⏳ 통합 지점 문서화 |
| 서버 구성 (스크립트·보안·동기화) | ✅ 완료 |
| 코드 서명 / Apple 공증 / macOS 빌드 | ⏳ 결정 C/D 후 ([docs/구현계획.md](docs/구현계획.md) M2) |

> 외부 게이트(Microsoft Azure 앱 승인, 코드서명 인증서, MC 서버 Java 25 런타임)는 본 저장소 밖에서 진행되며, 해당 부분은 통합 지점으로 명시해 두었다.

## 라이선스
[MIT](LICENSE) (런처 소스·스크립트·문서). 서드파티 모드는 각자 라이선스를 따르며 바이너리는 미포함(packwiz 메타데이터만).
