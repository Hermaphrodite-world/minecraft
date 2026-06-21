#!/usr/bin/env python3
"""RPG 팩 번역 전수조사 — 모든 jar(JIJ 중첩 포함)의 번역 가능 텍스트를 스캔해
현재 herma-ko-rpg 커버리지와 비교, 누락분을 보고한다.

기존 extract.py 는 top-level jar 의 assets/<ns>/lang/en_us.json 만 봤다. 이 스크립트는:
  - JIJ(META-INF/jars, META-INF/jarjar) 중첩 jar 까지 재귀 스캔
  - assets/<ns>/lang/en_us.json + ko_kr.json 전수 수집
  - assets/<ns>/patchouli_books/.../en_us/** (가이드북, 리소스팩 오버라이드 가능) 카운트
출력 ASCII.
"""
import sys, os, re, json, io, zipfile, glob
try: sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception: pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
JARS = os.path.join(ROOT, '_jars-rpg')
PACK = os.path.join(ROOT, 'herma-ko-rpg')

LANG = re.compile(r'^assets/([^/]+)/lang/([^/]+)\.json$')
BOOK = re.compile(r'^assets/([^/]+)/patchouli_books/([^/]+)/en_us/(.+\.json)$')
NEST = re.compile(r'^META-INF/(?:jars|jarjar)/.+\.jar$')

def load_json(b):
    try:
        d = json.loads(b.decode('utf-8'))
        return d if isinstance(d, dict) else {}
    except Exception:
        out = {}
        for k, v in re.findall(rb'"((?:[^"\\]|\\.)*)"\s*:\s*"((?:[^"\\]|\\.)*)"', b):
            try: out[json.loads('"'+k.decode()+'"')] = json.loads('"'+v.decode()+'"')
            except Exception: pass
        return out

ns_acc = {}   # ns -> {'en':{}, 'ko':{}}
books = {}     # ns -> set(book ids)
nest_seen = set()

def scan(data, depth=0):
    try:
        z = zipfile.ZipFile(io.BytesIO(data))
    except Exception:
        return
    for n in z.namelist():
        m = LANG.match(n)
        if m:
            ns, fn = m.group(1), m.group(2).lower()
            if fn in ('en_us', 'ko_kr'):
                ns_acc.setdefault(ns, {'en': {}, 'ko': {}})
                ns_acc[ns]['en' if fn == 'en_us' else 'ko'].update(load_json(z.read(n)))
            continue
        b = BOOK.match(n)
        if b:
            books.setdefault(b.group(1), set()).add(b.group(2)); continue
        if depth < 3 and NEST.match(n):
            try: scan(z.read(n), depth + 1)
            except Exception: pass

for jar in sorted(glob.glob(os.path.join(JARS, '*.jar'))):
    scan(open(jar, 'rb').read())

# 커버리지: herma-ko-rpg/assets/<ns>/lang/ko_kr.json (비어있지 않은 값)
def covered(ns):
    p = os.path.join(PACK, 'assets', ns, 'lang', 'ko_kr.json')
    if not os.path.exists(p): return {}
    d = json.load(open(p, encoding='utf-8'))
    return {k for k, v in d.items() if str(v).strip() != ''}

gap_ns = []   # (ns, en_total, uncovered_count, jij_only)
total_uncov = 0
pack_ns = set(os.listdir(os.path.join(PACK, 'assets'))) if os.path.isdir(os.path.join(PACK, 'assets')) else set()
for ns, d in sorted(ns_acc.items()):
    en = {k: v for k, v in d['en'].items() if str(v).strip() != ''}
    if not en: continue
    cov = covered(ns)
    modko = set(d['ko'])          # 모드 자체 ko (시드 가능)
    uncov = [k for k in en if k not in cov and k not in modko]
    if uncov:
        gap_ns.append((ns, len(en), len(uncov), ns not in pack_ns))
        total_uncov += len(uncov)

print("=== 누락 네임스페이스 (en 키 중 ko 없음, 모드자체 ko 도 없음) ===")
print("%-32s %7s %9s %s" % ("namespace", "en_keys", "uncov", "JIJ-only?"))
print("-"*64)
for ns, en, uncov, jij in sorted(gap_ns, key=lambda x: -x[2]):
    print("%-32s %7d %9d %s" % (ns, en, uncov, "JIJ" if jij else ""))
print("-"*64)
print("총 미번역 키: %d  (네임스페이스 %d개)" % (total_uncov, len(gap_ns)))
print("\n=== Patchouli 가이드북 (리소스팩 오버라이드 가능, lang 밖) ===")
for ns, bs in sorted(books.items()):
    print("  %-28s books: %s" % (ns, ",".join(sorted(bs))))
if not books:
    print("  (patchouli 책 없음 — modonomicon 등은 별도 포맷일 수 있음)")
print("\n전체 스캔 네임스페이스: %d, 책 보유 ns: %d" % (len(ns_acc), len(books)))
