using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ConnectionTest.Models;

public partial class DosCommand : ObservableObject
{
    // Windows API: システムの現在の ANSI コードページを取得する
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
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.CreateNoWindow = true;

            // エンコーディングの設定
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            uint cp = GetACP();
            Encoding encoding;
            try
            {
                encoding = (cp != 0) ? Encoding.GetEncoding((int)cp) : Encoding.UTF8;
            }
            catch
            {
                encoding = Encoding.UTF8;
            }

            p.StartInfo.StandardOutputEncoding = encoding;
            p.StartInfo.StandardErrorEncoding = encoding;

            bret = p.Start();

            // 標準出力とエラー出力をそれぞれ読み取って結合する
            string output = p.StandardOutput.ReadToEnd();
            string error = p.StandardError.ReadToEnd();

            StandardOutput = output + error;

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