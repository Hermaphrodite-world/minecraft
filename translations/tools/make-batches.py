#!/usr/bin/env python3
"""번역 배치 매니페스트 생성 — _work/*/*.todo.json 을 키 수 기준 bin-pack.

각 배치는 ~TARGET 키를 넘지 않게 todo 파일을 묶는다(파일 단위 — 한 파일은 한 배치).
엔진 배정: 일부 배치를 codex 로(병렬 페어), 나머지 claude.
출력: args 로 넘길 JSON 배열을 stdout 에. ASCII only.
"""
import json, glob, os, sys
try: sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception: pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WORK = os.path.join(ROOT, "_work")
TARGET = max(1, int(sys.argv[1])) if len(sys.argv) > 1 else 550
CODEX_EVERY = max(1, int(sys.argv[2])) if len(sys.argv) > 2 else 4  # N번째 배치마다 codex (0/음수 → ZeroDivisionError 방지, review F-5)

files = []
for p in sorted(glob.glob(os.path.join(WORK, "*", "*.todo.json"))):
    n = len(json.load(open(p, encoding="utf-8")))
    if n == 0:
        continue
    rel = os.path.relpath(p, ROOT).replace(os.sep, "/")  # translations/ 기준
    files.append((n, rel))

files.sort(reverse=True)  # 큰 것 먼저(bin-pack)

batches = []
cur, cur_keys = [], 0
def flush():
    global cur, cur_keys
    if cur:
        batches.append((cur, cur_keys)); cur, cur_keys = [], 0

for n, rel in files:
    if n >= TARGET:           # 단일 파일이 타깃 이상 -> 자체 배치
        batches.append(([rel], n)); continue
    if cur_keys + n > TARGET:
        flush()
    cur.append(rel); cur_keys += n
flush()

out = []
for i, (fl, keys) in enumerate(batches):
    out.append({
        "id": i,
        "engine": "codex" if (i % CODEX_EVERY == CODEX_EVERY - 1) else "claude",
        "keys": keys,
        "files": fl,
    })
total_keys = sum(b["keys"] for b in out)
ncodex = sum(1 for b in out if b["engine"] == "codex")
sys.stderr.write(f"batches={len(out)} total_keys={total_keys} codex={ncodex} claude={len(out)-ncodex}\n")
print(json.dumps(out, ensure_ascii=False))
