using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FreshOS;

/// <summary>Resolution-independent material artwork; no screenshot-sized UI textures.</summary>
public sealed class AeroMedallion : Grid
{
    public bool Scalloped { get; init; } = true;
    public bool Dark { get; init; }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double w=ActualWidth, h=ActualHeight;
        if(w<=0 || h<=0)return;
        var bounds=new Rect(1,1,w-2,h-2);
        Geometry outline;
        if(Scalloped)
        {
            var shape=new StreamGeometry();
            using(var ctx=shape.Open())
            {
                for(int i=0;i<96;i++)
                {
                    double a=i*Math.PI/48-Math.PI/2, r=i%2==0?.5:.474;
                    var pt=new Point(w/2+Math.Cos(a)*(w-2)*r,h/2+Math.Sin(a)*(h-2)*r);
                    if(i==0)ctx.BeginFigure(pt,true,true); else ctx.LineTo(pt,true,false);
                }
            }
            shape.Freeze(); outline=shape;
        }
        else outline=new EllipseGeometry(bounds);
        var material=new RadialGradientBrush
        {
            GradientOrigin=new Point(.48,.48),Center=new Point(.5,.5),RadiusX=.61,RadiusY=.61,
            GradientStops=new GradientStopCollection {
                new(Dark?Color.FromRgb(32,109,131):Color.FromRgb(231,255,254),0),
                new(Dark?Color.FromRgb(18,143,167):Color.FromRgb(49,218,242),.5),
                new(Dark?Color.FromRgb(5,82,111):Color.FromRgb(0,202,236),.74),
                new(Dark?Color.FromRgb(38,162,182):Color.FromRgb(102,237,246),1) }
        };
        dc.DrawGeometry(material,new Pen(new SolidColorBrush(Color.FromArgb(100,187,255,255)),.7),outline);
        dc.PushClip(outline);
        // An inset lens, barely-visible light falloff and a top rim catch.
        dc.DrawEllipse(null,new Pen(new SolidColorBrush(Color.FromArgb(12,0,123,166)),2),new Point(w*.5,h*.5),w*.353,h*.353);
        var sheen=new LinearGradientBrush(new GradientStopCollection {new(Color.FromArgb(85,255,255,255),0),new(Colors.Transparent,.46),new(Color.FromArgb(12,255,255,255),1)},90);
        dc.DrawRectangle(sheen,null,new Rect(0,0,w,h));
        dc.DrawEllipse(null,new Pen(new SolidColorBrush(Color.FromArgb(33,255,255,255)),1),new Point(w*.5,h*.49),w*.352,h*.352);
        dc.Pop();
    }
}

internal static class Aero
{
    public static Brush Tube(bool dark=false) => new LinearGradientBrush(new GradientStopCollection {
        new(dark?Color.FromRgb(70,145,161):Color.FromRgb(227,255,255),0),
        new(dark?Color.FromRgb(6,82,107):Color.FromRgb(0,201,237),.46),
        new(dark?Color.FromRgb(26,109,129):Color.FromRgb(99,230,245),.8),
        new(dark?Color.FromRgb(131,204,210):Colors.White,1)},0);
    public static Brush Mint(bool dark=false) => new LinearGradientBrush(new GradientStopCollection {
        new(dark?Color.FromRgb(77,123,132):Colors.White,0),
        new(dark?Color.FromRgb(21,124,127):Color.FromRgb(126,255,220),.22),
        new(dark?Color.FromRgb(0,85,107):Color.FromRgb(0,240,183),.65),
        new(dark?Color.FromRgb(34,137,144):Color.FromRgb(205,255,241),1)},90);
    static readonly Brush grain=CreateGrain();
    public static Brush Grain() => grain;
    static Brush CreateGrain()
    {
        var group=new DrawingGroup(); var random=new Random(27);
        for(int i=0;i<95;i++)group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromArgb((byte)random.Next(8,25),255,255,255)),null,new RectangleGeometry(new Rect(random.Next(64),random.Next(64),.6,.6))));
        group.Freeze(); var brush=new DrawingBrush(group) { TileMode=TileMode.Tile,ViewportUnits=BrushMappingMode.Absolute,Viewport=new Rect(0,0,64,64),Stretch=Stretch.None }; brush.Freeze(); return brush;
    }
    public static void Enter(FrameworkElement element,bool reduced)
    {
        if(reduced)return;
        element.RenderTransformOrigin=new Point(.5,.5);
        var scale=new ScaleTransform(1,1); var translate=new TranslateTransform();
        var transforms=new TransformGroup(); transforms.Children.Add(scale); transforms.Children.Add(translate); element.RenderTransform=transforms;
        var ease=new CubicEase { EasingMode=EasingMode.EaseOut };
        var duration=TimeSpan.FromMilliseconds(280);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(.965,1,duration) { EasingFunction=ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(.965,1,duration) { EasingFunction=ease });
        translate.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(12,0,duration) { EasingFunction=ease });
        element.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(180)));
    }
}
