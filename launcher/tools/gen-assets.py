#!/usr/bin/env python3
# 런처 에셋 생성 — 단일 소스(icon-source.png, 검은 캔버스 위 둥근 하우스 아트)에서 파생.
#  - 앱 아이콘(app.png/ico/icns): 둥근사각 마스크로 검은 배경/모서리 → 투명.
#  - 히어로(hero.png): 내부 씬 직사각 크롭(검정 없이 패널을 꽉 채움).
# 재현: python launcher/tools/gen-assets.py  (repo 루트, Pillow + numpy 필요)
import os
import sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "launcher", "tools", "icon-source.png")
ASSETS = os.path.join(ROOT, "launcher", "src", "HermaLauncher", "Assets")
MAC = os.path.join(ROOT, "launcher", "assets")
os.makedirs(ASSETS, exist_ok=True)
os.makedirs(MAC, exist_ok=True)

src = Image.open(SRC).convert("RGBA")
W, H = src.size
print(f"source: {SRC} {src.size}")

# 비-검정 콘텐츠 bbox (검은 테두리/모서리 제외)
rgb = np.asarray(src.convert("RGB"))
nz = rgb.sum(2) > 36
ys, xs = np.where(nz)
x0, y0, x1, y1 = int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())
print(f"content bbox: x[{x0}..{x1}] y[{y0}..{y1}]")


def sq(img, size):
    return img.resize((size, size), Image.LANCZOS)


# ── 앱 아이콘: 검은 캔버스를 border flood-fill 로 정확히 추출 → 투명 ──
# 고정 반지름 마스크는 아트 라운딩과 어긋나 검은 링이 남는다. 대신 4모서리에서 near-black 을
# flood-fill 해 "border 와 연결된 검정"만 투명화 → 아트 둘레 검은 링 제거 + 내부 어두운 씬
# (포탈 안쪽/그림자: border 와 비연결) 은 보존.
base = src.convert("RGB").copy()
SENT = (255, 0, 255)  # 씬에 없는 sentinel
for seed in [(0, 0), (W - 1, 0), (0, H - 1), (W - 1, H - 1)]:
    ImageDraw.floodfill(base, seed, SENT, thresh=30)
m = np.asarray(base)
canvas = (m[..., 0] == 255) & (m[..., 1] == 0) & (m[..., 2] == 255)
alpha = Image.fromarray(np.where(canvas, 0, 255).astype("uint8"), "L")
icon = src.copy()
icon.putalpha(alpha)

app_png = os.path.join(ASSETS, "app.png")
sq(icon, 512).save(app_png)
print(f"  app.png   -> 512 (투명 모서리)")

app_ico = os.path.join(ASSETS, "app.ico")
sizes = [16, 24, 32, 48, 64, 128, 256]
sq(icon, 256).save(app_ico, format="ICO", sizes=[(s, s) for s in sizes])
print(f"  app.ico   -> {sizes} (투명 모서리)")

app_icns = os.path.join(MAC, "app.icns")
try:
    sq(icon, 1024).save(app_icns, format="ICNS")
    print(f"  app.icns  -> 1024 (투명 모서리)")
except Exception as e:  # noqa: BLE001
    print(f"  app.icns  -> SKIPPED ({e}). macOS CI sips/iconutil 폴백.")

# ── 히어로: 내부 씬 정사각 크롭 (둥근 모서리 inset 안쪽 → 검정 0, 집 전체 노출) ──
# 코너 컷(반지름 ~246) 안쪽으로 ~160px inset. 정사각이라 패널 UniformToFill 시 집이 꽉 참.
cx0, cy0, cx1, cy1 = 160, 160, 1094, 1094
hero = src.convert("RGB").crop((cx0, cy0, cx1, cy1))
# 검증: 크롭 영역에 검정(테두리/코너) 잔존 없는지
ha = np.asarray(hero)
edge_min = min(int(ha[0].sum(axis=1).min()), int(ha[-1].sum(axis=1).min()),
               int(ha[:, 0].sum(axis=1).min()), int(ha[:, -1].sum(axis=1).min()))
hero_png = os.path.join(ASSETS, "hero.png")
hero.save(hero_png)
print(f"  hero.png  -> {hero.size} crop x[{cx0}..{cx1}] y[{cy0}..{cy1}], edge min-brightness={int(edge_min)} (>36 이면 검정 없음)")
if edge_min <= 36:
    print("  ⚠️ 히어로 크롭 가장자리에 검정 잔존 — inset 조정 필요")
print("done.")
