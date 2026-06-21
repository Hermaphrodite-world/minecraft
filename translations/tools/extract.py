#!/usr/bin/env python3
"""Herma 한국어 번역 리소스팩 — 추출 도구.

NONE/PARTIAL 41개 모드 JAR 을 받아(캐시) 네임스페이스별 en_us / ko_kr 를 추출,
'번역 필요 키'(en 에 있는데 ko 에 없는 키)를 todo 로 산출한다.

산출물:
  translations/_jars/<slug>.jar              JAR 캐시
  translations/_work/<slug>/<ns>.todo.json   {key: english}  (번역 필요분만)
  translations/herma-ko/assets/<ns>/lang/ko_kr.json  기존 ko 시드(있으면)
출력: ASCII only (Windows cp949 콘솔 회피).
"""
import sys, os, re, json, io, zipfile, urllib.request, concurrent.futures as cf
try: sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception: pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # translations/
JARS = os.path.join(ROOT, '_jars')
WORK = os.path.join(ROOT, '_work')
PACK = os.path.join(ROOT, 'herma-ko')
MODS = os.path.normpath(os.path.join(ROOT, '..', 'modpack', 'mods'))
for d in (JARS, WORK, PACK): os.makedirs(d, exist_ok=True)

NEED = [
 # NONE (26)
 'collective','explorers-compass','forge-config-api-port','open-parties-and-claims',
 'ping-wheel','placeholder-api','rrv','sound-physics-remastered','xaeros-world-map','yacl',
 'autoreconnectrf','capes','entity-model-features','entityculling','entitytexturefeatures',
 'lambdabettergrass','not-enough-animations','reeses-sodium-options','screencopy',
 'simple-auto-fishing','sodium','visuality','fastback','jamlib','ledger','warputils',
 # PARTIAL (15)
 'appleskin','creativecore','jade','xaeros-minimap','3dskinlayers','better-clouds',
 'chat-heads','controlling','enchantment-descriptions','fallingleaves',
 'inventory-profiles-next','libipn','presence-footsteps','searchables','rightclickharvest',
]

def pw_url(slug):
    p = os.path.join(MODS, slug + '.pw.toml')
    t = open(p, encoding='utf-8').read()
    m = re.search(r'^url\s*=\s*"([^"]*)"', t, re.M)
    return m.group(1) if m else None

def load_json(b):
    try:
        d = json.loads(b.decode('utf-8'))
        return d if isinstance(d, dict) else {}
    except Exception:
        # 관대한 파서: "key": "val" 추출
        out = {}
        for k, v in re.findall(rb'"((?:[^"\\]|\\.)*)"\s*:\s*"((?:[^"\\]|\\.)*)"', b):
            try: out[json.loads('"'+k.decode()+'"')] = json.loads('"'+v.decode()+'"')
            except Exception: pass
        return out

def dl(slug):
    p = os.path.join(JARS, slug + '.jar')
    if os.path.exists(p) and os.path.getsize(p) > 0:
        return p
    url = pw_url(slug)
    req = urllib.request.Request(url, headers={'User-Agent': 'herma-ko/1.0'})
    data = urllib.request.urlopen(req, timeout=120).read()
    open(p, 'wb').write(data)
    return p

def process(slug):
    try:
        jar = dl(slug)
        z = zipfile.ZipFile(jar)
        ns_data = {}  # ns -> {'en':{}, 'ko':{}}
        for n in z.namelist():
            m = re.match(r'^assets/([^/]+)/lang/([^/]+)\.json$', n)
            if not m: continue
            ns, fn = m.group(1), m.group(2).lower()
            if fn not in ('en_us', 'ko_kr'): continue
            ns_data.setdefault(ns, {'en': {}, 'ko': {}})
            ns_data[ns]['en' if fn == 'en_us' else 'ko'].update(load_json(z.read(n)))
        total_missing = 0; ns_report = []
        for ns, d in sorted(ns_data.items()):
            en, ko = d['en'], d['ko']
            if not en: continue
            missing = {k: en[k] for k in en if k not in ko}
            # seed 리소스팩 ko (기존 ko 그대로)
            seed_dir = os.path.join(PACK, 'assets', ns, 'lang')
            os.makedirs(seed_dir, exist_ok=True)
            seed_path = os.path.join(seed_dir, 'ko_kr.json')
            base = {}
            if os.path.exists(seed_path):
                base = json.load(open(seed_path, encoding='utf-8'))
            base.update(ko)  # 모드 기존 ko 반영
            json.dump(base, open(seed_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=2, sort_keys=True)
            wd = os.path.join(WORK, slug); os.makedirs(wd, exist_ok=True)
            # 전체 en 레퍼런스(검증용) — 항상 기록
            json.dump(en, open(os.path.join(wd, ns + '.en.json'), 'w', encoding='utf-8'),
                      ensure_ascii=False, indent=2, sort_keys=True)
            if missing:
                json.dump(missing, open(os.path.join(wd, ns + '.todo.json'), 'w', encoding='utf-8'),
                          ensure_ascii=False, indent=2, sort_keys=True)
            total_missing += len(missing)
            ns_report.append((ns, len(en), len(ko), len(missing)))
        return (slug, total_missing, ns_report, None)
    except Exception as e:
        return (slug, 0, [], str(e)[:60])

TARGETS = sys.argv[1:] or NEED   # 인자로 slug 지정 가능(신규 모드 배치). 없으면 기본 NEED.
with cf.ThreadPoolExecutor(max_workers=10) as ex:
    results = list(ex.map(process, TARGETS))

results.sort(key=lambda r: -r[1])
grand = 0
print("slug | missing_keys | namespaces")
print("-" * 60)
for slug, miss, nsr, err in results:
    if err:
        print("ERR  %-28s %s" % (slug, err)); continue
    grand += miss
    nss = ",".join("%s(miss %d/%d)" % (ns, m, en) for ns, en, ko, m in nsr)
    print("%-28s %5d   %s" % (slug, miss, nss))
print("-" * 60)
print("GRAND TOTAL missing keys to translate: %d" % grand)
print("work dir: translations/_work/ , seed pack: translations/herma-ko/")
# review F-3/CDX-004: slug 오류(오타/폐기 slug)는 ERR 로 표기 후 exit nonzero —
# 자동화 파이프라인이 추출 실패를 false-success(exit 0)로 통과하지 않게.
errs = [r[0] for r in results if r[3]]
if errs:
    print("ERROR: %d slug 추출 실패: %s" % (len(errs), ", ".join(errs)))
    sys.exit(1)
