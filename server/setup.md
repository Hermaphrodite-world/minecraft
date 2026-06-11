# 서버 셋업 가이드 (Fabric / MC 26.1.2)

> 비개발자도 따라할 수 있게 단계별. 전제: **Java 25** 설치(26.1.2 요구).

## 1. Fabric 서버 설치
```bash
# Fabric 설치 jar 다운로드: https://fabricmc.net/use/server/
java -jar fabric-installer.jar server \
  -mcversion 26.1.2 -downloadMinecraft -dir .
# => fabric-server-launch.jar 생성 (파일명 -launch, -launcher 아님)
```

## 2. EULA 동의
```bash
echo "eula=true" > eula.txt
```

## 3. 모드 동기화 (packwiz)
```bash
# packwiz-installer-bootstrap.jar 를 이 폴더에 받기:
#   https://github.com/packwiz/packwiz-installer-bootstrap/releases/latest
chmod +x sync-mods.sh
./sync-mods.sh        # server+both 모드만 설치 (PACK_TOML_URL 환경변수로 팩 URL 지정)
```

## 4. 보안 설정 (추가모드_서버스택.md §6)
- `server.properties` 는 이미 `online-mode=false`(지인 서버 — MS 로그인 없이 닉네임 기반) + `white-list=true` + `enforce-whitelist=true`.
  - offline 이라 화이트리스트는 **닉네임 매칭** — 친구 런처 닉네임을 정확히 등록해야 접속.
- 친구 추가: 서버 콘솔에서 `whitelist add <닉네임>` → `whitelist reload`.
- 운영자: `op <본인닉>` (OP는 최소화).

## 5. 권한/보호 모드 초기 설정
- **WarpUtils (텔레포트 — 밸런스 정책)**: 밸런스 보존을 위해 **home 계열만 남기고 나머지 텔레포트 비활성**. → **단계별 가이드: [server-warputils-balance.md](../docs/server-warputils-balance.md)**. OP 로 인게임/콘솔에서 `/warputils config <카테고리>` (카테고리: `homes`/`warps`/`tpas`/`back`/`tpr`/`general` — 탭 자동완성):
  - `warps` · `tpas` · `back` · `tpr` → 각 기능 **disabled = true** → `/warp`·`/setwarp`·`/tpa`·`/back`·`/tpr` 전부 제거(전원). 남는 건 home 계열뿐: `/home`·`/sethome`·`/delhome`·`/homes`.
  - `homes` → **delay = 3** (초) → `/home` 입력 후 **3초 워밍업, 그동안 움직이면 취소**(WarpUtils 기본: `common.delay.moved`). 설정은 `config/warputils/`(첫 서버 실행 시 생성)에 저장. delay 단위가 틱이면 `60`(=3초)으로.
  - ⚠️ **WarpUtils 는 LuckPerms 권한 노드를 지원하지 않는다**(권한 레벨 하드코딩 + 위 feature config 가 SoT). 과거 `warputils.command.home` LuckPerms 안내는 동작하지 않으므로 폐기.
- **Blastproof**: 첫 실행 후 `config/blastproof.json` — 크리퍼·TNT 블록피해 OFF, 엔드크리스탈·위더 ON 권장.
- **OPAC**: `config/openpartiesandclaims/` 에서 클레임 한도·보호 토글. 친구에게 `/claim`·`/trust <닉>` 안내.
- **Simple Voice Chat**: 방화벽 **UDP 24454** 인바운드 개방.
- **squaremap**: 웹 포트(기본 8080) — LAN 전용 또는 방화벽 규칙.

## 6. 기동
```bash
chmod +x start.sh
RAM=6G JAVA=/path/to/java25 ./start.sh    # 콘텐츠 많으면 RAM 상향
```

## 7. 백업 (Fastback)
- 서버 콘솔: `/fastback init` 후 `/fastback backup`. 자동화는 Fastback config.
- **복구 테스트 1회 필수** — 복원 안 되는 백업은 백업이 아님.

## 운영 메모
- 모드 추가/교체 = packwiz 팩 수정 → push → 서버 `./sync-mods.sh` → 재시작. 서버 바이너리 무수정.
- 클라이언트는 런처가 다음 실행 시 자동 동기화 — **재배포 불필요**.
- 성능 진단: `/spark profiler`, `/spark tps`. 청크 사전생성: `/chunky start`.
