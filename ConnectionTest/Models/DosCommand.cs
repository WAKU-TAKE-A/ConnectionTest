using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ConnectionTest.Models;

public partial class DosCommand : ObservableObject
{
    [DllImport("kernel32.dll")]
    private static extern uint GetACP();

    public string StandardOutput { get; private set; } = "";

    public bool Run(string command = "")
    {
        bool bret = false;
        using Process p = new Process();
        try
        {
            p.StartInfo.FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            p.StartInfo.Arguments = $"/c {command}";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding targetEncoding;

            // システムのACP（通常932）を取得
            uint cp = GetACP();
            Encoding acpEncoding = (cp != 0) ? Encoding.GetEncoding((int)cp) : Encoding.UTF8;

            // OSバージョンの判定（Windows 10は MajorVersion 10 かつ Build 22000未満）
            bool isWin10OrLower = Environment.OSVersion.Version.Major < 10 ||
                                 (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build < 22000);

            if (command.Contains("netsh", StringComparison.OrdinalIgnoreCase))
            {
                // Win10以前のnetshはACP(932)、それ以降（Win11等）はUTF-8で処理
                targetEncoding = isWin10OrLower ? acpEncoding : new UTF8Encoding(false);
            }
            else
            {
                // netsh以外（ipconfig等）は基本的にACPに従う
                targetEncoding = acpEncoding;
            }

            p.StartInfo.StandardOutputEncoding = targetEncoding;
            p.StartInfo.StandardErrorEncoding = targetEncoding;

            bret = p.Start();
            StandardOutput = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit();
        }
        catch (Exception ex)
        {
            bret = false;
            StandardOutput = $"{command}は異常終了しました: {ex.Message}";
        }
        return bret;
    }
}