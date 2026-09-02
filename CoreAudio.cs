using System.Runtime.InteropServices;

namespace WindowsMicAutoMute;

public sealed record AudioCaptureDevice(string Id, string FriendlyName, int State);

internal enum DataFlow
{
    Render = 0,
    Capture = 1,
    All = 2
}

[Flags]
internal enum DeviceState : uint
{
    Active = 0x00000001,
    Disabled = 0x00000002,
    NotPresent = 0x00000004,
    Unplugged = 0x00000008,
    All = 0x0000000F
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropertyKey
{
    public Guid FormatId;
    public uint PropertyId;

    public PropertyKey(Guid formatId, uint propertyId)
    {
        FormatId = formatId;
        PropertyId = propertyId;
    }
}

[StructLayout(LayoutKind.Explicit)]
internal struct PropVariant
{
    [FieldOffset(0)] public ushort VariantType;
    [FieldOffset(8)] public IntPtr PointerValue;
}

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
[ClassInterface(ClassInterfaceType.None)]
internal class MMDeviceEnumeratorComObject
{
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    int EnumAudioEndpoints(DataFlow dataFlow, DeviceState stateMask, out IMMDeviceCollection devices);
    int GetDefaultAudioEndpoint(DataFlow dataFlow, int role, out IMMDevice device);
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
    int RegisterEndpointNotificationCallback(IntPtr client);
    int UnregisterEndpointNotificationCallback(IntPtr client);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection
{
    int GetCount(out uint deviceCount);
    int Item(uint index, out IMMDevice device);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    int Activate(ref Guid interfaceId, uint clsCtx, IntPtr activationParams,
        [MarshalAs(UnmanagedType.Interface)] out object interfacePointer);
    int OpenPropertyStore(int access, out IPropertyStore properties);
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    int GetState(out DeviceState state);
}

[ComImport]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyStore
{
    int GetCount(out uint propertyCount);
    int GetAt(uint index, out PropertyKey key);
    int GetValue(ref PropertyKey key, out PropVariant value);
    int SetValue(ref PropertyKey key, ref PropVariant value);
    int Commit();
}

[ComImport]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioEndpointVolume
{
    int RegisterControlChangeNotify(IntPtr notify);
    int UnregisterControlChangeNotify(IntPtr notify);
    int GetChannelCount(out uint channelCount);
    int SetMasterVolumeLevel(float level, ref Guid eventContext);
    int SetMasterVolumeLevelScalar(float level, ref Guid eventContext);
    int GetMasterVolumeLevel(out float level);
    int GetMasterVolumeLevelScalar(out float level);
    int SetChannelVolumeLevel(uint channelNumber, float level, ref Guid eventContext);
    int SetChannelVolumeLevelScalar(uint channelNumber, float level, ref Guid eventContext);
    int GetChannelVolumeLevel(uint channelNumber, out float level);
    int GetChannelVolumeLevelScalar(uint channelNumber, out float level);
    int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid eventContext);
    int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    int GetVolumeStepInfo(out uint step, out uint stepCount);
    int VolumeStepUp(ref Guid eventContext);
    int VolumeStepDown(ref Guid eventContext);
    int QueryHardwareSupport(out uint hardwareSupportMask);
    int GetVolumeRange(out float minDb, out float maxDb, out float incrementDb);
}

internal static class CoreAudio
{
    private const uint ClsCtxInprocServer = 0x1;
    private const int StgmRead = 0;
    private const ushort VtLpWStr = 31;
    private static readonly Guid AudioEndpointVolumeId = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    private static readonly PropertyKey FriendlyNameKey = new(
        new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);

    public static IReadOnlyList<AudioCaptureDevice> GetActiveCaptureDevices()
    {
        var result = new List<AudioCaptureDevice>();
        var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
        IMMDeviceCollection? collection = null;
        try
        {
            CheckHResult(enumerator.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active, out collection),
                "録音エンドポイントの列挙");
            collection.GetCount(out var count);
            for (uint i = 0; i < count; i++)
            {
                IMMDevice? device = null;
                IPropertyStore? properties = null;
                try
                {
                    CheckHResult(collection.Item(i, out device), "録音デバイスの取得");
                    CheckHResult(device.GetId(out var id), "デバイスIDの取得");
                    CheckHResult(device.GetState(out var state), "デバイス状態の取得");
                    CheckHResult(device.OpenPropertyStore(StgmRead, out properties), "デバイス名の取得");
                    var key = FriendlyNameKey;
                    CheckHResult(properties.GetValue(ref key, out var value), "デバイス名の取得");
                    var name = ReadString(value);
                    if (!string.IsNullOrWhiteSpace(name))
                        result.Add(new AudioCaptureDevice(id, name, (int)state));
                    PropVariantClear(ref value);
                }
                finally
                {
                    Release(properties);
                    Release(device);
                }
            }
            return result;
        }
        finally
        {
            Release(collection);
            Release(enumerator);
        }
    }

    public static bool GetMute(AudioCaptureDevice target)
    {
        using var endpoint = OpenEndpoint(target.Id);
        CheckHResult(endpoint.Volume.GetMute(out var muted), "ミュート状態の取得");
        return muted;
    }

    public static void SetMute(AudioCaptureDevice target, bool mute)
    {
        using var endpoint = OpenEndpoint(target.Id);
        var context = Guid.Empty;
        CheckHResult(endpoint.Volume.SetMute(mute, ref context), mute ? "ミュート" : "ミュート解除");
    }

    private static EndpointHandle OpenEndpoint(string id)
    {
        var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
        IMMDevice? device = null;
        try
        {
            CheckHResult(enumerator.GetDevice(id, out device), "録音エンドポイントのオープン");
            var interfaceId = AudioEndpointVolumeId;
            CheckHResult(device.Activate(ref interfaceId, ClsCtxInprocServer,
                IntPtr.Zero, out var volumeObject), "EndpointVolume APIの取得");
            return new EndpointHandle(enumerator, device, (IAudioEndpointVolume)volumeObject);
        }
        catch
        {
            Release(device);
            Release(enumerator);
            throw;
        }
    }

    private static string ReadString(PropVariant value)
    {
        if (value.VariantType != VtLpWStr || value.PointerValue == IntPtr.Zero)
            return "";
        return Marshal.PtrToStringUni(value.PointerValue) ?? "";
    }

    private static void CheckHResult(int hResult, string operation)
    {
        if (hResult < 0)
            Marshal.ThrowExceptionForHR(hResult, new IntPtr(-1));
    }

    private static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
            Marshal.FinalReleaseComObject(value);
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant value);

    private sealed class EndpointHandle : IDisposable
    {
        public EndpointHandle(IMMDeviceEnumerator enumerator, IMMDevice device, IAudioEndpointVolume volume)
        {
            Enumerator = enumerator;
            Device = device;
            Volume = volume;
        }

        public IMMDeviceEnumerator Enumerator { get; }
        public IMMDevice Device { get; }
        public IAudioEndpointVolume Volume { get; }

        public void Dispose()
        {
            Release(Volume);
            Release(Device);
            Release(Enumerator);
        }
    }
}
