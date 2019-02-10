# ConnectionTest

自分のIPアドレスを取得します。<br>
また「ping」、「ipconfig」、「netsh interface ip set address」などをGUIから利用します。

Visual Studio 2015のソリューションです。MVVMのインフラストラクチャー「Livet」を使っています。

「DOBON.NET」の

* [「ネットワーク接続の状況が変更されたことを知る」](https://dobon.net/vb/dotnet/internet/detectinternetconnect.html#changed)
* [「ネットワークインターフェイスのIPアドレスが変更されたことを知る」](https://dobon.net/vb/dotnet/internet/networkaddresschanged.html)
* [「Pingを送信する」](https://dobon.net/vb/dotnet/internet/ping.html)
* [「DOSコマンドを実行し出力データを取得する」](https://dobon.net/vb/dotnet/process/standardoutput.html)
* [「AssemblyName.Versionから取得する」](https://dobon.net/vb/dotnet/file/myversioninfo.html#section4)

「＠IT」の

* [「WindowsのnetshコマンドでTCP/IPのパラメータを設定する」](http://www.atmarkit.co.jp/ait/articles/1002/05/news097.html)

などを参考にしました。

# 使い方の説明

![ConnectionTest001.jp](https://raw.githubusercontent.com/WAKU-TAKE-A/ConnectionTest/master/img/ConnectionTest001.jpg)

ネットワークに接続されていれば、「ネットワークアダプタ」にアダプタ名が表示されます。

選択することで、「自分のIPアドレス」を確認することができます。

基本的に自分のIPアドレスが変化した時は自動で、「アダプタ名」や「自分のIPアドレス」は変更されますが、「IPの再取得」を押すことで強制的に更新することも可能です。

「自分の***」のテキストボックスを変更し、「上記設定に変更」ボタンを押すことで変更することができます。ただし、管理者権限で本アプリを起動していなくてはなりません。

「接続チェック(ping))」はDOSコマンドのping、「ネット状況確認(ipconfig))」はipconfigです。

「接続可能なIPの確認」は、第4オクテットを1～255（自分を除く）の全てにpingを行います。

# ライセンス

本アプリケーションは「MIT」ライセンスです。

本アプリケーションでは、Livetを利用しています。Livetは、尾上雅則 氏が作成したMVVMのインフラストラクチャーです。非常に使い易いインフラストラクチャーで、MVVMを勉強し始めたころから使わせていただいています。

Livetは「zlib/libpng」ライセンスです。
