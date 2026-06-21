# Sparse Structures × 구조물 추가 모드(MES) structure_set 레지스트리 NPE — 헤드리스 서버 부팅 스모크로 검출

## 증상

모드팩(packwiz, Fabric/MC 26.1.2)에 RPG/던전/편의 모드 86개를 한 번에 추가한 뒤, **정적 검증은 전부 그린**이었다:

- `packwiz refresh` 성공, index.toml 정합 OK
- 중복 mod-id 0건 (전 jar `fabric.mod.json` 파싱)
- 신규 전이 의존(team_reborn_energy/biolith/diagonalblocks/iteminteractions/statuemenus/multiloaderdataextensions) 전부 JIJ(jar-in-jar) 번들 확인 → 미해결 의존 없음

그런데 **실제 서버 부팅 시 크래시**:

```
[Worker-Main/ERROR]: Registry loading errors:
> Errors in registry minecraft:worldgen/structure_set:
>> Errors in element mes:enderbloom_grove:
Caused by: java.lang.NullPointerException: Cannot read field "left" because "r" is null
    at io.github.maxencedc.sparsestructures.StructureSetsSet.addStructureSet(StructureSetsSet.java:10)
    at net.minecraft.resources.RegistryLoadTask$PendingRegistration.handler$...$sparsestructures$loadFromResource
[main/WARN]: Failed to load datapacks, can't proceed with server load.
Caused by: net.minecraft.ReportedException: Registry Loading
Caused by: java.lang.IllegalStateException: Failed to load registries due to errors
```

→ worldgen 레지스트리 로드 실패 → **서버가 'Done'에 도달 못 하고 부팅 불가**.

## 원인

**Sparse Structures(`sparsestructures`)** 는 구조물 과밀 방지를 위해 `minecraft:worldgen/structure_set` 로딩에 mixin 한다. 그 mixin(`StructureSetsSet.addStructureSet`)이 **다른 구조물 추가 모드 MES(Moog's End Structures)** 가 정의한 structure_set `mes:enderbloom_grove` 를 처리하다가, 기대한 필드(frequency/placement 관련 `left`)가 `null` 이라 **NullPointerException** 을 던진다.

레지스트리 로딩은 한 element 라도 예외가 나면 **전체 datapack 로드를 중단**(`Failed to load registries due to errors`)하므로 서버가 못 뜬다.

핵심: 이건 **두 모드 조합에서만** 나는 런타임 충돌이다. 각 모드는 단독으론 정상이고, mod-id 중복·의존성·packwiz 정합 같은 **정적 검사로는 절대 안 잡힌다**(녹색 빌드 ≠ 결함 없음). 한 번에 다수 모드를 배치 추가할 때 전형적으로 잠복한다.

## 해결

**Sparse Structures 제거**(과밀 방지 헬퍼 < 실제 컨텐츠 모드 MES). NPE 는 mixin 코드 레벨이라 config 로 못 푼다.

```bash
cd modpack && packwiz remove sparsestructures && packwiz refresh
# build-pack.sh 의 모드 목록에서도 제거
```

제거 후 재부팅 → **`Done (11.078s)!` 도달, 170 모드 정상 로드, 잔여 ERROR 0** 으로 검증 완료.

> 대안(택1): MES 를 빼도 되지만 MES 는 실제 End 구조물 컨텐츠라 spacing 헬퍼보다 가치가 높아 Sparse Structures 를 버리는 게 맞다. 구조물 과밀은 각 구조물 모드의 자체 config(spacing/separation)로 조절 가능.

## 예방 — 헤드리스 서버 부팅 스모크 테스트 (핵심 검출 기법)

다수 모드를 배치 추가/변경한 모드팩은 **배포 전 반드시 실제 서버를 한 번 띄워** registry/mixin 충돌을 검출한다. MC 클라 GUI·MS 로그인 없이 **서버 측(both+server 모드)** 충돌을 잡는 실전 스모크:

1. **Java 런타임**: 런처(CmlLib)가 이미 받아둔 Mojang 런타임을 재사용 — `%APPDATA%/.minecraft/runtime/windows-x64/java-runtime-epsilon/bin/java.exe` (epsilon = **Java 25**, MC 26.1.x 요구). 별도 JDK 설치 불필요.
2. **Fabric 서버**: `https://meta.fabricmc.net/v2/versions/loader/<MC>/<loader>/<installer>/server/jar` 로 런처 jar 다운로드. ⚠️ installer 버전 파싱 시 meta JSON 의 공백(`"version": "1.1.1"`) 주의 — grep `"version":"..."`(공백無)면 빈 값→404→9바이트 "Not Found" jar. PK 매직으로 유효성 확인.
3. **모드 동기화**: `java -jar packwiz-installer-bootstrap.jar -g -s server <pack.toml URL>`. 베타라면 브랜치 raw URL(`raw.githubusercontent.com/<org>/<repo>/<branch>/modpack/pack.toml`)을 쓰면 **베타 배포 경로까지 동시 검증**. (`-s server` = both+server 모드만 설치)
4. **부팅 + 판정**: `eula=true`, `online-mode=false`, `<java> -Xmx4G -jar fabric-server.jar nogui` → 로그를 폴링해 `Done \([0-9]` 도달(PASS) vs `Registry loading errors`/`Mod resolution`/`Incompatible mod set`/`Failed to load datapacks`(FAIL) 감지, 타임아웃 후 프로세스 kill.

**미검증으로 남는 것**: 클라 전용 모드(sodium/iris/셰이더/shoulder-surfing 등)와 실제 게임플레이/렌더는 서버 부팅으로 검증 안 됨 → 실 PC 클라 스모크 필요.

추가 정적 사전점검(부팅 전 빠른 거름):
- 전 jar `fabric.mod.json` → mod-id 중복 검사
- `depends` 중 pack/JIJ(`META-INF/jars/`)에 없는 것 = 진짜 누락 후보

## 관련 문서

- [docs/research/2026-06-20-multimode-magic-rpg.md](../research/2026-06-20-multimode-magic-rpg.md) — 모드팩 구성/적용 SoT
- 글로벌 교훈: "AI/workflow-배치 구현 코드는 녹색 빌드 ≠ 결함 없음 — 머지 전 적대적/런타임 검증" (CLAUDE.md Common Pitfalls)
- [mc-26-resourcepack-compat-whitelist-drops-compatible.md](mc-26-resourcepack-compat-whitelist-drops-compatible.md) — 같은 프로젝트, "UI/설정 추측 말고 실 런타임 로그로 검증" 동일 축
