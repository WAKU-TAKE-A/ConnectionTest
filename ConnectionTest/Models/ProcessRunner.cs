using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectionTest.Models;

internal static class ProcessRunner
{
    public static async Task<(int ExitCode, string Output, string Error)> RunAsync(
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
        Task<string> output = process.StandardOutput.ReadToEndAsync(cancellation.Token);
        Task<string> error = process.StandardError.ReadToEndAsync(cancellation.Token);
        Task completion = Task.WhenAll(output, error, process.WaitForExitAsync(cancellation.Token));
        try
        {
            await completion.WaitAsync(cancellation.Token).ConfigureAwait(false);
            return (process.ExitCode, await output.ConfigureAwait(false), await error.ConfigureAwait(false));
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
