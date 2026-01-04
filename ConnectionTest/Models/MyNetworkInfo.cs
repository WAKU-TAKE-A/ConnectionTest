using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ConnectionTest.Models;

public partial class MyNetworkInfo : ObservableObject
{
    public List<string> Interface { get; private set; } = new();
    public List<string> IP { get; private set; } = new();
    public List<string> SubnetMask { get; private set; } = new();
    public List<string?> Gateway { get; private set; } = new();

    public void RefreshNetworkInterface()
    {
        Interface.Clear(); IP.Clear(); SubnetMask.Clear(); Gateway.Clear();
        
        var nis = NetworkInterface.GetAllNetworkInterfaces();
        
        // 優先順位を考慮して取得: 有線/無線を優先し、仮想アダプタを後回しにする
        var filteredNis = nis
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .OrderByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet || 
                                    ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);

        foreach (var ni in filteredNis)
        {
            // 仮想アダプタ（WSL, Hyper-V, VMware等）をスキップしたい場合はここで名前判定
            string name = ni.Name.ToLower();
            if (name.Contains("virtual") || name.Contains("vbox") || name.Contains("vmware") || name.Contains("vethernet"))
                continue;

            var ipips = ni.GetIPProperties();
            foreach (var ip in ipips.UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    Interface.Add(ni.Name);
                    IP.Add(ip.Address.ToString());
                    SubnetMask.Add(ip.IPv4Mask.ToString());
                    var gw = ipips.GatewayAddresses.FirstOrDefault()?.Address.ToString();
                    Gateway.Add(gw);
                }
            }
        }
    }
}
