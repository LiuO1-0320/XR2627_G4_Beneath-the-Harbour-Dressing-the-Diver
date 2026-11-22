using System;
using System.Runtime.InteropServices;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
[UnityEditor.XR.OpenXR.Features.OpenXRFeature(
    UiName = "Visitor headset presence", Company = "Harbour", Version = "1.0.0",
    BuildTargetGroups = new[] { UnityEditor.BuildTargetGroup.Standalone, UnityEditor.BuildTargetGroup.Android },
    OpenxrExtensionStrings = "XR_EXT_user_presence", FeatureId = "harbour.staff.userpresence")]
#endif
public sealed class VisitorHeadsetPresence : OpenXRFeature
{
    // Unity OpenXR assumes presence on systems without a sensor. Check the system
    // capability once per XR system, then read its live value on the telemetry heartbeat.
    static bool supportsPresence, sessionRunning;
    ulong instance;
    public static bool IsWorn
    {
        get
        {
            if (!sessionRunning || !supportsPresence) return false;

            var headset = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            // Prefer the headset sensor when available, including a reported removal.
            if (headset.isValid && headset.TryGetFeatureValue(CommonUsages.userPresence, out var worn))
                return worn;

            return OpenXRUtility.IsUserPresent;
        }
    }

    protected override bool OnInstanceCreate(ulong xrInstance)
    {
        instance = xrInstance;
        supportsPresence = sessionRunning = false;
        return true;
    }

    protected override void OnSystemChange(ulong system)
    {
        supportsPresence = false;
        if (instance == 0 || system == 0 || xrGetInstanceProcAddr == IntPtr.Zero ||
            !OpenXRRuntime.IsExtensionEnabled("XR_EXT_user_presence")) return;

        var resolve = Marshal.GetDelegateForFunctionPointer<GetInstanceProcAddr>(xrGetInstanceProcAddr);
        if (resolve(instance, "xrGetSystemProperties", out var function) != 0 || function == IntPtr.Zero) return;
        var getProperties = Marshal.GetDelegateForFunctionPointer<GetSystemProperties>(function);
        var presence = Marshal.AllocHGlobal(Marshal.SizeOf<PresenceProperties>());
        try
        {
            Marshal.StructureToPtr(new PresenceProperties { type = 1000470001 }, presence, false);
            var properties = new SystemProperties { type = 5, next = presence, systemName = new byte[256] };
            if (getProperties(instance, system, ref properties) == 0)
                supportsPresence = Marshal.PtrToStructure<PresenceProperties>(presence).supportsUserPresence != 0;
        }
        finally { Marshal.FreeHGlobal(presence); }
    }

    protected override void OnSessionBegin(ulong session) => sessionRunning = true;
    protected override void OnSessionEnd(ulong session) => sessionRunning = false;
    protected override void OnSessionDestroy(ulong session) => sessionRunning = false;
    protected override void OnInstanceDestroy(ulong xrInstance)
    {
        instance = 0;
        supportsPresence = sessionRunning = false;
    }

    // OpenXR 1.0 ABI: XrSystemProperties with an XrSystemUserPresencePropertiesEXT next chain.
    [StructLayout(LayoutKind.Sequential)]
    struct SystemProperties
    {
        public int type;
        public IntPtr next;
        public ulong systemId;
        public uint vendorId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public byte[] systemName;
        public uint maxSwapchainImageHeight, maxSwapchainImageWidth, maxLayerCount;
        public uint orientationTracking, positionTracking;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PresenceProperties
    {
        public int type;
        public IntPtr next;
        public uint supportsUserPresence;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate int GetInstanceProcAddr(ulong instance, [MarshalAs(UnmanagedType.LPStr)] string name, out IntPtr function);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate int GetSystemProperties(ulong instance, ulong system, ref SystemProperties properties);
}
