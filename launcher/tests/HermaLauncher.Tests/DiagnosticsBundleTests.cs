using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using HermaLauncher.Services;
using Xunit;

namespace HermaLauncher.Tests;

// 진단 ZIP 번들 — system-info.txt + 최근 로그를 한 파일로. 로그가 없어도 zip + sysinfo 는 생성.
public class DiagnosticsBundleTests
{
    [Fact]
    public void Create_bundles_logs_and_system_info()
    {
        var root = Path.Combine(Path.GetTempPath(), $"herma-diag-{Guid.NewGuid():N}");
        var logDir = Path.Combine(root, "logs");
        var outDir = Path.Combine(root, "out");
        Directory.CreateDirectory(logDir);
        try
        {
            File.WriteAllText(Path.Combine(logDir, "launcher-20260615.log"), "launcher log line");
            File.WriteAllText(Path.Combine(logDir, "game-20260615-120000.log"), "game log line");

            var zip = DiagnosticsBundle.Create(logDir, outDir, "SYSINFO-MARKER");

            Assert.NotNull(zip);
            Assert.True(File.Exists(zip!));
            using var archive = ZipFile.OpenRead(zip!);
            var names = archive.Entries.Select(e => e.FullName).ToList();
            Assert.Contains("system-info.txt", names);
            Assert.Contains(names, n => n.StartsWith("logs/launcher-", StringComparison.Ordinal));
            Assert.Contains(names, n => n.StartsWith("logs/game-", StringComparison.Ordinal));
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* best-effort */ } }
    }

    [Fact]
    public void Create_with_no_logs_still_produces_zip_with_sysinfo()
    {
        var root = Path.Combine(Path.GetTempPath(), $"herma-diag-{Guid.NewGuid():N}");
        var logDir = Path.Combine(root, "logs");
        var outDir = Path.Combine(root, "out");
        Directory.CreateDirectory(logDir);
        try
        {
            var zip = DiagnosticsBundle.Create(logDir, outDir, "ONLY-SYSINFO");

            Assert.NotNull(zip);
            using var archive = ZipFile.OpenRead(zip!);
            Assert.Contains("system-info.txt", archive.Entries.Select(e => e.FullName));
        }
        finally { try { Directory.Delete(root, recursive: true); } catch { /* best-effort */ } }
    }
}
