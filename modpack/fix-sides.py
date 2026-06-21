#!/usr/bin/env python3
"""packwiz side 교정 — Modrinth client_side/server_side 선언 + 의존성 그래프 기준.

packwiz 자동감지는 서버권위 컨텐츠(월드젠/구조물)를 종종 both 로 잘못 넣는다.
이러면 (1) 클라가 불필요하게 다운로드 (2) client-미지원 의존(lithostitched 등)이
both 모드의 의존으로 클라에 끌려와 크래시. 본 스크립트가 env 선언으로 교정한다.

★ 의존성 인식 (review 후속): env 만 보면 안 된다 — `client_side=optional/server_side=required`
  라이브러리라도 **client/both 모드가 hard-require** 하면 클라에 있어야 한다(없으면 Fabric
  'Incompatible mods' 크래시). 실제 사례: combatify(both)→defaulted, veinminer-client(client)→
  veinminer. 따라서 server 로 좁히기 전에 "client 측 모드가 require 하는 dep 인가"를 확인하고,
  그렇다면 both 로 둔다.

안전 규칙: **현재 side 가 both 인 것만** 좁힌다. 명시적 client/server 는 의도로 보고 미수정(멱등).

사용: python fix-sides.py <slug> [slug ...]   (build-pack.sh 는 RPG+CONV 신규분 전달)
인자가 없으면 mods/*.pw.toml 전체 감사.
"""
import json, re, sys, glob, os, urllib.parse, urllib.request

UA = {"User-Agent": "Hermaphrodite-world/fix-sides"}


def _get(url):
    try:
        with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=15) as r:
            return json.load(r)
    except Exception:
        return None


def project(slug):
    """Modrinth project 메타: (project_id, client_side, server_side) 또는 None."""
    d = _get(f"https://api.modrinth.com/v2/project/{slug}")
    if not isinstance(d, dict) or "id" not in d:
        return None
    return d["id"], d.get("client_side"), d.get("server_side")


def required_dep_ids(slug):
    """이 mod 의 최신 fabric/26.1.2 버전이 'required' 로 거는 의존 project_id 집합."""
    q = urllib.parse.urlencode({"loaders": json.dumps(["fabric"]),
                                "game_versions": json.dumps(["26.1.2"])})
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
        return "server"  # 서버권위 컨텐츠 — 클라는 바닐라 동기화로 렌더
    return "both"


def cur_side(path):
    m = re.search(r'^side = "(\w+)"', open(path, encoding="utf-8").read(), re.M)
    return m.group(1) if m else "both"


def main(argv):
    if argv:
        slugs = [s for s in argv if os.path.exists(f"mods/{s}.pw.toml")]
    else:
        slugs = [os.path.basename(p)[:-8] for p in sorted(glob.glob("mods/*.pw.toml"))]

    # 1) 각 slug 의 env-추천 + required 의존 수집 (1 slug = 2 API 호출)
    meta = {}   # slug -> {"pid": project_id, "rec": env추천}
    deps = {}   # slug -> set(required project_id)
    for slug in slugs:
        p = project(slug)
        if p is None:
            print(f"  ? {slug}: 메타 조회 실패 (그대로 둠)")
            continue
        pid, c, s = p
        meta[slug] = {"pid": pid, "rec": recommend(c, s), "c": c, "s": s}
        deps[slug] = required_dep_ids(slug)

    # 2) 의존성 그래프: client 측(both/client) 모드가 require 하는 project_id 집합
    needs_client = set()
    for slug, ds in deps.items():
        if meta.get(slug, {}).get("rec") in ("both", "client"):
            needs_client |= ds

    # 3) 적용 — both 인 것만, 의존-그래프 가드 적용
    changed = 0
    for slug in slugs:
        if slug not in meta:
            continue
        path = f"mods/{slug}.pw.toml"
        if cur_side(path) != "both":
            continue  # 명시 client/server 는 의도 → 미수정
        want = meta[slug]["rec"]
        if want == "server" and meta[slug]["pid"] in needs_client:
            # client/both 모드의 hard-require 의존 → 클라에 있어야 함. 좁히지 않는다.
            print(f"  keep {slug}: both (client 모드가 require 하는 의존 — server 좁힘 방지)")
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
