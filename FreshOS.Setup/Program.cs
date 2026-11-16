using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
namespace FreshOS.Setup;
static class Program
{
    [STAThread] public static void Main()
    {
        var target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","FreshOS");
        if(MessageBox.Show("Welcome to FreshOS\n\nInstall for your account in:\n"+target+"\n\nCreates a desktop shortcut. No administrator rights required. Does not replace system apps or enable startup. Close FreshOS before updating.\n\nContinue?","FreshOS • Setup",MessageBoxButton.YesNo,MessageBoxImage.Information)!=MessageBoxResult.Yes)return;
        try
        {
            Directory.CreateDirectory(target);
            using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("FreshOS.Setup.payload.zip")??throw new IOException("Installer payload is missing.");
            using var zip=new ZipArchive(stream,ZipArchiveMode.Read);
            zip.ExtractToDirectory(target,true);
            // Per-user shortcut only; no shell registration or elevation.
            var type=Type.GetTypeFromProgID("WScript.Shell");
            if(type!=null) { dynamic shell=Activator.CreateInstance(type)!; dynamic link=shell.CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"FreshOS.lnk")); link.TargetPath=Path.Combine(target,"FreshOS.exe"); link.WorkingDirectory=target; link.Description="FreshOS • A breath of fresh air"; link.Save(); }
            if(MessageBox.Show("FreshOS is ready.\n\nTo uninstall: exit FreshOS, disable sign-in launch if enabled, delete the installation folder and desktop shortcut. Preferences are in %LOCALAPPDATA%\\FreshOS.\n\nOpen FreshOS now?","FreshOS",MessageBoxButton.YesNo)==MessageBoxResult.Yes)Process.Start(new ProcessStartInfo(Path.Combine(target,"FreshOS.exe")) { UseShellExecute=true });
        }
        catch(Exception e) { MessageBox.Show("Installation could not finish:\n"+e.Message+"\n\nYour system shell has not been modified.","FreshOS • Setup"); }
    }
}
