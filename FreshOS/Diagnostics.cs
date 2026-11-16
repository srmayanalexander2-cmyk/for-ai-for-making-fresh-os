using System.IO;
namespace FreshOS;
internal static class Diagnostics
{
    static readonly object gate=new();
    public static string Folder => Path.Combine(Preferences.Folder,"Logs");
    public static void Write(string message,Exception? exception=null)
    {
        try
        {
            lock(gate)
            {
                Directory.CreateDirectory(Folder); var file=Path.Combine(Folder,"freshos.log");
                if(File.Exists(file) && new FileInfo(file).Length>1_000_000)File.Move(file,Path.Combine(Folder,"freshos.previous.log"),true);
                File.AppendAllText(file,$"{DateTimeOffset.Now:O} {message}\n{exception}\n");
            }
        }
        catch { /* Logging must never prevent recovery or exit. */ }
    }
}
