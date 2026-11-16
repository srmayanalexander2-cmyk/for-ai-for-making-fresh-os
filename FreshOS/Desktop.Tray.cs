using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
namespace FreshOS;
public sealed partial class Desktop
{
    Border? trayPanel;
    StackPanel? trayWindows;
    TextBlock? trayCount;
    string traySignature="";
    void ShowTray()
    {
        if(trayPanel!=null) { CloseTray(); return; }
        trayArrow?.SetOpen(true,MotionReduced); launcherTrayArrow?.SetOpen(true,MotionReduced);
        var content=new StackPanel();
        var header=new DockPanel(); var close=Button("×",CloseTray); System.Windows.Automation.AutomationProperties.SetAutomationId(close,"FreshOS.Tray.Close"); close.ToolTip="Close app tray"; DockPanel.SetDock(close,Dock.Right); header.Children.Add(close); header.Children.Add(Text("App tray",25)); content.Children.Add(header);
        trayCount=Text("",13); content.Children.Add(trayCount);
        trayWindows=new StackPanel(); content.Children.Add(new ScrollViewer { Content=trayWindows,MaxHeight=220,VerticalScrollBarVisibility=ScrollBarVisibility.Auto });
        var links=new WrapPanel(); content.Children.Add(links);
        links.Children.Add(Button("Files",()=> { CloseTray(); ShowFiles(location); }));
        links.Children.Add(Button("Settings",()=> { CloseTray(); Settings(); }));
        links.Children.Add(Button("Sound",()=> { CloseTray(); ShowVolume(); }));
        links.Children.Add(Button("Network",()=> { CloseTray(); Platform.Open("ms-settings:network"); }));
        content.Children.Add(Text("Shows real open app windows. Windows notification-area icons remain on the host desktop; they are not mirrored here.",11));
        trayPanel=Bubble(new ScrollViewer { Content=content,MaxHeight=Math.Max(180,ActualHeight-180),VerticalScrollBarVisibility=ScrollBarVisibility.Auto },360); trayPanel.Background=Aero.Mint(prefs.Dark); trayPanel.SetValue(TextElement.ForegroundProperty,Ink);
        trayPanel.HorizontalAlignment=HorizontalAlignment.Left; trayPanel.VerticalAlignment=VerticalAlignment.Top; trayPanel.Margin=new Thickness(64,100,0,0);
        trayPanel.MaxWidth=Math.Max(260,ActualWidth-90); trayPanel.MaxHeight=Math.Max(300,ActualHeight-120);
        System.Windows.Automation.AutomationProperties.SetAutomationId(trayPanel,"FreshOS.Tray.Panel");
        Panel.SetZIndex(trayPanel,60); workspace.Children.Add(trayPanel); traySignature=""; RefreshTray(); Aero.Enter(trayPanel,MotionReduced);
    }
    void CloseTray()
    {
        if(trayPanel!=null)workspace.Children.Remove(trayPanel);
        trayArrow?.SetOpen(false,MotionReduced); launcherTrayArrow?.SetOpen(false,MotionReduced);
        trayPanel=null; trayWindows=null; trayCount=null; traySignature="";
    }
    void RefreshTray()
    {
        if(trayPanel==null || trayWindows==null || trayCount==null)return;
        var windows=Platform.Windows();
        var signature=string.Join("\n",windows.Select(w=>$"{w.Handle}:{w.Title}"));
        if(signature==traySignature && trayWindows.Children.Count>0)return;
        traySignature=signature; trayWindows.Children.Clear();
        trayCount.Text=windows.Count==0?"No other app windows open":$"{windows.Count} open app window{(windows.Count==1?"":"s")}";
        if(windows.Count==0)trayWindows.Children.Add(Text("Launch an app from All Apps or Files. It will appear here while its window is open.",13));
        foreach(var window in windows)
        {
            var b=Button(window.Title,()=> { CloseTray(); Platform.Activate(window.Handle); });
            b.HorizontalContentAlignment=HorizontalAlignment.Left;
            b.Content=new TextBlock { Text=window.Title,TextTrimming=TextTrimming.CharacterEllipsis,MaxWidth=260 };
            b.ToolTip="Switch to "+window.Title; trayWindows.Children.Add(b);
        }
    }
}
