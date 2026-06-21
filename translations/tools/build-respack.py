#!/usr/bin/env python3
"""herma-ko 소스 -> 배포용 리소스팩 zip 빌드.

translations/herma-ko/ 의 내용(pack.mcmeta + assets/**)을 zip 루트에 담아
modpack/resourcepacks/herma-korean.zip 으로 출력한다(packwiz 가 배포).
재현성: 번역 갱신 후 이 스크립트 재실행 + `packwiz refresh`.
"""
import zipfile, os, sys
try: sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception: pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # translations/
# 팩 파라미터화(env) — 기본 Fabric 보존. RPG: HERMA_PACK_SRC=herma-ko-rpg HERMA_PACK_DIR=modpack-rpg
SRC = os.path.join(ROOT, os.environ.get('HERMA_PACK_SRC', 'herma-ko'))
OUT = os.path.normpath(os.path.join(ROOT, '..', os.environ.get('HERMA_PACK_DIR', 'modpack'),
                                    'resourcepacks', 'herma-korean.zip'))

files = []
for r, _, fs in os.walk(SRC):
    for f in fs:
        files.append(os.path.join(r, f))
files.sort()  # 결정적 순서(재현성)

os.makedirs(os.path.dirname(OUT), exist_ok=True)
with zipfile.ZipFile(OUT, 'w', zipfile.ZIP_DEFLATED) as z:
    for full in files:
        arc = os.path.relpath(full, SRC).replace(os.sep, '/')  # pack.mcmeta / assets/... 가 루트
        z.write(full, arc)
print("built %s (%d files, %d bytes)" % (OUT, len(files), os.path.getsize(OUT)))
