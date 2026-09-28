using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConnectionTest.Models;

public class AdapterDto
{
    public string Name { get; set; } = "";
    public bool IsEnabled { get; set; }
}

public class NetworkAdapterInfo
{
    public string Name { get; init; } = "";
    public string IP { get; init; } = "";
    public string SubnetMask { get; init; } = "";
    public string Gateway { get; init; } = "";
    public bool? DhcpEnabled { get; init; }
    public bool IsEnabled { get; init; }
}

public class MyNetworkInfo
{
    internal const string AdapterScript = @"
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$adapters = @(Get-NetAdapter |
    Where-Object { $_.Virtual -eq $false -and $_.Name -notmatch 'virtual|vbox|vmware|vethernet' } |
    Sort-Object -Property @{Expression={$_.InterfaceType -eq 6 -or $_.InterfaceType -eq 71}; Descending=$true} |
    Select-Object Name, @{Name='IsEnabled';Expression={$_.Status -ne 'Disabled'}})
ConvertTo-Json -InputObject $adapters -Compress";

    public async Task<IReadOnlyList<NetworkAdapterInfo>> RefreshNetworkInterfaceAsync()
    {
        string base64 = Convert.ToBase64String(Encoding.Unicode.GetBytes(AdapterScript));
        var startInfo = new ProcessStartInfo("powershell.exe",
            $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {base64}")
        {
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        var result = await ProcessRunner.RunAsync(startInfo, TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"アダプター取得失敗 (ExitCode={result.ExitCode}): {result.Error}");

        List<AdapterDto> dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<AdapterDto>>(result.Output)
                ?? throw new JsonException("アダプター一覧が null です。");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"アダプター一覧のJSON解析失敗: {result.Output}", ex);
        }

        var adapters = new List<NetworkAdapterInfo>();
        var allNi = NetworkInterface.GetAllNetworkInterfaces();
        foreach (var dto in dtos)
        {
            string ip = "", mask = "", gateway = "";
            bool? dhcp = null;
            if (dto.IsEnabled)
            {
                var ni = allNi.FirstOrDefault(n => n.Name == dto.Name)
                    ?? throw new InvalidOperationException($"アダプターが取得中に見つからなくなりました: {dto.Name}");
                var props = ni.GetIPProperties();
                try
                {
                    dhcp = props.GetIPv4Properties()?.IsDhcpEnabled;
                }
                catch (Exception ex)
                {
                    AppLog.Error($"DHCP情報取得: {dto.Name}", ex);
                }
                var unicast = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
                if (unicast != null)
                {
                    ip = unicast.Address.ToString();
                    mask = unicast.IPv4Mask.ToString();
                }
                gateway = props.GatewayAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    ?.Address.ToString() ?? "";
            }
            adapters.Add(new NetworkAdapterInfo
            {
                Name = dto.Name,
                IP = ip,
                SubnetMask = mask,
                Gateway = gateway,
                DhcpEnabled = dhcp,
                IsEnabled = dto.IsEnabled
            });
        }
        return adapters.AsReadOnly();
    }
}
