using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectionTest.Models;

internal static class ProcessRunner
{
    public static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        ProcessStartInfo startInfo, TimeSpan timeout)
    {
        var result = await RunBytesAsync(startInfo, timeout).ConfigureAwait(false);
        Encoding outputEncoding = startInfo.StandardOutputEncoding ?? Encoding.UTF8;
        Encoding errorEncoding = startInfo.StandardErrorEncoding ?? Encoding.UTF8;
        return (result.ExitCode,
            outputEncoding.GetString(result.Output).TrimStart('\uFEFF'),
            errorEncoding.GetString(result.Error).TrimStart('\uFEFF'));
    }

    public static async Task<(int ExitCode, byte[] Output, byte[] Error)> RunBytesAsync(
        ProcessStartInfo startInfo, TimeSpan timeout)
    {
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        using var process = new Process { StartInfo = startInfo };
        using var cancellation = new CancellationTokenSource(timeout);
        process.Start();
        // Drain both pipes concurrently to avoid deadlocks on a full pipe.
        using var outputBuffer = new MemoryStream();
        using var errorBuffer = new MemoryStream();
        Task output = process.StandardOutput.BaseStream.CopyToAsync(outputBuffer, cancellation.Token);
        Task error = process.StandardError.BaseStream.CopyToAsync(errorBuffer, cancellation.Token);
        Task completion = Task.WhenAll(output, error, process.WaitForExitAsync(cancellation.Token));
        try
        {
            await completion.WaitAsync(cancellation.Token).ConfigureAwait(false);
            return (process.ExitCode, outputBuffer.ToArray(), errorBuffer.ToArray());
        }
        catch
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                AppLog.Error("外部コマンドの終了処理", ex);
            }
            _ = completion.ContinueWith(t => { _ = t.Exception; },
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            if (cancellation.IsCancellationRequested)
                throw new TimeoutException($"外部コマンドが {timeout.TotalSeconds:0} 秒以内に終了しませんでした。");
            throw;
        }
    }
}
