# 청크 트림 가이드 — 거점만 남기고 나머지 새 지형으로 재생성

> 서버에 모드(신규 바이옴·구조물)가 많이 추가됐습니다. 그런데 **이미 생성된 청크에는 새 지형이 안 나옵니다**(이미 만들어진 곳은 그대로). 그래서 **거점 주변만 보존하고, 그 바깥의 (대부분 미탐험) 청크를 삭제**하면, 다음에 그쪽을 방문할 때 **새 worldgen으로 재생성**됩니다.
>
> 이 가이드는 같이 받은 **`herma-chunk-trim.py`** 스크립트를 macOS 서버에서 안전하게 실행하는 방법입니다.

---

## 0. 한눈에 보기

- **보존되는 영역**: 거점을 감싸는 **원**(중심 청크 `(44.5, 162)`, 반지름 약 `17.76` 청크 ≈ `284` 블록).
- **삭제(재생성)되는 영역**: 그 원 **바깥**의 이미 생성된 청크 전부.
- **안전장치**: 기본은 **미리보기(삭제 안 함)** / `--apply` 시 **삭제 수를 보여주고 `yes` 확인** 후 진행 / 실행 전 **자동 백업(`level.dat`+`data/`+region/entities/poi)** / **보존 청크가 0이면 자동 중단**(좌표·경로 오류 방지) / 이미 만든 청크는 "미생성 표시"만 해 게임이 재생성.

> ⚠️ **새로 생성되는 청크에만 새 지형이 적용**됩니다. 보존한 거점 안쪽은 그대로예요. 보존/삭제 경계에는 지형이 살짝 어긋나는 "이음새(seam)"가 생길 수 있습니다(정상).

---

## 1. 준비물

| 항목 | 확인 방법 |
|---|---|
| **python3** | 터미널에 `python3 --version` → 버전 나오면 OK. 없으면 `xcode-select --install` 또는 `brew install python3` |
| **서버 완전 종료** | ★가장 중요★ 서버가 켜진 채 region 파일을 건드리면 **월드 손상**. 콘솔에서 `stop` 후 프로세스 완전 종료 확인 |
| **여유 디스크** | 백업으로 region/entities/poi 가 복사됩니다(월드 크기만큼). 공간 확보 |

---

## 2. 월드 폴더 경로 찾기

서버를 실행하는 폴더 안의 **`world`** 폴더입니다. (이름은 `server.properties` 의 `level-name=` 값 — 기본 `world`.)

```bash
# 예: 서버를 ~/minecraft-server 에서 돌린다면
ls ~/minecraft-server/world
# → dimensions  data  level.dat  playerdata ... 가 보이면 맞음
```

> MC 26.x 부터 청크는 `world/dimensions/minecraft/overworld/region/` 에 있습니다(예전 `world/region` 아님). 스크립트가 알아서 처리하니 **`world` 폴더 경로만** 알면 됩니다.

---

## 3. 스크립트 설정 — 편집 불필요 ✨

파일을 열어 고칠 필요 없습니다. **월드 경로를 명령행 인자로 넘기면** 됩니다(다음 단계). 의존성 설치도 없음 — `python3` 만 있으면 어느 컴퓨터에서나 그대로 실행됩니다.

> 거점 보존 원 좌표는 이미 기본값(A 27 159 · B 62 165)으로 들어 있어 따로 줄 필요 없습니다. 거점이 다르면 `--a X Z --b X Z` 로만 바꾸면 됩니다(파일 수정 X).

---

## 4. 미리보기 (삭제 안 함, 먼저 꼭 실행)

```bash
cd <스크립트가 있는 폴더>
python3 herma-chunk-trim.py "/경로/server/world"
```
(경로에 공백이 있으면 큰따옴표로 감싸세요. 경로 = 2단계에서 찾은 `world` 폴더.)

출력 예시:
```
WORLD     = /Users/이름/minecraft-server/world
보존 원   = 중심 청크(44.5, 162.0), 반지름 17.76 청크 (≈ 284 블록)
모드      = DRY-RUN (미리보기, 삭제 안 함)
  차원 minecraft/overworld → .../world/dimensions/minecraft/overworld
----------------------------------------
보존 청크          : 1234     ← 거점 등 남길 청크 수
삭제(재생성) 청크  : 56789    ← 새 지형으로 바뀔 청크 수
통째 삭제 region   : 12 파일
```

- **`보존 청크`** 숫자가 0이면 좌표/경로가 잘못된 것 → 멈추고 점검(아래 FAQ).
- 숫자가 합리적이면 다음 단계로.

---

## 5. 실제 적용

```bash
python3 herma-chunk-trim.py "/경로/server/world" --apply
```

- 삭제할 청크 수를 먼저 보여주고 **`yes` 입력을 받아야** 진행합니다(오타 방지). 아무거나 입력하면 취소.
- `yes` 후 **자동 백업**(`world_trim-backup-날짜시간` 폴더에 `level.dat`·`level.dat_old`·`data/`·region/entities/poi 복사) → 삭제.
- **보존 청크가 0**으로 나오면(좌표/경로 오류) 삭제 안 하고 자동 중단합니다.
- 완료 메시지가 나오면 끝.

> 자동화 스크립트에서 확인 프롬프트 없이 돌려야 하면 `--apply --force`. (대화형 터미널에서는 `yes` 권장)
> ★완전한 안전을 원하면 실행 전에 `world` 폴더 전체를 별도로 한 번 더 복사해 두세요(백업은 핵심 폴더만 담습니다).

---

## 6. 서버 시작 + 확인

1. 서버 시작.
2. 거점은 그대로인지 확인.
3. 거점에서 조금 멀리(보존 원 바깥, ≈300블록 이상) 이동 → 새 바이옴/구조물이 생성되는지 확인.

---

## 7. 문제 생기면 원복 (백업 복구)

백업 폴더 구조: `world_trim-backup-<날짜시간>/<월드명>/` 안에 `level.dat`·`data/`·`dimensions/.../{region,entities,poi}` 가 들어 있습니다.

```bash
# ★ 서버 종료 상태에서 ★
BK=~/minecraft-server/world_trim-backup-20260623-191113   # ← 실제 백업 폴더명
W=~/minecraft-server/world

# 삭제 후 새로 생성된 청크와 섞이지 않게, 현재 청크 폴더를 먼저 비우고 백업으로 교체
rm -rf "$W/dimensions/minecraft/overworld/region" \
       "$W/dimensions/minecraft/overworld/entities" \
       "$W/dimensions/minecraft/overworld/poi"
cp -R "$BK/world/dimensions/minecraft/overworld/"{region,entities,poi} \
      "$W/dimensions/minecraft/overworld/"
# level.dat / data 도 원복(구조물·raids·맵 참조 일관성)
cp -R "$BK/world/level.dat" "$BK/world/data" "$W/"
```
(백업 폴더 안 실제 경로를 보고 맞게 복사하세요. 처리한 차원이 여러 개면 각 차원 반복.)

---

## 8. 옵션 (필요할 때만, 명령행 플래그 — 파일 수정 X)

| 플래그 | 의미 | 예시 |
|---|---|---|
| `--pad N` | 보존 원을 N청크 더 크게(거점 넉넉히) | `... "world" --pad 4` |
| `--dims D...` | 처리 차원(기본 오버월드). 네더/엔드/모드차원 추가 | `... "world" --dims minecraft/overworld minecraft/the_nether` |
| `--a X Z` / `--b X Z` | 보존 원 두 꼭짓점(청크 좌표). 거점이 다르면 | `... "world" --a 27 159 --b 62 165` |
| `--force` | `--apply` 시 yes 확인 생략(자동화용) | `... "world" --apply --force` |
| `--help` | 전체 사용법 | `python3 herma-chunk-trim.py --help` |

---

## 9. FAQ / 트러블슈팅

**Q. `보존 청크: 0` 으로 나와요.**
→ 거점 좌표가 보존 원 밖이거나 WORLD 경로가 다른 월드입니다. 게임에서 거점에 서서 `F3` → **Chunk** 줄의 X·Z 가 대략 X 27~62, Z 159~165 범위인지 확인. 다르면 `A`,`B` 를 그 값으로 수정.

**Q. `region/entities/poi 폴더를 못 찾음`.**
→ `WORLD` 가 `world` 폴더가 아니거나, 아직 한 번도 안 띄운 월드. `ls $WORLD/dimensions/minecraft/overworld` 에 `region` 이 있는지 확인.

**Q. `python3: command not found`.**
→ `xcode-select --install` 후 재시도, 또는 `brew install python3`.

**Q. 적용했는데 거점 근처도 새 지형으로 바뀌었어요.**
→ 거점이 보존 원 밖이었던 것. 백업으로 원복(7번) 후 좌표(`A`,`B`) 조정해서 다시.

**Q. 파일 용량이 안 줄어요.**
→ 정상입니다(삭제된 청크는 "미생성 표시"만 — 게임이 재생성). 용량 회수가 필요하면 MCA Selector 의 region 최적화를 쓰면 되지만 필수 아님.

---

## 10. 핵심 주의 3가지

1. **서버 끄고 실행** (켜진 채 금지).
2. **미리보기 먼저**, 숫자 확인 후 `--apply`.
3. **백업 폴더 보관** (문제 시 원복용). 정상 확인되면 나중에 삭제.

---

*기술 메모: 거점 두 꼭짓점 A(27,159)·B(62,165) 의 외접원(중심=중점, 반지름=대각선/2)을 보존 영역으로 사용. `.mca` region 헤더의 location 엔트리를 0으로 만들어 해당 청크를 "미생성"으로 표시(데이터는 안전, 게임이 재생성). MC 26.x `world/dimensions/<ns>/<dim>/{region,entities,poi}` 구조 + 구버전 폴백 지원. 실제 26.1.2 월드로 dry-run·apply 검증 완료.*
