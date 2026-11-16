using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
namespace FreshOS;
public sealed partial class Desktop
{
    void AeroWelcome()
    {
        var body=ShowPanel("A breath of fresh air",650);
        var scene=new Grid { Height=300,Margin=new Thickness(0,0,0,14) };
        var lens=new Border { Width=240,CornerRadius=new CornerRadius(110),Background=Aero.Tube(prefs.Dark),Opacity=.72,HorizontalAlignment=HorizontalAlignment.Center }; scene.Children.Add(lens);
        var titles=new StackPanel { VerticalAlignment=VerticalAlignment.Center };
        var welcome=Text("Welcome!",70); welcome.Foreground=new LinearGradientBrush(Color.FromRgb(255,255,186),Color.FromRgb(208,255,58),90); welcome.TextAlignment=TextAlignment.Center; welcome.FontWeight=FontWeights.SemiBold; welcome.Margin=new Thickness(0); titles.Children.Add(welcome);
        var reflection=Text("Welcome!",70); reflection.TextAlignment=TextAlignment.Center; reflection.Margin=new Thickness(0,-8,0,0); reflection.FontWeight=FontWeights.SemiBold; reflection.LayoutTransform=new ScaleTransform(1,-.72); reflection.OpacityMask=new LinearGradientBrush(Colors.Transparent,Color.FromArgb(120,255,255,255),90); titles.Children.Add(reflection); scene.Children.Add(titles);
        // A tiny leaf stem, drawn as curves rather than relying on emoji rendering.
        var leaf=new System.Windows.Shapes.Path { Data=Geometry.Parse("M 16,70 C -12,31 1,3 8,0 C 44,13 52,44 16,70 Z M 16,70 L 11,15 M 16,70 L 18,94"),Fill=new LinearGradientBrush(Colors.YellowGreen,Colors.ForestGreen,45),Stroke=new SolidColorBrush(Color.FromRgb(78,147,59)),StrokeThickness=1,Width=50,Height=95,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(42,20,0,0) }; scene.Children.Add(leaf);
        body.Children.Add(scene);
        var sub=Text("Your familiar world. A fresher perspective.",22); sub.TextAlignment=TextAlignment.Center; body.Children.Add(sub);
        var start=Button("Use FreshOS     ➜",()=> { prefs.SetupDone=true; prefs.Save(); ClosePanel(); PlayBoot(); }); System.Windows.Automation.AutomationProperties.SetAutomationId(start,"FreshOS.Welcome.Start"); start.HorizontalAlignment=HorizontalAlignment.Center; start.Padding=new Thickness(28,14,28,14); start.FontSize=22; start.Background=Aero.Mint(prefs.Dark); body.Children.Add(start);
        body.Children.Add(Text("Real apps. Real files. Nothing replaced.\nF11 • fullscreen    Ctrl+Tab • windows    F12 • leave anytime",12));
    }
}
