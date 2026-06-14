using System;

namespace HermaLauncher.Services;

// P3-3: 호스트 물리 RAM 기반 권장 MaxRamMb 자동 산정. 설정(LauncherSettings)에 사용자 override 가
// 있으면 그것을 우선한다. 하드코딩 4096 고정(저사양엔 과다, 고사양엔 과소)을 대체.
public static class RamAdvisor
{
    // 모드팩(77 mods) 안정 구동 하한 / 단일 인스턴스에 과할당 방지 상한.
    public const int MinRamMb = 2048;
    public const int MaxRamMb = 8192;
    // 저사양 호스트(물리 RAM < 4GB)용 절대 하한. 일반 하한(2048)을 강제하면 물리의 2/3 이상을
    // 잡아 OS/스왑 압박 → 오히려 불안정. 이 경우 하한을 낮춰 절반만 권장(Codex MEDIUM-4).
    public const int LowHostMinRamMb = 1024;
    private const long LowHostThresholdMb = 4096;

    // 물리 RAM 의 절반을 clamp. GC.GetGCMemoryInfo().TotalAvailableMemoryBytes 는
    // 데스크톱(컨테이너 제한 없음)에서 ~물리 RAM 과 같다 — 추가 P/Invoke 없이 크로스플랫폼.
    public static int RecommendedMaxRamMb()
    {
        try
        {
            var totalBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            if (totalBytes <= 0)
                return LauncherConfig.DefaultMaxRamMb;
            var totalMb = totalBytes / (1024L * 1024L);
            var floor = totalMb < LowHostThresholdMb ? LowHostMinRamMb : MinRamMb;
            return (int)Math.Clamp(totalMb / 2, floor, MaxRamMb);
        }
        catch
        {
            return LauncherConfig.DefaultMaxRamMb;
        }
    }

    // 실제 적용값: 설정 override(>0) 가 있으면 그 값을(범위 clamp), 없으면 자동 권장값.
    public static int EffectiveMaxRamMb()
    {
        var ov = LauncherSettings.Load().MaxRamMbOverride;
        if (ov is { } mb && mb > 0)
            return Math.Clamp(mb, MinRamMb, MaxRamMb);
        return RecommendedMaxRamMb();
    }
}
