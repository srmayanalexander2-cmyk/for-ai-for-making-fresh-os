using System.Windows;
namespace FreshOS;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e) { base.OnStartup(e); DispatcherUnhandledException += (_, a) => { MessageBox.Show(a.Exception.Message, "FreshOS • Something went wrong"); a.Handled = true; }; new Desktop().Show(); }
}
