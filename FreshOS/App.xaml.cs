using System.IO;
using System.Windows;
namespace FreshOS;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if(e.Args.Contains("--window-fixture")) { new Window { Title="FreshOS interaction fixture",Width=320,Height=160,Content=new System.Windows.Controls.TextBlock { Text="FreshOS test window",Margin=new Thickness(20) } }.Show(); return; }
        if(e.Args.Contains("--self-test"))
        {
            ShutdownMode=ShutdownMode.OnExplicitShutdown;
            var directory=e.Args.SkipWhile(a=>a!="--report-directory").Skip(1).FirstOrDefault()??Path.Combine(Environment.CurrentDirectory,"test-results");
            Dispatcher.BeginInvoke(new Action(async ()=>
            {
                int exit=0;
                try { await Desktop.RunInteractionTests(directory); }
                catch(Exception error) { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString()); exit=1; }
                Shutdown(exit);
            }));
            return;
        }
        Diagnostics.Write($"FreshOS 0.3 starting • {Environment.OSVersion} • .NET {Environment.Version}");
        DispatcherUnhandledException+=(_,a)=>
        {
            Diagnostics.Write("Unhandled UI error",a.Exception);
            MessageBox.Show(a.Exception.Message+"\n\nDetails: "+Diagnostics.Folder,"FreshOS • Something went wrong"); a.Handled=true;
        };
        new Desktop().Show();
    }
}
