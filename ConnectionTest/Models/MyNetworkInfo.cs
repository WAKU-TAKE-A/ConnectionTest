using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Livet;

// user add
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ConnectionTest.Models
{
    public class MyNetworkInfo : NotificationObject
    {
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */

        public List<string> Interface = new List<string>();
        public List<string> IP = new List<string>();
        public List<string> SubnetMask = new List<string>();
        public List<string> Gateway = new List<string>();
 
        private NetworkInterface[] nis = null;

        public MyNetworkInfo()
        {
            // do nothing
        }

        public void clear()
        {
            Interface.Clear();
            IP.Clear();
            SubnetMask.Clear();
            Gateway.Clear();

            // 一度newしないと、GUIに反映されない
            Interface = new List<string>();
            IP = new List<string>();
            SubnetMask = new List<string>();
            Gateway = new List<string>();
        }

        public void RefreshNetworkInterface()
        {
            this.clear();
            nis = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface ni in nis)
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    IPInterfaceProperties ipips = ni.GetIPProperties();

                    foreach (UnicastIPAddressInformation ip in ipips.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork) // only IPv4
                        {
                            Interface.Add(ni.Name);
                            IP.Add(ip.Address.ToString());
                            SubnetMask.Add(ip.IPv4Mask.ToString());

                            if (Gateway.Count == 0)
                            {
                                Gateway.Add(null);
                            }
                        }
                    }

                    if (Interface.Count == 0)
                    {
                        Interface.Add(ni.Name);
                        IP.Add(null);
                        SubnetMask.Add(null);
                    }
                }
            }
        }
    }
}
