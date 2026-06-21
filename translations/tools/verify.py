#!/usr/bin/env python3
"""커버리지 검증 + 플레이스홀더 무결성 점검. 출력 ASCII.

- 커버리지: en 레퍼런스의 모든 키가 리소스팩 ko_kr.json 에 비어있지 않게 존재하는가.
- 플레이스홀더: en 값의 %s/%d/{0}/§x 등이 ko 값에도 동일 개수로 보존됐는가(누락 경고).
"""
import sys, os, json, glob, re
try: sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception: pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WORK = os.path.join(ROOT, os.environ.get('HERMA_WORK', '_work'))
PACK = os.path.join(ROOT, os.environ.get('HERMA_PACK_SRC', 'herma-ko'))
# 서식 코드(§a/&a)는 번역자가 정당하게 변경 가능 → 비교에서 제외.
# 누락 시 깨지는 format-critical 플레이스홀더(%s/%d/%f/%1$s/{0}/{name})만 검사.
PH = re.compile(r'%(?:\d+\$)?[sdf]|%%|\{\d*\}|\{[a-zA-Z_]+\}')

def L(p):
    return json.load(open(p, encoding='utf-8')) if os.path.exists(p) else {}

def ph(s):
    return sorted(PH.findall(str(s)))

total_en = total_cov = 0
incomplete = []; phwarn = []
per_mod = {}
for en_path in glob.glob(os.path.join(WORK, '*', '*.en.json')):
    slug = os.path.basename(os.path.dirname(en_path))
    ns = os.path.basename(en_path)[:-len('.en.json')]
    en = L(en_path)
    ko = L(os.path.join(PACK, 'assets', ns, 'lang', 'ko_kr.json'))
    miss = 0
    for k, v in en.items():
        if str(v).strip() == '':
            continue  # 빈 원본(레이아웃 spacer) -> 번역 불필요, 커버로 간주
        kv = ko.get(k)
        if kv is None or str(kv).strip() == '':
            miss += 1; continue
        if ph(v) != ph(kv):
            phwarn.append((slug, ns, k))
    cov = len(en) - miss
    pm = per_mod.setdefault(slug, [0, 0])
    pm[0] += len(en); pm[1] += cov
    total_en += len(en); total_cov += cov
    if miss:
        incomplete.append((slug, ns, miss, len(en)))

pct = 100.0 * total_cov / max(total_en, 1)
print("=== COVERAGE: %d/%d keys = %.2f%% ===" % (total_cov, total_en, pct))
print("mods fully done: %d / %d" %
      (sum(1 for s, (t, c) in per_mod.items() if t == c), len(per_mod)))
if incomplete:
    print("\nINCOMPLETE namespaces (%d):" % len(incomplete))
    for slug, ns, miss, tot in sorted(incomplete, key=lambda x: -x[2]):
        print("  %-28s %-26s missing %d/%d" % (slug, ns, miss, tot))
if phwarn:
    print("\nPLACEHOLDER MISMATCH (%d keys) - review:" % len(phwarn))
    for slug, ns, k in phwarn[:40]:
        print("  %-24s %-22s %s" % (slug, ns, k))
    if len(phwarn) > 40:
        print("  ... +%d more" % (len(phwarn) - 40))
if pct >= 100.0 and not incomplete:
    print("\n*** 100% COVERAGE REACHED ***")
