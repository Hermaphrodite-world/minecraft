# NeoForge 전용 서버를 Windows Git Bash 에서 `run.sh` 로 띄우면 classpath `:` 구분자 크래시 — `win_args.txt` 써야

> NeoForge(`--installServer`)는 `run.sh`(+`unix_args.txt`, classpath 구분자 `:`)와 `run.bat`(+`win_args.txt`, `;`)를
> 둘 다 만든다. Windows(Git Bash 포함)에서 무심코 `run.sh`/`unix_args.txt` 로 띄우면 JVM 이 `:` 를 경로 일부로
> 해석해 **부트 레이어 초기화 즉시 실패**한다.

## 증상

서버가 2줄 로그만 남기고 즉시 종료(`Done` 도달 못 함):

```
Error occurred during initialization of boot layer
java.nio.file.InvalidPathException: Illegal char <:> at index 70:
  libraries/cpw/mods/bootstraplauncher/2.0.2/bootstraplauncher-2.0.2.jar:libraries/cpw/mods/securejarhandler/...jar:...
```

`index 70` 의 `<:>` = 두 번째 jar 앞의 classpath 구분자 `:`.

## 원인

- `run.sh` 는 `java @user_jvm_args.txt @libraries/net/neoforged/neoforge/<v>/unix_args.txt "$@"` 를 실행.
- `unix_args.txt` 의 `-p`(module path)/classpath 가 jar 들을 **`:`** 로 join (Unix 규약).
- **Windows JVM 의 path 구분자는 `;`** — `:` 를 만나면 경로 리터럴의 illegal char(`InvalidPathException`)로 본다.
- Git Bash 라 `.sh` 가 자연스러워 보여 무심코 `sh run.sh` 한 게 함정. (Git Bash 는 셸일 뿐, 그 안의 `java` 는 **Windows JVM**.)
- 같은 머신의 이전 성공 부팅은 `run.bat`(Windows) 로 띄웠던 것 — `unix_args` vs `win_args` 차이를 못 본 채 `run.sh` 재시도하면 재현.

## 해결

Windows 에서는 `win_args.txt`(구분자 `;`) 를 쓴다. Git Bash 에서도 java 직접 호출 가능(java 가 argfile 을 파싱하므로 `;` 정상):

```bash
# run.bat 과 동일한 인자, Git Bash 에서:
java @user_jvm_args.txt @libraries/net/neoforged/neoforge/<version>/win_args.txt nogui
# 또는 cmd //c "run.bat nogui"
```

`win_args.txt` 의 forward-slash 상대경로는 Windows java 가 그대로 수용. cwd 는 서버 폴더.

## 부수 기법 — 헤드리스 서버로 아이템/태그 열거 (KubeJS)

게임 실행 없이 모드 아이템 ID(예: PMMO 게이팅용 `c:swords`/`c:armors` 멤버)를 뽑으려면:

- `kubejs/server_scripts/` 에 덤프 스크립트: `ServerEvents.loaded(e => { JsonIO.write('kubejs/out.json', [...]) })`.
  전체 아이템 = `Item.getTypeList()`, 태그 멤버 = `Ingredient.of('#c:swords').itemIds`. 둘 다 try/catch + fallback.
- **Rhino 함정**: 콜백 안 `const candTags = [...]` 가 `redeclaration of var` 로 죽을 수 있음 → **배열 inline**(이름 없는 `;[...].forEach`)로 회피.
- **auto-stop**: `(sleep 180; echo stop) | java @... nogui > boot.log 2>&1` — stdin 으로 `stop` 전달해 깔끔 종료(백그라운드 + 종료 알림). 월드 기생성 시 `Done` 이 ~2s.
- 콘솔 한글 **cp949 mojibake** 는 무시(파일은 UTF-8 정상 — § windows-subprocess-cp949).

## 예방

- Windows 서버 부팅 = `run.bat` / `win_args.txt` 고정. `run.sh`/`unix_args.txt` 는 Linux/macOS 전용.
- **부팅 부작용**: MC 서버는 부팅 시 `server.properties` 를 기본 포맷으로 **재작성**(주석·구조 손실). tracked scaffold(주석 포함)면 부팅 후 `git checkout -- server.properties` 로 원복.
- 일회성 KubeJS 덤프 스크립트는 검증 후 제거(향후 서버 부팅 로그 오염 방지).
