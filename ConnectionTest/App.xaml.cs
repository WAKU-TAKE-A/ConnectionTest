using System.Windows;
using System.IO;
using System.Reflection;

namespace ConnectionTest;

public partial class App : Application
{
    public static readonly string ApplicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
}
