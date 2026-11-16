using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
namespace FreshOS;
public sealed partial class Desktop
{
    void ShowAeroLauncher()
    {
        ClosePanel(); launcherOpen=true; startArrow?.SetOpen(true,MotionReduced);
        var layout=new Grid { Margin=new Thickness(4) };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(400) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(280) });
        var appsLens=new AeroMedallion { Width=396,Height=396,Dark=prefs.Dark,VerticalAlignment=VerticalAlignment.Center };
        layout.Children.Add(appsLens);
        // Everything sits in a measured inner rectangle, not overlaid Canvas coordinates.
        var appContents=new Grid { Width=270,Height=270,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center };
        foreach(var height in new[]{36d,42,148,38})appContents.RowDefinitions.Add(new RowDefinition { Height=new GridLength(height) });
        appsLens.Children.Add(appContents);
        var title=Text("All Apps",26); title.TextWrapping=TextWrapping.NoWrap; title.TextAlignment=TextAlignment.Center; title.FontWeight=FontWeights.SemiBold; title.Margin=new Thickness(0); appContents.Children.Add(title);
        var search=new TextBox { ToolTip="Search installed apps",MinHeight=34,Padding=new Thickness(10,5,10,5),FontSize=13,Margin=new Thickness(4) }; System.Windows.Automation.AutomationProperties.SetName(search,"Search installed applications"); Grid.SetRow(search,1); appContents.Children.Add(search);
        var tiles=new UniformGrid { Columns=3,Rows=2 }; Grid.SetRow(tiles,2); appContents.Children.Add(tiles);
        var paging=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center }; Grid.SetRow(paging,3); appContents.Children.Add(paging);
        int page=0; const int pageSize=6; var pageLabel=Text("",12); pageLabel.MinWidth=48; pageLabel.TextWrapping=TextWrapping.NoWrap; pageLabel.TextAlignment=TextAlignment.Center;
        void Render()
        {
            var filtered=apps.Where(a=>a.Name.Contains(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)).ToList(); int count=Math.Max(1,(filtered.Count+pageSize-1)/pageSize); page=Math.Clamp(page,0,count-1); tiles.Children.Clear();
            foreach(var app in filtered.Skip(page*pageSize).Take(pageSize))
            {
                var content=new StackPanel(); var icon=new Image { Width=30,Height=30,Margin=new Thickness(0,0,0,3) }; content.Children.Add(icon); _=LoadIcon(icon,app.Path);
                content.Children.Add(new TextBlock { Text=app.Name,FontSize=11,TextWrapping=TextWrapping.NoWrap,TextTrimming=TextTrimming.CharacterEllipsis,TextAlignment=TextAlignment.Center,Width=76 });
                var b=Button(app.Name,()=>Launch(app.Path)); b.Content=content; b.Padding=new Thickness(2); b.Margin=new Thickness(1); b.Background=Brushes.Transparent; b.BorderBrush=Brushes.Transparent; b.ToolTip=app.Name; tiles.Children.Add(b);
            }
            if(filtered.Count==0) { var empty=Text("No matches",12); empty.TextWrapping=TextWrapping.Wrap; tiles.Children.Add(empty); }
            pageLabel.Text=$"{page+1} / {count}";
        }
        var previous=Button("‹",()=> { page=Math.Max(0,page-1); Render(); }); previous.Padding=new Thickness(10,0,10,0); previous.MinHeight=28; paging.Children.Add(previous); paging.Children.Add(pageLabel);
        var next=Button("›",()=> { page++; Render(); }); next.Padding=new Thickness(10,0,10,0); next.MinHeight=28; paging.Children.Add(next); search.TextChanged+=(_,_)=> { page=0; Render(); }; Render();
        var right=new StackPanel { VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,0,0) }; Grid.SetColumn(right,1); layout.Children.Add(right);
        var timeLens=new AeroMedallion { Width=250,Height=250,Dark=prefs.Dark,HorizontalAlignment=HorizontalAlignment.Center }; right.Children.Add(timeLens);
        var timeContent=new StackPanel { VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center,Width=180 };
        largeClock=Text(DateTime.Now.ToString("h:mm"),46); largeClock.TextWrapping=TextWrapping.NoWrap; largeClock.TextAlignment=TextAlignment.Center; largeClock.FontWeight=FontWeights.SemiBold; largeClock.Margin=new Thickness(0);
        largeDate=Text(DateTime.Now.ToString("MMM d"),23); largeDate.TextWrapping=TextWrapping.NoWrap; largeDate.TextAlignment=TextAlignment.Center; largeDate.Margin=new Thickness(0,3,0,0);
        System.Windows.Automation.AutomationProperties.SetAutomationId(largeClock,"FreshOS.Launcher.Clock"); System.Windows.Automation.AutomationProperties.SetAutomationId(largeDate,"FreshOS.Launcher.Date");
        timeContent.Children.Add(largeClock); timeContent.Children.Add(largeDate); timeLens.Children.Add(timeContent);
        var trayShortcut=Button("App tray",ShowTray); trayShortcut.Background=Aero.Mint(prefs.Dark); var trayContents=new StackPanel { Orientation=Orientation.Horizontal }; launcherTrayArrow=new FlipArrow(24); launcherTrayArrow.SetOpen(trayPanel!=null,true); trayContents.Children.Add(launcherTrayArrow); trayContents.Children.Add(Text("App tray",16)); trayShortcut.Content=trayContents; System.Windows.Automation.AutomationProperties.SetAutomationId(trayShortcut,"FreshOS.Launcher.Tray"); right.Children.Add(trayShortcut);
        var shortcuts=new UniformGrid { Columns=2 }; shortcuts.Children.Add(Button("Files",()=>ShowFiles(location))); var settings=Button("Settings",Settings); System.Windows.Automation.AutomationProperties.SetAutomationId(settings,"FreshOS.Settings"); shortcuts.Children.Add(settings); shortcuts.Children.Add(Button("Power",Power)); shortcuts.Children.Add(Button("Close",ClosePanel)); right.Children.Add(shortcuts);
        // Only shrink modestly on the supported minimum size; no tiny full-artboard scaling.
        var scale=new Viewbox { Child=layout,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly };
        panel=new Border { Child=scale,Width=700,Height=480,Background=Brushes.Transparent,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(78,24,0,0) }; panel.SetValue(TextElement.ForegroundProperty,Ink); workspace.Children.Add(panel); ResizePanel(); Aero.Enter(panel,MotionReduced);
    }
    static async Task LoadIcon(Image target,string path) { try { target.Source=await Task.Run(()=>Icons.For(path)); } catch { } }
}
