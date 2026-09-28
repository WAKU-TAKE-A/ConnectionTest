using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Principal;
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
    private IReadOnlyList<NetworkAdapterInfo> _adapters = Array.Empty<NetworkAdapterInfo>();
    private bool _networkInfoCurrent;
    [ObservableProperty] private string _networkInfoMessage = "";

    [ObservableProperty] private string _myVersion = "ConnectionTest";
    [ObservableProperty] private string _statusConnection = "***";
    [ObservableProperty] private string _colorStatusConnection = "Red";
    [ObservableProperty] private List<string> _interface = new();
    [ObservableProperty] private int _slctdInterface = -1;
    [ObservableProperty] private bool _isNotBusy = true;
    [ObservableProperty] private int _ip0 = 192, _ip1 = 168, _ip2 = 0, _ip3 = 0;
    [ObservableProperty] private int _msk0 = 255, _msk1 = 255, _msk2 = 255, _msk3 = 0;
    [ObservableProperty] private int _gate0 = 0, _gate1 = 0, _gate2 = 0, _gate3 = 0;
    [ObservableProperty] private bool? _enDhcp = null;
    [ObservableProperty] private bool _enAdapter = true;
    [ObservableProperty] private bool _isAdapterEnabled = true;

    [ObservableProperty] private int _ip0dst = 192, _ip1dst = 168, _ip2dst = 0, _ip3dst = 0;
    [ObservableProperty] private int _timeout_ms = 200;
    [ObservableProperty] private string _resultText = "";
    [ObservableProperty] private int _portNum = 80;
    [ObservableProperty] private string _existPortQry = "Hidden";

    public void Initialize()
    {
        var asm = Assembly.GetExecutingAssembly();
        var ver = asm.GetName().Version;
        var adminStatus = IsRunningAsAdministrator() ? Resources.StrAdmin : "";
        MyVersion = $"ConnectionTest {ver?.ToString(4) ?? "0.0.0.0"}_{(Environment.Is64BitProcess ? "x64" : "x86")} {adminStatus}";
        LoadSettings();
        CheckStatusConnection();
        NetworkChange.NetworkAvailabilityChanged += (s, e) => QueueNetworkRefresh();
        NetworkChange.NetworkAddressChanged += (s, e) => QueueNetworkRefresh();
    }

    private void QueueNetworkRefresh()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted) return;
        dispatcher.BeginInvoke(new Action(CheckStatusConnection));
    }

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    private bool _refreshPending;
    private bool _refreshRunning;

    private async void CheckStatusConnection()
    {
        _refreshPending = true;
        if (_refreshRunning) return;
        _refreshRunning = true;
        try
        {
            do
            {
                _refreshPending = false;
                bool available = NetworkInterface.GetIsNetworkAvailable();
                StatusConnection = available ? "Connected" : "Disconnected";
                ColorStatusConnection = available ? "LimeGreen" : "Red";
                await RefreshIP();
            } while (_refreshPending);
        }
        catch (Exception ex)
        {
            AppLog.Error("ネットワーク変更通知", ex);
            NetworkInfoMessage = "ネットワーク状態の確認に失敗しました。再取得してください。";
        }
        finally { _refreshRunning = false; }
    }
    partial void OnSlctdInterfaceChanged(int value)
    {
        if (value < 0 || value >= _adapters.Count)
        {
            EnDhcp = null;
            EnAdapter = IsAdapterEnabled = false;
            Ip0 = Ip1 = Ip2 = Ip3 = 0;
            Msk0 = Msk1 = Msk2 = Msk3 = 0;
            Gate0 = Gate1 = Gate2 = Gate3 = 0;
            return;
        }
        if (value >= 0 && value < _adapters.Count)
        {
            EnDhcp = _adapters[value].DhcpEnabled;
            EnAdapter = _adapters[value].IsEnabled;
            IsAdapterEnabled = _adapters[value].IsEnabled;

            if (!string.IsNullOrEmpty(_adapters[value].IP))
            {
                var v = _adapters[value].IP.Split('.');
                if (v.Length == 4)
                {
                    Ip0 = int.Parse(v[0]); Ip1 = int.Parse(v[1]); Ip2 = int.Parse(v[2]); Ip3 = int.Parse(v[3]);
                }
            }
            else
            {
                Ip0 = Ip1 = Ip2 = Ip3 = 0;
            }

            if (!string.IsNullOrEmpty(_adapters[value].SubnetMask))
            {
                var v = _adapters[value].SubnetMask.Split('.');
                if (v.Length == 4)
                {
                    Msk0 = int.Parse(v[0]); Msk1 = int.Parse(v[1]); Msk2 = int.Parse(v[2]); Msk3 = int.Parse(v[3]);
                }
            }
            else
            {
                Msk0 = Msk1 = Msk2 = Msk3 = 0;
            }

            if (_adapters.Count > value)
            {
                var gw = _adapters[value].Gateway;
                if (!string.IsNullOrEmpty(gw))
                {
                    var v = gw.Split('.');
                    if (v.Length == 4)
                    {
                        Gate0 = int.Parse(v[0]); Gate1 = int.Parse(v[1]); Gate2 = int.Parse(v[2]); Gate3 = int.Parse(v[3]);
                        return;
                    }
                }
            }
            Gate0 = Gate1 = Gate2 = Gate3 = 0;
        }
    }

    [RelayCommand]
    private Task RefreshIP() => RunBusyAsync(FetchNetworkInfoCoreAsync);

    private readonly System.Threading.SemaphoreSlim _operationLock = new(1, 1);
    private int _busyCount;

    private async Task RunBusyAsync(Func<Task> action)
    {
        _busyCount++;
        IsNotBusy = false;
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            await _operationLock.WaitAsync();
            try { await action(); }
            finally { _operationLock.Release(); }
        }
        catch (Exception ex)
        {
            AppLog.Error("画面操作", ex);
            ResultText = $"処理に失敗しました: {ex.Message}";
        }
        finally
        {
            IsNotBusy = --_busyCount == 0;
            if (IsNotBusy) Mouse.OverrideCursor = null;
        }
    }

    private async Task FetchNetworkInfoCoreAsync()
    {
        try
        {
            string? selectedName = SlctdInterface >= 0 && SlctdInterface < _adapters.Count
                ? _adapters[SlctdInterface].Name : null;
            // Build the snapshot off the UI thread, then publish it together on the UI thread.
            var adapters = await Task.Run(() => info.RefreshNetworkInterfaceAsync());
            SlctdInterface = -1;
            _adapters = adapters;
            Interface = adapters.Select(a => a.Name).ToList();
            _networkInfoCurrent = true;
            int targetIdx = Interface.IndexOf(selectedName ?? "");
            if (targetIdx < 0)
                targetIdx = Interface.Count == 0 ? -1 : 0;
            if (selectedName == null)
            {
                for (int i = 0; i < adapters.Count; i++)
                    if (!string.IsNullOrEmpty(adapters[i].IP) && !adapters[i].IP.StartsWith("169.254."))
                    { targetIdx = i; break; }
            }
            SlctdInterface = targetIdx;
            OnSlctdInterfaceChanged(targetIdx);
            NetworkInfoMessage = adapters.Any(a => a.IsEnabled && a.DhcpEnabled == null)
                ? "DHCP状態を取得できないアダプターがあります（チェック欄は未確定表示）。" : "";
            PortQryCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _networkInfoCurrent = false;
            AppLog.Error("ネットワーク情報取得", ex);
            NetworkInfoMessage = "取得に失敗しました。前回の情報を保持しています。再取得してください。";
        }
    }
    [RelayCommand]
    private async Task SetIP()
    {
        if (!_networkInfoCurrent || SlctdInterface < 0 || SlctdInterface >= _adapters.Count)
        {
            ResultText = "ネットワーク情報を再取得してから設定してください。";
            return;
        }

        // A refresh may run while the confirmation dialog is open or while this operation waits.
        int selectedIndex = SlctdInterface;
        string selectedName = _adapters[selectedIndex].Name;
        bool requestedEnabled = EnAdapter;
        bool? requestedDhcp = EnDhcp;
        string requestedIp = $"{Ip0}.{Ip1}.{Ip2}.{Ip3}";
        string requestedMask = $"{Msk0}.{Msk1}.{Msk2}.{Msk3}";
        string requestedGateway = $"{Gate0}.{Gate1}.{Gate2}.{Gate3}";
        if (MessageBox.Show(Resources.MsgConfirmIPChange, Resources.MsgConfirm, MessageBoxButton.YesNo) == MessageBoxResult.No) return;

        await RunBusyAsync(async () =>
        {
            if (!IsSelectedAdapterCurrent(selectedIndex, selectedName))
            {
                ResultText = "選択したアダプターの状態が変わりました。再確認してから設定してください。";
                return;
            }
            if (requestedEnabled && _adapters[selectedIndex].IsEnabled && requestedDhcp == null)
            {
                ResultText = "DHCP状態が不明です。DHCPの有効・無効を選択してください。";
                return;
            }
            bool isCurrentlyEnabled = _adapters[selectedIndex].IsEnabled;

            if (!requestedEnabled)
            {
                if (isCurrentlyEnabled)
                {
                    bool succeeded = await cmd.RunAdapterPowerShellAsync(false, selectedName);
                    ResultText = SettingResult(succeeded);
                }
                await FetchNetworkInfoCoreAsync();
            }
            else
            {
                if (!isCurrentlyEnabled)
                {
                    bool succeeded = await cmd.RunAdapterPowerShellAsync(true, selectedName);
                    ResultText = SettingResult(succeeded);
                    await FetchNetworkInfoCoreAsync();
                }
                else
                {
                    string[] netshArgs = requestedDhcp == true
                        ? new[] { "interface", "ip", "set", "address", selectedName, "dhcp" }
                        : new[] { "interface", "ip", "set", "address", selectedName, "static", requestedIp, requestedMask, requestedGateway };
                    bool succeeded = await cmd.RunExecutableAsync("netsh.exe", netshArgs,
                        operation: $"netsh interface ip set address: {selectedName}");
                    ResultText = SettingResult(succeeded);
                    await FetchNetworkInfoCoreAsync();
                }
            }
        });
    }

    private string SettingResult(bool succeeded) => succeeded
        ? cmd.StandardOutput
        : $"設定に失敗しました。ネットワーク状態を確認してください。\r\n{cmd.StandardOutput}";

    private bool IsSelectedAdapterCurrent(int index, string name) =>
        _networkInfoCurrent && SlctdInterface == index && index >= 0 && index < _adapters.Count &&
        _adapters[index].Name == name;

    [RelayCommand]
    private async Task Ping()
    {
        await RunBusyAsync(async () =>
        {
            ValidatePingTimeout();
            await cmd.RunExecutableAsync("ping.exe", new[] { $"{Ip0dst}.{Ip1dst}.{Ip2dst}.{Ip3dst}", "-n", "1", "-w", Timeout_ms.ToString() },
                TimeSpan.FromMilliseconds(Timeout_ms + 5000));
            ResultText = cmd.StandardOutput;
        });
    }

    [RelayCommand]
    private async Task Ipconfig()
    {
        await RunBusyAsync(async () =>
        {
            await cmd.RunExecutableAsync("ipconfig.exe", Array.Empty<string>());
            ResultText = cmd.StandardOutput;
        });
    }

    [RelayCommand]
    private async Task PingAll()
    {
        string startIp = $"{Ip0}.{Ip1}.{Ip2}.1";
        string endIp = $"{Ip0}.{Ip1}.{Ip2}.254";
        string confirmMsg = string.Format(Resources.MsgConfirmPingAll, startIp, endIp);

        if (MessageBox.Show(confirmMsg, Resources.MsgConfirm, MessageBoxButton.YesNo) == MessageBoxResult.No) return;

        await RunBusyAsync(async () =>
        {
            ValidatePingTimeout();
            string prefix = $"{Ip0}.{Ip1}.{Ip2}";
            int timeout = Timeout_ms;
            var results = new System.Collections.Concurrent.ConcurrentBag<string>();
            var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            using var cancellation = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(60));
            bool timedOut = false;
            try
            {
                await Parallel.ForEachAsync(Enumerable.Range(1, 254), new ParallelOptions
                {
                    MaxDegreeOfParallelism = 32,
                    CancellationToken = cancellation.Token
                }, async (i, token) =>
                {
                    string targetIp = $"{prefix}.{i}";
                    try
                    {
                        using var p = new Ping();
                        var reply = await p.SendPingAsync(targetIp, timeout).WaitAsync(token);
                        if (reply.Status == IPStatus.Success)
                        {
                            int ttl = reply.Options?.Ttl ?? 0;
                            // (Linux/Camera) に変更
                            string type = ttl switch { <= 64 => "(Linux/NetworkCamera)", <= 128 => "(Windows)     ", _ => "(NetDevice)   " };
                            results.Add($"{targetIp,-15} {type}");
                        }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                    catch (Exception ex) { errors.Add(new InvalidOperationException($"Ping: {targetIp}", ex)); }
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                timedOut = true;
                AppLog.Error("一斉Ping", new TimeoutException("60秒で打ち切りました。"));
            }
            if (!errors.IsEmpty) AppLog.Error("一斉Ping", new AggregateException(errors));
            var sortedList = results.OrderBy(s => int.Parse(s.Split(' ')[0].Split('.')[3]));
            ResultText = (Resources.Culture?.Name.StartsWith("ja") == true ? "一斉Ping 結果 (種別はTTLによる推定):\r\n" : "Ping All Results (Estimated by TTL):\r\n")
                         + string.Join("\r\n", sortedList);
            if (timedOut) ResultText += "\r\n60秒で打ち切りました。上記は途中までの結果です。";
            if (!errors.IsEmpty) ResultText += $"\r\n{errors.Count} 件の実行エラーがありました。";
        });
    }

    [RelayCommand(CanExecute = nameof(CanPortQry))]
    private async Task PortQry()
    {
        await RunBusyAsync(async () =>
        {
            string targetIp = $"{Ip0dst}.{Ip1dst}.{Ip2dst}.{Ip3dst}";
            ResultText = Resources.Culture?.Name.StartsWith("ja") == true ? "PortQry 実行中...\r\n" : "Running PortQry...\r\n";
            await cmd.RunExecutableAsync(Path.Combine(APPL_DNAME, FNM_PORTQRY),
                new[] { "-n", targetIp, "-p", "tcp", "-e", PortNum.ToString() });
            ResultText = cmd.StandardOutput;
        });
    }

    private bool CanPortQry()
    {
        bool exists = File.Exists(Path.Combine(APPL_DNAME, FNM_PORTQRY));
        ExistPortQry = exists ? "Visible" : "Hidden";
        return exists;
    }

    [RelayCommand]
    private void ChangeInitIP()
    {
        if (IsNotBusy) LoadSettings();
    }

    private void ValidatePingTimeout()
    {
        if (Timeout_ms < 1 || Timeout_ms > 60000)
            throw new ArgumentOutOfRangeException(nameof(Timeout_ms), "Pingの待ち時間は1～60000msで指定してください。");
    }

    private void LoadSettings()
    {
        string path = Path.Combine(APPL_DNAME, FNM_INIT_XML);
        try
        {
            if (File.Exists(path))
            {
                var serializer = new XmlSerializer(typeof(InitIp));
                using var sr = new StreamReader(path);
                if (serializer.Deserialize(sr) is InitIp config)
                {
                    Ip0 = config.Ip0; Ip1 = config.Ip1; Ip2 = config.Ip2; Ip3 = config.Ip3;
                    Msk0 = config.Msk0; Msk1 = config.Msk1; Msk2 = config.Msk2; Msk3 = config.Msk3;
                    Gate0 = config.Gate0; Gate1 = config.Gate1; Gate2 = config.Gate2; Gate3 = config.Gate3;
                    Ip0dst = config.Ip0dst; Ip1dst = config.Ip1dst; Ip2dst = config.Ip2dst; Ip3dst = config.Ip3dst;
                    // 起動時等のログ出力を削除
                }
            }
            else
            {
                SaveSettings();
                // 起動時等のログ出力を削除
            }
        }
        catch (Exception ex) { AppLog.Error($"設定ファイル: {path}", ex); }
    }

    private void SaveSettings()
    {
        string path = Path.Combine(APPL_DNAME, FNM_INIT_XML);
        try
        {
            var config = new InitIp
            {
                Ip0 = Ip0,
                Ip1 = Ip1,
                Ip2 = Ip2,
                Ip3 = Ip3,
                Msk0 = Msk0,
                Msk1 = Msk1,
                Msk2 = Msk2,
                Msk3 = Msk3,
                Gate0 = Gate0,
                Gate1 = Gate1,
                Gate2 = Gate2,
                Gate3 = Gate3,
                Ip0dst = Ip0dst,
                Ip1dst = Ip1dst,
                Ip2dst = Ip2dst,
                Ip3dst = Ip3dst
            };
            var serializer = new XmlSerializer(typeof(InitIp));
            using var sw = new StreamWriter(path);
            serializer.Serialize(sw, config);
        }
        catch (Exception ex) { AppLog.Error($"設定ファイル: {path}", ex); }
    }
}
