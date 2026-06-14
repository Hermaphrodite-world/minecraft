using System.Reflection;

namespace HermaLauncher.Services;

// 런처 버전(진단 로그·UI 표시용). 릴리스 CI 가 태그로 InformationalVersion 을 주입(P2-4);
// 없으면 AssemblyVersion. 둘 다 기본값이면 "dev".
public static class AppInfo
{
    public static string Version { get; } = ResolveVersion();

    private static string ResolveVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var plus = info.IndexOf('+'); // "1.0.0+gitsha" → "1.0.0"
            var v = plus > 0 ? info[..plus] : info;
            return v == "0.1.0" ? "dev" : v; // csproj 정적 기본값이면 dev 로 취급
        }
        var ver = asm.GetName().Version;
        return ver is null ? "dev" : $"{ver.Major}.{ver.Minor}.{ver.Build}";
    }
}
