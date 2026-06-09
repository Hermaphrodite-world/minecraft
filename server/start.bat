@echo off
REM Hermaphrodite World — Fabric 서버 기동 (Windows)
REM 전제: setup.md 절차로 fabric-server-launch.jar 생성 + eula.txt 동의 완료.
REM MC 26.1.2 는 Java 25 필요. JAVA 환경변수로 경로 지정 가능.
setlocal
cd /d "%~dp0"

if "%JAVA%"=="" set "JAVA=java"
if "%RAM%"=="" set "RAM=4G"
if "%JAR%"=="" set "JAR=fabric-server-launch.jar"

"%JAVA%" ^
 -Xms%RAM% -Xmx%RAM% ^
 -XX:+UseG1GC -XX:+ParallelRefProcEnabled -XX:MaxGCPauseMillis=200 ^
 -XX:+UnlockExperimentalVMOptions -XX:+DisableExplicitGC ^
 -XX:G1NewSizePercent=30 -XX:G1MaxNewSizePercent=40 -XX:G1HeapRegionSize=8M ^
 -XX:G1ReservePercent=20 -XX:G1HeapWastePercent=5 -XX:G1MixedGCCountTarget=4 ^
 -XX:InitiatingHeapOccupancyPercent=15 -XX:G1MixedGCLiveThresholdPercent=90 ^
 -XX:G1RSetUpdatingPauseTimePercent=5 -XX:SurvivorRatio=32 ^
 -XX:+PerfDisableSharedMem -XX:MaxTenuringThreshold=1 ^
 -XX:+UseStringDeduplication ^
 -Daikars.new.flags=true ^
 -jar "%JAR%" nogui

endlocal
