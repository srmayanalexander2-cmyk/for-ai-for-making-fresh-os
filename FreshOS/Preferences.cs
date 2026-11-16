using System.IO;
using System.Text.Json;
namespace FreshOS;
public class Preferences
{
    public bool Dark { get; set; }
    public bool ReducedMotion { get; set; }
    public bool ShowDock { get; set; }
    public bool MaterialTexture { get; set; } = true;
    public bool AutoLock { get; set; }
    public bool Screensaver { get; set; } = true;
    public int LockMinutes { get; set; } = 10;
    public string Wallpaper { get; set; } = "";
    public string BootSound { get; set; } = "";
    public bool SetupDone { get; set; }
    public static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FreshOS");
    public static Preferences Load()
    {
        try
        {
            var value=JsonSerializer.Deserialize<Preferences>(File.ReadAllText(Path.Combine(Folder,"settings.json")))??new();
            value.LockMinutes=Math.Clamp(value.LockMinutes,1,60);
            value.Wallpaper??=""; value.BootSound??="";
            return value;
        }
        catch { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(Folder);
        var temporary=Path.Combine(Folder,"settings."+Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            File.WriteAllText(temporary,JsonSerializer.Serialize(this));
            File.Move(temporary,Path.Combine(Folder,"settings.json"),true);
        }
        finally { if(File.Exists(temporary))File.Delete(temporary); }
    }
}
