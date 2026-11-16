using System.Runtime.InteropServices;
namespace FreshOS;

/// <summary>Default multimedia render endpoint. COM ownership is scoped to the open volume panel.</summary>
internal sealed class AudioEndpoint : IDisposable
{
    [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class Enumerator { }
    [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IEnumerator
    {
        void EnumAudioEndpoints(int flow,uint state,out IntPtr devices);
        void GetDefaultAudioEndpoint(int flow,int role,out IDevice device);
    }
    [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IDevice
    {
        void Activate(ref Guid iid,uint context,IntPtr parameters,[MarshalAs(UnmanagedType.IUnknown)] out object instance);
    }
    [ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IVolume
    {
        void RegisterControlChangeNotify(IntPtr notify);
        void UnregisterControlChangeNotify(IntPtr notify);
        void GetChannelCount(out uint count);
        void SetMasterVolumeLevel(float level,ref Guid context);
        void SetMasterVolumeLevelScalar(float level,ref Guid context);
        void GetMasterVolumeLevel(out float level);
        void GetMasterVolumeLevelScalar(out float level);
        void SetChannelVolumeLevel(uint channel,float level,ref Guid context);
        void SetChannelVolumeLevelScalar(uint channel,float level,ref Guid context);
        void GetChannelVolumeLevel(uint channel,out float level);
        void GetChannelVolumeLevelScalar(uint channel,out float level);
        void SetMute([MarshalAs(UnmanagedType.Bool)] bool muted,ref Guid context);
        void GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);
    }
    readonly IVolume volume;
    public AudioEndpoint()
    {
        var enumerator=(IEnumerator)new Enumerator(); IDevice? device=null;
        try { enumerator.GetDefaultAudioEndpoint(0,1,out device); var iid=typeof(IVolume).GUID; device.Activate(ref iid,23,IntPtr.Zero,out var endpoint); volume=(IVolume)endpoint; }
        finally { if(device!=null)Marshal.ReleaseComObject(device); Marshal.ReleaseComObject(enumerator); }
    }
    public double Level { get { volume.GetMasterVolumeLevelScalar(out var value); return value*100; } set { var context=Guid.Empty; volume.SetMasterVolumeLevelScalar((float)Math.Clamp(value/100,0,1),ref context); } }
    public bool Muted { get { volume.GetMute(out var value); return value; } set { var context=Guid.Empty; volume.SetMute(value,ref context); } }
    public void Dispose() => Marshal.ReleaseComObject(volume);
}
