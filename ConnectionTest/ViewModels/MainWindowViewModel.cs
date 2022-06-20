using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using ConnectionTest.Models;

// user add
using System.Net.NetworkInformation;
using System.Windows.Input;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

namespace ConnectionTest.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        /* コマンド、プロパティの定義にはそれぞれ 
         * 
         *  lvcom   : ViewModelCommand
         *  lvcomn  : ViewModelCommand(CanExecute無)
         *  llcom   : ListenerCommand(パラメータ有のコマンド)
         *  llcomn  : ListenerCommand(パラメータ有のコマンド・CanExecute無)
         *  lprop   : 変更通知プロパティ(.NET4.5ではlpropn)
         *  
         * を使用してください。
         * 
         * Modelが十分にリッチであるならコマンドにこだわる必要はありません。
         * View側のコードビハインドを使用しないMVVMパターンの実装を行う場合でも、ViewModelにメソッドを定義し、
         * LivetCallMethodActionなどから直接メソッドを呼び出してください。
         * 
         * ViewModelのコマンドを呼び出せるLivetのすべてのビヘイビア・トリガー・アクションは
         * 同様に直接ViewModelのメソッドを呼び出し可能です。
         */

        /* ViewModelからViewを操作したい場合は、View側のコードビハインド無で処理を行いたい場合は
         * Messengerプロパティからメッセージ(各種InteractionMessage)を発信する事を検討してください。
         */

        /* Modelからの変更通知などの各種イベントを受け取る場合は、PropertyChangedEventListenerや
         * CollectionChangedEventListenerを使うと便利です。各種ListenerはViewModelに定義されている
         * CompositeDisposableプロパティ(LivetCompositeDisposable型)に格納しておく事でイベント解放を容易に行えます。
         * 
         * ReactiveExtensionsなどを併用する場合は、ReactiveExtensionsのCompositeDisposableを
         * ViewModelのCompositeDisposableプロパティに格納しておくのを推奨します。
         * 
         * LivetのWindowテンプレートではViewのウィンドウが閉じる際にDataContextDisposeActionが動作するようになっており、
         * ViewModelのDisposeが呼ばれCompositeDisposableプロパティに格納されたすべてのIDisposable型のインスタンスが解放されます。
         * 
         * ViewModelを使いまわしたい時などは、ViewからDataContextDisposeActionを取り除くか、発動のタイミングをずらす事で対応可能です。
         */

        /* UIDispatcherを操作する場合は、DispatcherHelperのメソッドを操作してください。
         * UIDispatcher自体はApp.xaml.csでインスタンスを確保してあります。
         * 
         * LivetのViewModelではプロパティ変更通知(RaisePropertyChanged)やDispatcherCollectionを使ったコレクション変更通知は
         * 自動的にUIDispatcher上での通知に変換されます。変更通知に際してUIDispatcherを操作する必要はありません。
         */

        private readonly string APPL_DNAME = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
        private const string FNM_PORTQRY = "PortQry.exe";

        private MyNetworkInfo info = new MyNetworkInfo();
        private DosCommand cmd = new DosCommand();

        private InitIp init_ip = new InitIp();

        public void Initialize()
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();

            if (System.Environment.Is64BitProcess)
            {
                MyVersion = "ConnectionTest " + asm.GetName().Version.ToString() + "_x64";
            }
            else
            {
                MyVersion = "ConnectionTest " + asm.GetName().Version.ToString() + "_x86";
            }

            checkStatusConnection();

            NetworkChange.NetworkAvailabilityChanged += new NetworkAvailabilityChangedEventHandler(NetworkChange_NetworkAvailabilityChanged);
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(NetworkChange_NetworkAddressChanged);

            PortQryCommand.RaiseCanExecuteChanged();
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            checkStatusConnection();
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            checkStatusConnection();
        }

        private void checkStatusConnection()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                StatusConnection = Properties.Resources.StrCon;
                ColorStatusConnection = "LimeGreen";
            }
            else
            {
                StatusConnection = Properties.Resources.StrNotCon;
                ColorStatusConnection = "Red";
            }
        }

        private bool fileExists(string fname)
        {
            bool bRet = true;

            if (string.IsNullOrWhiteSpace(fname))
            {
                bRet = false;
            }
            else
            {
                FileInfo fi = new FileInfo(fname);
                bRet = fi.Exists;
            }

            return bRet;
        }

        private InitIp ReadXml()
        {
            InitIp ret = new InitIp();

            try
            {
                string fname = Path.Combine(App.ApplicationFolder, "InitIp.xml");

                //XmlSerializerオブジェクトを作成
                //オブジェクトの型を指定する
                XmlSerializer serializer = new XmlSerializer(typeof(InitIp));

                if (File.Exists(fname))
                {
                    //読み込むファイルを開く
                    StreamReader sr = new StreamReader(fname, new UTF8Encoding(false));

                    //XMLファイルから読み込み、逆シリアル化する
                    ret = (InitIp)serializer.Deserialize(sr);

                    //ファイルを閉じる
                    sr.Close();
                }
                else
                {
                    //書き込むファイルを開く（UTF-8 BOM無し）
                    StreamWriter sw = new StreamWriter(fname, false, new UTF8Encoding(false));

                    //シリアル化し、XMLファイルに保存する
                    serializer.Serialize(sw, ret);

                    //ファイルを閉じる
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                ResultText = MethodBase.GetCurrentMethod().Name + " : \r\n" + ex.Message;
            }

            return ret;
        }

        // 変更通知プロパティ

        #region MyVersion変更通知プロパティ
        private string _MyVersion = "***";

        public string MyVersion
        {
            get
            { return _MyVersion; }
            set
            { 
                if (_MyVersion == value)
                    return;
                _MyVersion = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region StatusConnection変更通知プロパティ
        private string _StatusConnection = "***";

        public string StatusConnection
        {
            get
            { return _StatusConnection; }
            set
            { 
                //if (_StatusConnection == value)
                //    return;
                _StatusConnection = value;
                RaisePropertyChanged();
                
                RefreshIPCommand.RaiseCanExecuteChanged();
                PingCommand.RaiseCanExecuteChanged();
                PingAllCommand.RaiseCanExecuteChanged();

                RefreshIP();
            }
        }
        #endregion

        #region Interface変更通知プロパティ
        private List<string> _Interface = new List<string>();

        public List<string> Interface
        {
            get
            { return _Interface; }
            set
            { 
                //if (_Interface == value)
                //    return;
                _Interface = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region SlctdInterface変更通知プロパティ
        private int _SlctdInterface = 0;

        public int SlctdInterface
        {
            get
            { return _SlctdInterface; }
            set
            { 
                //if (_SlctdInterface == value)
                //    return;
                _SlctdInterface = value;
                RaisePropertyChanged();

                if (info.IP.Count == 0 || info.IP[SlctdInterface] == null)
                {
                    Ip0 = Ip1 = Ip2 = Ip3 = 0;
                    Msk0 = Msk1 = Msk2 = Msk3 = 0;
                }
                else
                {
                    string[] ips = info.IP[value].Split('.');
                    Ip0 = int.Parse(ips[0]);
                    Ip1 = int.Parse(ips[1]);
                    Ip2 = int.Parse(ips[2]);
                    Ip3 = int.Parse(ips[3]);
                    string[] msks = info.SubnetMask[value].Split('.');
                    Msk0 = int.Parse(msks[0]);
                    Msk1 = int.Parse(msks[1]);
                    Msk2 = int.Parse(msks[2]);
                    Msk3 = int.Parse(msks[3]);
                }

                if (info.IP.Count == 0 || info.Gateway.Count == 0 || info.Gateway[SlctdInterface] == null)
                {
                    Gate0 = Gate1 = Gate2 = Gate3 = 0;
                }
                else
                {
                    string[] gts = info.Gateway[value].Split('.');
                    Gate0 = int.Parse(gts[0]);
                    Gate1 = int.Parse(gts[1]);
                    Gate2 = int.Parse(gts[2]);
                    Gate3 = int.Parse(gts[3]);
                }
            }
        }
        #endregion

        #region Ip0変更通知プロパティ
        private int _Ip0 = 192;

        public int Ip0
        {
            get
            { return _Ip0; }
            set
            { 
                if (_Ip0 == value)
                    return;
                _Ip0 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Ip1変更通知プロパティ
        private int _Ip1 = 168;

        public int Ip1
        {
            get
            { return _Ip1; }
            set
            { 
                if (_Ip1 == value)
                    return;
                _Ip1 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Ip2変更通知プロパティ
        private int _Ip2 = 0;

        public int Ip2
        {
            get
            { return _Ip2; }
            set
            { 
                if (_Ip2 == value)
                    return;
                _Ip2 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Ip3変更通知プロパティ
        private int _Ip3 = 0;

        public int Ip3
        {
            get
            { return _Ip3; }
            set
            { 
                if (_Ip3 == value)
                    return;
                _Ip3 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Msk0変更通知プロパティ
        private int _Msk0 = 255;

        public int Msk0
        {
            get
            { return _Msk0; }
            set
            { 
                if (_Msk0 == value)
                    return;
                _Msk0 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Msk1変更通知プロパティ
        private int _Msk1 = 255;

        public int Msk1
        {
            get
            { return _Msk1; }
            set
            { 
                if (_Msk1 == value)
                    return;
                _Msk1 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Msk2変更通知プロパティ
        private int _Msk2 = 255;

        public int Msk2
        {
            get
            { return _Msk2; }
            set
            { 
                if (_Msk2 == value)
                    return;
                _Msk2 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Msk3変更通知プロパティ
        private int _Msk3 = 0;

        public int Msk3
        {
            get
            { return _Msk3; }
            set
            { 
                if (_Msk3 == value)
                    return;
                _Msk3 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Gate0変更通知プロパティ
        private int _Gate0 = 0;

        public int Gate0
        {
            get
            { return _Gate0; }
            set
            {
                if (_Gate0 == value)
                    return;
                _Gate0 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Gate1変更通知プロパティ
        private int _Gate1 = 0;

        public int Gate1
        {
            get
            { return _Gate1; }
            set
            {
                if (_Gate1 == value)
                    return;
                _Gate1 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Gate2変更通知プロパティ
        private int _Gate2 = 0;

        public int Gate2
        {
            get
            { return _Gate2; }
            set
            {
                if (_Gate2 == value)
                    return;
                _Gate2 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Gate3変更通知プロパティ
        private int _Gate3 = 0;

        public int Gate3
        {
            get
            { return _Gate3; }
            set
            {
                if (_Gate3 == value)
                    return;
                _Gate3 = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region EnDhcp変更通知プロパティ
        private bool _EnDhcp = false;

        public bool EnDhcp
        {
            get
            { return _EnDhcp; }
            set
            { 
                if (_EnDhcp == value)
                    return;
                _EnDhcp = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Ip0dst変更通知プロパティ
        private int _Ip0dst = 192;

        public int Ip0dst
        {
            get
            { return _Ip0dst; }
            set
            { 
                if (_Ip0dst == value)
                    return;
                _Ip0dst = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Ip1dst変更通知プロパティ
        private int _Ip1dst = 168;

        public int Ip1dst
        {
            get
            { return _Ip1dst; }
            set
            { 
                if (_Ip1dst == value)
                    return;
                _Ip1dst = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Ip2dst変更通知プロパティ
        private int _Ip2dst = 0;

        public int Ip2dst
        {
            get
            { return _Ip2dst; }
            set
            { 
                if (_Ip2dst == value)
                    return;
                _Ip2dst = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Ip3dst変更通知プロパティ
        private int _Ip3dst = 0;

        public int Ip3dst
        {
            get
            { return _Ip3dst; }
            set
            { 
                if (_Ip3dst == value)
                    return;
                _Ip3dst = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Timeout_ms変更通知プロパティ
        private int _Timeout_ms = 200;

        public int Timeout_ms
        {
            get
            { return _Timeout_ms; }
            set
            { 
                if (_Timeout_ms == value)
                    return;
                _Timeout_ms = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region ResultText変更通知プロパティ
        private string _ResultText = "";

        public string ResultText
        {
            get
            { return _ResultText; }
            set
            { 
                if (_ResultText == value)
                    return;
                _ResultText = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region ColorStatusConnection変更通知プロパティ
        private string _ColorStatusConnection = "Red";

        public string ColorStatusConnection
        {
            get
            { return _ColorStatusConnection; }
            set
            { 
                if (_ColorStatusConnection == value)
                    return;
                _ColorStatusConnection = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region PortNum変更通知プロパティ
        private int _PortNum = 80;

        public int PortNum
        {
            get
            { return _PortNum; }
            set
            { 
                if (_PortNum == value)
                    return;
                _PortNum = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region ExistPortQry変更通知プロパティ
        private string _ExistPortQry = "Hidden";

        public string ExistPortQry
        {
            get
            { return _ExistPortQry; }
            set
            { 
                if (_ExistPortQry == value)
                    return;
                _ExistPortQry = value;
                RaisePropertyChanged();
            }
        }
        #endregion
        
        // コマンド

        #region RefreshIPCommand
        private ViewModelCommand _RefreshIPCommand;

        public ViewModelCommand RefreshIPCommand
        {
            get
            {
                if (_RefreshIPCommand == null)
                {
                    _RefreshIPCommand = new ViewModelCommand(RefreshIP, CanRefreshIP);
                }
                return _RefreshIPCommand;
            }
        }

        public bool CanRefreshIP()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        public void RefreshIP()
        {
            info.RefreshNetworkInterface();
            Interface = info.Interface;
            SlctdInterface = 0;

            PortQryCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region SetIPCommand
        private ViewModelCommand _SetIPCommand;

        public ViewModelCommand SetIPCommand
        {
            get
            {
                if (_SetIPCommand == null)
                {
                    _SetIPCommand = new ViewModelCommand(SetIP);
                }
                return _SetIPCommand;
            }
        }

        public void SetIP()
        {
            var dialog_res = System.Windows.Forms.MessageBox.Show(
                Properties.Resources.StrQuestion,
                "Question",
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Question);

            if (dialog_res == System.Windows.Forms.DialogResult.No)
            {
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;

            string str_cmd = "";

            if (EnDhcp)
            {
                str_cmd = string.Format(
                    "netsh interface ip set address \"{0}\" dhcp",
                    Interface[SlctdInterface]);
            }
            else
            {
                if (Gate0 == 0 && Gate1 == 0 && Gate2 == 0 && Gate3 == 0)
                {
                    str_cmd = string.Format(
                        "netsh interface ip set address \"{0}\" static {1}.{2}.{3}.{4} {5}.{6}.{7}.{8}",
                        Interface[SlctdInterface],
                        Ip0, Ip1, Ip2, Ip3,
                        Msk0, Msk1, Msk2, Msk3);
                }
                else
                {
                    str_cmd = string.Format(
                        "netsh interface ip set address \"{0}\" static {1}.{2}.{3}.{4} {5}.{6}.{7}.{8} {9}.{10}.{11}.{12}",
                        Interface[SlctdInterface],
                        Ip0, Ip1, Ip2, Ip3,
                        Msk0, Msk1, Msk2, Msk3,
                        Gate0, Gate1, Gate2, Gate3);
                }
            }

            try
            {
                bool bret = cmd.Run(str_cmd);

                if (bret)
                {
                    ResultText += cmd.StandardOutput;
                }
                else
                {
                    ResultText = "Error occurred : \r\n";
                    ResultText += cmd.StandardOutput;
                }
                
                RefreshIP();
            }
            catch (Exception ex)
            {
                ResultText = "Error occurred : \r\n";
                ResultText += ex.Message;
            }

            PortQryCommand.RaiseCanExecuteChanged();
            Mouse.OverrideCursor = null;
        }
        #endregion

        #region PingCommand
        private ViewModelCommand _PingCommand;

        public ViewModelCommand PingCommand
        {
            get
            {
                if (_PingCommand == null)
                {
                    _PingCommand = new ViewModelCommand(Ping, CanPing);
                }
                return _PingCommand;
            }
        }

        public bool CanPing()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        public void Ping()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            bool bret = cmd.Run(string.Format("ping {0}.{1}.{2}.{3} -n 1 -w {4}", Ip0dst, Ip1dst, Ip2dst, Ip3dst, Timeout_ms));

            if (bret)
            {
                ResultText = cmd.StandardOutput;
            }
            else
            {
                ResultText = cmd.StandardOutput + "\r\n(Check the LAN connection, connection IP address, etc.)";
            }

            PortQryCommand.RaiseCanExecuteChanged();
            Mouse.OverrideCursor = null;
        }
        #endregion

        #region IpconfigCommand
        private ViewModelCommand _IpconfigCommand;

        public ViewModelCommand IpconfigCommand
        {
            get
            {
                if (_IpconfigCommand == null)
                {
                    _IpconfigCommand = new ViewModelCommand(Ipconfig);
                }
                return _IpconfigCommand;
            }
        }

        public void Ipconfig()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            bool bret = cmd.Run("ipconfig");

            if (bret)
            {
                ResultText = cmd.StandardOutput;
            }
            else
            {
                ResultText = cmd.StandardOutput + "\r\n(Unknown error)";
            }

            PortQryCommand.RaiseCanExecuteChanged();
            Mouse.OverrideCursor = null;
        }
        #endregion

        #region PingAllCommand
        private ViewModelCommand _PingAllCommand;

        public ViewModelCommand PingAllCommand
        {
            get
            {
                if (_PingAllCommand == null)
                {
                    _PingAllCommand = new ViewModelCommand(PingAll, CanPingAll);
                }
                return _PingAllCommand;
            }
        }

        public bool CanPingAll()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        public void PingAll()
        {
            string dialog_msg = string.Format(Properties.Resources.StrQuestionTwo + "\r\n({0}.{1}.{2}.1 ～ {0}.{1}.{2}.255)", Ip0, Ip1, Ip2);

            var dialog_res = System.Windows.Forms.MessageBox.Show(
                dialog_msg, 
                "Question",
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Question);

            if (dialog_res == System.Windows.Forms.DialogResult.No)
            {
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;

            //Ping png = new Ping();
            ResultText = Properties.Resources.StrResPingAll + " : \r\n\r\n";

            List<int> ip4 = new List<int>();

            Parallel.For(0, 255, i => {

                if (i != Ip3)
                {
                    Ping png = new Ping();
                    string ip = string.Format("{0}.{1}.{2}.{3}", Ip0, Ip1, Ip2, i);
                    var rep = png.Send(ip, Timeout_ms);

                    if (rep.Status == IPStatus.Success)
                    {
                        ip4.Add(i);
                    }
                }

            });

            if (ip4.Count != 0)
            {
                ip4.Sort();

                foreach (int i in ip4)
                {
                    string ip = string.Format("{0}.{1}.{2}.{3}", Ip0, Ip1, Ip2, i);
                    ResultText += ip + "\r\n";
                }
            }

            PortQryCommand.RaiseCanExecuteChanged();
            Mouse.OverrideCursor = null;
        }
        #endregion

        #region PortQryCommand
        private ViewModelCommand _PortQryCommand;

        public ViewModelCommand PortQryCommand
        {
            get
            {
                if (_PortQryCommand == null)
                {
                    _PortQryCommand = new ViewModelCommand(PortQry, CanPortQry);
                }
                return _PortQryCommand;
            }
        }

        public bool CanPortQry()
        {
            string portqry_path = Path.Combine(APPL_DNAME, FNM_PORTQRY);
            bool chk = fileExists(portqry_path);

            if (chk)
            {
                ExistPortQry = "Visible";
            }
            else
            {
                ExistPortQry = "Hidden";
            }

            return chk;
        }

        public void PortQry()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            string portqry_path = Path.Combine(APPL_DNAME, FNM_PORTQRY);

            if (fileExists(portqry_path))
            {
                bool bret = cmd.Run(string.Format(portqry_path + " -n {0}.{1}.{2}.{3} -e {4}", Ip0dst, Ip1dst, Ip2dst, Ip3dst, PortNum.ToString()));

                if (bret)
                {
                    ResultText = cmd.StandardOutput;
                }
                else
                {
                    ResultText = cmd.StandardOutput + "\r\n(Unknown error)";
                }
            }
            else
            {
                ExistPortQry = "Hidden";
            }

            Mouse.OverrideCursor = null;
        }
        #endregion

        #region IpconfigCommand
        private ViewModelCommand _ChangeInitIPCommand;

        public ViewModelCommand ChangeInitIPCommand
        {
            get
            {
                if (_ChangeInitIPCommand == null)
                {
                    _ChangeInitIPCommand = new ViewModelCommand(ChangeInitIP);
                }
                return _ChangeInitIPCommand;
            }
        }

        public void ChangeInitIP()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            init_ip = ReadXml().Clone();

            Ip0 = init_ip.Ip0;
            Ip1 = init_ip.Ip1;
            Ip2 = init_ip.Ip2;
            Ip3 = init_ip.Ip3;
            Msk0 = init_ip.Msk0;
            Msk1 = init_ip.Msk1;
            Msk2 = init_ip.Msk2;
            Msk3 = init_ip.Msk3;
            Gate0 = init_ip.Gate0;
            Gate1 = init_ip.Gate1;
            Gate2 = init_ip.Gate2;
            Gate3 = init_ip.Gate3;
            Ip0dst = init_ip.Ip0dst;
            Ip1dst = init_ip.Ip1dst;
            Ip2dst = init_ip.Ip2dst;
            Ip3dst = init_ip.Ip3dst;

            PortQryCommand.RaiseCanExecuteChanged();
            Mouse.OverrideCursor = null;
        }
        #endregion
    }
}
