# ConnectionTest

自分のIPアドレスを取得・設定します。また「ping」、「ipconfig」、「netsh interface ip set address」などのDOSコマンドをGUIから利用します。

接続チェック／IPアドレス設定を、DOSプロンプトやコントロールパネルを開かずに実行するために作りました。

本アプリケーションは、Visual Studio 2015で作成しています。MVVMのインフラストラクチャー「Livet」を使っています。

「DOBON.NET」の

* [「ネットワーク接続の状況が変更されたことを知る」](https://dobon.net/vb/dotnet/internet/detectinternetconnect.html#changed)
* [「ネットワークインターフェイスのIPアドレスが変更されたことを知る」](https://dobon.net/vb/dotnet/internet/networkaddresschanged.html)
* [「Pingを送信する」](https://dobon.net/vb/dotnet/internet/ping.html)
* [「DOSコマンドを実行し出力データを取得する」](https://dobon.net/vb/dotnet/process/standardoutput.html)
* [「AssemblyName.Versionから取得する」](https://dobon.net/vb/dotnet/file/myversioninfo.html#section4)

「＠IT」の

* [「WindowsのnetshコマンドでTCP/IPのパラメータを設定する」](http://www.atmarkit.co.jp/ait/articles/1002/05/news097.html)

などを参考にしました。

# バイナリー

[Release](https://github.com/WAKU-TAKE-A/ConnectionTest/releases) にx86版の実行ファイル（ConnectionTest.zip）をおいておきます。

# 使い方の概要

![ConnectionTest001.jpg](https://raw.githubusercontent.com/WAKU-TAKE-A/ConnectionTest/master/img/ConnectionTest001.jpg)

ネットワークに接続されていれば、「ネットワークアダプタ」にアダプタ名が表示されます。

アダプタ名を選択することで、「自分のIPアドレス」を確認することができます。

自分のIPアドレスが変化した時は、「アダプタ名」や「自分のIPアドレス」が自動的に更新されます。「IPの再取得」を押すことで手動で更新することもできます。

「自分の～」のテキストボックスを変更し、「上記設定に変更」ボタンを押すことで所望の値に変更することができます。「DHCPをONに設定」にチェックを入れるとDHCPを有効にします。ちなみに、管理者権限で本アプリを起動していないと本機能は使えませんので注意してください。

Windows10においてIPの重複があった場合、以下のようなダイアログが出ることがあります。（システムの環境で異なるようです）

![ConnectionTest001.jpg](https://raw.githubusercontent.com/WAKU-TAKE-A/ConnectionTest/master/img/dialog003.jpg)

「接続チェック(ping))」はDOSコマンドのping、「ネット状況確認(ipconfig))」はipconfigです。

「接続可能なIPの確認」は、第4オクテットの1～255（自分を除く）の全てにpingを行います。こちらはC#のSystem.Net.NetworkInformation.Pingをマルチスレッドで実行しています。

# 注意点

Windows10の場合、以下のようなダイアログが表示され起動しないかもしれません。

![ConnectionTest001.jpg](https://raw.githubusercontent.com/WAKU-TAKE-A/ConnectionTest/master/img/dialog001.jpg)

その時は、「ConnectionTest.exe」のプロバティの下方の、「許可する」にチェックを入れて下さい。

![ConnectionTest001.jpg](https://raw.githubusercontent.com/WAKU-TAKE-A/ConnectionTest/master/img/dialog002.jpg)

# ライセンス

本アプリケーションは「[MITライセンス](https://ja.wikipedia.org/wiki/MIT_License)」です。

本アプリケーションでは、Livetを利用しています。Livetは、尾上雅則 氏が作成したMVVMのインフラストラクチャーです。非常に使い易いインフラストラクチャーで、MVVMを勉強し始めたころから使わせていただいています。

Livetは「[zlib/libpngライセンス](https://ja.wikipedia.org/wiki/Zlib_License)」です。

ともに、オープンソースソフトウェアです。
