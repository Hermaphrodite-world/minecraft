# Herma Launcher — Windows 로컬 게시 (미서명, self-contained 단일 exe).
# 사용: pwsh launcher/publish-win.ps1
# 결과: publish/win-x64/HermaLauncher.exe (.NET 미설치 PC 에서도 실행).
$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'src/HermaLauncher/HermaLauncher.csproj'
$out  = Join-Path $PSScriptRoot '../publish/win-x64'

dotnet publish $proj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $out

Write-Host "`n게시 완료: $out/HermaLauncher.exe" -ForegroundColor Green
Write-Host "※ 미서명 — 첫 실행 시 SmartScreen '추가 정보 -> 실행' 안내 필요(지인 한정, 결정 D)." -ForegroundColor Yellow
