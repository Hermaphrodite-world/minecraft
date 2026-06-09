#!/usr/bin/env bash
# Herma Launcher — macOS .app 번들 조립 + ad-hoc 서명 (공증 X, 결정 C "인증 나중").
#
# Apple Silicon(arm64)은 커널(AMFI)이 모든 실행 페이지에 서명을 요구해서, 미서명 Mach-O 는
# 실행 즉시 "killed: 9" 로 죽는다. 그래서 **ad-hoc 서명(`codesign -s -`)은 필수**다(공증과 별개).
#   - ad-hoc 서명: 실행은 됨. Gatekeeper "확인되지 않은 개발자" 경고는 우클릭→열기 1회로 통과(미서명 Windows exe 와 동급).
#   - 공증(notarization): 경고조차 없이 더블클릭 실행. Apple Developer($99/년) 필요 → 최종 단계 보류.
#
# codesign 은 macOS 전용 → 이 스크립트는 macOS(CI macos-14 러너 또는 친구 Mac)에서 실행.
#
# 사용:  bash launcher/build-mac-app.sh <publishDir> <outDir>
#   publishDir : dotnet publish -r osx-arm64 결과 디렉토리 (기본 publish/osx-arm64)
#   outDir     : .app + .zip 산출 위치 (기본 publish/mac)
#   VERSION 환경변수로 버전 지정 (기본 0.1.0)
set -euo pipefail

PUBLISH_DIR="${1:-publish/osx-arm64}"
OUT_DIR="${2:-publish/mac}"
VERSION="${VERSION:-0.1.0}"
APP_NAME="HermaLauncher"
EXEC_NAME="HermaLauncher"          # apphost 파일명 (= csproj AssemblyName)
BUNDLE_ID="io.hermaphroditeworld.launcher"
DISPLAY_NAME="Herma Launcher"

if [[ ! -x "$PUBLISH_DIR/$EXEC_NAME" && ! -f "$PUBLISH_DIR/$EXEC_NAME" ]]; then
  echo "ERROR: apphost not found: $PUBLISH_DIR/$EXEC_NAME (먼저 dotnet publish -r osx-arm64)" >&2
  exit 1
fi

APP="$OUT_DIR/$APP_NAME.app"
echo "==> .app 번들 조립: $APP (v$VERSION)"
rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

# 1) publish 산출(apphost + *.dll + *.dylib + deps)을 통째로 Contents/MacOS 로.
#    .NET self-contained 앱은 apphost 가 자기 옆의 dll/dylib 을 찾으므로 한 디렉토리에 둔다.
cp -R "$PUBLISH_DIR/." "$APP/Contents/MacOS/"
chmod +x "$APP/Contents/MacOS/$EXEC_NAME"

# 2) (있으면) 아이콘
if [[ -f "launcher/assets/app.icns" ]]; then
  cp "launcher/assets/app.icns" "$APP/Contents/Resources/app.icns"
  ICON_KEY='  <key>CFBundleIconFile</key><string>app.icns</string>'
else
  ICON_KEY=''
fi

# 3) Info.plist
cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>$DISPLAY_NAME</string>
  <key>CFBundleDisplayName</key><string>$DISPLAY_NAME</string>
  <key>CFBundleIdentifier</key><string>$BUNDLE_ID</string>
  <key>CFBundleVersion</key><string>$VERSION</string>
  <key>CFBundleShortVersionString</key><string>$VERSION</string>
  <key>CFBundleExecutable</key><string>$EXEC_NAME</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>CFBundleDevelopmentRegion</key><string>en</string>
  <!-- 실제 .NET 10 osx-arm64 런타임이 선언한 minOS = 12.0 (libcoreclr.dylib LC_BUILD_VERSION). -->
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>LSApplicationCategoryType</key><string>public.app-category.games</string>
$ICON_KEY
</dict>
</plist>
PLIST

# 3b) plist 무결성 검증 (Codex audit #3).
plutil -lint "$APP/Contents/Info.plist"
test "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$APP/Contents/Info.plist")" = "$EXEC_NAME" \
  || { echo "ERROR: CFBundleExecutable != $EXEC_NAME" >&2; exit 1; }

# 4) ad-hoc 서명 — inside-out (Codex audit #1, certain).
#    Apple 은 `--deep` 를 "비상용" 으로만 인정 → 중첩 Mach-O(dylib + apphost) 를 먼저 개별 서명한 뒤
#    번들을 마지막에 서명해야 서명 seal 이 올바르게 기록된다. 관리형 *.dll(PE/CIL)은 Mach-O 가
#    아니라 `file` 매칭에서 제외되므로 서명 대상이 아니다.
echo "==> ad-hoc codesign (inside-out)"
while IFS= read -r -d '' f; do
  if file "$f" | grep -q 'Mach-O'; then
    codesign --force --sign - "$f"
  fi
done < <(find "$APP/Contents/MacOS" -type f -print0)
codesign --force --sign - "$APP"                       # 번들 마지막 서명(seal)
codesign --verify --deep --strict --verbose=4 "$APP" || { echo "codesign verify 실패" >&2; exit 1; }

# 5) 배포용 zip (ditto: 서명/심볼릭 보존하며 압축 — zip 보다 안전).
mkdir -p "$OUT_DIR"
ZIP="$OUT_DIR/$APP_NAME-macos-arm64.zip"
rm -f "$ZIP"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$ZIP"

# 5b) zip 왕복 후 서명 보존 재검증 (Codex audit #4) — 친구가 받는 상태 그대로 검증.
CHECK_DIR="$(mktemp -d)"
ditto -x -k "$ZIP" "$CHECK_DIR"
codesign --verify --deep --strict --verbose=4 "$CHECK_DIR/$APP_NAME.app" \
  || { echo "ERROR: zip 왕복 후 서명 깨짐" >&2; rm -rf "$CHECK_DIR"; exit 1; }
rm -rf "$CHECK_DIR"

echo "==> 완료:"
echo "    app: $APP"
echo "    zip: $ZIP"
echo "    (미공증 — 친구 Mac 에서 처음 1회: 우클릭→열기. docs/macos-setup.md 참조)"
