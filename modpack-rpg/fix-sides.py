#!/usr/bin/env python3
"""packwiz side 교정 (NeoForge 팩) — Modrinth env + 의존성 그래프 기준.

modpack/fix-sides.py 의 NeoForge 변종. 로직 동일, 조회 loader=neoforge.
- env(client_side/server_side)로 both 오감지를 server/client 로 좁힘.
- ★ 의존성 가드: client/both 모드가 hard-require 하는 dep 은 server 로 좁히지 않음
  (Fabric 측 defaulted/veinminer 'Incompatible mods' 크래시와 동일 클래스 방지).
- both 인 것만 수정, 명시 client/server 미수정, 멱등.
사용: python fix-sides.py <slug...>  (없으면 mods/*.pw.toml 전체)
"""
import json, re, sys, glob, os, urllib.parse, urllib.request

UA = {"User-Agent": "Hermaphrodite-world/fix-sides-neoforge"}
LOADER = "neoforge"
GV = "1.21.1"


def _get(url):
    try:
        with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=15) as r:
            return json.load(r)
    except Exception:
        return None


def project(slug):
    d = _get(f"https://api.modrinth.com/v2/project/{slug}")
    if not isinstance(d, dict) or "id" not in d:
        return None
    return d["id"], d.get("client_side"), d.get("server_side")


def required_dep_ids(slug):
    q = urllib.parse.urlencode({"loaders": json.dumps([LOADER]), "game_versions": json.dumps([GV])})
    v = _get(f"https://api.modrinth.com/v2/project/{slug}/version?{q}")
    out = set()
    if isinstance(v, list) and v:
        for d in v[0].get("dependencies", []):
            if d.get("dependency_type") == "required" and d.get("project_id"):
                out.add(d["project_id"])
    return out


def recommend(c, s):
    if c == "unsupported":
        return "server"
    if s == "unsupported":
        return "client"
    if c == "optional" and s == "required":
        return "server"
    return "both"


def cur_side(path):
    m = re.search(r'^side = "(\w+)"', open(path, encoding="utf-8").read(), re.M)
    return m.group(1) if m else "both"


def main(argv):
    slugs = [s for s in argv if os.path.exists(f"mods/{s}.pw.toml")] if argv \
        else [os.path.basename(p)[:-8] for p in sorted(glob.glob("mods/*.pw.toml"))]
    meta, deps = {}, {}
    for slug in slugs:
        p = project(slug)
        if p is None:
            print(f"  ? {slug}: 메타 조회 실패 (그대로 둠)"); continue
        pid, c, s = p
        meta[slug] = {"pid": pid, "rec": recommend(c, s), "c": c, "s": s}
        deps[slug] = required_dep_ids(slug)
    needs_client = set()
    for slug, ds in deps.items():
        if meta.get(slug, {}).get("rec") in ("both", "client"):
            needs_client |= ds
    changed = 0
    for slug in slugs:
        if slug not in meta:
            continue
        path = f"mods/{slug}.pw.toml"
        if cur_side(path) != "both":
            continue
        want = meta[slug]["rec"]
        if want == "server" and meta[slug]["pid"] in needs_client:
            print(f"  keep {slug}: both (client 모드가 require 하는 의존)")
            continue
        if want != "both":
            txt = open(path, encoding="utf-8").read()
            txt = re.sub(r'^side = "\w+"', f'side = "{want}"', txt, count=1, flags=re.M)
            open(path, "w", encoding="utf-8").write(txt)
            print(f"  fix {slug}: both -> {want}  (client={meta[slug]['c']}, server={meta[slug]['s']})")
            changed += 1
    print(f"side 교정 {changed}건.")


if __name__ == "__main__":
    main(sys.argv[1:])
