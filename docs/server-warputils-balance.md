# WarpUtils 밸런스 설정 가이드 — "home만 + 3초 워밍업"

> 친구 서버 밸런스 보존용. **목표**: `/warp`·`/tpa`·`/back`·`/tpr` 같은 순간이동을 **전부 제거**하고,
> 남기는 건 **`/home`·`/sethome`** 뿐. 단 `/home` 은 **입력 후 3초 가만히 있어야 이동**(움직이면 취소).
>
> 적용 대상: 서버 운영자(OP). WarpUtils 설정은 `config/warputils/`(서버 첫 실행 시 생성)에 저장되는
> **런타임 데이터**라 repo 가 아니라 **서버에서 직접** 적용합니다.

---

## 전제

1. 서버를 **최소 1회 기동**해 `config/warputils/` 가 생성돼 있어야 함 (`server/setup.md` §1~6).
2. 본인이 **OP** 여야 함: 서버 콘솔에서 `op <본인닉>`.
3. 적용 위치: **서버 콘솔**(명령 앞 `/` 생략 가능) 또는 **게임 내 OP 채팅**.

---

## 1단계 — 밸런스 파괴 기능 비활성 (warps / tpas / back / tpr)

각 기능을 `disabled = true` 로. **탭 자동완성**으로 정확한 인자를 확인하며 입력(아래는 구조):

```
/warputils config warps disabled set true
/warputils config tpas  disabled set true
/warputils config back  disabled set true
/warputils config tpr   disabled set true
```

- `/warputils config ` 까지 친 뒤 **Tab** 을 누르면 카테고리(`homes`/`warps`/`tpas`/`back`/`tpr`/`general`)와
  설정(`disabled`/`delay`/`cooldown`/…), 동작(`set`/`get`/`reset`)이 차례로 자동완성됩니다.
- 성공 시 메시지 예: `Warps have been disabled!` (`feature.warps.disabled`).
- 결과: `/warp`·`/setwarp`·`/delwarp`·`/tpa`·`/tpaccept`·`/back`·`/tpr` 등이 **전원(OP 포함) 사용 불가**.

> 남는 명령: `/home`, `/sethome`, `/delhome`, `/homes` 만.

---

## 2단계 — `/home` 에 3초 워밍업

`homes` 의 `delay` 를 **3** 으로:

```
/warputils config homes delay set 3
```

- WarpUtils 의 `delay` = **워밍업(가만히 있어야 이동)**. 진행 중 **움직이면 취소**되고
  `Your teleportation was cancelled!`(`common.delay.moved`) 가 뜸. 진행 바도 표시됨.
- ⚠️ **단위 확인**: 값이 **초** 면 `3`, **틱** 이면 `60`(= 3초). `set` 후 `/home` 을 직접 써보고
  실제 대기 시간으로 확인하세요. (어긋나면 `delay set 60` 또는 `delay set 3` 로 조정.)
- (선택) 전투 중 텔레포트 차단을 원하면 `general` 또는 fight-cooldown 설정을 추가로.

---

## 3단계 — 확인

1. **일반 유저(비-OP) 친구**가 접속해 `/home` → 3초 워밍업 후 이동, **움직이면 취소** 확인.
2. `/warp`, `/tpa`, `/back` 입력 → "기능 비활성"/명령 없음 확인.
3. `/sethome` 으로 집 저장 → `/home` 으로 복귀 확인.

---

## 참고 — config 파일 직접 편집 (대안)

인게임 명령 대신 파일을 편집해도 됨. `config/warputils/` 안의 카테고리별 파일에서:

- 각 비활성 기능: `"disabled": true`
- `homes`: `"delay": 3` (단위는 위와 동일하게 확인)

편집 후 **서버 재시작**(또는 `/warputils config ... reset`/reload 가 있으면 그것)으로 반영.
정확한 파일명·키·단위가 헷갈리면, 생성된 `config/warputils/` 폴더를 공유해 주면 정확한 값으로 정리해 드립니다.

> ❌ **LuckPerms 안 됨**: WarpUtils 는 LuckPerms 권한 노드를 지원하지 않습니다(권한 레벨 하드코딩).
> `lp ... warputils.command.home` 류는 동작하지 않으니 위 WarpUtils config 가 유일한 SoT 입니다.

---

## 친구에게 안내할 한 줄

> "이 서버는 밸런스를 위해 **순간이동은 `/home`(+`/sethome`)만** 돼. `/home` 치면 **3초 가만히** 있어야
> 이동하고, 그 사이 움직이면 취소돼. 워프·tpa·back 은 막아놨어."
