using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HermaLauncher.Services;

// (4) packwiz 동기화. ★ (3)에서 CmlLib가 확보한 java 실행파일 경로를 그대로 재사용한다
//    (구현계획 §4 불변식 — Java-before-packwiz 닭/달걀 해소, MultiMC $INST_JAVA 패턴).
//    -g(GUI off) -s client 고정, --pack-folder = 외부 데이터 디렉토리.
public sealed class PackwizService : IPackwizService
{
    // packFolder = mods 를 받을 게임 디렉토리(--pack-folder). null = AppPaths.GameDir(커스텀 런처 기본).
    //   공식 런처 installer 는 공식 .minecraft 안의 전용 폴더(예: <.minecraft>/herma)를 넘긴다.
    public async Task RunAsync(
        string javaExecutable,
        string packTomlUrl,
        IProgress<StageUpdate> progress,
        CancellationToken ct,
        string? packFolder = null)
    {
        if (string.IsNullOrWhiteSpace(javaExecutable) || !File.Exists(javaExecutable))
            throw new LaunchStageException(LaunchStage.Packwiz,
                "Java 실행 파일을 찾지 못해 모드 동기화를 진행할 수 없어요. 잠시 후 다시 시도해 주세요.");

        var folder = string.IsNullOrWhiteSpace(packFolder) ? AppPaths.GameDir : packFolder;
        Directory.CreateDirectory(folder);

        // 첫 실행 시 packwiz-installer-bootstrap.jar 자동 내려받기(없으면).
        await EnsureBootstrapAsync(progress, ct).ConfigureAwait(false);

        progress.Report(StageUpdate.Of(LaunchStage.Packwiz, "모드팩 동기화 중…"));

        // Windows에서 javaw.exe는 stdout이 없으므로 java.exe로 정규화(구현계획 M1-5 함정).
        var java = NormalizeToConsoleJava(javaExecutable);

        var psi = new ProcessStartInfo
        {
            FileName = java,
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // 한국어 Windows(cp949)에서 packwiz 한글 출력 mojibake 방지(P1-6). 자식은 UTF-8 출력.
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-jar");
        psi.ArgumentList.Add(AppPaths.BootstrapJar);
        psi.ArgumentList.Add("-g");
        psi.ArgumentList.Add("-s");
        psi.ArgumentList.Add("client");
        psi.ArgumentList.Add("--pack-folder");
        psi.ArgumentList.Add(folder);
        // 첫 실행 네트워크/rate-limit 내성: 번들 installer.jar 있으면 self-update 생략 가능
        // psi.ArgumentList.Add("--bootstrap-no-update");
        psi.ArgumentList.Add(packTomlUrl);

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stderr = new StringBuilder();

        proc.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                progress.Report(StageUpdate.Of(LaunchStage.Packwiz, e.Data!.Trim()));
                AppLog.Raw("packwiz", e.Data!);
            }
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                stderr.AppendLine(e.Data);
                AppLog.Raw("packwiz!", e.Data!);
            }
        };

        if (!proc.Start())
            throw new LaunchStageException(LaunchStage.Packwiz, "모드 동기화 프로세스를 시작하지 못했어요.");

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        // 취소 시(런처 닫기 포함, S1) 자식 java 프로세스를 동기적으로 정리(고아 프로세스 방지).
        await using var killOnCancel = ct.Register(() =>
        {
            try
            {
                if (!proc.HasExited)
                    proc.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                // 대개 '이미 종료됨' race. 그 외(권한/OS 제약)로 kill 이 실패하면 고아가 남을 수 있으니
                // 최소한 로그로 관측 가능하게 한다(Codex S1 재리뷰 Q3 — silent 차단 제거).
                AppLog.Warn(LaunchStage.Packwiz, "동기화 취소 시 자식 프로세스 종료 실패(고아 가능): " + ex.Message);
            }
        });

        await proc.WaitForExitAsync(ct).ConfigureAwait(false);

        if (proc.ExitCode != 0)
            throw new LaunchStageException(LaunchStage.Packwiz,
                $"모드 동기화에 실패했어요(코드 {proc.ExitCode}). 네트워크를 확인하고 다시 시도해 주세요.\n{Trim(stderr)}");

        progress.Report(StageUpdate.Of(LaunchStage.Packwiz, "모드팩 동기화 완료", 1.0));
    }

    // packwiz-installer-bootstrap 을 특정 버전+SHA-256 으로 핀(공급망 보호, P2-1). latest 무핀/무검증 제거.
    //   v0.0.3 의 jar 해시 = a8fbb24...(2026-06-15 검증). 새 버전 채택 시 둘 다 함께 갱신.
    private const string BootstrapVersion = "v0.0.3";
    private const string BootstrapUrl =
        "https://github.com/packwiz/packwiz-installer-bootstrap/releases/download/" + BootstrapVersion +
        "/packwiz-installer-bootstrap.jar";
    private const string BootstrapSha256 = "a8fbb24dc604278e97f4688e82d3d91a318b98efc08d5dbfcbcbcab6443d116c";

    // 번들/캐시에 bootstrap jar 가 없거나 해시 불일치면 핀된 릴리스에서 받고 무결성 검증(원자적 저장).
    private static async Task EnsureBootstrapAsync(IProgress<StageUpdate> progress, CancellationToken ct)
    {
        if (File.Exists(AppPaths.BootstrapJar))
        {
            if (await FileSha256MatchesAsync(AppPaths.BootstrapJar, BootstrapSha256, ct).ConfigureAwait(false))
                return; // 캐시가 핀된 해시와 일치 → 신뢰
            AppLog.Warn(LaunchStage.Packwiz, "캐시된 packwiz bootstrap 해시 불일치 — 재다운로드");
            try { File.Delete(AppPaths.BootstrapJar); } catch { /* best-effort */ }
        }

        progress.Report(StageUpdate.Of(LaunchStage.Packwiz, "모드 동기화 도구 내려받는 중…"));
        try
        {
            using var http = new HttpClient();
            using var resp = await http.GetAsync(BootstrapUrl, HttpCompletionOption.ResponseHeadersRead, ct)
                                       .ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var tmp = AppPaths.BootstrapJar + ".tmp";
            await using (var fs = File.Create(tmp))
                await resp.Content.CopyToAsync(fs, ct).ConfigureAwait(false);

            // 다운로드 무결성 검증 — 불일치면 폐기 + 실패(공급망 공격/손상 차단).
            if (!await FileSha256MatchesAsync(tmp, BootstrapSha256, ct).ConfigureAwait(false))
            {
                try { File.Delete(tmp); } catch { }
                AppLog.Error(LaunchStage.Packwiz, "packwiz bootstrap 다운로드 무결성 검증 실패(해시 불일치)");
                throw new LaunchStageException(LaunchStage.Packwiz,
                    "모드 동기화 도구 무결성 검증에 실패했어요. 잠시 후 다시 시도해 주세요.");
            }
            File.Move(tmp, AppPaths.BootstrapJar, overwrite: true);
        }
        catch (LaunchStageException) { throw; }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new LaunchStageException(LaunchStage.Packwiz,
                "모드 동기화 도구를 내려받지 못했어요. 네트워크를 확인하고 다시 시도해 주세요.", ex);
        }
    }

    private static async Task<bool> FileSha256MatchesAsync(string path, string expectedHex, CancellationToken ct)
    {
        try
        {
            await using var fs = File.OpenRead(path);
            var hash = await System.Security.Cryptography.SHA256.HashDataAsync(fs, ct).ConfigureAwait(false);
            return string.Equals(Convert.ToHexStringLower(hash), expectedHex, StringComparison.OrdinalIgnoreCase);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; } // 취소는 전파(정상 캐시 오삭제 방지, Codex)
        catch { return false; } // IO/mismatch 등 → 재다운로드 유도
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
