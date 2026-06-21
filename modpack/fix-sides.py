#!/usr/bin/env python3
"""packwiz side 교정 — Modrinth client_side/server_side 선언 기준.

packwiz 자동감지는 서버권위 컨텐츠(월드젠/구조물)를 종종 both 로 잘못 넣는다.
이러면 (1) 클라가 불필요하게 다운로드 (2) client-미지원 의존(lithostitched 등)이
both 모드의 의존으로 클라에 끌려와 크래시. 본 스크립트가 env 선언으로 교정한다.

안전 규칙: **현재 side 가 both 인 것만** env 가 한쪽이라고 하면 좁힌다.
명시적 client/server 는 의도된 값일 수 있으니 건드리지 않는다(멱등).

사용: python fix-sides.py <slug> [slug ...]
인자가 없으면 mods/*.pw.toml 전체(both 인 것만) 감사.
"""
import json, re, sys, glob, os, urllib.request

UA = {"User-Agent": "Hermaphrodite-world/fix-sides"}


def env(slug):
    try:
        req = urllib.request.Request(f"https://api.modrinth.com/v2/project/{slug}", headers=UA)
        with urllib.request.urlopen(req, timeout=15) as r:
            d = json.load(r)
        return d.get("client_side"), d.get("server_side")
    except Exception:
        return None, None


def recommend(c, s):
    if c == "unsupported":
        return "server"
    if s == "unsupported":
        return "client"
    if c == "optional" and s == "required":
        return "server"  # 서버권위 컨텐츠 — 클라는 바닐라 동기화로 렌더
    return "both"


def main(argv):
    if argv:
        files = [f"mods/{s}.pw.toml" for s in argv if os.path.exists(f"mods/{s}.pw.toml")]
    else:
        files = sorted(glob.glob("mods/*.pw.toml"))
    changed = 0
    for f in files:
        slug = os.path.basename(f)[:-8]
        txt = open(f, encoding="utf-8").read()
        m = re.search(r'^side = "(\w+)"', txt, re.M)
        cur = m.group(1) if m else "both"
        if cur != "both":
            continue  # 명시 client/server 는 의도로 보고 건드리지 않음
        c, s = env(slug)
        if c is None:
            print(f"  ? {slug}: 메타 조회 실패 (그대로 둠)")
            continue
        want = recommend(c, s)
        if want != "both":
            txt = re.sub(r'^side = "\w+"', f'side = "{want}"', txt, count=1, flags=re.M)
            open(f, "w", encoding="utf-8").write(txt)
            print(f"  fix {slug}: both -> {want}  (client={c}, server={s})")
            changed += 1
    print(f"side 교정 {changed}건.")


if __name__ == "__main__":
    main(sys.argv[1:])
