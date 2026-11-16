using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace FreshOS;
internal static class Platform
{
    [DllImport("user32.dll")] static extern bool EnumWindows(Func<IntPtr,IntPtr,bool> callback, IntPtr p);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder text, int size);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint id);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h,int command);
    [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LastInput p);
    [StructLayout(LayoutKind.Sequential)] struct LastInput { public uint size; public uint time; }
    public static double IdleSeconds { get { var p = new LastInput { size=8 }; return GetLastInputInfo(ref p) ? unchecked((uint)Environment.TickCount-p.time)/1000d : 0; } }
    public static List<(IntPtr Handle,string Title)> Windows() { var items=new List<(IntPtr,string)>(); EnumWindows((h,_)=> { GetWindowThreadProcessId(h,out var id); if(IsWindowVisible(h) && id!=Environment.ProcessId) { var s=new StringBuilder(512); GetWindowText(h,s,512); if(s.Length>0) items.Add((h,s.ToString())); } return true; },IntPtr.Zero); return items; }
    public static void Activate(IntPtr h) { ShowWindow(h,9); SetForegroundWindow(h); }
    public static void Open(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute=true });
    public static List<(string Name,string Path)> Apps() {
        var result=new List<(string,string)>();
        foreach(var root in new[]{Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)}) {
            try { foreach(var f in Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories).Where(f=>f.EndsWith(".lnk",StringComparison.OrdinalIgnoreCase)||f.EndsWith(".url",StringComparison.OrdinalIgnoreCase))) result.Add((System.IO.Path.GetFileNameWithoutExtension(f),f)); } catch { }
        }
        return result.DistinctBy(x=>x.Item1).OrderBy(x=>x.Item1).ToList();
    }
    // Send standard volume keys: respects the user's current default audio endpoint.
    [DllImport("user32.dll")] static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
    public static void Volume(byte key) { keybd_event(key,0,0,UIntPtr.Zero); keybd_event(key,0,2,UIntPtr.Zero); }
}
