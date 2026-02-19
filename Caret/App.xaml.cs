using System.Text;
using System.Windows;

namespace Caret;

public partial class App : Application
{
    private string[]? _startupFiles;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        if (e.Args.Length > 0)
        {
            _startupFiles = e.Args;
        }
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (_startupFiles != null)
        {
            var files = _startupFiles;
            _startupFiles = null;

            if (MainWindow is MainWindow mainWin)
            {
                foreach (var file in files)
                {
                    if (System.IO.File.Exists(file))
                        mainWin.OpenFile(file);
                }
            }
        }
    }
}
