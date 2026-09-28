using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConnectionTest.Models;

public class DosCommand
{
    [DllImport("kernel32.dll")]
    private static extern uint GetACP();

    public string StandardOutput { get; private set; } = "";

    public Task<bool> RunAsync(string command, TimeSpan? timeout = null)
    {
        var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe");
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(command);
        return RunCoreAsync(startInfo, command, timeout, null);
    }

    public Task<bool> RunExecutableAsync(string executable, IEnumerable<string> arguments,
        TimeSpan? timeout = null, Encoding? outputEncoding = null, string? operation = null)
    {
        var startInfo = new ProcessStartInfo(executable);
        foreach (string argument in arguments) startInfo.ArgumentList.Add(argument);
        return RunCoreAsync(startInfo, operation ?? executable, timeout, outputEncoding);
    }

    public Task<bool> RunAdapterPowerShellAsync(bool enable, string adapterName)
    {
        string verb = enable ? "Enable-NetAdapter" : "Disable-NetAdapter";
        string script = BuildAdapterPowerShellScript(enable, adapterName);
        string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        return RunExecutableAsync("powershell.exe",
            new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand", encoded },
            TimeSpan.FromSeconds(60), Encoding.UTF8, $"{verb}: {adapterName}");
    }

    internal static string BuildAdapterPowerShellScript(bool enable, string adapterName)
    {
        string verb = enable ? "Enable-NetAdapter" : "Disable-NetAdapter";
        string escapedName = adapterName.Replace("'", "''");
        return $"$ErrorActionPreference='Stop'; [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false); $adapter = @(Get-NetAdapter | Where-Object {{ $_.Name -ceq '{escapedName}' }}); if ($adapter.Count -ne 1) {{ throw 'Adapter not found or ambiguous.' }}; {verb} -InputObject $adapter[0] -Confirm:$false";
    }

    private async Task<bool> RunCoreAsync(ProcessStartInfo startInfo, string operation,
        TimeSpan? timeout, Encoding? outputEncoding)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding;
            try
            {
                uint cp = GetACP();
                encoding = cp != 0 ? Encoding.GetEncoding((int)cp) : Encoding.UTF8;
            }
            catch (Exception ex)
            {
                AppLog.Error("コマンド出力の文字コード取得", ex);
                encoding = Encoding.UTF8;
            }
            startInfo.StandardOutputEncoding = outputEncoding ?? encoding;
            startInfo.StandardErrorEncoding = outputEncoding ?? encoding;
            var result = await ProcessRunner.RunAsync(startInfo, timeout ?? TimeSpan.FromSeconds(60)).ConfigureAwait(false);
            StandardOutput = result.Output + result.Error;
            if (result.ExitCode == 0) return true;
            AppLog.Error($"コマンド実行: {operation}",
                new InvalidOperationException($"ExitCode={result.ExitCode}\n{StandardOutput}"));
            return false;
        }
        catch (Exception ex)
        {
            AppLog.Error($"コマンド実行: {operation}", ex);
            StandardOutput = string.Format(Properties.Resources.StrAbnormalEnd, operation, ex.Message);
            return false;
        }
    }
}
