#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Herma World 청크 트림 — 거점 '원(circle)' 안쪽만 보존, 바깥 청크는 삭제(재생성).
목적: 26.1.2 신규 모드의 worldgen(새 바이옴/구조물)을 기존 월드의 바깥 영역에 적용.

순수 Python 표준 라이브러리만 사용 — 의존성 설치 불필요. python3 만 있으면 어느 컴퓨터에서나 실행.
파일을 편집할 필요 없이 **월드 경로를 인자로** 넘기면 됩니다.

== 사용법 (macOS / Linux / Windows, python3 필요) ==
  ★★ 반드시 서버를 완전히 종료한 뒤 실행 ★★

  # 1) 미리보기 (삭제 안 함, 먼저 꼭)
  python3 herma-chunk-trim.py "/경로/server/world"

  # 2) 실제 적용 (삭제수 표시 후 yes 확인 → 자동 백업 → 삭제)
  python3 herma-chunk-trim.py "/경로/server/world" --apply

  # 자동화(확인 프롬프트 생략):
  python3 herma-chunk-trim.py "/경로/server/world" --apply --force

  옵션:
    --a X Z       보존 원 꼭짓점 1 (청크 좌표, 기본 27 159)
    --b X Z       보존 원 꼭짓점 2 (청크 좌표, 기본 62 165)
    --pad N       보존 원 반지름에 여유 N 청크 추가 (기본 0)
    --dims D...   처리 차원 (기본 minecraft/overworld;
                  네더 minecraft/the_nether, 엔드 minecraft/the_end, 모드차원 <modid>/<dim>)

== 안전장치 ==
  - 기본은 미리보기(삭제 0) / --apply 는 삭제수 표시 + 'yes' 확인(--force 로만 생략)
  - 실행 전 자동 백업: level.dat·level.dat_old·data/·region/entities/poi
  - 보존 청크가 0이면(좌표/경로 오류 추정) 자동 중단
  - 그래도 ★완전한 안전을 원하면 실행 전 world 폴더 전체를 따로 복사★ 권장

== 보존 영역 ==
  두 꼭짓점(청크 좌표)을 지름 양 끝점으로 하는 원(= 거점 bounding box 외접원). Y 무시.
  기본 A(27,159)·B(62,165) → 중심 (44.5,162), 반지름 17.76 청크 (≈ 284 블록).
"""
import os, sys, math, re, shutil, time, argparse

# ===== 기본값 (인자로 덮어쓰기 가능) =====
DEFAULT_WORLD = "~/minecraft-server/world"   # 인자 없이 실행 시 폴백 (보통 인자로 넘김)
DEFAULT_A = (27, 159)
DEFAULT_B = (62, 165)
DEFAULT_DIMS = ["minecraft/overworld"]
SUBDIRS = ["region", "entities", "poi"]

RE_MCA = re.compile(r"^r\.(-?\d+)\.(-?\d+)\.mca$")
ZERO = b"\x00\x00\x00\x00"


def parse_args():
    ap = argparse.ArgumentParser(
        description="Minecraft 청크 트림 — 거점 원 보존, 바깥 삭제(재생성).",
        formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("world", nargs="?", default=DEFAULT_WORLD,
                    help="월드 폴더 경로 (level.dat 이 있는 폴더). 예: /srv/mc/world")
    ap.add_argument("--apply", action="store_true", help="실제 삭제 (없으면 미리보기)")
    ap.add_argument("--force", action="store_true", help="--apply 시 yes 확인 생략(자동화용)")
    ap.add_argument("--a", nargs=2, type=int, metavar=("X", "Z"), help="보존 원 꼭짓점1 (청크좌표)")
    ap.add_argument("--b", nargs=2, type=int, metavar=("X", "Z"), help="보존 원 꼭짓점2 (청크좌표)")
    ap.add_argument("--pad", type=int, default=0, help="보존 원 반지름 여유 청크")
    ap.add_argument("--dims", nargs="+", default=None, help="처리 차원 (기본 minecraft/overworld)")
    return ap.parse_args()


# --- 전역 설정 (parse 후 채움; 함수들이 참조) ---
ARGS = parse_args()
WORLD = os.path.abspath(os.path.expanduser(ARGS.world))
A = tuple(ARGS.a) if ARGS.a else DEFAULT_A
B = tuple(ARGS.b) if ARGS.b else DEFAULT_B
RADIUS_PAD = ARGS.pad
DIMS = ARGS.dims if ARGS.dims else DEFAULT_DIMS
APPLY = ARGS.apply
FORCE = ARGS.force
CX = (A[0] + B[0]) / 2.0
CZ = (A[1] + B[1]) / 2.0
R = math.hypot(B[0] - A[0], B[1] - A[1]) / 2.0 + RADIUS_PAD
R2 = R * R


def dim_base(dim):
    """차원 폴더 경로. MC 26.x(dimensions/...) 우선, 구버전(1.x) 폴백."""
    p = os.path.join(WORLD, "dimensions", *dim.split("/"))
    if os.path.isdir(p):
        return p
    fb = {"minecraft/overworld": WORLD,
          "minecraft/the_nether": os.path.join(WORLD, "DIM-1"),
          "minecraft/the_end": os.path.join(WORLD, "DIM1")}
    return fb.get(dim, p)


def inside(cx, cz):
    return (cx - CX) ** 2 + (cz - CZ) ** 2 <= R2


def analyze_file(path):
    """읽기 전용 분석. (kept, deleted, remove_whole, bad)."""
    try:
        if os.path.getsize(path) < 4096:
            return (0, 0, False, True)
        with open(path, "rb") as f:
            header = f.read(4096)
        if len(header) < 4096:
            return (0, 0, False, True)
    except OSError:
        return (0, 0, False, True)
    m = RE_MCA.match(os.path.basename(path))
    rx, rz = int(m.group(1)), int(m.group(2))
    kept = deleted = 0
    for i in range(1024):
        off = i * 4
        if header[off:off + 4] == ZERO:
            continue
        cx = rx * 32 + (i % 32)
        cz = rz * 32 + (i // 32)
        if inside(cx, cz):
            kept += 1
        else:
            deleted += 1
    if deleted == 0:
        return (kept, 0, False, False)
    if kept == 0:
        return (0, deleted, True, False)
    return (kept, deleted, False, False)


def apply_header(path):
    """원 밖 청크 location 엔트리를 0 으로(부분 region)."""
    m = RE_MCA.match(os.path.basename(path))
    rx, rz = int(m.group(1)), int(m.group(2))
    with open(path, "rb") as f:
        header = bytearray(f.read(4096))
    for i in range(1024):
        off = i * 4
        if header[off:off + 4] == ZERO:
            continue
        cx = rx * 32 + (i % 32)
        cz = rz * 32 + (i // 32)
        if not inside(cx, cz):
            header[off:off + 4] = ZERO
    with open(path, "r+b") as f:
        f.seek(0)
        f.write(header)


def backup(targets, bdir):
    os.makedirs(bdir, exist_ok=True)
    wb = os.path.basename(WORLD.rstrip("/\\"))
    for d in targets:
        shutil.copytree(d, os.path.join(bdir, os.path.relpath(d, os.path.dirname(WORLD))))
    for extra in ["level.dat", "level.dat_old", "data"]:
        src = os.path.join(WORLD, extra)
        if not os.path.exists(src):
            continue
        dst = os.path.join(bdir, wb, extra)
        if os.path.isdir(src):
            shutil.copytree(src, dst)
        else:
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copy2(src, dst)


def main():
    print(f"WORLD     = {WORLD}")
    print(f"보존 원   = 중심 청크({CX}, {CZ}), 반지름 {R:.2f} 청크 (≈ {R*16:.0f} 블록)")
    print(f"모드      = {'APPLY (실제 삭제)' if APPLY else 'DRY-RUN (미리보기, 삭제 안 함)'}")

    if not os.path.isdir(WORLD):
        sys.exit(f"!! 월드 폴더가 없습니다: {WORLD}\n   첫 인자로 실제 월드 경로를 넘기세요. 예) python3 herma-chunk-trim.py \"/srv/mc/world\"")
    if not os.path.isfile(os.path.join(WORLD, "level.dat")):
        sys.exit(f"!! level.dat 이 없습니다: {WORLD}\n   level.dat 이 있는 '월드' 폴더를 지정했는지 확인하세요.")

    targets = []
    for dim in DIMS:
        base = dim_base(dim)
        for sub in SUBDIRS:
            d = os.path.join(base, sub)
            if os.path.isdir(d):
                targets.append(d)
        print(f"  차원 {dim} → {base}")
    if not targets:
        sys.exit("!! region/entities/poi 폴더를 못 찾음. 월드 경로/차원(--dims) 확인.")

    # ---- PASS 1: 읽기 전용 집계 ----
    keep = dele = removed = bad = 0
    plan = []
    for d in targets:
        for fn in os.listdir(d):
            if not RE_MCA.match(fn):
                continue
            path = os.path.join(d, fn)
            k, x, whole, is_bad = analyze_file(path)
            if is_bad:
                bad += 1
                print(f"  ! 손상/절단 region 건너뜀: {path}")
                continue
            keep += k; dele += x
            if x > 0:
                plan.append((path, whole))
                if whole:
                    removed += 1

    print("-" * 40)
    print(f"보존 청크          : {keep}")
    print(f"삭제(재생성) 청크  : {dele}")
    print(f"통째 삭제 region   : {removed} 파일")
    if bad:
        print(f"건너뛴 손상파일    : {bad} (수동 점검 권장)")

    if keep == 0 and dele > 0:
        sys.exit("\n!! 보존 청크가 0입니다 — 좌표(--a/--b)나 월드 경로 오류로 보입니다.\n"
                 "   안전을 위해 중단합니다. 게임에서 거점에 서서 F3 → Chunk X·Z 가 "
                 f"A{A}·B{B} 범위인지 확인 후 --a/--b 로 조정하세요.")

    if not APPLY:
        print(">> DRY-RUN 이었습니다. 숫자 확인 후 실제 적용:  같은 명령에 --apply 추가")
        return

    if not FORCE:
        if not sys.stdin.isatty():
            sys.exit("!! 비대화형 환경 — 확인 입력 불가. 의도 확실하면 --force 를 추가하세요.")
        reply = input(f"\n실제로 {dele} 청크 삭제 + {removed} region 파일 제거. 계속하려면 'yes' 입력: ")
        if reply.strip().lower() != "yes":
            sys.exit("취소됨.")

    ts = time.strftime("%Y%m%d-%H%M%S")
    bdir = WORLD.rstrip("/\\") + f"_trim-backup-{ts}"
    if os.path.exists(bdir):
        bdir += f"-{os.getpid()}"
    print(f"백업 생성 중 → {bdir}  (region/entities/poi + level.dat + data/)")
    backup(targets, bdir)
    print("백업 완료.")

    for path, whole in plan:
        if whole:
            os.remove(path)
        else:
            apply_header(path)
    print(">> 완료. 서버 시작하면 삭제 영역이 새 worldgen 으로 재생성됩니다.")
    print(f"   문제 시 백업({bdir})의 내용을 원위치로 복사해 원복하세요.")


if __name__ == "__main__":
    main()
