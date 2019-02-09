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
        public List<string> Mask = new List<string>();
 
        private NetworkInterface[] nis = null;

        public MyNetworkInfo()
        {
            // do nothing
        }

        public void clear()
        {
            Interface.Clear();
            IP.Clear();
            Mask.Clear();

            // 一度newしないと、GUIに反映されない
            Interface = new List<string>();
            IP = new List<string>();
            Mask = new List<string>();
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
                            Mask.Add(ip.IPv4Mask.ToString());
                        }
                    }
                }
            }
        }
    }
}
