# Modrinth version API — 로더 필터 누락 시 거짓 "가용성" (다른 로더 버전이 잡힘)

> 특정 로더(Fabric) 모드팩에서 모드 가용성을 확인할 때, `loaders` 필터 없이 `game_versions`만
> 쿼리하면 *다른 로더*(Forge/NeoForge) 버전까지 세어 "가능"으로 오판한다.

## 증상

26.1.2(Fabric) 팩에 추가할 마법 모드를 조사하며 Modrinth API로 가용성을 확인:
- "**Occultism 49버전 / Forbidden Arcanus 7버전 → 26.1.2 가능**"이라 사용자에게 보고
- 이후 `loaders=["fabric"]` 필터로 재확인하니 **둘 다 0** (Forge/NeoForge 전용 모드, Fabric 미지원)
- 사용자에게 정정("26.1.2 Fabric엔 이 둘 불가") — 오보고 1회 발생

## 원인

Modrinth version 엔드포인트를 **`game_versions`만** 필터하면 *모든 로더*의 버전이 반환된다:

```
# 잘못: 모든 로더(Forge/NeoForge/Fabric/Quilt) 버전이 다 잡힘
/v2/project/occultism/version?game_versions=["26.1.2"]   → 49건 (전부 NeoForge/Forge)
```

Occultism·Forbidden Arcanus는 26.1.2 **NeoForge/Forge** 빌드는 있으나 **Fabric 빌드는 0**. 그래서
"49/7건 존재"는 사실이지만, **Fabric 팩 기준 가용성은 0**이다. 버전 수만 보고 "가능"으로 단정한 게 오류.

## 해결

가용성 체크는 **항상 `loaders=[대상 로더]`를 동반**한다:

```
# 올바름: 대상 로더로 필터
/v2/project/occultism/version?loaders=["fabric"]&game_versions=["26.1.2"]   → 0건 = 불가
```

```python
def avail(slug, loader, gv):
    q = urllib.parse.urlencode({"loaders": json.dumps([loader]),
                                "game_versions": json.dumps([gv])})
    d = api(f"https://api.modrinth.com/v2/project/{slug}/version?{q}")
    return len(d) if isinstance(d, list) else 0
```

- **대조군 검증**: 팩에 *실제로 들어있는* 모드(sodium/jade 등)를 같은 쿼리로 확인해 쿼리가 동작하는지 교차검증.

## 예방

- 특정 로더 모드팩의 모드 **가용성/이식성 판단 시 `game_versions` + `loaders` 둘 다 필터**.
- **"버전 수 > 0" ≠ "내 팩에서 가능"** — 로더 일치를 반드시 확인. 멀티로더 모드(Architectury 기반)와 단일로더 모드를 구분.
- 사용자에게 가용성 보고 전, 1개라도 의심되면 대상 로더 필터로 재확인(특히 Forge/NeoForge 전통 모드인 마법/콘텐츠 모드는 Fabric 미지원이 흔함).

## 추가 (2026-06-23): 단일 플랫폼 조회 = 거짓 *음성* — Modrinth↔CurseForge 커버리지 불일치

위 `loaders` 누락은 거짓 **양성**(false-positive). 그 **거울**이 단일 플랫폼만 조회한 거짓 **음성**(false-negative):

- **실사례**: RPG 게이팅용 모드(Pufferfish's Skills, Dynamic Difficulty)의 26.1 가용성을 **Modrinth 로만** 확인 → "1.21.x 천장, 26.1 없음"으로 판단(거짓 음성).
- 그러나 **CurseForge 엔 26.1 빌드 존재**: `puffish_skills-0.17.4-26.1-fabric.jar`(2026-04). Modrinth 와 CF 의 **버전 커버리지가 다르다**(CF 가 더 최신인 경우 있음).
- **로더별로도 갈림**: Dynamic Difficulty 는 **NeoForge `1.2.0+26.1.2`** 는 있으나 **Fabric 은 `1.1.1+1.21.11` 천장** — "26.1.2 있음"이 로더에 따라 참/거짓.
- **CF 모드팩 pack-inclusion ≠ native 버전**: CurseForge 모드팩은 파일의 MC 버전을 강제하지 않아 **1.21.x 빌드를 26.1.2 팩에 cross-version 으로 끼워넣는다**(new-age-adventures 가 그 예). "팩이 26.1.2 = 그 안 모드가 전부 26.1.2 네이티브"는 거짓.

**결론(양방향)**: 모드 가용성은 ① `loaders`+`game_versions` 둘 다 필터(false-positive 차단) ② **Modrinth 와 CurseForge 양쪽** 확인(false-negative 차단) ③ 로더별 빌드 분리 확인. 확정은 `packwiz <modrinth|curseforge> add`(실 해석/실패가 곧 검증) 또는 양 플랫폼 files 페이지 교차.

## 관련 문서

- [neoforge-server-windows-gitbash-unix-args-classpath-crash.md](neoforge-server-windows-gitbash-unix-args-classpath-crash.md) — 같은 RPG 작업: 헤드리스 서버 실행/아이템 열거
- [mc-26-resourcepack-compat-whitelist-drops-compatible.md](mc-26-resourcepack-compat-whitelist-drops-compatible.md) — MC 가용성/런타임 검증 일반
- [modpack-packwiz-side-singleplayer-needs-server-mods.md](modpack-packwiz-side-singleplayer-needs-server-mods.md) — 동일 RPG 팩 작업의 side 분류 함정
