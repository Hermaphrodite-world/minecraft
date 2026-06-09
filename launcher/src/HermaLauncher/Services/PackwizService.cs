using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HermaLauncher.Services;

// (4) packwiz 동기화. ★ (3)에서 CmlLib가 확보한 java 실행파일 경로를 그대로 재사용한다
//    (구현계획 §4 불변식 — Java-before-packwiz 닭/달걀 해소, MultiMC $INST_JAVA 패턴).
//    -g(GUI off) -s client 고정, --pack-folder = 외부 데이터 디렉토리.
public sealed class PackwizService
{
    public async Task RunAsync(
        string javaExecutable,
        string packTomlUrl,
        IProgress<StageUpdate> progress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(javaExecutable) || !File.Exists(javaExecutable))
            throw new LaunchStageException(LaunchStage.Packwiz,
                "Java 실행 파일을 찾지 못해 모드 동기화를 진행할 수 없어요. 잠시 후 다시 시도해 주세요.");

        if (!File.Exists(AppPaths.BootstrapJar))
            throw new LaunchStageException(LaunchStage.Packwiz,
                "packwiz-installer-bootstrap.jar 가 없어요. 런처 재설치가 필요할 수 있어요.");

        progress.Report(StageUpdate.Of(LaunchStage.Packwiz, "모드팩 동기화 중…"));

        // Windows에서 javaw.exe는 stdout이 없으므로 java.exe로 정규화(구현계획 M1-5 함정).
        var java = NormalizeToConsoleJava(javaExecutable);

        var psi = new ProcessStartInfo
        {
            FileName = java,
            WorkingDirectory = AppPaths.GameDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-jar");
        psi.ArgumentList.Add(AppPaths.BootstrapJar);
        psi.ArgumentList.Add("-g");
        psi.ArgumentList.Add("-s");
        psi.ArgumentList.Add("client");
        psi.ArgumentList.Add("--pack-folder");
        psi.ArgumentList.Add(AppPaths.GameDir);
        // 첫 실행 네트워크/rate-limit 내성: 번들 installer.jar 있으면 self-update 생략 가능
        // psi.ArgumentList.Add("--bootstrap-no-update");
        psi.ArgumentList.Add(packTomlUrl);

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stderr = new StringBuilder();

        proc.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                progress.Report(StageUpdate.Of(LaunchStage.Packwiz, e.Data!.Trim()));
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                stderr.AppendLine(e.Data);
        };

        if (!proc.Start())
            throw new LaunchStageException(LaunchStage.Packwiz, "모드 동기화 프로세스를 시작하지 못했어요.");

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        // 취소 시 자식 java 프로세스를 정리(고아 프로세스 방지).
        await using var killOnCancel = ct.Register(() =>
        {
            try
            {
                if (!proc.HasExited)
                    proc.Kill(entireProcessTree: true);
            }
            catch { /* 이미 종료됨 */ }
        });

        await proc.WaitForExitAsync(ct).ConfigureAwait(false);

        if (proc.ExitCode != 0)
            throw new LaunchStageException(LaunchStage.Packwiz,
                $"모드 동기화에 실패했어요(코드 {proc.ExitCode}). 네트워크를 확인하고 다시 시도해 주세요.\n{Trim(stderr)}");

        progress.Report(StageUpdate.Of(LaunchStage.Packwiz, "모드팩 동기화 완료", 1.0));
    }

    private static string NormalizeToConsoleJava(string javaExe)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            javaExe.EndsWith("javaw.exe", StringComparison.OrdinalIgnoreCase))
        {
            var consoleJava = Path.Combine(Path.GetDirectoryName(javaExe)!, "java.exe");
            if (File.Exists(consoleJava))
                return consoleJava;
        }
        return javaExe;
    }

    private static string Trim(StringBuilder sb)
    {
        var s = sb.ToString();
        return s.Length > 600 ? s[^600..] : s;
    }
}
