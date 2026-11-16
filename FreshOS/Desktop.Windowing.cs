using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
namespace FreshOS;
public sealed partial class Desktop
{
    bool fullscreen=true;
    IntPtr shellHandle;
    [StructLayout(LayoutKind.Sequential)] struct NativeRect { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] struct MonitorInfo { public int Size; public NativeRect Monitor,Work; public uint Flags; }
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    void InitializeShellWindow()
    {
        shellHandle=new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(shellHandle)?.AddHook(ShellWindowHook);
        int corner=1; DwmSetWindowAttribute(shellHandle,33,ref corner,4);
        if(selfTest) { fullscreen=false; return; }
        Dispatcher.BeginInvoke(new Action(()=>SetFullscreen(true)));
    }
    IntPtr ShellWindowHook(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled)
    {
        if(message==0x0112)
        {
            long command=wParam.ToInt64() & 0xFFF0;
            // Only this app: no native move/resize/maximize gestures. Alt+F4 (SC_CLOSE) remains untouched.
            if(command is 0xF000 or 0xF010 or 0xF030)handled=true;
        }
        if(fullscreen && message is 0x007E or 0x02E0)Dispatcher.BeginInvoke(new Action(()=>SetFullscreen(true)));
        return IntPtr.Zero;
    }
    MonitorInfo CurrentMonitor()
    {
        var info=new MonitorInfo { Size=Marshal.SizeOf<MonitorInfo>() };
        if(!GetMonitorInfo(MonitorFromWindow(shellHandle,2),ref info))throw new InvalidOperationException("Could not read display bounds.");
        return info;
    }
    void PlaceShell(NativeRect bounds)
    {
        WindowState=WindowState.Normal;
        // Pixel bounds from the native monitor API avoid DPI rounding gaps at the taskbar edge.
        if(!SetWindowPos(shellHandle,IntPtr.Zero,bounds.Left,bounds.Top,bounds.Right-bounds.Left,bounds.Bottom-bounds.Top,0x0034))throw new InvalidOperationException("Could not position FreshOS.");
    }
    void SetFullscreen(bool enabled)
    {
        fullscreen=enabled; if(shellHandle==IntPtr.Zero)return;
        var info=CurrentMonitor();
        if(enabled)PlaceShell(info.Monitor);
        else { var r=info.Work; int w=Math.Min(1280,r.Right-r.Left),h=Math.Min(800,r.Bottom-r.Top); r.Left+=(r.Right-r.Left-w)/2; r.Top+=(r.Bottom-r.Top-h)/2; r.Right=r.Left+w; r.Bottom=r.Top+h; PlaceShell(r); }
    }
    void SnapShell(string position)
    {
        if(position=="Fullscreen") { ClosePanel(); SetFullscreen(true); return; }
        var info=CurrentMonitor(); var r=info.Work; var dpi=System.Windows.Media.VisualTreeHelper.GetDpi(this);
        int half=(r.Right-r.Left)/2;
        if(position is "Left" or "Right")
        {
            if(half/dpi.DpiScaleX<MinWidth) { MessageBox.Show("This display is too narrow for a readable half-width FreshOS window. Use Fullscreen or Center.","FreshOS layout"); return; }
            if(position=="Left")r.Right=r.Left+half; else r.Left=r.Right-half;
            ClosePanel(); fullscreen=false; PlaceShell(r);
        }
        else { ClosePanel(); SetFullscreen(false); }
    }
    void ShowSnapLayouts()
    {
        var p=ShowPanel("FreshOS window layout",560);
        p.Children.Add(Text("Custom positions for the FreshOS shell. Native resize borders and snap/maximize controls are disabled for this window.",15));
        var choices=new System.Windows.Controls.Primitives.UniformGrid { Columns=2 };
        foreach(string choice in new[]{"Fullscreen","Center","Left","Right"})choices.Children.Add(Button(choice,()=>SnapShell(choice)));
        p.Children.Add(choices); p.Children.Add(Text("F11 toggles fullscreen. System taskbar access, Alt+Tab and F12 exit remain available. Other applications keep their normal window controls.",12));
    }
}
