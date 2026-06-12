#!/usr/bin/env python3
# 런처 에셋 생성 — 단일 소스(icon-source.png)에서 app 아이콘(.ico/.png/.icns) + 히어로 이미지 파생.
# 재현: python launcher/tools/gen-assets.py  (repo 루트에서 실행, Pillow 필요)
import os
import sys
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))  # repo root
SRC = os.path.join(ROOT, "launcher", "tools", "icon-source.png")
ASSETS = os.path.join(ROOT, "launcher", "src", "HermaLauncher", "Assets")
MAC = os.path.join(ROOT, "launcher", "assets")
os.makedirs(ASSETS, exist_ok=True)
os.makedirs(MAC, exist_ok=True)

src = Image.open(SRC).convert("RGBA")
print(f"source: {SRC} {src.size}")


def sq(img, size):
    return img.resize((size, size), Image.LANCZOS)


# 1) app.png — Avalonia 창/작업표시줄 아이콘 (512)
app_png = os.path.join(ASSETS, "app.png")
sq(src, 512).save(app_png)
print(f"  app.png   -> {app_png} (512)")

# 2) app.ico — Windows exe 아이콘 (멀티사이즈)
app_ico = os.path.join(ASSETS, "app.ico")
sizes = [16, 24, 32, 48, 64, 128, 256]
sq(src, 256).save(app_ico, format="ICO", sizes=[(s, s) for s in sizes])
print(f"  app.ico   -> {app_ico} ({sizes})")

# 3) hero.png — 헤더 우측 히어로 패널 (700, 둥근 보더 안에서 클립됨)
hero_png = os.path.join(ASSETS, "hero.png")
sq(src, 700).save(hero_png)
print(f"  hero.png  -> {hero_png} (700)")

# 4) app.icns — macOS 번들 아이콘. Pillow ICNS 저장 시도, 실패 시 CI(sips) 폴백 안내.
app_icns = os.path.join(MAC, "app.icns")
try:
    sq(src, 1024).save(app_icns, format="ICNS")
    print(f"  app.icns  -> {app_icns} (1024)")
except Exception as e:  # noqa: BLE001
    print(f"  app.icns  -> SKIPPED (Pillow ICNS 실패: {e}). macOS CI 의 sips/iconutil 로 생성 필요.")
    sys.exit(0)

print("done.")
