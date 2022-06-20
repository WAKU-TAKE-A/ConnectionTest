using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Livet;

// user add
using System.Diagnostics;

namespace ConnectionTest.Models
{
    public class DosCommand : NotificationObject
    {
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */

        public string StandardOutput = "";

        public bool Run(string command = "")
        {
            // 結果をfalseにセット
            bool bret = false;

            // Processオブジェクトを作成
            Process p = new Process();

            try
            {
                // ComSpec(cmd.exe)のパスを取得して、FileNameプロパティに指定
                p.StartInfo.FileName = System.Environment.GetEnvironmentVariable("ComSpec");

                // 出力を読み取れるようにする
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = false;

                //ウィンドウを表示しないようにする
                p.StartInfo.CreateNoWindow = true;

                //コマンドラインを指定（"/c"は実行後閉じるために必要）
                p.StartInfo.Arguments = "/c " + command;

                bret = p.Start();

                //出力を読み取る
                StandardOutput = p.StandardOutput.ReadToEnd();

                //プロセス終了まで待機する
                //WaitForExitはReadToEndの後である必要がある
                //(親プロセス、子プロセスでブロック防止のため)
                p.WaitForExit();
            }
            catch (Exception)
            {
                bret = false;
                StandardOutput = command + "は異常終了しました";
            }

            p.Close();
            return bret;
        }
    }
}
