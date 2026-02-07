using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConnectionTest.Models;
using ConnectionTest.Properties;

namespace ConnectionTest.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly string APPL_DNAME = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
    private const string FNM_PORTQRY = "PortQry.exe";
    private const string FNM_INIT_XML = "init_ip.xml";

    private readonly MyNetworkInfo info = new();
    private readonly DosCommand cmd = new();

    [ObservableProperty] private string _myVersion = "ConnectionTest";
    [ObservableProperty] private string _statusConnection = "***";
    [ObservableProperty] private string _colorStatusConnection = "Red";
    [ObservableProperty] private List<string> _interface = new();
    [ObservableProperty] private int _slctdInterface = -1;
    [ObservableProperty] private bool _isNotBusy = true;
    [ObservableProperty] private int _ip0=192, _ip1=168, _ip2=0, _ip3=0;
    [ObservableProperty] private int _msk0=255, _msk1=255, _msk2=255, _msk3=0;
    [ObservableProperty] private int _gate0=0, _gate1=0, _gate2=0, _gate3=0;
    [ObservableProperty] private bool _enDhcp = false;
    [ObservableProperty] private int _ip0dst=192, _ip1dst=168, _ip2dst=0, _ip3dst=0;
    [ObservableProperty] private int _timeout_ms = 200;
    [ObservableProperty] private string _resultText = "";
    [ObservableProperty] private int _portNum = 80;
    [ObservableProperty] private string _existPortQry = "Hidden";

    public void Initialize()
    {
        var asm = Assembly.GetExecutingAssembly();
        var ver = asm.GetName().Version;
        MyVersion = $"ConnectionTest {ver?.ToString(4) ?? "0.0.0.0"}_{(Environment.Is64BitProcess ? "x64" : "x86")}";
        LoadSettings();
        CheckStatusConnection();
        NetworkChange.NetworkAvailabilityChanged += (s, e) => Application.Current.Dispatcher.Invoke(CheckStatusConnection);
        NetworkChange.NetworkAddressChanged += (s, e) => Application.Current.Dispatcher.Invoke(CheckStatusConnection);
    }

    private void CheckStatusConnection()
    {
        bool available = NetworkInterface.GetIsNetworkAvailable();
        StatusConnection = available ? "Connected" : "Disconnected";
        ColorStatusConnection = available ? "LimeGreen" : "Red";
        RefreshIP();
    }

    partial void OnSlctdInterfaceChanged(int value)
    {
        if (value >= 0 && value < info.IP.Count)
        {
            if (info.IP[value] != null) {
                var v = info.IP[value].Split('.');
                if(v.Length == 4) {
                    Ip0 = int.Parse(v[0]); Ip1 = int.Parse(v[1]); Ip2 = int.Parse(v[2]); Ip3 = int.Parse(v[3]);
                }
            }
            if (info.SubnetMask[value] != null) {
                var v = info.SubnetMask[value].Split('.');
                if(v.Length == 4) {
                    Msk0 = int.Parse(v[0]); Msk1 = int.Parse(v[1]); Msk2 = int.Parse(v[2]); Msk3 = int.Parse(v[3]);
                }
            }
            if (info.Gateway.Count > value) {
                var gStr = info.Gateway[value];
                if (!string.IsNullOrEmpty(gStr)) {
                    var v = gStr.Split('.');
                    if(v.Length == 4) {
                        Gate0 = int.Parse(v[0]); Gate1 = int.Parse(v[1]); Gate2 = int.Parse(v[2]); Gate3 = int.Parse(v[3]);
                        return;
                    }
                }
            }
            Gate0 = Gate1 = Gate2 = Gate3 = 0;
        }
    }

    [RelayCommand]
    private void RefreshIP()
    {
        info.RefreshNetworkInterface();
        Interface = new List<string>(info.Interface);
        if (Interface.Count > 0) {
            int targetIdx = 0;
            for(int i=0; i < info.IP.Count; i++) {
                if (!info.IP[i].StartsWith("169.254")) { targetIdx = i; break; }
            }
            SlctdInterface = targetIdx;
            OnSlctdInterfaceChanged(SlctdInterface);
        } else {
            SlctdInterface = -1;
        }
        PortQryCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task SetIP()
    {
        if (MessageBox.Show(Resources.MsgConfirmIPChange, Resources.MsgConfirm, MessageBoxButton.YesNo) == MessageBoxResult.No) return;
        if (SlctdInterface < 0) return;

        IsNotBusy = false;
        Mouse.OverrideCursor = Cursors.Wait;
        string str_cmd = EnDhcp ? $"netsh interface ip set address \"{Interface[SlctdInterface]}\" dhcp"
            : $"netsh interface ip set address \"{Interface[SlctdInterface]}\" static {Ip0}.{Ip1}.{Ip2}.{Ip3} {Msk0}.{Msk1}.{Msk2}.{Msk3} {Gate0}.{Gate1}.{Gate2}.{Gate3}";
        await Task.Run(() => cmd.Run(str_cmd));
        ResultText = cmd.StandardOutput;
        RefreshIP();
        Mouse.OverrideCursor = null;
        IsNotBusy = true;
    }

    [RelayCommand]
    private async Task Ping()
    {
        IsNotBusy = false;
        Mouse.OverrideCursor = Cursors.Wait;
        await Task.Run(() => cmd.Run($"ping {Ip0dst}.{Ip1dst}.{Ip2dst}.{Ip3dst} -n 1 -w {Timeout_ms}"));
        ResultText = cmd.StandardOutput;
        Mouse.OverrideCursor = null;
        IsNotBusy = true;
    }

    [RelayCommand]
    private async Task Ipconfig()
    {
        IsNotBusy = false;
        Mouse.OverrideCursor = Cursors.Wait;
        await Task.Run(() => cmd.Run("ipconfig"));
        ResultText = cmd.StandardOutput;
        Mouse.OverrideCursor = null;
        IsNotBusy = true;
    }

    [RelayCommand]
    private async Task PingAll()
    {
        string startIp = $"{Ip0}.{Ip1}.{Ip2}.1";
        string endIp = $"{Ip0}.{Ip1}.{Ip2}.254";
        string confirmMsg = string.Format(Resources.MsgConfirmPingAll, startIp, endIp);

        if (MessageBox.Show(confirmMsg, Resources.MsgConfirm, MessageBoxButton.YesNo) == MessageBoxResult.No) return;
        
        IsNotBusy = false;
        Mouse.OverrideCursor = Cursors.Wait;
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();
        await Task.Run(() => Parallel.For(1, 255, i => {
            string targetIp = $"{Ip0}.{Ip1}.{Ip2}.{i}";
            using var p = new Ping();
            try {
                var reply = p.Send(targetIp, Timeout_ms);
                if (reply.Status == IPStatus.Success) {
                    int ttl = reply.Options?.Ttl ?? 0;
                    // (Linux/Camera) に変更
                    string type = ttl switch { <= 64 => "(Linux/NetworkCamera)", <= 128 => "(Windows)     ", _ => "(NetDevice)   " };
                    results.Add($"{targetIp,-15} {type}");
                }
            } catch {}
        }));
        var sortedList = results.OrderBy(s => int.Parse(s.Split(' ')[0].Split('.')[3]));
        ResultText = (Resources.Culture?.Name.StartsWith("ja") == true ? "一斉Ping 結果 (種別はTTLによる推定):\r\n" : "Ping All Results (Estimated by TTL):\r\n") 
                     + string.Join("\r\n", sortedList);
        Mouse.OverrideCursor = null;
        IsNotBusy = true;
    }

    [RelayCommand(CanExecute = nameof(CanPortQry))]
    private async Task PortQry()
    {
        IsNotBusy = false;
        Mouse.OverrideCursor = Cursors.Wait;
        string targetIp = $"{Ip0dst}.{Ip1dst}.{Ip2dst}.{Ip3dst}";
        string rawCommand = $"cd /d \"{APPL_DNAME}\" && {FNM_PORTQRY} -n {targetIp} -p tcp -e {PortNum}";
        ResultText = Resources.Culture?.Name.StartsWith("ja") == true ? "PortQry 実行中...\r\n" : "Running PortQry...\r\n";
        await Task.Run(() => cmd.Run(rawCommand));
        ResultText = cmd.StandardOutput;
        Mouse.OverrideCursor = null;
        IsNotBusy = true;
    }

    private bool CanPortQry()
    {
        bool exists = File.Exists(Path.Combine(APPL_DNAME, FNM_PORTQRY));
        ExistPortQry = exists ? "Visible" : "Hidden";
        return exists;
    }

    [RelayCommand]
    private void ChangeInitIP() => LoadSettings();

    private void LoadSettings()
    {
        string path = Path.Combine(APPL_DNAME, FNM_INIT_XML);
        try {
            if (File.Exists(path)) {
                var serializer = new XmlSerializer(typeof(InitIp));
                using var sr = new StreamReader(path);
                if (serializer.Deserialize(sr) is InitIp config) {
                    Ip0 = config.Ip0; Ip1 = config.Ip1; Ip2 = config.Ip2; Ip3 = config.Ip3;
                    Msk0 = config.Msk0; Msk1 = config.Msk1; Msk2 = config.Msk2; Msk3 = config.Msk3;
                    Gate0 = config.Gate0; Gate1 = config.Gate1; Gate2 = config.Gate2; Gate3 = config.Gate3;
                    Ip0dst = config.Ip0dst; Ip1dst = config.Ip1dst; Ip2dst = config.Ip2dst; Ip3dst = config.Ip3dst;
                    // 起動時等のログ出力を削除
                }
            } else {
                SaveSettings();
                // 起動時等のログ出力を削除
            }
        } catch { }
    }

    private void SaveSettings()
    {
        string path = Path.Combine(APPL_DNAME, FNM_INIT_XML);
        try {
            var config = new InitIp {
                Ip0 = Ip0, Ip1 = Ip1, Ip2 = Ip2, Ip3 = Ip3,
                Msk0 = Msk0, Msk1 = Msk1, Msk2 = Msk2, Msk3 = Msk3,
                Gate0 = Gate0, Gate1 = Gate1, Gate2 = Gate2, Gate3 = Gate3,
                Ip0dst = Ip0dst, Ip1dst = Ip1dst, Ip2dst = Ip2dst, Ip3dst = Ip3dst
            };
            var serializer = new XmlSerializer(typeof(InitIp));
            using var sw = new StreamWriter(path);
            serializer.Serialize(sw, config);
        } catch { }
    }
}
