using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
namespace FreshOS;
/// <summary>A vector arrow with an explicit open state, independent from its button's label.</summary>
public sealed class FlipArrow : Grid
{
    readonly RotateTransform rotation=new();
    public bool IsOpen { get; private set; }
    public double Angle => rotation.Angle;
    public FlipArrow(double size=32)
    {
        Width=size; Height=size; IsHitTestVisible=false; RenderTransformOrigin=new Point(.5,.5); RenderTransform=rotation;
        Children.Add(new Viewbox { Child=new System.Windows.Shapes.Path { Data=Geometry.Parse("M 1,10 L 22,10 L 22,2 L 39,19 L 22,36 L 22,28 L 1,28 Z"),Fill=new LinearGradientBrush(new GradientStopCollection {new(Colors.White,0),new(Color.FromRgb(111,255,170),.35),new(Color.FromRgb(0,234,100),.65),new(Color.FromRgb(204,255,211),1)},90),Stroke=new SolidColorBrush(Color.FromArgb(150,225,255,237)),StrokeThickness=.6 },Stretch=Stretch.Uniform });
    }
    public void SetOpen(bool open,bool reduced)
    {
        IsOpen=open; double target=open?180:0;
        if(reduced) { rotation.BeginAnimation(RotateTransform.AngleProperty,null); rotation.Angle=target; return; }
        rotation.BeginAnimation(RotateTransform.AngleProperty,new DoubleAnimation(rotation.Angle,target,TimeSpan.FromMilliseconds(240)) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseInOut } });
    }
}
