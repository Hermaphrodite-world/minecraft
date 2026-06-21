#!/usr/bin/env python3
"""번역 병합: 기존 모드 ko(seed) + 신규 번역(.ko.json) -> 리소스팩 ko_kr.json.

각 (slug, ns) 마다 en 레퍼런스의 모든 키에 대해:
  final[key] = 기존 모드 ko 가 있으면 그것, 없으면 신규 .ko.json 의 값
결과를 translations/herma-ko/assets/<ns>/lang/ko_kr.json 으로 기록.
미번역(en 키인데 ko 없음) 잔여를 보고. 출력 ASCII.
"""
import sys, os, json, glob
try: sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception: pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
# 팩 파라미터화(env) — 기본 Fabric 보존. RPG: HERMA_WORK=_work-rpg HERMA_PACK_SRC=herma-ko-rpg
WORK = os.path.join(ROOT, os.environ.get('HERMA_WORK', '_work'))
PACK = os.path.join(ROOT, os.environ.get('HERMA_PACK_SRC', 'herma-ko'))

def L(p):
    return json.load(open(p, encoding='utf-8')) if os.path.exists(p) else {}

total_en = total_cov = 0
incomplete = []
for en_path in glob.glob(os.path.join(WORK, '*', '*.en.json')):
    slug = os.path.basename(os.path.dirname(en_path))
    ns = os.path.basename(en_path)[:-len('.en.json')]
    en = L(en_path)
    newko = L(os.path.join(WORK, slug, ns + '.ko.json'))
    seed_path = os.path.join(PACK, 'assets', ns, 'lang', 'ko_kr.json')
    seed = L(seed_path)  # 추출 시 심은 기존 모드 ko
    final = dict(seed)   # 기존 ko 보존
    miss = 0
    for k in en:
        if str(en[k]).strip() == '':
            final[k] = en[k]   # 원본이 빈 값(레이아웃 spacer 등) -> 번역 대상 아님, 그대로 둠
            continue
        if k in final and str(final[k]).strip() != '':
            continue
        if k in newko and str(newko[k]).strip() != '':
            final[k] = newko[k]
        else:
            miss += 1
    os.makedirs(os.path.dirname(seed_path), exist_ok=True)
    json.dump(final, open(seed_path, 'w', encoding='utf-8'),
              ensure_ascii=False, indent=2, sort_keys=True)
    cov = len(en) - miss
    total_en += len(en); total_cov += cov
    if miss:
        incomplete.append((slug, ns, miss, len(en)))

print("merged. en keys: %d, covered: %d (%.1f%%)" %
      (total_en, total_cov, 100.0 * total_cov / max(total_en, 1)))
if incomplete:
    print("\nINCOMPLETE (%d namespaces):" % len(incomplete))
    for slug, ns, miss, tot in sorted(incomplete, key=lambda x: -x[2]):
        print("  %-28s %-26s missing %d/%d" % (slug, ns, miss, tot))
else:
    print("ALL namespaces 100% covered.")
