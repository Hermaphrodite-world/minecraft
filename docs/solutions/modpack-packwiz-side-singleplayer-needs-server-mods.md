# packwiz 모드팩 side 분류 — 싱글플레이(통합 서버)는 server 모드가 필요

> 전용서버+thin-client 최적화로 side="server" 를 쓰면, 싱글플레이로 플레이/테스트되는 팩이
> (1) client 모드의 hard-dep 누락 크래시 + (2) 월드젠/구조물 컨텐츠 부재로 깨진다.

## 증상

런처 RPG 채널(NeoForge 1.21.1)로 게임을 띄우자 클라이언트가 로딩 직전 FATAL:

```
[main/ERROR]: Missing or unsupported mandatory dependencies:
    Mod ID: 'smartbrainlib', Requested by: 'occultism', Expected range: '[1.14.5,)', Actual version: '[MISSING]'
[Render thread/FATAL] ClientModLoader: Error during pre-loading phase:
    Mod occultism requires smartbrainlib 1.14.5 or above — Currently, smartbrainlib is not installed
```

`smartbrainlib` 은 팩에 분명히 선언돼 있는데 클라이언트엔 안 깔렸다. 더 심각한 건 — 크래시를 고쳐도
**싱글플레이에 RPG 구조물/바이옴(terralith·incendium·던전 등)이 하나도 안 나올** 상태였다(로드된 모드
목록에 server 분류 14개가 전부 빠져 있었음).

## 원인

1. **side 분류가 "전용서버 + 얇은 클라" 모델로 돼 있었다.** `fix-sides.py` 가 Modrinth `env`
   (client_side/server_side) 기준으로 월드젠/구조물/라이브러리 14개를 `side = "server"` 로 좁혔다
   (멀티플레이에선 클라가 worldgen 을 안 받아도 서버가 구조물을 네트워크로 보내주므로 유효한 최적화).
2. **런처 클라 동기화는 `client + both` 만 받는다** → `server` 모드 전부 제외.
3. **싱글플레이는 *통합 서버*를 돌린다** → server 몫 모드가 클라에 **반드시** 있어야 한다. 그런데 없으니:
   - `smartbrainlib`(server) 가 `occultism`(both, 클라에서 동작) 의 **hard-dependency** 인데 누락
     → FML 의존성 검사 실패로 **크래시**.
   - `terralith`/구조물 등 월드젠(server) 부재 → 크래시는 안 나도 **RPG 컨텐츠가 안 생성됨**.
4. **검증 사각**: 머지 전 헤드리스 **서버** 부팅 스모크(`-s server` = server+both)는 통과했다 —
   server 경로엔 server 모드가 다 있으니까. 하지만 **클라(client+both) 경로**, 특히 **싱글플레이
   (클라=통합서버라 server 모드도 필요)** 조합은 검증되지 않았다. 정적 검증·서버 스모크 통과 ≠
   클라/싱글플레이 정상.

## 해결

런처 재빌드 불필요(모드팩은 런타임 raw 동기화) — 모드팩 데이터만 수정 후 push.

```diff
# 14개 .pw.toml (cristel-lib, dungeons-and-taverns, incendium, lithostitched, moogs-*,
#  smartbrainlib, structory(+towers), tectonic, terralith, towns-and-towers,
#  when-dungeons-arise, yungs-better-dungeons)
- side = "server"
+ side = "both"
```

- 결과: 싱글플레이 클라(client+both) = 60모드 전부 보유 → **의존성 누락 구조적으로 불가능**.
  전용 서버(server+both)는 클라 전용 렌더(sodium/iris/simply-tooltips=client)만 제외 → 정합 유지.
- `fix-sides.py` 를 **싱글플레이 모델**로 재작성: `recommend()` 가 절대 `"server"` 를 반환하지 않음
  (`server_side == "unsupported"` 인 클라 전용 렌더만 `"client"`, 그 외 전부 `"both"`).
- `packwiz refresh` 로 index/pack 해시 갱신. 사용자는 런처 재실행만 하면 자동 재동기화.

근거 보강: 같은 모드셋이 직전 **서버 부팅 스모크에서 `Done` 도달**(server+both 로 로드 검증) →
싱글플레이 통합 서버도 동일 로딩이므로 컨텐츠가 정상 로드된다.

## 예방

- **싱글플레이로 플레이/테스트되는 팩에는 `side = "server"` 를 쓰지 말 것.** 클라가 통합 서버를
  돌려 모든 컨텐츠가 필요하다 → `"both"`/`"client"` 만 사용. (전용 서버는 `server+both` 동기화라
  클라 전용 렌더 모드가 자동 제외돼 손해 없음.)
- **라이브러리/의존성 모드는 기본 `"both"`.** client/both 모드가 hard-require 하는 dep 을 server 로
  좁히면 그 client 의 로드가 깨진다(멀티에서도 클라 크래시). (이전 Fabric 팩의 `defaulted`/`veinminer`
  'Incompatible mods' 크래시와 동일 클래스.)
- **검증 비대칭 인지**: `-s server` 부팅 스모크는 **클라 누락을 못 잡는다**. side 변경 후엔 (a) 클라
  (client+both) 동기화로도 확인하거나 (b) "client/both 모드의 모든 required dep 이 client/both 인가",
  "싱글플레이가 server 컨텐츠를 갖는가" 를 별도 점검.
- **Windows Git Bash `sed -i` 라인엔딩 함정**: CRLF 파일을 `sed -i` 하면 LF 로 변환돼 1줄 변경이
  파일 전체 diff 로 번진다. 의도(필드 1개)만 바꾸려면 변경 후 원본 라인엔딩으로 원복(Python
  `replace(b'\n', b'\r\n')`)하고 `packwiz refresh` 재실행.

## 관련 문서

- [sparsestructures-mes-registry-npe-server-boot-test.md](sparsestructures-mes-registry-npe-server-boot-test.md) — 서버 부팅 스모크 기법(이번 사각의 그 검증)
- `modpack-rpg/fix-sides.py` — 싱글플레이 모델로 재작성된 side 교정기
- `server-rpg/setup.md` — 전용 서버(server+both)는 여전히 클라 전용 렌더만 제외
