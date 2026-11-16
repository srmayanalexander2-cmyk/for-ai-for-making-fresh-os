using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace FreshOS;
internal static class Icons
{
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct ShellInfo
    {
        public IntPtr Icon; public int IconIndex; public uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)] public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=80)] public string TypeName;
    }
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SHGetFileInfo(string path,uint attributes,out ShellInfo info,uint size,uint flags);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
    static readonly System.Collections.Concurrent.ConcurrentDictionary<string,Lazy<ImageSource?>> cache=new(StringComparer.OrdinalIgnoreCase);
    public static ImageSource? For(string path)
    {
        return cache.GetOrAdd(path,p=>new Lazy<ImageSource?>(()=>Load(p))).Value;
    }
    static ImageSource? Load(string path)
    {
        ImageSource? source=null;
        // Never retain native HICON handles after the WPF bitmap has been copied.
        if(SHGetFileInfo(path,0,out var info,(uint)Marshal.SizeOf<ShellInfo>(),0x100)!=IntPtr.Zero && info.Icon!=IntPtr.Zero)
        {
            try { source=Imaging.CreateBitmapSourceFromHIcon(info.Icon,Int32Rect.Empty,BitmapSizeOptions.FromWidthAndHeight(40,40)); source.Freeze(); }
            finally { DestroyIcon(info.Icon); }
        }
        return source;
    }
}
