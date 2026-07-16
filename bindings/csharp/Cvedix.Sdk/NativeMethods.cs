using System.Runtime.InteropServices;

namespace Cvedix.Sdk;

/// <summary>
/// Raw P/Invoke mapping of <c>cvedix_c_api.h</c> (libcvedix_capi.so).
/// Prefer the high-level <see cref="CvedixPipeline"/> API.
/// </summary>
public static partial class NativeMethods
{
    private const string Lib = "cvedix_capi";

    // ── Log levels ──
    public const int LogError = 1;
    public const int LogWarn = 2;
    public const int LogInfo = 3;
    public const int LogDebug = 4;

    // ── YOLO versions ──
    public const int Yolo11 = 0;
    public const int Yolo26 = 1;
    public const int Yolo12 = 2;
    public const int RfDetr = 3;

    /// <summary>Mirrors <c>cvedix_detection_t</c>.</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DetectionNative
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int ClassId;
        public float Score;
        public int TrackId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Label;
    }

    /// <summary>Mirrors <c>cvedix_result_callback_t</c>.</summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ResultCallback(
        int frameIndex, int channel, int frameWidth, int frameHeight,
        IntPtr detections, int count, IntPtr userData);

    // ── Runtime ──
    [LibraryImport(Lib)] public static partial int cvedix_init(int logLevel);
    [LibraryImport(Lib)] public static partial IntPtr cvedix_version();
    [LibraryImport(Lib)] public static partial IntPtr cvedix_last_error();

    public static string VersionString() => Marshal.PtrToStringAnsi(cvedix_version()) ?? "";
    public static string LastError() => Marshal.PtrToStringAnsi(cvedix_last_error()) ?? "";

    // ── Nodes ──
    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr cvedix_file_src_create(
        string name, int channel, string filePath, float resizeRatio, int loop, string? gstDecoder);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr cvedix_rtsp_src_create(
        string name, int channel, string rtspUrl, float resizeRatio, string? gstDecoder);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr cvedix_yolo_detector_create(
        string name, string modelPath, int yoloVersion, string? labelsPath,
        float confThreshold, float nmsThreshold);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr cvedix_sort_tracker_create(string name);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr cvedix_bytetrack_tracker_create(
        string name, float trackThresh, float highThresh, float matchThresh,
        int trackBuffer, int frameRate);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr cvedix_osd_create(string name, string? font);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr cvedix_rtsp_des_create(
        string name, int channel, int port, string? streamName, int bitrateKbps, int drawOsd);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr cvedix_file_des_create(
        string name, int channel, string saveDir, string? namePrefix,
        int maxMinutesPerFile, int bitrateKbps, int drawOsd);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr cvedix_web_des_create(
        string name, int channel, int port, int jpegQuality);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr cvedix_app_des_create(string name, int channel);

    // ── Callback ──
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int cvedix_app_des_set_callback(
        IntPtr appDes, ResultCallback callback, IntPtr userData);

    // ── Pipeline ──
    [LibraryImport(Lib)]
    public static partial int cvedix_node_attach_to(
        IntPtr node, [In] IntPtr[] upstreams, int count);

    [LibraryImport(Lib)] public static partial int cvedix_node_detach(IntPtr node);
    [LibraryImport(Lib)] public static partial int cvedix_node_detach_recursively(IntPtr node);
    [LibraryImport(Lib)] public static partial int cvedix_src_start(IntPtr src);
    [LibraryImport(Lib)] public static partial int cvedix_src_stop(IntPtr src);
    [LibraryImport(Lib)] public static partial int cvedix_node_destroy(IntPtr node);
}
