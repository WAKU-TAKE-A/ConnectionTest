namespace ConnectionTest.Properties {
    using System;
    
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        private static global::System.Resources.ResourceManager resourceMan;
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() { }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ConnectionTest.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get { return resourceCulture; }
            set { resourceCulture = value; }
        }
        
        public static string StrNIC => ResourceManager.GetString("StrNIC", resourceCulture) ?? "";
        public static string StrMyIP => ResourceManager.GetString("StrMyIP", resourceCulture) ?? "";
        public static string StrMySubnetMask => ResourceManager.GetString("StrMySubnetMask", resourceCulture) ?? "";
        public static string StrMyGateway => ResourceManager.GetString("StrMyGateway", resourceCulture) ?? "";
        public static string StrDHCP => ResourceManager.GetString("StrDHCP", resourceCulture) ?? "";
        public static string StrReIP => ResourceManager.GetString("StrReIP", resourceCulture) ?? "";
        public static string StrSetToAbove => ResourceManager.GetString("StrSetToAbove", resourceCulture) ?? "";
        public static string StrDestIP => ResourceManager.GetString("StrDestIP", resourceCulture) ?? "";
        public static string StrPingTimeout => ResourceManager.GetString("StrPingTimeout", resourceCulture) ?? "";
        public static string StrPing => ResourceManager.GetString("StrPing", resourceCulture) ?? "";
        public static string StrIpconfig => ResourceManager.GetString("StrIpconfig", resourceCulture) ?? "";
        public static string StrCheckIPs => ResourceManager.GetString("StrCheckIPs", resourceCulture) ?? "";
        public static string StrPort => ResourceManager.GetString("StrPort", resourceCulture) ?? "";
        public static string StrPortQry => ResourceManager.GetString("StrPortQry", resourceCulture) ?? "";
        
        // 新しく追加したメッセージ用の定義
        public static string MsgConfirmIPChange => ResourceManager.GetString("MsgConfirmIPChange", resourceCulture) ?? "";
        public static string MsgConfirmPingAll => ResourceManager.GetString("MsgConfirmPingAll", resourceCulture) ?? "";
        public static string MsgConfirm => ResourceManager.GetString("MsgConfirm", resourceCulture) ?? "";
    }
}
