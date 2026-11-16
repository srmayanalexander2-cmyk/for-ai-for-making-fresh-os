using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FreshOS;
public sealed partial class Desktop
{
    StackPanel ControlBubble(string title)
    {
        var p=ShowPanel(title,326);
        if(panel!=null)
        {
            panel.SetValue(System.Windows.Documents.TextElement.ForegroundProperty,new SolidColorBrush(Color.FromRgb(90,104,25)));
            panel.Resources["FreshInk"]=new SolidColorBrush(Color.FromRgb(90,104,25));
            panel.Resources["FreshButton"]=new SolidColorBrush(Color.FromArgb(170,255,255,225));
            panel.Background=new LinearGradientBrush(new GradientStopCollection {new(Colors.White,0),new(Color.FromRgb(247,255,102),.35),new(Color.FromRgb(226,255,0),.7),new(Color.FromRgb(254,255,194),1)},90);
            panel.HorizontalAlignment=HorizontalAlignment.Center; panel.VerticalAlignment=VerticalAlignment.Bottom; panel.Margin=new Thickness(75,0,0,98);
            // Small, vector-drawn companion; its face is a real shape, never a font-dependent emoji.
            var face=new Canvas { Width=70,Height=70,IsHitTestVisible=false };
            face.Children.Add(new Ellipse { Width=70,Height=70,Fill=new RadialGradientBrush(Colors.LightYellow,Colors.Yellow),Stroke=new SolidColorBrush(Color.FromRgb(177,184,35)),StrokeThickness=.7 });
            foreach(double x in new[]{23d,44d}) { var eye=new Ellipse { Width=6,Height=11,Fill=Brushes.Black }; Canvas.SetLeft(eye,x); Canvas.SetTop(eye,20); face.Children.Add(eye); }
            var smile=new System.Windows.Shapes.Path { Data=Geometry.Parse("M 16,41 C 24,61 48,61 56,41"),Stroke=Brushes.Black,StrokeThickness=2,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round }; face.Children.Add(smile);
            var host=new Grid(); var child=panel.Child; panel.Child=null;
            host.Children.Add(child);
            var companion=new Border { Child=face,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(-28,0,0,-84) }; host.Children.Add(companion);
            host.Children.Add(new Polygon { Points=new PointCollection {new(0,0),new(35,0),new(0,29)},Fill=new SolidColorBrush(Color.FromRgb(246,255,153)),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(20,0,0,-28),IsHitTestVisible=false });
            panel.Child=host;
        }
        p.SetValue(System.Windows.Documents.TextElement.ForegroundProperty,new SolidColorBrush(Color.FromRgb(90,104,25)));
        return p;
    }
    void ShowAeroVolume()
    {
        var p=ControlBubble("Volume");
        AudioEndpoint endpoint;
        try { endpoint=new AudioEndpoint(); }
        catch { p.Children.Add(Text("No default audio device is available.",13)); p.Children.Add(Button("Audio settings",()=>Platform.Open("ms-settings:sound"))); return; }
        var label=Text("",12); label.TextAlignment=TextAlignment.Center;
        var slider=new Slider { Minimum=0,Maximum=100,Value=endpoint.Level,SmallChange=2,LargeChange=10,Margin=new Thickness(8,0,8,0),ToolTip="System volume",IsMoveToPointEnabled=true };
        System.Windows.Automation.AutomationProperties.SetName(slider,"System volume");
        bool syncing=false;
        slider.ValueChanged+=(_,_)=> { if(!syncing)Safe(()=> { endpoint.Level=slider.Value; }); label.Text=$"{slider.Value:0}%  •  Default output"; };
        p.Children.Add(slider); p.Children.Add(label);
        var mute=Button("",()=> { endpoint.Muted=!endpoint.Muted; }); p.Children.Add(mute);
        void Refresh() { try { syncing=true; if(!slider.IsMouseCaptureWithin)slider.Value=endpoint.Level; label.Text=$"{slider.Value:0}%  •  Default output"; mute.Content=endpoint.Muted?"Unmute":"Mute"; } catch { } finally { syncing=false; } }
        var poll=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(400) }; poll.Tick+=(_,_)=>Refresh();
        var owner=panel!; owner.Unloaded+=(_,_)=> { poll.Stop(); endpoint.Dispose(); }; Refresh(); poll.Start();
    }
    void ShowAeroBrightness()
    {
        var p=ControlBubble("Brightness"); var label=Text("",12); label.TextAlignment=TextAlignment.Center;
        var slider=new Slider { Minimum=20,Maximum=100,Value=(1-shade.Opacity)*100,Margin=new Thickness(8),IsMoveToPointEnabled=true,ToolTip="FreshOS interface brightness" };
        System.Windows.Automation.AutomationProperties.SetName(slider,"FreshOS brightness");
        void Update() { shade.Opacity=1-slider.Value/100; label.Text=$"{slider.Value:0}%  •  FreshOS only"; }
        slider.ValueChanged+=(_,_)=>Update(); Update(); p.Children.Add(slider); p.Children.Add(label); p.Children.Add(Text("A soft dimmer for your FreshOS desktop. Monitor brightness and other apps are unchanged.",11));
    }
}
