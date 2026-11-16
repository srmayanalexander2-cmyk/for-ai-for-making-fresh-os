using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FreshOS;
public sealed partial class Desktop
{
    void ShowAeroLauncher()
    {
        ClosePanel(); launcherOpen=true; if(launchButton!=null)launchButton.Content="⬅";
        // A single uniformly-scaled artboard preserves the hand-made composition on small displays.
        var art=new Canvas { Width=740,Height=620 };
        var appsLens=new AeroMedallion { Width=424,Height=424,Dark=prefs.Dark };
        Canvas.SetLeft(appsLens,8); Canvas.SetTop(appsLens,187); art.Children.Add(appsLens);
        var appContents=new StackPanel { Width=300,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,0,4) }; appsLens.Children.Add(appContents);
        var title=Text("All Apps",27); title.TextAlignment=TextAlignment.Center; title.Foreground=Ink; title.FontWeight=FontWeights.SemiBold; title.Margin=new Thickness(0,0,0,3); appContents.Children.Add(title);
        var search=new TextBox { ToolTip="Search your PC’s app shortcuts",Height=34,Padding=new Thickness(10,5),FontSize=12,Margin=new Thickness(16,3,16,4) }; System.Windows.Automation.AutomationProperties.SetName(search,"Search installed applications"); appContents.Children.Add(search);
        var tiles=new UniformGrid { Columns=3,Rows=2,Height=146 }; appContents.Children.Add(tiles);
        var paging=new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center }; appContents.Children.Add(paging);
        int page=0; const int pageSize=6;
        var pageLabel=Text("",11); pageLabel.MinWidth=55; pageLabel.TextAlignment=TextAlignment.Center;
        void Render()
        {
            var filtered=apps.Where(a=>a.Name.Contains(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)).ToList();
            int count=Math.Max(1,(filtered.Count+pageSize-1)/pageSize); page=Math.Clamp(page,0,count-1); tiles.Children.Clear();
            foreach(var app in filtered.Skip(page*pageSize).Take(pageSize))
            {
                var content=new StackPanel();
                var iconImage=new Image { Width=32,Height=32,Margin=new Thickness(0,0,0,4) }; content.Children.Add(iconImage);
                _=LoadIcon(iconImage,app.Path);
                content.Children.Add(new TextBlock { Text=app.Name,FontSize=10,TextTrimming=TextTrimming.CharacterEllipsis,TextAlignment=TextAlignment.Center,MaxWidth=84 });
                var b=Button(app.Name,()=>Launch(app.Path)); b.Content=content; b.Padding=new Thickness(3); b.Margin=new Thickness(2); b.Background=Brushes.Transparent; b.BorderBrush=Brushes.Transparent; b.ToolTip=app.Name; System.Windows.Automation.AutomationProperties.SetName(b,app.Name); tiles.Children.Add(b);
            }
            if(filtered.Count==0)tiles.Children.Add(Text("No apps found",12));
            pageLabel.Text=$"{page+1} / {count}";
        }
        var previous=Button("‹",()=> { page--; Render(); }); previous.Padding=new Thickness(10,0,10,0); previous.MinHeight=25; paging.Children.Add(previous); paging.Children.Add(pageLabel);
        var next=Button("›",()=> { page++; Render(); }); next.Padding=new Thickness(10,0,10,0); next.MinHeight=25; paging.Children.Add(next);
        search.TextChanged+=(_,_)=> { page=0; Render(); }; Render();
        var power=Button("⏻",Power); power.Width=38; power.Height=36; power.Padding=new Thickness(0); power.ToolTip="Power & rest"; power.HorizontalAlignment=HorizontalAlignment.Center; power.VerticalAlignment=VerticalAlignment.Bottom; power.Margin=new Thickness(0,0,0,22); appsLens.Children.Add(power);
        var timeLens=new AeroMedallion { Width=300,Height=300,Dark=prefs.Dark }; Canvas.SetLeft(timeLens,338); Canvas.SetTop(timeLens,10); art.Children.Add(timeLens);
        var timeContent=new StackPanel { VerticalAlignment=VerticalAlignment.Center }; largeClock=Text(DateTime.Now.ToString("h:mm"),58); largeClock.TextAlignment=TextAlignment.Center; largeClock.FontWeight=FontWeights.SemiBold; largeClock.Margin=new Thickness(0); largeDate=Text(DateTime.Now.ToString("M/dd"),28); largeDate.TextAlignment=TextAlignment.Center; largeDate.Margin=new Thickness(0); timeContent.Children.Add(largeClock); timeContent.Children.Add(largeDate); timeLens.Children.Add(timeContent);
        var speech=new Grid { Width=158,Height=128 }; var bubble=new Border { Background=Aero.Mint(prefs.Dark),CornerRadius=new CornerRadius(54),Height=104,VerticalAlignment=VerticalAlignment.Top,BorderBrush=new SolidColorBrush(Color.FromArgb(100,255,255,255)),BorderThickness=new Thickness(1) };
        var trayText=Text(tasks.Children.Count==0?"No apps\nin Tray":$"{tasks.Children.Count} recent\napps",20); trayText.TextAlignment=TextAlignment.Center; trayText.FontWeight=FontWeights.SemiBold; bubble.Child=trayText; speech.Children.Add(bubble); speech.Children.Add(new Polygon { Points=new PointCollection { new(29,94),new(29,125),new(65,99) },Fill=Aero.Mint(prefs.Dark),IsHitTestVisible=false }); Canvas.SetLeft(speech,0); Canvas.SetTop(speech,68); art.Children.Add(speech);
        var shortcuts=new StackPanel { Orientation=Orientation.Horizontal }; shortcuts.Children.Add(Button("▱  Files",()=>ShowFiles(location))); var settingsButton=Button("⚙  Settings",Settings); System.Windows.Automation.AutomationProperties.SetAutomationId(settingsButton,"FreshOS.Settings"); shortcuts.Children.Add(settingsButton); shortcuts.Children.Add(Button("×",ClosePanel)); Canvas.SetLeft(shortcuts,433); Canvas.SetTop(shortcuts,362); art.Children.Add(shortcuts);
        var scale=new Viewbox { Child=art,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly };
        panel=new Border { Child=scale,Width=740,Height=620,Background=Brushes.Transparent,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(65,0,0,0) }; panel.SetValue(TextElement.ForegroundProperty,Ink); workspace.Children.Add(panel); ResizePanel(); Aero.Enter(panel,MotionReduced);
    }
    static async Task LoadIcon(Image target,string path)
    {
        try { target.Source=await Task.Run(()=>Icons.For(path)); }
        catch { /* A broken shortcut must never prevent the launcher from opening. */ }
    }

}
