#!/usr/bin/env python3
"""packwiz side 교정 (NeoForge RPG 팩) — 싱글플레이(통합 서버) 기준.

이 팩은 싱글플레이로 플레이/테스트된다(런처 RPG 채널 = 멀티 자동접속 생략).
싱글플레이 클라는 *통합 서버*를 돌리므로 월드젠/구조물/라이브러리 등 모든 컨텐츠를
가져야 한다 → 절대 "server" 로 좁히지 않는다. 전용 서버(server-rpg)는 server+both 를
동기화하므로 클라 전용 렌더 모드(server_side=unsupported, 예: sodium/iris)만 자동 제외된다.

권장 side 는 둘뿐:
  - server_side == "unsupported" (클라 전용 렌더) -> "client"
  - 그 외 모두                                   -> "both"

명시 "client" 는 보존(손으로 지정한 렌더 전용). "both"/"server" 는 위 규칙으로 교정. 멱등.
사용: python fix-sides.py <slug...> (없으면 mods/*.pw.toml 전체)

배경(2026-06-21 실기기 크래시): 과거 Modrinth env 기반으로 server 로 좁힌 월드젠/구조물
14개(terralith/incendium/structory/when-dungeons-arise/dungeons-and-taverns/towns-and-towers/
tectonic/lithostitched/moogs-*/cristel-lib/yungs-better-dungeons)와 smartbrainlib(occultism 의
클라 hard-dep)이 싱글플레이 클라에서 누락 → occultism 'smartbrainlib MISSING' 크래시 +
구조물 미생성. dedicated-server+thin-client 최적화가 싱글플레이엔 부적합 → 본 모델로 해소.
"""
import json, re, sys, glob, os
import urllib.request

UA = {"User-Agent": "Hermaphrodite-world/fix-sides-neoforge"}


def _get(url):
    try:
        with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=15) as r:
            return json.load(r)
    except Exception:
        return None


def env(slug):
    d = _get(f"https://api.modrinth.com/v2/project/{slug}")
    if not isinstance(d, dict) or "id" not in d:
        return None
    return d.get("client_side"), d.get("server_side")


def recommend(c, s):
    # 싱글플레이 클라가 모든 컨텐츠를 가져야 하므로 "server" 는 추천하지 않는다.
    if s == "unsupported":  # 서버 미지원 = 클라 전용(렌더)
        return "client"
    return "both"           # 싱글플레이 클라 필수(전용 서버도 both 동기화로 받음)


def cur_side(path):
    m = re.search(r'^side = "(\w+)"', open(path, encoding="utf-8").read(), re.M)
    return m.group(1) if m else "both"


def main(argv):
    slugs = [s for s in argv if os.path.exists(f"mods/{s}.pw.toml")] if argv \
        else [os.path.basename(p)[:-8] for p in sorted(glob.glob("mods/*.pw.toml"))]
    changed = 0
    for slug in slugs:
        path = f"mods/{slug}.pw.toml"
        cur = cur_side(path)
        e = env(slug)
        if e is None:
            print(f"  ? {slug}: 메타 조회 실패 (그대로 둠)"); continue
        c, s = e
        want = recommend(c, s)
        # 손으로 지정한 client(렌더 전용)는 보존 — Modrinth 가 both 라 해도 클라 전용 의도 유지.
        if cur == "client" and want != "client":
            print(f"  keep {slug}: client (수동 지정 보존)"); continue
        if want != cur:
            txt = open(path, encoding="utf-8").read()
            txt = re.sub(r'^side = "\w+"', f'side = "{want}"', txt, count=1, flags=re.M)
            open(path, "w", encoding="utf-8").write(txt)
            print(f"  fix {slug}: {cur} -> {want}  (client={c}, server={s})")
            changed += 1
    print(f"side 교정 {changed}건.")


if __name__ == "__main__":
    main(sys.argv[1:])
