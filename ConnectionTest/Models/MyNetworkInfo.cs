using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;


namespace ConnectionTest.Models;

public class AdapterDto
{
    public string Name { get; set; } = "";
    public string IP { get; set; } = "";
    public string SubnetMask { get; set; } = "";
    public string Gateway { get; set; } = "";
    public bool DhcpEnabled { get; set; }
    public bool IsEnabled { get; set; }
}

public class MyNetworkInfo
{
    public List<string> Interface { get; private set; } = new();
    public List<string> IP { get; private set; } = new();
    public List<string> SubnetMask { get; private set; } = new();
    public List<string?> Gateway { get; private set; } = new();
    public List<bool> DhcpEnabled { get; private set; } = new();
    public List<bool> IsEnabled { get; private set; } = new();

    public void RefreshNetworkInterface()
    {
        var tempInterface = new List<string>();
        var tempIP = new List<string>();
        var tempSubnetMask = new List<string>();
        var tempGateway = new List<string?>();
        var tempDhcpEnabled = new List<bool>();
        var tempIsEnabled = new List<bool>();

        string psScript = @"Get-NetAdapter | Where-Object { $_.Virtual -eq $false -and $_.Name -notmatch 'virtual|vbox|vmware|vethernet' } | Sort-Object -Property @{Expression={$_.InterfaceType -eq 6 -or $_.InterfaceType -eq 71}; Descending=$true} | Select-Object Name, @{Name='IsEnabled';Expression={$_.Status -ne 'Disabled'}} | ConvertTo-Json -Compress";

        var bytes = System.Text.Encoding.Unicode.GetBytes(psScript);
        string base64 = Convert.ToBase64String(bytes);

        using Process p = new Process();
        p.StartInfo.FileName = "powershell.exe";
        p.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {base64}";
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.CreateNoWindow = true;
        p.Start();

        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();

        try
        {
            var dtos = JsonSerializer.Deserialize<List<AdapterDto>>(output);
            if (dtos != null)
            {
                var allNi = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var dto in dtos)
                {
                    string ip = "";
                    string mask = "";
                    string gw = "";
                    bool dhcp = false;

                    if (dto.IsEnabled)
                    {
                        var ni = allNi.FirstOrDefault(n => n.Name == dto.Name);
                        if (ni != null)
                        {
                            var props = ni.GetIPProperties();
                            try 
                            {
                                var ipv4Props = props.GetIPv4Properties();
                                dhcp = ipv4Props != null && ipv4Props.IsDhcpEnabled;
                            } 
                            catch { }

                            var unicast = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                            if (unicast != null)
                            {
                                ip = unicast.Address.ToString();
                                mask = unicast.IPv4Mask.ToString();
                            }

                            var gateway = props.GatewayAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                            if (gateway != null)
                            {
                                gw = gateway.Address.ToString();
                            }
                        }
                    }

                    tempInterface.Add(dto.Name);
                    tempIP.Add(ip);
                    tempSubnetMask.Add(mask);
                    tempGateway.Add(gw);
                    tempDhcpEnabled.Add(dhcp);
                    tempIsEnabled.Add(dto.IsEnabled);
                }
            }
        }
        catch { }

        // Thread-safe atomic assignment
        Interface = tempInterface;
        IP = tempIP;
        SubnetMask = tempSubnetMask;
        Gateway = tempGateway;
        DhcpEnabled = tempDhcpEnabled;
        IsEnabled = tempIsEnabled;
    }
}
