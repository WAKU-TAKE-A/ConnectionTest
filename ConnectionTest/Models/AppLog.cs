using System;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConnectionTest.Models;

internal static class AppLog
{
    // Bounded, asynchronous logging avoids blocking the UI and unbounded memory growth.
    private static readonly Channel<string> Entries = Channel.CreateBounded<string>(
        new BoundedChannelOptions(256) { SingleReader = true, FullMode = BoundedChannelFullMode.DropOldest });
    private static readonly Task Writer = Task.Run(WriteAsync);

    public static void Error(string operation, Exception exception) =>
        Entries.Writer.TryWrite($"{DateTimeOffset.Now:O} {operation}{Environment.NewLine}{exception}{Environment.NewLine}");

    private static async Task WriteAsync()
    {
        await foreach (string entry in Entries.Reader.ReadAllAsync())
        {
            try
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ConnectionTest", "Logs");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, $"{DateTime.Now:yyyy-MM-dd}.log");
                if (File.Exists(path) && new FileInfo(path).Length >= 5 * 1024 * 1024)
                    continue;
                await File.AppendAllTextAsync(path, entry, Encoding.UTF8);
            }
            catch (Exception)
            {
                // Logging is best effort; never recursively log a logging failure.
            }
        }
    }
}
