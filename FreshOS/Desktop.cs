using System.Windows.Documents;
using System.IO;
using System.IO.Compression;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace FreshOS;
public sealed partial class Desktop : Window
{
    readonly Preferences prefs;
    readonly bool selfTest;
    readonly Grid root=new(), workspace=new(), shade=new();
    readonly StackPanel tasks=new();
    readonly TextBlock clock=new();
    readonly DispatcherTimer timer=new() { Interval=TimeSpan.FromSeconds(1) };
    readonly List<(string Name,string Path)> apps=new();
    TextBlock? largeClock, largeDate, lockClock;
    ScrollViewer? panelScroll;
    Button? launchButton;
    FlipArrow? startArrow, trayArrow, launcherTrayArrow;
    TextBlock? desktopClock;
    Border? panel; Grid? lockView; bool launcherOpen, super; int taps; string location=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    Brush Ink => new SolidColorBrush(prefs.Dark?Color.FromRgb(222,250,250):Color.FromRgb(33,81,93));
    Brush Glass => new LinearGradientBrush(prefs.Dark?Color.FromArgb(245,23,58,72):Color.FromArgb(245,247,255,255),prefs.Dark?Color.FromArgb(235,17,104,119):Color.FromArgb(239,115,240,231),90);
    public Desktop(bool testMode=false)
    {
        selfTest=testMode; prefs=testMode?new Preferences { ReducedMotion=true,SetupDone=true }:Preferences.Load();
        Title="FreshOS"; Icon=new BitmapImage(new Uri("pack://application:,,,/Assets/FreshOS.ico")); Width=1280; Height=800; MinWidth=800; MinHeight=600; WindowState=WindowState.Normal; WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.NoResize;
        FontFamily=new FontFamily("Segoe UI"); FontSize=14; UseLayoutRounding=true; SnapsToDevicePixels=true;
        TextOptions.SetTextFormattingMode(this,TextFormattingMode.Display);
        Content=root; ApplyWallpaper();
        root.Children.Add(workspace); root.Children.Add(shade); shade.Background=Brushes.Black; shade.Opacity=0; shade.IsHitTestVisible=false;
        SourceInitialized+=(_,_)=>InitializeShellWindow();
        BuildDesktop(); SizeChanged+=(_,_)=>ResizePanel(); Closed+=(_,_)=>timer.Stop(); PreviewKeyDown+=Keys; timer.Tick+=(_,_)=>Tick(); if(!testMode)timer.Start();
        if(!testMode)Loaded+=async (_,_)=> { Boot(); var bootPanel=panel; var discovered=await Task.Run(Platform.Apps); apps.AddRange(discovered); if(launcherOpen)ShowApps(); await Task.Delay(650); if(IsLoaded && panel==bootPanel) { ClosePanel(); if(!prefs.SetupDone) Welcome(); else PlayBoot(); } };
    }
    static TextBlock Text(string s,double size=16) => new() { Text=s,FontSize=size,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(8),VerticalAlignment=VerticalAlignment.Center };
    Button Button(string text,Action action)
    {
        var b=new Button { Content=new TextBlock { Text=text,TextWrapping=TextWrapping.Wrap,TextAlignment=TextAlignment.Center } };
        System.Windows.Automation.AutomationProperties.SetName(b,text);
        b.Click+=(_,_)=>Safe(action);
        b.MouseEnter+=(_,_)=> { if(!MotionReduced)b.BeginAnimation(OpacityProperty,new System.Windows.Media.Animation.DoubleAnimation(.88,1,TimeSpan.FromMilliseconds(140))); };
        return b;
    }
    void Safe(Action a) { try { a(); } catch(Exception e) { if(selfTest)throw; Diagnostics.Write("UI action failed",e); MessageBox.Show(e.Message,"FreshOS",MessageBoxButton.OK,MessageBoxImage.Information); } }
    void ApplyWallpaper()
    {
        try
        {
            var image=new BitmapImage(); image.BeginInit(); image.CacheOption=BitmapCacheOption.OnLoad; image.DecodePixelWidth=3840;
            image.UriSource=string.IsNullOrEmpty(prefs.Wallpaper)?new Uri("pack://application:,,,/Assets/Sky.jpg"):new Uri(prefs.Wallpaper); image.EndInit(); image.Freeze();
            root.Background=new ImageBrush(image) { Stretch=Stretch.UniformToFill };
        }
        catch { prefs.Wallpaper=""; root.Background=new SolidColorBrush(Colors.SkyBlue); }
    }
    Border Bubble(UIElement content,double width=440)
    {
        var layers=new Grid();
        layers.Children.Add(new Border { CornerRadius=new CornerRadius(35),BorderBrush=new SolidColorBrush(Color.FromArgb(95,255,255,255)),BorderThickness=new Thickness(1),Margin=new Thickness(2),IsHitTestVisible=false });
        if(prefs.MaterialTexture)layers.Children.Add(new Border { Background=Aero.Grain(),IsHitTestVisible=false,Margin=new Thickness(8) });
        layers.Children.Add(new Border { Padding=new Thickness(22),Child=content });
        return new Border { Child=layers,Width=width,CornerRadius=new CornerRadius(38),Background=Glass,BorderBrush=new SolidColorBrush(Color.FromArgb(220,240,255,255)),BorderThickness=new Thickness(1),Effect=new DropShadowEffect { Color=Color.FromRgb(0,75,100),BlurRadius=26,Opacity=.16,ShadowDepth=7 } };
    }
    void BuildDesktop()
    {
        ClosePanel(); CloseTray();
        if(clock.Parent is Panel clockParent)clockParent.Children.Remove(clock);
        if(tasks.Parent is Panel taskParent)taskParent.Children.Remove(tasks);
        workspace.Children.Clear(); workspace.ForegroundSafe(Ink);
        Resources["FreshInk"]=Ink;
        Resources["FreshButton"]=new LinearGradientBrush(prefs.Dark?Color.FromRgb(43,107,123):Color.FromArgb(238,255,255,255),prefs.Dark?Color.FromRgb(22,72,91):Color.FromArgb(210,191,244,238),90);
        var rail=new Border { Width=64,Height=420,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,CornerRadius=new CornerRadius(0,0,29,0),Background=Aero.Tube(prefs.Dark),BorderBrush=new SolidColorBrush(Color.FromArgb(160,255,255,255)),BorderThickness=new Thickness(0,0,1,1) };
        var railItems=new StackPanel { Margin=new Thickness(2,8,2,8) }; rail.Child=railItems;
        var browser=Button("◉",()=> { var a=apps.FirstOrDefault(a=>a.Name.Contains("Chrome",StringComparison.OrdinalIgnoreCase)||a.Name.Contains("Edge",StringComparison.OrdinalIgnoreCase)); if(a.Path!=null)Launch(a.Path); else ShowApps(); });
        browser.Padding=new Thickness(0); browser.FontSize=29; browser.Background=Brushes.Transparent; browser.BorderBrush=Brushes.Transparent; browser.ToolTip="Open your browser"; railItems.Children.Add(browser);
        var caption=Text("Apps",10); caption.TextAlignment=TextAlignment.Center; caption.Margin=new Thickness(0); railItems.Children.Add(caption);
        railItems.Children.Add(new Border { Height=115 });
        clock.TextAlignment=TextAlignment.Center; clock.FontSize=16; clock.TextWrapping=TextWrapping.NoWrap; clock.Margin=new Thickness(0,0,0,10); clock.LineHeight=24; clock.Foreground=Ink; railItems.Children.Add(clock);
        foreach(var item in new (string Glyph,string Tip,Action Action)[]{("◀","Your windows · Ctrl+Tab",ShowSwitcher),("➜","App tray",ShowTray),("♫","Volume",ShowVolume),("☼","Brightness",ShowBrightness)})
        {
            var b=Button(item.Glyph,item.Action);
            string controlId=item.Tip.StartsWith("Your windows")?"Switcher":item.Tip.StartsWith("App tray")?"Tray":item.Tip.StartsWith("Volume")?"Volume":"Brightness";
            System.Windows.Automation.AutomationProperties.SetAutomationId(b,"FreshOS.Rail."+controlId);
            System.Windows.Automation.AutomationProperties.SetName(b,item.Tip);
            b.Padding=new Thickness(0); b.Margin=new Thickness(0); b.MinHeight=29; b.FontSize=22; b.Background=Brushes.Transparent; b.BorderBrush=Brushes.Transparent; b.ToolTip=item.Tip;
            if(controlId=="Tray") { trayArrow=new FlipArrow(24); b.Content=trayArrow; }
            railItems.Children.Add(b);
        }
        Panel.SetZIndex(rail,50); workspace.Children.Add(rail);
        var lens=new AeroMedallion { Scalloped=false,Dark=prefs.Dark,Width=166,Height=166,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(-96,430,0,0) };
        launchButton=Button("➜",()=> { if(launcherOpen)ClosePanel(); else ShowApps(); });
        System.Windows.Automation.AutomationProperties.SetAutomationId(launchButton,"FreshOS.Launcher");
        startArrow=new FlipArrow(46); launchButton.Content=startArrow;
        launchButton.FontSize=46; launchButton.Foreground=new LinearGradientBrush(Colors.White,Color.FromRgb(0,255,108),90); launchButton.Background=Brushes.Transparent; launchButton.BorderBrush=Brushes.Transparent; launchButton.Padding=new Thickness(0); launchButton.Margin=new Thickness(98,0,0,0); launchButton.ToolTip="Open / close All Apps";
        lens.Children.Add(launchButton); workspace.Children.Add(lens);
        desktopClock=Text("",16); desktopClock.TextAlignment=TextAlignment.Right; desktopClock.TextWrapping=TextWrapping.NoWrap; desktopClock.Foreground=Ink;
        System.Windows.Automation.AutomationProperties.SetAutomationId(desktopClock,"FreshOS.Desktop.Clock");
        var timeCard=new Border { Child=desktopClock,Background=Glass,CornerRadius=new CornerRadius(20),Padding=new Thickness(10,4,10,4),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(12) }; workspace.Children.Add(timeCard);
        if(prefs.ShowDock)
        {
            var bottom=new Border { Background=Glass,CornerRadius=new CornerRadius(27),Padding=new Thickness(8,3,8,3),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(75,0,20,18) };
            var dock=new WrapPanel(); bottom.Child=dock;
            dock.Children.Add(Button("All Apps",ShowApps)); dock.Children.Add(Button("Files",()=>ShowFiles(location))); dock.Children.Add(Button("Settings",Settings)); dock.Children.Add(tasks); dock.Children.Add(Button("Lock",()=>Lock(false))); workspace.Children.Add(bottom);
        }
        var menu=new ContextMenu(); foreach(var (label,action) in new (string,Action)[]{("All apps",ShowApps),("Files",()=>ShowFiles(location)),("Personalize",Settings),("FreshOS window layout",ShowSnapLayouts),("Lock",()=>Lock(false)),("Exit FreshOS",Close)}) { var m=new MenuItem { Header=label }; m.Click+=(_,_)=>Safe(action); menu.Items.Add(m); } workspace.ContextMenu=menu;
        Tick();
    }
    void Tick() { clock.Text=DateTime.Now.ToString("h:mm\nM/dd"); if(desktopClock!=null)desktopClock.Text=DateTime.Now.ToString("h:mm tt  •  MMM d"); if(lockClock!=null)lockClock.Text=DateTime.Now.ToString("h:mm"); if(largeClock!=null)largeClock.Text=DateTime.Now.ToString("h:mm"); if(largeDate!=null)largeDate.Text=DateTime.Now.ToString("MMM d"); RefreshTray(); if(prefs.AutoLock && lockView==null && Platform.IdleSeconds>=prefs.LockMinutes*60) Lock(prefs.Screensaver); }
    void Keys(object sender,KeyEventArgs e) { if(e.Key==Key.Escape) { if(lockView!=null) Unlock(); else if(trayPanel!=null)CloseTray(); else ClosePanel(); e.Handled=true; } if(e.Key==Key.F12) { Close(); e.Handled=true; } if(e.Key==Key.F11) { SetFullscreen(!fullscreen); e.Handled=true; } if(lockView==null && e.Key==Key.Tab && Keyboard.Modifiers==ModifierKeys.Control) { ShowSwitcher(); e.Handled=true; } }
    void ResizePanel()
    {
        if(trayPanel!=null)
        {
            trayPanel.MaxWidth=Math.Max(260,ActualWidth-90); trayPanel.MaxHeight=Math.Max(300,ActualHeight-120);
            if(trayPanel.Child is Grid layers && layers.Children.OfType<Border>().LastOrDefault()?.Child is ScrollViewer trayScroll)trayScroll.MaxHeight=Math.Max(180,ActualHeight-180);
        }
        if(panel==null)return;
        panel.MaxWidth=Math.Max(240,ActualWidth-100);
        panel.MaxHeight=Math.Max(180,ActualHeight-70);
        if(panelScroll!=null)panelScroll.MaxHeight=Math.Max(120,ActualHeight-205);
    }
    StackPanel ShowPanel(string title,double width=680)
    {
        ClosePanel(); var outer=new DockPanel(); var head=new DockPanel(); DockPanel.SetDock(head,Dock.Top);
        var close=Button("×",ClosePanel); System.Windows.Automation.AutomationProperties.SetAutomationId(close,"FreshOS.Panel.Close"); close.FontSize=22; close.ToolTip="Close · Escape"; close.Width=40; close.Padding=new Thickness(4); DockPanel.SetDock(close,Dock.Right); head.Children.Add(close);
        var heading=Text(title,26); heading.FontWeight=FontWeights.Light; head.Children.Add(heading); outer.Children.Add(head);
        var body=new StackPanel(); panelScroll=new ScrollViewer { Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,PanningMode=PanningMode.VerticalOnly,Margin=new Thickness(0,12,0,0) }; outer.Children.Add(panelScroll);
        panel=Bubble(outer,width); panel.HorizontalAlignment=HorizontalAlignment.Center; panel.VerticalAlignment=VerticalAlignment.Center; panel.SetValue(TextElement.ForegroundProperty,Ink); workspace.Children.Add(panel); ResizePanel(); Aero.Enter(panel,MotionReduced); return body;
    }
    bool MotionReduced => prefs.ReducedMotion || !SystemParameters.ClientAreaAnimation;
    void ClosePanel()
    {
        if(panel!=null)
        {
            var old=panel; old.IsHitTestVisible=false;
            if(MotionReduced)workspace.Children.Remove(old);
            else
            {
                var fade=new System.Windows.Media.Animation.DoubleAnimation(0,TimeSpan.FromMilliseconds(110));
                fade.Completed+=(_,_)=>workspace.Children.Remove(old);
                old.BeginAnimation(OpacityProperty,fade);
            }
        }
        panel=null; panelScroll=null; largeClock=null; largeDate=null; launcherOpen=false;
        startArrow?.SetOpen(false,MotionReduced); launcherTrayArrow=null;
    }
    void Boot() { var p=ShowPanel("freshOS",380); p.Children.Add(Text("◉",72)); p.Children.Add(Text("A breath of fresh air.",22)); p.Children.Add(new ProgressBar { IsIndeterminate=true,Height=6,Margin=new Thickness(10) }); }
    void PlayBoot() { if(File.Exists(prefs.BootSound)) Safe(()=>new SoundPlayer(prefs.BootSound).Play()); }
    void Welcome() => AeroWelcome();
    void ShowApps() => ShowAeroLauncher();
    void Launch(string path) { Platform.Open(path); ClosePanel(); var b=Button(Path.GetFileNameWithoutExtension(path),()=>Platform.Open(path)); b.MaxWidth=180; if(tasks.Children.Count>3) tasks.Children.RemoveAt(0); tasks.Children.Add(b); }
    void ShowSwitcher() { var p=ShowPanel("Your windows",760); p.Children.Add(Text("Real apps, right where you left them. Select a window to return to it.")); var windows=Platform.Windows(); foreach(var w in windows) p.Children.Add(Button("▣   "+w.Title,()=> { ClosePanel(); Platform.Activate(w.Handle); })); if(windows.Count==0) p.Children.Add(Text("No other app windows are open.")); }
    void ShowVolume() => ShowAeroVolume();
    void ShowBrightness() => ShowAeroBrightness();
    void Power() { var p=ShowPanel("See you soon",450); p.Children.Add(Button("Lock desktop",()=>Lock(false))); p.Children.Add(Button("Rest • screensaver",()=>Lock(true))); p.Children.Add(Button("Restart FreshOS",()=> { Platform.Open(Environment.ProcessPath!); Close(); })); p.Children.Add(Button("Exit FreshOS",Close)); p.Children.Add(Text("Rest does not terminate any of your apps or change your PC’s power policy.")); }
    void Lock(bool saver) { CloseTray(); ClosePanel(); if(lockView!=null)return; lockView=new Grid { Background=new SolidColorBrush(Color.FromArgb(215,11,62,84)) }; var p=new StackPanel { VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center }; p.SetValue(TextElement.ForegroundProperty,Brushes.White); var time=Text(DateTime.Now.ToString("h:mm"),96); lockClock=time; time.Foreground=Brushes.White; time.TextAlignment=TextAlignment.Center; p.Children.Add(time); var date=Text(DateTime.Now.ToString("dddd, MMMM d"),24); date.Foreground=Brushes.White; p.Children.Add(date); p.Children.Add(Button("☻  Welcome back • unlock",Unlock)); p.Children.Add(Text("FreshOS privacy screen • not a secure account lock",13)); lockView.Children.Add(p); if(saver && !MotionReduced) { var a=new System.Windows.Media.Animation.DoubleAnimation(.35,1,TimeSpan.FromSeconds(4)) { AutoReverse=true,RepeatBehavior=System.Windows.Media.Animation.RepeatBehavior.Forever }; time.BeginAnimation(OpacityProperty,a); } Panel.SetZIndex(lockView,100); workspace.Children.Add(lockView); }
    void Unlock() { if(lockView!=null)workspace.Children.Remove(lockView); lockView=null; lockClock=null; }
    void ShowFiles(string path)
    {
        if(!Directory.Exists(path)) { MessageBox.Show("This folder does not exist or is not accessible:\n"+path,"FreshOS • Files"); return; } location=Path.GetFullPath(path); var p=ShowPanel("▱  Files",880);
        var nav=new WrapPanel(); p.Children.Add(nav); nav.Children.Add(Button("↑",()=>ShowFiles(Directory.GetParent(location)?.FullName??location)));
        foreach(var (name,dir) in new[]{("Home",Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),("Desktop",Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)),("Documents",Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)),("Downloads",Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"))}) nav.Children.Add(Button(name,()=>ShowFiles(dir)));
        var address=new TextBox { Text=location }; address.KeyDown+=(_,e)=> { if(e.Key==Key.Enter)Safe(()=>ShowFiles(address.Text)); }; p.Children.Add(address);
        var list=new ListBox { Height=310,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Foreground=Ink,Margin=new Thickness(4),DisplayMemberPath="Label" }; p.Children.Add(list);
        VirtualizingPanel.SetIsVirtualizing(list,true); VirtualizingPanel.SetVirtualizationMode(list,VirtualizationMode.Recycling);
        var filesPanel=panel; var status=Text("Reading folder…",12); p.Children.Add(status);
        _=LoadDirectoryAsync(path,list,status,filesPanel);
        void OpenSelected() { if(list.SelectedItem is Entry e) { if(e.Directory)ShowFiles(e.Path); else Platform.Open(e.Path); } }
        list.MouseDoubleClick+=(_,_)=>Safe(OpenSelected); var actions=new WrapPanel(); p.Children.Add(actions);
        actions.Children.Add(Button("Open",OpenSelected)); actions.Children.Add(Button("New folder",()=>Prompt("Folder name",name=> { Directory.CreateDirectory(ChildPath(location,name)); ShowFiles(location); })));
        actions.Children.Add(Button("Rename",()=> { if(list.SelectedItem is Entry e)Prompt("Rename to",name=> { var target=ChildPath(Path.GetDirectoryName(e.Path)!,name); if(e.Directory)Directory.Move(e.Path,target); else File.Move(e.Path,target); ShowFiles(location); }); }));
        actions.Children.Add(Button("Copy to…",()=> { if(list.SelectedItem is Entry e && !e.Directory) { var save=new SaveFileDialog { FileName=Path.GetFileName(e.Path) }; if(save.ShowDialog()==true)_=RunFileOperation("Copying file…",()=>File.Copy(e.Path,save.FileName,true)); } }));
        actions.Children.Add(Button("Recycle",()=> { if(list.SelectedItem is Entry e && MessageBox.Show("Move to Recycle Bin?\n"+e.Path,"FreshOS",MessageBoxButton.YesNo)==MessageBoxResult.Yes) { if(e.Directory)Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(e.Path,Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin); else Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(e.Path,Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin); ShowFiles(location); } }));
        actions.Children.Add(Button("Extract ZIP",()=> { if(list.SelectedItem is Entry e && e.Path.EndsWith(".zip",StringComparison.OrdinalIgnoreCase)) { var destination=Path.Combine(location,Path.GetFileNameWithoutExtension(e.Path)+" extracted"); if(MessageBox.Show("Extract to "+destination+"? Only extract archives you trust.","FreshOS",MessageBoxButton.YesNo)==MessageBoxResult.Yes) { _=RunFileOperation("Unpacking archive…",()=>ZipFile.ExtractToDirectory(e.Path,destination,false)); } } else MessageBox.Show("Select a ZIP archive. Other archive formats can be opened in an installed archive app."); }));
        p.Children.Add(Text("Double-click to open • Changes affect your real files • Deletions use the Recycle Bin",12));
    }
    async Task LoadDirectoryAsync(string path,ListBox list,TextBlock status,Border? owner)
    {
        try
        {
            var entries=await Task.Run(()=>Directory.EnumerateDirectories(path).Order(StringComparer.OrdinalIgnoreCase).Select(p=>new Entry(p,true)).Concat(Directory.EnumerateFiles(path).Order(StringComparer.OrdinalIgnoreCase).Select(p=>new Entry(p,false))).ToList());
            if(panel!=owner)return;
            list.ItemsSource=entries; status.Text=$"{entries.Count(e=>e.Directory)} folders  ·  {entries.Count(e=>!e.Directory)} files";
        }
        catch(Exception error) { if(panel==owner)status.Text="Could not read this folder: "+error.Message; }
    }
    async Task RunFileOperation(string title,Action action)
    {
        var path=location; var body=ShowPanel(title,420); var owner=panel;
        body.Children.Add(new ProgressBar { IsIndeterminate=true,Height=5,Margin=new Thickness(8,16,8,16) });
        body.Children.Add(Text("You can close this panel; the operation will continue in the background.",12));
        try { await Task.Run(action); if(panel==owner)ShowFiles(path); }
        catch(Exception error) { if(panel==owner) { var p=ShowPanel("Operation incomplete",500); p.Children.Add(Text(error.Message)); p.Children.Add(Text("Some files may already have been written. Inspect the destination before trying again.",12)); p.Children.Add(Button("Back to files",()=>ShowFiles(path))); } else MessageBox.Show(error.Message,"FreshOS • File operation incomplete"); }
    }
    sealed record Entry(string Path,bool Directory) { public string Label => (Directory?"▱   ":"▪   ")+System.IO.Path.GetFileName(Path); }
    static string ChildPath(string parent,string name) { if(string.IsNullOrWhiteSpace(name)||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name is "." or "..")throw new IOException("Please use a valid file or folder name."); return Path.Combine(parent,name); }
    void Prompt(string title,Action<string> submit) { var dialog=new Window { Title=title,Width=400,Height=170,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize }; var p=new StackPanel { Margin=new Thickness(16) }; var input=new TextBox(); p.Children.Add(input); p.Children.Add(Button("Save",()=> { submit(input.Text); dialog.Close(); })); dialog.Content=p; dialog.ShowDialog(); }
    void Settings()
    {
        var p=ShowPanel("⚙  Make yourself at home",740);
        p.Children.Add(Button("FreshOS fullscreen & custom snap layouts",ShowSnapLayouts));
        p.Children.Add(Text("PERSONALIZATION",12));
        var motion=new CheckBox { Content="Reduced motion • instant transitions",IsChecked=prefs.ReducedMotion }; motion.Click+=(_,_)=> { prefs.ReducedMotion=motion.IsChecked==true; prefs.Save(); }; p.Children.Add(motion);
        var texture=new CheckBox { Content="Fine glass texture & surface detail",IsChecked=prefs.MaterialTexture }; texture.Click+=(_,_)=> { prefs.MaterialTexture=texture.IsChecked==true; prefs.Save(); Settings(); }; p.Children.Add(texture);
        var dock=new CheckBox { Content="Show optional bottom dock (off for reference layout)",IsChecked=prefs.ShowDock }; dock.Click+=(_,_)=> { prefs.ShowDock=dock.IsChecked==true; prefs.Save(); BuildDesktop(); Settings(); }; p.Children.Add(dock);
        var dark=new CheckBox { Content="Moonlight • dark glass",IsChecked=prefs.Dark }; System.Windows.Automation.AutomationProperties.SetAutomationId(dark,"FreshOS.Theme"); dark.Click+=(_,_)=> { prefs.Dark=dark.IsChecked==true; prefs.Save(); BuildDesktop(); Settings(); }; p.Children.Add(dark);
        p.Children.Add(Button("Choose wallpaper…",()=> { var f=new OpenFileDialog { Filter="Pictures|*.jpg;*.jpeg;*.png;*.bmp" }; if(f.ShowDialog()==true) { prefs.Wallpaper=f.FileName; prefs.Save(); ApplyWallpaper(); } }));
        p.Children.Add(Button("Restore cloud wallpaper",()=> { prefs.Wallpaper=""; prefs.Save(); ApplyWallpaper(); }));
        p.Children.Add(Button("Custom boot sound (.wav)…",()=> { var f=new OpenFileDialog { Filter="Wave audio|*.wav" }; if(f.ShowDialog()==true) { prefs.BootSound=f.FileName; prefs.Save(); PlayBoot(); } }));
        p.Children.Add(Button("Clear boot sound",()=> { prefs.BootSound=""; prefs.Save(); }));
        p.Children.Add(Text("REST & PRIVACY",12));
        var auto=new CheckBox { Content="Show privacy screen after inactivity",IsChecked=prefs.AutoLock }; auto.Click+=(_,_)=> { prefs.AutoLock=auto.IsChecked==true; prefs.Save(); }; p.Children.Add(auto);
        p.Children.Add(Text("Idle timeout • minutes")); var minutes=new Slider { Minimum=1,Maximum=60,TickFrequency=1,IsSnapToTickEnabled=true,Value=prefs.LockMinutes,Margin=new Thickness(12) }; var value=Text(prefs.LockMinutes+" minutes"); minutes.ValueChanged+=(_,_)=> { prefs.LockMinutes=(int)minutes.Value; value.Text=prefs.LockMinutes+" minutes"; prefs.Save(); }; p.Children.Add(minutes); p.Children.Add(value);
        var saver=new CheckBox { Content="Breathing-clock screensaver",IsChecked=prefs.Screensaver }; saver.Click+=(_,_)=> { prefs.Screensaver=saver.IsChecked==true; prefs.Save(); }; p.Children.Add(saver);
        p.Children.Add(Text("FreshOS respects your system sleep settings. This privacy screen is not a password lock. Use your system account lock for security.",12));
        p.Children.Add(Button("FreshOS 0.4 • "+(super?"Super features unlocked":"About this little world"),()=> { taps++; if(taps>=10) { super=true; Settings(); } }));
        if(super) { p.Children.Add(Text("THE FOURTH WALL",20)); var startup=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"FreshOS.cmd"); var enabled=new CheckBox { Content="Launch FreshOS when I sign in (off by default)",IsChecked=File.Exists(startup) }; enabled.Click+=(_,_)=>Safe(()=> { if(enabled.IsChecked==true)File.WriteAllText(startup,"@echo off\r\nstart \"\" \""+Environment.ProcessPath+"\"\r\n"); else if(File.Exists(startup))File.Delete(startup); }); p.Children.Add(enabled); p.Children.Add(Button("Host system settings",()=>Platform.Open("ms-settings:"))); p.Children.Add(Button("Replay welcome",Welcome)); }
        p.Children.Add(Text("F12 or Alt+F4 always exits. No system apps are replaced. External app borders remain under their app’s control.",12));
        p.Children.Add(Button("Open diagnostic logs",()=> { Directory.CreateDirectory(Diagnostics.Folder); Platform.Open(Diagnostics.Folder); }));
        p.Children.Add(Button("Leave FreshOS",Close));
    }
}
static class UIExtensions { public static void ForegroundSafe(this Panel p,Brush brush) => p.SetValue(TextElement.ForegroundProperty,brush); }
