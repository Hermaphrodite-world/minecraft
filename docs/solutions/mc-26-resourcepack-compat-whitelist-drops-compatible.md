# MC 26.1.2 리소스팩 호환성 — incompatibleResourcePacks 에 호환 팩을 넣으면 드롭됨

## 증상

커스텀 런처(packwiz + CmlLib)로 띄운 MC 26.1.2 에서 모드팩 리소스팩(Better-Leaves, Default Dark Mode,
Fresh Animations, Nautilus 3D, 한국어 번역 보충팩 등)이 리소스팩 화면의 "사용 가능"으로만 빠지고
"선택됨"에 안 들어감. 사용자가 수동으로 "선택됨"에 옮기고 재시작해도 다시 "사용 가능"으로 되돌아감.
유일하게 vanilla-connected-glass 만 빨강(force-enabled)으로 로드됨.

## 잘못된 진단 (4 릴리스 낭비)

처음엔 `options.txt` 의 `resourcePacks` 에는 모든 팩이 있으나 `incompatibleResourcePacks` 화이트리스트엔
vanilla-connected-glass 1개만 있는 걸 보고 **"화이트리스트 누락 → MC 가 드롭"** 으로 진단했다. 그래서
런처가 모든 활성 팩을 `incompatibleResourcePacks` 에 추가하는 "whitelist-ensure" 를 넣어 v0.1.7~0.1.9 를
릴리스했다. **전부 틀렸다 — whitelist 가 해결책이 아니라 원인이었다.**

검증을 시뮬레이션(options.txt 내용 점검)으로만 했지, 실제 MC 가 팩을 로드하는지는 한 번도 안 봤던 게 문제.

## 진짜 root cause (실측)

오프라인으로 MC 26.1.2 를 직접 띄우고 `logs/latest.log` 의 `Reloading ResourceManager:` 목록을 본 결과:

1. **호환 팩을 `incompatibleResourcePacks` 에 넣으면 MC 가 오히려 드롭한다.**
   ```
   [Render thread/INFO]: Removed resource pack file/Better-Leaves-9.5.zip
     from incompatibility list because it's now compatible
   ```
   MC 26.1.2 는 incompat 목록의 팩이 실제로는 호환되면 "now compatible" 이라며 incompat 에서 빼는데,
   그 과정에서 active(로드) 셋에서도 빠져 그 세션에 로드되지 않는다. 런처가 매 실행 다시 incompat 에
   넣으니 영구히 로드 안 됨.

2. **모드팩 팩들은 원래 호환이다.** MC 26.1.2 의 pack_version 은 `resource_major=84`(클라 `version.json`
   에서 확인). 팩들은 `supported_formats` range(예 Better-Leaves `[15,255]`)로 84 를 포함 → 호환.
   `incompatibleResourcePacks` 가 필요 없다. resourcePacks 에 넣기만 하면 로드된다.

3. **pack_format ≥ 65 면 `supported_formats` 키 자체가 거부된다.**
   ```
   JsonParseException: Pack key supported_formats is deprecated starting from pack format 65.
     Remove supported_formats from your pack.mcmeta.
   ```
   그래서 우리 한국어팩(herma-korean)을 "고치려고" `pack_format: 84` 로 바꿨더니 오히려
   `Removed ... no longer compatible` 로 제거됐다. 원본 `pack_format: 15`(+ supported_formats [15,255])가
   정상 로드된다. **format 을 올리지 말 것.**

## 해결

- **런처(ClientDefaults): `incompatibleResourcePacks` 에 손대지 않는다.** 팩 활성화는 `resourcePacks` 에
  추가(apply-once)만으로 충분. whitelist-ensure 제거(v0.1.10).
- **한국어팩 pack_format 은 원본 15 유지.** 84 로 올리면 supported_formats deprecated 로 거부됨.
- 검증: 동일 인스턴스에서 `incompatibleResourcePacks=["file/vanilla-connected-glass-0.9.zip"]` 만일 때
  7개 file 팩 전부 `Reloading ResourceManager` 에 로드됨("removed" 메시지 0). 사용자 실 환경 v0.1.10
  실행 로그로 최종 확인.

## 예방 / 교훈

- **"로드되나/적용되나" 류 질문은 실 앱을 직접 띄워 로그로 검증한다.** 리소스팩 화면의 "사용 가능/선택됨"
  표시나 options.txt 내용만으로 추측하지 말 것. MC 는 `logs/latest.log` 의 `Reloading ResourceManager:`
  가 실제 로드된 팩의 SoT.
- **오프라인 MC 테스트 하니스**: CmlLib `MSession.CreateOfflineSession` + `BuildProcessAsync(versionId)`
  로 MS 로그인·서버 접속 없이 MC 를 띄워 리소스팩 로드만 검증 가능(quickPlay 미주입 → 타이틀 화면까지).
  `WEBVIEW2`/인증 불필요. 인스턴스의 설치된 version(`fabric-loader-x-26.1.2`)을 그대로 사용.
- **"force-enable/whitelist 가 안 되면 더 강하게 whitelist" 라는 직관을 의심하라.** MC 의 incompat 메커니즘은
  "사용자가 비호환을 인지하고 강제로 켠 것"을 기억하는 용도라, 실제 호환 팩에 적용하면 역효과.
- 시뮬레이션(파일 내용 점검) 통과 ≠ 런타임 검증 통과. (CLAUDE.md § Verification Discipline 와 동일 축)
