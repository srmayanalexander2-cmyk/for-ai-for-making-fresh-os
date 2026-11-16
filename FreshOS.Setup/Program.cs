using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace FreshOS.Setup;
static class Program
{
    [STAThread] public static int Main(string[] args)
    {
        var app=new Application();
        if(args.Contains("--self-test"))
        {
            try
            {
                var wizard=new SetupWizard(); wizard.Show(); wizard.UpdateLayout();
                if(wizard.Icon==null)throw new InvalidOperationException("Setup icon missing.");
                using var payload=Assembly.GetExecutingAssembly().GetManifestResourceStream("FreshOS.Setup.payload.zip")??throw new IOException("Payload missing.");
                using var archive=new ZipArchive(payload,ZipArchiveMode.Read);
                if(!archive.Entries.Any(e=>e.Name=="FreshOS.exe"))throw new IOException("App not packaged.");
                wizard.Close(); return 0;
            }
            catch(Exception error) { File.WriteAllText(Path.Combine(Environment.CurrentDirectory,"setup-test-failure.txt"),error.ToString()); return 1; }
        }
        return app.Run(new SetupWizard());
    }
}
sealed class SetupWizard : Window
{
    readonly string target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","FreshOS");
    readonly StackPanel page=new(); readonly Button back=new(),next=new(),cancel=new(); readonly TextBlock heading=new(),status=new(); readonly ProgressBar progress=new();
    readonly CheckBox desktopShortcut=new() { Content="Create a desktop shortcut",IsChecked=true,Margin=new Thickness(8,10,8,10) };
    readonly CheckBox dark=new() { Content="Use Moonlight dark theme",Margin=new Thickness(8,10,8,10) };
    readonly CheckBox launch=new() { Content="Open FreshOS when setup finishes",IsChecked=true,Margin=new Thickness(8,10,8,10) };
    int step; bool installing,complete; CancellationTokenSource? cancellation;
    static TextBlock Label(string text,double size=16) => new() { Text=text,FontSize=size,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(8,10,8,10) };
    public SetupWizard()
    {
        Title="FreshOS Setup"; Width=860; Height=650; MinWidth=680; MinHeight=540; WindowStartupLocation=WindowStartupLocation.CenterScreen;
        Icon=new BitmapImage(new Uri("pack://application:,,,/Assets/FreshOS.ico")); FontFamily=new FontFamily("Segoe UI"); FontSize=15; Foreground=new SolidColorBrush(Color.FromRgb(30,81,98)); UseLayoutRounding=true;
        Background=new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Assets/Sky.jpg"))) { Stretch=Stretch.UniformToFill };
        var card=new Border { Background=new SolidColorBrush(Color.FromArgb(242,237,255,253)),CornerRadius=new CornerRadius(28),Margin=new Thickness(24),Padding=new Thickness(26) }; Content=card;
        var grid=new Grid(); grid.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto }); grid.RowDefinitions.Add(new RowDefinition()); grid.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto }); card.Child=grid;
        heading.FontSize=30; heading.FontWeight=FontWeights.Light; heading.Margin=new Thickness(8,4,8,12); heading.TextWrapping=TextWrapping.Wrap; grid.Children.Add(heading);
        var scroll=new ScrollViewer { Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled }; Grid.SetRow(scroll,1); grid.Children.Add(scroll);
        var footer=new Grid { Margin=new Thickness(0,16,0,0) }; footer.ColumnDefinitions.Add(new ColumnDefinition()); footer.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto }); footer.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto }); footer.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto }); Grid.SetRow(footer,2); grid.Children.Add(footer);
        Configure(back,"Back",()=> { step--; Render(); }); Configure(next,"Next",()=>Advance()); Configure(cancel,"Cancel",()=> { if(installing)cancellation?.Cancel(); else Close(); }); Grid.SetColumn(back,1); Grid.SetColumn(next,2); Grid.SetColumn(cancel,3); footer.Children.Add(back); footer.Children.Add(next); footer.Children.Add(cancel);
        Closing+=(_,e)=> { if(installing) { e.Cancel=true; cancellation?.Cancel(); } }; Render();
    }
    static void Configure(Button button,string text,Action action)
    {
        button.Content=text; button.MinWidth=94; button.MinHeight=42; button.Margin=new Thickness(4); button.Padding=new Thickness(14,8,14,8); button.Background=new SolidColorBrush(Color.FromRgb(170,245,225)); button.Click+=(_,_)=>action();
    }
    void Render()
    {
        page.Children.Clear(); back.IsEnabled=step>0 && !complete; next.IsEnabled=true; next.Content=step==0?"Next":step==1?"Install":"Finish";
        if(step==0)
        {
            heading.Text="Welcome to FreshOS Setup"; page.Children.Add(new Image { Source=Icon,Width=80,Height=80,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(8) });
            page.Children.Add(Label("A little fresher. A desktop of your own.",26));
            page.Children.Add(Label("This wizard installs FreshOS for your account and configures its appearance. No administrator access is needed. Your existing desktop, files and apps stay in place."));
            page.Children.Add(Label("FreshOS is an immersive desktop app, not a replacement operating system. F12 and Alt+F4 always let you leave. Sign-in startup stays off unless you later enable it in settings.",14));
        }
        else if(step==1)
        {
            heading.Text="Make yourself at home";
            page.Children.Add(Label("Installation folder",14)); page.Children.Add(new TextBox { Text=target,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,Padding=new Thickness(10),Margin=new Thickness(8) });
            page.Children.Add(desktopShortcut); page.Children.Add(dark); page.Children.Add(launch);
            page.Children.Add(Label("A Start menu shortcut is also created. Close any running FreshOS before updating. No system apps, power policies or file associations are replaced.",14));
        }
        else
        {
            heading.Text="Your fresh start is ready"; page.Children.Add(Label("FreshOS is installed.",26)); page.Children.Add(Label(target,14));
            page.Children.Add(Label("Use the Start arrow for apps and the small tray arrow for open windows. Both arrows animate when opened. F11 toggles fullscreen; F12 exits."));
            page.Children.Add(Label("To uninstall, disable sign-in startup if enabled, exit FreshOS, then delete the installation folder and its shortcuts. Your settings are stored separately in %LOCALAPPDATA%\\FreshOS.",13));
            cancel.Visibility=Visibility.Collapsed;
        }
    }
    async void Advance()
    {
        if(complete) { if(launch.IsChecked==true)Process.Start(new ProcessStartInfo(Path.Combine(target,"FreshOS.exe")) { UseShellExecute=true }); Close(); return; }
        if(step==0) { step=1; Render(); return; }
        installing=true; cancellation=new CancellationTokenSource(); back.IsEnabled=next.IsEnabled=false; heading.Text="Installing FreshOS…"; page.Children.Clear(); status.Text="Preparing your fresh desktop"; status.TextWrapping=TextWrapping.Wrap; page.Children.Add(status); progress.Height=8; progress.Margin=new Thickness(8,20,8,20); progress.IsIndeterminate=true; page.Children.Add(progress);
        string staging=target+".staging-"+Guid.NewGuid().ToString("N"),backup=target+".previous-"+Guid.NewGuid().ToString("N");
        try
        {
            foreach(var process in Process.GetProcessesByName("FreshOS")) { process.Dispose(); throw new IOException("Close FreshOS before installing or updating, then try again."); }
            await Task.Run(()=>
            {
                Directory.CreateDirectory(staging);
                using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("FreshOS.Setup.payload.zip")??throw new IOException("Installer payload is missing.");
                using var archive=new ZipArchive(stream,ZipArchiveMode.Read);
                foreach(var entry in archive.Entries)
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    var destination=Path.GetFullPath(Path.Combine(staging,entry.FullName));
                    if(!destination.StartsWith(Path.GetFullPath(staging)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Invalid installer archive path.");
                    if(string.IsNullOrEmpty(entry.Name))Directory.CreateDirectory(destination);
                    else { Directory.CreateDirectory(Path.GetDirectoryName(destination)!); entry.ExtractToFile(destination,true); }
                }
                cancellation.Token.ThrowIfCancellationRequested();
                if(Directory.Exists(target))Directory.Move(target,backup);
                try { Directory.Move(staging,target); } catch { if(Directory.Exists(backup))Directory.Move(backup,target); throw; }
            });
            CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Programs));
            if(desktopShortcut.IsChecked==true)CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            string settingsFolder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FreshOS"); Directory.CreateDirectory(settingsFolder); string settingsFile=Path.Combine(settingsFolder,"settings.json"); JsonObject settings;
            try { settings=JsonNode.Parse(File.ReadAllText(settingsFile))?.AsObject()??new(); } catch { settings=new(); }
            settings["SetupDone"]=true; settings["Dark"]=dark.IsChecked==true; File.WriteAllText(settingsFile,settings.ToJsonString());
            try { if(Directory.Exists(backup))Directory.Delete(backup,true); } catch { }
            complete=true; step=2;
        }
        catch(OperationCanceledException) { status.Text="Setup cancelled. No system settings were changed."; }
        catch(Exception error) { MessageBox.Show(error.Message,"FreshOS Setup",MessageBoxButton.OK,MessageBoxImage.Information); }
        finally { installing=false; cancellation.Dispose(); cancellation=null; try { if(Directory.Exists(staging))Directory.Delete(staging,true); } catch { } }
        Render();
    }
    void CreateShortcut(string folder)
    {
        var type=Type.GetTypeFromProgID("WScript.Shell")??throw new IOException("Shortcut creation is unavailable.");
        dynamic shell=Activator.CreateInstance(type)!; dynamic link=shell.CreateShortcut(Path.Combine(folder,"FreshOS.lnk"));
        try { link.TargetPath=Path.Combine(target,"FreshOS.exe"); link.WorkingDirectory=target; link.IconLocation=Path.Combine(target,"FreshOS.exe")+",0"; link.Description="FreshOS • A breath of fresh air"; link.Save(); }
        finally { System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link); System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell); }
    }
}
