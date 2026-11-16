using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace FreshOS;
public sealed partial class Desktop
{
    /// <summary>Windows-only in-process interaction/layout checks, not a hardware or accessibility certification.</summary>
    public static async Task RunInteractionTests(string directory)
    {
        Directory.CreateDirectory(directory);
        var checks=new List<string>(); var desktop=new Desktop(true) { WindowState=WindowState.Normal,Width=1280,Height=800,Left=0,Top=0 };
        Process? fixture=null;
        var temporary=Path.Combine(Path.GetTempPath(),"FreshOS-tests-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary);
        try
        {
            desktop.Show(); await desktop.Dispatcher.InvokeAsync(()=>desktop.UpdateLayout(),DispatcherPriority.ApplicationIdle);
            Button FindButton(string id) => Descendants(desktop).OfType<Button>().FirstOrDefault(b=>AutomationProperties.GetAutomationId(b)==id)??throw new Exception("Missing button: "+id);
            void Check(bool condition,string name) { if(!condition)throw new Exception("FAILED: "+name); checks.Add(name); }
            void Click(Button button) { button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); desktop.UpdateLayout(); }
            void CheckText(TextBlock text,string label)
            {
                desktop.UpdateLayout();
                var measured=new FormattedText(text.Text,System.Globalization.CultureInfo.CurrentCulture,text.FlowDirection,new Typeface(text.FontFamily,text.FontStyle,text.FontWeight,text.FontStretch),text.FontSize,text.Foreground,VisualTreeHelper.GetDpi(text).PixelsPerDip);
                if(text.TextWrapping==TextWrapping.Wrap)measured.MaxTextWidth=Math.Max(1,text.ActualWidth);
                Check(text.ActualWidth>0 && text.ActualHeight>0,label+" is visible");
                Check(measured.WidthIncludingTrailingWhitespace<=text.ActualWidth+2 && measured.Height<=text.ActualHeight+2,label+" text fits its measured box");
            }
            void HitRail(string id)
            {
                var button=FindButton(id); desktop.UpdateLayout();
                Check(button.ActualWidth>0 && button.ActualHeight>0,id+" has a visible hit target");
                var point=button.TranslatePoint(new Point(button.ActualWidth/2,button.ActualHeight/2),desktop);
                var hit=desktop.InputHitTest(point) as DependencyObject;
                bool found=false;
                while(hit!=null) { if(hit==button) { found=true; break; } hit=VisualTreeHelper.GetParent(hit); }
                Check(found,id+" is not covered by launcher or another element");
            }
            foreach(var size in new[]{(1280d,800d),(800d,600d)})
            {
                desktop.Width=size.Item1; desktop.Height=size.Item2;
                await desktop.Dispatcher.InvokeAsync(()=>desktop.UpdateLayout(),DispatcherPriority.ApplicationIdle);
                foreach(string id in new[]{"Tray","Volume","Brightness","Switcher"})HitRail("FreshOS.Rail."+id);
                CheckText(desktop.clock,"rail clock/date"); CheckText(desktop.desktopClock!,"desktop clock/date");
                Click(FindButton("FreshOS.Rail.Tray")); Check(desktop.trayPanel!=null && desktop.panel==null,"tray opens from desktop without Start");
                Check(desktop.trayArrow!.IsOpen && desktop.trayArrow.Angle==180,"tray arrow points to open state");
                desktop.ShowApps(); desktop.UpdateLayout(); Check(desktop.trayPanel!=null && desktop.launcherOpen,"tray and Start have independent state");
                CheckText(desktop.largeClock!,"launcher time"); CheckText(desktop.largeDate!,"launcher date");
                Check(desktop.startArrow!.IsOpen && desktop.startArrow.Angle==180,"Start arrow points to open state");
                var clockBounds=desktop.largeClock!.TransformToAncestor(desktop.panel!).TransformBounds(new Rect(desktop.largeClock.RenderSize));
                var dateBounds=desktop.largeDate!.TransformToAncestor(desktop.panel!).TransformBounds(new Rect(desktop.largeDate.RenderSize));
                Check(!clockBounds.IntersectsWith(dateBounds),"launcher date and time do not overlap");
                desktop.ClosePanel(); Check(desktop.trayPanel!=null,"closing Start does not destroy tray");
                Check(!desktop.startArrow!.IsOpen && desktop.startArrow.Angle==0,"Start arrow returns to closed state");
                Click(FindButton("FreshOS.Rail.Tray")); Check(desktop.trayPanel==null,"rail toggles tray closed");
                Check(!desktop.trayArrow!.IsOpen && desktop.trayArrow.Angle==0,"tray arrow returns to closed state");
                Click(FindButton("FreshOS.Rail.Brightness"));
                var brightness=Descendants(desktop.panel!).OfType<Slider>().Single(); brightness.Value=45;
                Check(Math.Abs(desktop.shade.Opacity-.55)<.001,"brightness slider changes shell dimmer");
                desktop.shade.Opacity=0; desktop.ClosePanel();
                Click(FindButton("FreshOS.Rail.Volume")); Check(desktop.panel!=null,"volume opens directly, with device or explicit fallback"); desktop.ClosePanel();
                desktop.prefs.Dark=!desktop.prefs.Dark; desktop.BuildDesktop();
                Click(FindButton("FreshOS.Rail.Tray")); Check(desktop.trayPanel!=null,"tray survives theme rebuild"); desktop.CloseTray();
            }
            desktop.Width=1280; desktop.Height=800; desktop.UpdateLayout();
            // Real animation timing, separate from the deterministic reduced-motion layout cases.
            var animated=new FlipArrow(); animated.SetOpen(true,false); await Task.Delay(350);
            Check(animated.IsOpen && Math.Abs(animated.Angle-180)<.5,"arrow animation reaches open angle");
            animated.SetOpen(false,false); await Task.Delay(350);
            Check(!animated.IsOpen && Math.Abs(animated.Angle)<.5,"arrow animation reaches closed angle");
            Check(desktop.Icon!=null,"main window has the FreshOS icon");
            Check(desktop.ResizeMode==ResizeMode.NoResize && desktop.WindowStyle==WindowStyle.None,"shell has no native resize or snap chrome");
            desktop.SetFullscreen(true); await desktop.Dispatcher.InvokeAsync(()=>desktop.UpdateLayout(),DispatcherPriority.ApplicationIdle);
            var monitor=desktop.CurrentMonitor(); GetWindowRect(desktop.shellHandle,out var fullscreenBounds);
            Check(fullscreenBounds.Left==monitor.Monitor.Left && fullscreenBounds.Top==monitor.Monitor.Top && fullscreenBounds.Right==monitor.Monitor.Right && fullscreenBounds.Bottom==monitor.Monitor.Bottom,"fullscreen covers native monitor bounds, not taskbar work area");
            desktop.SetFullscreen(false); await desktop.Dispatcher.InvokeAsync(()=>desktop.UpdateLayout(),DispatcherPriority.ApplicationIdle);
            Check(!desktop.fullscreen,"custom centered layout exits fullscreen without enabling native resize chrome");
            File.WriteAllText(Path.Combine(temporary,"sample.txt"),"FreshOS file-browser fixture");
            desktop.ShowFiles(temporary); desktop.UpdateLayout();
            for(int i=0;i<60 && Descendants(desktop.panel!).OfType<ListBox>().Single().Items.Count==0;i++)await Task.Delay(50);
            var list=Descendants(desktop.panel!).OfType<ListBox>().Single();
            Check(list.Items.OfType<Entry>().Any(e=>Path.GetFileName(e.Path)=="sample.txt"),"file browser asynchronously displays real filesystem entries");
            desktop.ClosePanel();
            fixture=Process.Start(new ProcessStartInfo(Environment.ProcessPath!,"--window-fixture") { UseShellExecute=true })??throw new Exception("Cannot start fixture");
            for(int i=0;i<60 && !Platform.Windows().Any(w=>w.Title=="FreshOS interaction fixture");i++)await Task.Delay(100);
            desktop.ShowTray(); desktop.UpdateLayout(); desktop.RefreshTray();
            Check(Descendants(desktop.trayPanel!).OfType<Button>().Any(b=>AutomationProperties.GetName(b)=="FreshOS interaction fixture"),"tray lists a real window belonging to another process");
            SaveImage(desktop,directory,"tray.png");
            fixture.CloseMainWindow(); await fixture.WaitForExitAsync(); desktop.RefreshTray();
            Check(!Descendants(desktop.trayPanel!).OfType<Button>().Any(b=>AutomationProperties.GetName(b)=="FreshOS interaction fixture"),"tray removes a closed external window");
            desktop.CloseTray(); desktop.ShowApps(); desktop.UpdateLayout(); SaveImage(desktop,directory,"launcher.png"); desktop.ClosePanel();
            desktop.Lock(false); Check(desktop.lockView!=null && Panel.GetZIndex(desktop.lockView)>50,"privacy screen covers rail"); desktop.Unlock();
            desktop.Settings(); desktop.UpdateLayout(); Check(Descendants(desktop.panel!).OfType<CheckBox>().Any(b=>AutomationProperties.GetAutomationId(b)=="FreshOS.Theme"),"settings builds a usable theme control"); desktop.ClosePanel();
            File.WriteAllText(Path.Combine(directory,"interaction-tests.json"),JsonSerializer.Serialize(new { passed=checks.Count,checks },new JsonSerializerOptions { WriteIndented=true }));
        }
        catch(Exception error)
        {
            throw new Exception("Interaction test failed after: "+(checks.LastOrDefault()??"initialization"),error);
        }
        finally
        {
            if(fixture!=null) { if(!fixture.HasExited) { fixture.CloseMainWindow(); if(!fixture.WaitForExit(3000))fixture.Kill(); } fixture.Dispose(); }
            desktop.CloseTray(); desktop.ClosePanel(); desktop.Close(); Directory.Delete(temporary,true);
        }
    }
    static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Descendants(VisualTreeHelper.GetChild(root,i)))yield return child;
    }
    static void SaveImage(Desktop desktop,string directory,string name)
    {
        desktop.UpdateLayout();
        var bitmap=new RenderTargetBitmap((int)desktop.ActualWidth,(int)desktop.ActualHeight,96,96,PixelFormats.Pbgra32); bitmap.Render(desktop.root);
        var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var file=File.Create(Path.Combine(directory,name)); encoder.Save(file);
    }
}
