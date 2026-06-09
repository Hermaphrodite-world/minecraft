#!/usr/bin/env bash
# 서버 모드 동기화 — packwiz 팩의 server+both 모드만 받는다.
# 팩 변경(push) 후 서버 재시작 전 실행하거나 cron/systemd로 자동화.
# 전제: packwiz-installer-bootstrap.jar 가 이 디렉토리에 있고, Java 25 사용 가능.
set -euo pipefail
cd "$(dirname "$0")"

JAVA="${JAVA:-java}"
PACK_TOML_URL="${PACK_TOML_URL:-https://hermaphrodite-world.github.io/modpack/pack.toml}"
BOOTSTRAP="${BOOTSTRAP:-packwiz-installer-bootstrap.jar}"

if [ ! -f "$BOOTSTRAP" ]; then
  echo "ERROR: $BOOTSTRAP 가 없습니다. 아래에서 받으세요:"
  echo "  https://github.com/packwiz/packwiz-installer-bootstrap/releases/latest"
  exit 1
fi

# -g(GUI off) -s server : 서버 몫(server+both)만 설치. 제거된 모드 자동 삭제.
exec "$JAVA" -jar "$BOOTSTRAP" -g -s server "$PACK_TOML_URL"
