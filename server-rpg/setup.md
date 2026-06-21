# RPG 서버 셋업 가이드 (NeoForge / MC 1.21.1)

> 비개발자도 따라할 수 있게 단계별. 전제: **Java 21** 설치(1.21.1 요구 — 정식 서버의 Java 25 와 다름).
> 정식(Fabric 26.1.2) 서버와는 **완전히 별도 폴더**에서 운영한다(월드·모드·버전 모두 다름).

## 1. NeoForge 서버 설치

```bash
# NeoForge 21.1.234 인스톨러 다운로드:
#   https://maven.neoforged.net/releases/net/neoforged/neoforge/21.1.234/neoforge-21.1.234-installer.jar
java -jar neoforge-21.1.234-installer.jar --installServer
# => libraries/ , run.sh , run.bat , user_jvm_args.txt 생성
#    (Fabric 처럼 단일 -launch.jar 가 아니라 run 스크립트 + @argfile 방식)
```

## 2. EULA 동의

```bash
echo "eula=true" > eula.txt
```

## 3. JVM 메모리/플래그 설정

`--installServer` 가 만든 `user_jvm_args.txt` 를 이 폴더의 [user_jvm_args.txt](user_jvm_args.txt) 내용으로 교체.
RAM 만 바꾸려면 맨 위 `-Xms6G` / `-Xmx6G` 두 줄을 동일 값으로 수정(예: `8G`).
`run.sh` / `run.bat` 이 이 파일을 자동으로 읽으므로 별도 start 스크립트는 불필요.

## 4. 모드 동기화 (packwiz)

```bash
# packwiz-installer-bootstrap.jar 를 이 폴더에 받기:
#   https://github.com/packwiz/packwiz-installer-bootstrap/releases/latest
chmod +x sync-mods.sh
./sync-mods.sh        # server+both 모드만 설치. 기본 팩 URL = rpg 브랜치 modpack-rpg
```

기본 `PACK_TOML_URL` 은 런처의 RPG 채널과 동일한 `rpg` 브랜치 raw 주소다. 다른 팩을 쓰려면
`PACK_TOML_URL=... ./sync-mods.sh` 로 덮어쓴다.

## 5. 보안 설정 (정식 서버와 동일 정책)

- `server.properties` 는 이미 `online-mode=false`(지인 서버 — MS 로그인 없이 닉네임 기반) +
  `white-list=true` + `enforce-whitelist=true` + `allow-flight=true`(Iron's Spells/Artifacts 비행).
  - offline 이라 화이트리스트는 **닉네임 매칭** — 친구 런처 닉네임을 정확히 등록해야 접속.
- 친구 추가: 서버 콘솔에서 `whitelist add <닉네임>` → `whitelist reload`.
- 운영자: `op <본인닉>` (OP는 최소화).
- ⚠️ 정식(Fabric 25565) 서버와 동시에 켤 경우 `server-port` 를 다르게(예: 25566) 두어 충돌 방지.

## 6. 기동

```bash
chmod +x run.sh
./run.sh nogui          # Linux/macOS — user_jvm_args.txt 의 RAM/플래그를 자동 적용
# Windows: run.bat nogui
```

첫 기동은 월드 생성 + 구조물 데이터 로딩으로 수 분 걸릴 수 있다. 콘솔에 `Done (...)! For help, type "help"`
가 뜨면 정상 기동 완료.

## 7. 백업

- 이 팩에는 Fastback(인게임 백업)이 없다 → **서버 정지 후 폴더 전체 복사**가 기본:
  ```bash
  # 서버 콘솔에서 stop → 종료 후
  cp -r world world-backup-$(date +%Y%m%d)
  ```
- 운영 자동화 시 `world/` (+ `world_nether`/`world_the_end` 가 분리 생성되면 함께) 를 주기 백업.
- **복구 테스트 1회 필수** — 복원 안 되는 백업은 백업이 아님.

## 운영 메모

- 모드 추가/교체 = packwiz 팩(modpack-rpg) 수정 → push → 서버 `./sync-mods.sh` → 재시작. 서버 바이너리 무수정.
- 클라이언트는 런처의 **RPG 채널**이 다음 실행 시 자동 동기화 — 재배포 불필요.
- 이 팩에는 정식 서버에 있던 일부 운영 모드(Simple Voice Chat / squaremap / Open Parties and Claims /
  WarpUtils / Blastproof / Fastback)가 **없다**. 해당 기능이 필요하면 1.21.1 NeoForge 대응 버전을
  modpack-rpg 에 추가한 뒤 본 가이드에 절차를 보강할 것.
- 성능 진단(Spark) 모드는 미포함 — 필요 시 추가.
