using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ConnectionTest.Models;

public partial class DosCommand : ObservableObject
{
    public string StandardOutput { get; private set; } = "";

    public bool Run(string command = "")
    {
        bool bret = false;
        using Process p = new Process();
        try
        {
            p.StartInfo.FileName = Environment.GetEnvironmentVariable("ComSpec");
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Arguments = "/c " + command;

            bret = p.Start();
            StandardOutput = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
        }
        catch (Exception)
        {
            bret = false;
            StandardOutput = command + "は異常終了しました";
        }
        return bret;
    }
}
