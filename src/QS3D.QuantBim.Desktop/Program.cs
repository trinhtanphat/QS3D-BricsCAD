using System;
using System.Windows.Forms;

namespace QS3D.QuantBim.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 1 && string.Equals(args[0], "--self-test", StringComparison.OrdinalIgnoreCase))
        {
            DesktopSelfTest.Run();
            return;
        }

        Application.Run(new QuantBimMainForm());
    }
}
