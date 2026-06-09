#!/usr/bin/env bash
# Hermaphrodite World — Fabric 서버 기동 (Linux/macOS)
# 전제: setup.md 절차로 fabric-server-launch.jar 생성 + eula.txt 동의 완료.
# MC 26.1.2 는 Java 25 필요 — JAVA 변수로 경로 지정 가능.
set -euo pipefail
cd "$(dirname "$0")"

JAVA="${JAVA:-java}"          # Java 25 경로로 오버라이드 가능: JAVA=/path/to/java25 ./start.sh
RAM="${RAM:-4G}"             # 콘텐츠 많으면 6G~8G. Xms==Xmx 유지.
JAR="${JAR:-fabric-server-launch.jar}"

# Aikar's flags (G1GC) — Fabric/Vanilla 호환
exec "$JAVA" \
  -Xms"$RAM" -Xmx"$RAM" \
  -XX:+UseG1GC -XX:+ParallelRefProcEnabled -XX:MaxGCPauseMillis=200 \
  -XX:+UnlockExperimentalVMOptions -XX:+DisableExplicitGC \
  -XX:G1NewSizePercent=30 -XX:G1MaxNewSizePercent=40 -XX:G1HeapRegionSize=8M \
  -XX:G1ReservePercent=20 -XX:G1HeapWastePercent=5 -XX:G1MixedGCCountTarget=4 \
  -XX:InitiatingHeapOccupancyPercent=15 -XX:G1MixedGCLiveThresholdPercent=90 \
  -XX:G1RSetUpdatingPauseTimePercent=5 -XX:SurvivorRatio=32 \
  -XX:+PerfDisableSharedMem -XX:MaxTenuringThreshold=1 \
  -XX:+UseStringDeduplication \
  -Daikars.new.flags=true \
  -jar "$JAR" nogui
