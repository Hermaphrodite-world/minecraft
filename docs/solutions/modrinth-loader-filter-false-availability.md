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

## 관련 문서

- [mc-26-resourcepack-compat-whitelist-drops-compatible.md](mc-26-resourcepack-compat-whitelist-drops-compatible.md) — MC 가용성/런타임 검증 일반
- [modpack-packwiz-side-singleplayer-needs-server-mods.md](modpack-packwiz-side-singleplayer-needs-server-mods.md) — 동일 RPG 팩 작업의 side 분류 함정
