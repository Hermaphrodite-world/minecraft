# 런처 CmlLib / Velopack 통합 노트

`launcher/src/HermaLauncher/Services/CmlLibServices.cs` 의 스텁이 참조하는 **실제 구현 코드**.
CmlLib.Core 4.x 의 정확한 API 시그니처를 **설치된 어셈블리에서 확인(IntelliSense/디컴파일)한 뒤** 아래 코드를 스텁 위치에 채워 활성화한다. research가 명시한 must-verify 2종(FabricInstaller v4 시그니처 · device-code 콜백 속성명) 때문에 빌드 검증 단계에서는 의도적으로 스텁으로 두었다(fabricated 동작 방지 — CLAUDE.md § Fabrication Honesty).

추가 패키지(통합 시):
```
dotnet add package XboxAuthNet.Game.Msal     # device-code MSAL 확장 (필수)
dotnet add package Velopack                    # 자체 업데이트(R7)
```

---

## §Update — Velopack (Program.Main 첫 줄 + UpdateService)

```csharp
// Program.cs Main() 최상단 — Avalonia 초기화 이전, 강제(구현계획 §4 불변식 1)
public static void Main(string[] args)
{
    VelopackApp.Build().Run();
    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}
```
```csharp
// VelopackUpdateService.CheckAndApplyAsync
var mgr = new UpdateManager(
    new GithubSource("https://github.com/Hermaphrodite-world/launcher", null, false));
var info = await mgr.CheckForUpdatesAsync();
if (info is null)
{
    progress.Report(StageUpdate.Of(LaunchStage.Update, "최신 버전입니다", 1.0));
    return false;
}
await mgr.DownloadUpdatesAsync(info, p =>
    progress.Report(StageUpdate.Of(LaunchStage.Update, $"업데이트 다운로드 {p}%", p / 100.0)));
mgr.ApplyUpdatesAndRestart(info);
return true;
// 소스 부재/네트워크 오류는 try/catch 로 감싸 graceful skip (Codex M7) — 예외 전파 금지.
```

## §Auth — device-code (CmlLib.Core.Auth.Microsoft 3.3.1 + XboxAuthNet.Game.Msal)

```csharp
// CmlLibAuthService.AuthenticateAsync — 클라이언트 ID 설정 확인 후
var app = await MsalClientHelper.BuildApplicationWithCache(LauncherConfig.AzureClientId);
var loginHandler = JELoginHandlerBuilder.BuildDefault();

// 1) silent 우선
MSession session;
try
{
    var silent = loginHandler.CreateAuthenticatorWithDefaultAccount(default);
    silent.AddMsalOAuth(app, msal => msal.Silent());
    silent.AddXboxAuthForJE(xbox => xbox.Basic());
    silent.AddJEAuthenticator();
    session = await silent.ExecuteForLauncherAsync();
}
catch
{
    // 2) device-code fallback — user_code/verification_uri 를 UI로
    var dc = loginHandler.CreateAuthenticatorWithNewAccount(default);
    dc.AddMsalOAuth(app, msal => msal.DeviceCode(code =>
    {
        // ※ 속성명 검증 필요: code.Message / code.VerificationUrl / code.UserCode
        progress.Report(StageUpdate.Of(LaunchStage.Auth,
            $"브라우저에서 {code.VerificationUrl} 접속 후 코드 입력: {code.UserCode}"));
        return Task.CompletedTask;
    }));
    dc.AddXboxAuthForJE(xbox => xbox.Basic());
    dc.AddJEAuthenticator();
    session = await dc.ExecuteForLauncherAsync();
}

// 소유권/XSTS 오류 분기 (구현계획 Codex H2): AddJEAuthenticator 가 404(미구매) 시 throw,
// XSTS XErr(2148916238 미성년 / 2148916235 지역 / 2148916227 밴)도 별도 메시지.
return new AuthSession(session.Username, session.UUID, session.AccessToken, IsOffline: false);
```
> Azure 앱: public client, `consumers` authority, scope `XboxLive.signin offline_access`, "Allow public client flows"=Yes. 등록 후 aka.ms/mce-reviewappid 승인(미승인 시 api.minecraftservices.com 403).

## §Java — CmlLib 자동 설치 + 경로 캐싱 (PackwizService 재사용)

```csharp
// CmlLibMinecraftService.EnsureJavaAsync
var path = new MinecraftPath(AppPaths.GameDir);
var launcher = new MinecraftLauncher(path);
// Java 설치는 게임 install 의 일부로 수행되므로, 여기서는 vanilla install 의 Java 단계까지
// 진행하거나 IJavaPathResolver 로 해석:
var resolver = new MinecraftJavaPathResolver(path);  // ※ 생성자/메서드명 검증
var javaPath = resolver.GetJavaBinaryPath(/* javaVersion */, RulesEvaluatorContext.Default);
// Windows: <runtime>/bin/javaw.exe (PackwizService 가 java.exe 로 정규화)
// macOS  : <runtime>/jre.bundle/Contents/Home/bin/java  (arm64 = mac-os-arm64, 26.1=Java25)
return javaPath;
```

## §Launch — Fabric 설치 + ServerIp 주입 실행

```csharp
// CmlLibMinecraftService.LaunchAsync
var path = new MinecraftPath(AppPaths.GameDir);
var launcher = new MinecraftLauncher(path);
launcher.FileProgressChanged += (_, e) =>
    progress.Report(StageUpdate.Of(LaunchStage.Fabric, e.Name ?? "설치 중",
        e.TotalTasks > 0 ? (double)e.ProgressedTasks / e.TotalTasks : (double?)null));
launcher.ByteProgressChanged += (_, e) => { /* 바이트 진행률 */ };

// Fabric 설치 (※ FabricInstaller 시그니처/네임스페이스 검증)
var fabric = new FabricInstaller(new HttpClient());
string versionId = await fabric.Install(LauncherConfig.MinecraftVersion, path);
// loader 고정 원하면: await fabric.Install(mcVersion, loaderVersion, path);

var isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
var option = new MLaunchOption
{
    Session = ToMSession(session),
    MaximumRamMb = LauncherConfig.DefaultMaxRamMb,
    ServerIp = LauncherConfig.ServerIp,       // 1-클릭 자동 접속
    ServerPort = LauncherConfig.ServerPort,
    DockName = isOSX ? LauncherConfig.MacDockName : null,   // macOS 창 포커스 필수
};

var proc = await launcher.InstallAndBuildProcessAsync(versionId, option);
proc.Start();
```
> (6) 직전 서버 ping 확인(서버 다운 시 친화 메시지, Codex M7), (5.5) 세션 재검증은
> 오케스트레이터에서 `IAuthService.RevalidateAsync` 로 이미 호출됨.

---

## side / 데이터 디렉토리 불변식 (이미 코드 반영)
- 모든 런타임 데이터는 `AppPaths.DataRoot`(번들 밖) — 서명 파손 방지.
- packwiz 는 `EnsureJavaAsync` 가 반환한 java 로 실행(불변식). `LaunchOrchestrator` 가 순서 보장.
