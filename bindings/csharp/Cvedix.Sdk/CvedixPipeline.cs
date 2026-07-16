using System.Runtime.InteropServices;

namespace Cvedix.Sdk;

/// <summary>One detected/tracked object.</summary>
public readonly record struct Detection(
    int X, int Y, int Width, int Height,
    int ClassId, float Score, int TrackId, string Label)
{
    public override string ToString() =>
        $"{Label}({Score:F2}) track={TrackId} [{X},{Y} {Width}x{Height}]";
}

/// <summary>Per-frame metadata.</summary>
public readonly record struct FrameInfo(int FrameIndex, int Channel, int Width, int Height);

/// <summary>Thrown when a native CVEDIX call fails.</summary>
public sealed class CvedixException : Exception
{
    public CvedixException(string operation)
        : base($"{operation}: {NativeMethods.LastError()}") { }
}

/// <summary>
/// High-level wrapper over the CVEDIX C API.
/// </summary>
/// <example>
/// <code>
/// using var pipeline = new CvedixPipeline();
/// var src = pipeline.FileSource("src", 0, "video.mp4", 0.5f, loop: true);
/// var det = pipeline.YoloDetector("det", "model.onnx", NativeMethods.Yolo11, "labels.txt");
/// var app = pipeline.AppDestination("app", 0, (frame, dets) =>
/// {
///     foreach (var d in dets) Console.WriteLine(d);
/// });
/// det.AttachTo(src);
/// app.AttachTo(det);
/// pipeline.Start(src);
/// </code>
/// </example>
public sealed class CvedixPipeline : IDisposable
{
    /// <summary>Result listener for app destinations.</summary>
    public delegate void ResultListener(FrameInfo frame, IReadOnlyList<Detection> detections);

    /// <summary>Handle to one pipeline node.</summary>
    public sealed class Node
    {
        internal IntPtr Handle { get; }
        internal bool IsSource { get; }

        internal Node(IntPtr handle, bool isSource)
        {
            Handle = handle;
            IsSource = isSource;
        }

        /// <summary>Attach this node downstream of the given nodes.</summary>
        public Node AttachTo(params Node[] upstreams)
        {
            var ptrs = upstreams.Select(n => n.Handle).ToArray();
            Check(NativeMethods.cvedix_node_attach_to(Handle, ptrs, ptrs.Length), "attach_to");
            return this;
        }
    }

    private readonly List<Node> _nodes = new();
    // Delegates must stay strongly referenced while native code holds the function pointer.
    private readonly List<NativeMethods.ResultCallback> _retainedCallbacks = new();
    private bool _disposed;

    public CvedixPipeline(int logLevel = NativeMethods.LogInfo)
    {
        Check(NativeMethods.cvedix_init(logLevel), "init");
    }

    public static string CoreVersion => NativeMethods.VersionString();

    // ── Node factories ────────────────────────────────────────────────────

    public Node FileSource(string name, int channel, string path, float resizeRatio, bool loop) =>
        Register(NativeMethods.cvedix_file_src_create(name, channel, path, resizeRatio, loop ? 1 : 0, null),
            "FileSource", isSource: true);

    public Node RtspSource(string name, int channel, string url, float resizeRatio) =>
        Register(NativeMethods.cvedix_rtsp_src_create(name, channel, url, resizeRatio, null),
            "RtspSource", isSource: true);

    public Node YoloDetector(string name, string modelPath, int yoloVersion, string? labelsPath,
                             float confThreshold = 0.45f, float nmsThreshold = 0.5f) =>
        Register(NativeMethods.cvedix_yolo_detector_create(name, modelPath, yoloVersion, labelsPath,
            confThreshold, nmsThreshold), "YoloDetector", isSource: false);

    public Node SortTracker(string name) =>
        Register(NativeMethods.cvedix_sort_tracker_create(name), "SortTracker", isSource: false);

    public Node ByteTracker(string name) =>
        Register(NativeMethods.cvedix_bytetrack_tracker_create(name, 0.5f, 0.6f, 0.8f, 30, 30),
            "ByteTracker", isSource: false);

    public Node Osd(string name) =>
        Register(NativeMethods.cvedix_osd_create(name, null), "Osd", isSource: false);

    public Node RtspDestination(string name, int channel, int port) =>
        Register(NativeMethods.cvedix_rtsp_des_create(name, channel, port, null, 1024, 1),
            "RtspDestination", isSource: false);

    public Node FileDestination(string name, int channel, string saveDir) =>
        Register(NativeMethods.cvedix_file_des_create(name, channel, saveDir, null, 2, 1024, 1),
            "FileDestination", isSource: false);

    /// <summary>Browser MJPEG debug output at http://localhost:{port}.</summary>
    public Node WebDestination(string name, int channel, int port) =>
        Register(NativeMethods.cvedix_web_des_create(name, channel, port, 75),
            "WebDestination", isSource: false);

    /// <summary>App destination delivering per-frame detections to <paramref name="listener"/>.</summary>
    public Node AppDestination(string name, int channel, ResultListener listener)
    {
        var node = Register(NativeMethods.cvedix_app_des_create(name, channel),
            "AppDestination", isSource: false);

        NativeMethods.ResultCallback cb = (frameIndex, ch, w, h, detPtr, count, _) =>
        {
            var list = new List<Detection>(count);
            var size = Marshal.SizeOf<NativeMethods.DetectionNative>();
            for (int i = 0; i < count; i++)
            {
                var raw = Marshal.PtrToStructure<NativeMethods.DetectionNative>(detPtr + i * size);
                list.Add(new Detection(raw.X, raw.Y, raw.Width, raw.Height,
                    raw.ClassId, raw.Score, raw.TrackId, raw.Label));
            }
            listener(new FrameInfo(frameIndex, ch, w, h), list);
        };
        _retainedCallbacks.Add(cb);
        Check(NativeMethods.cvedix_app_des_set_callback(node.Handle, cb, IntPtr.Zero), "set_callback");
        return node;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public void Start(Node source) => Check(NativeMethods.cvedix_src_start(source.Handle), "start");

    public void Stop(Node source) => Check(NativeMethods.cvedix_src_stop(source.Handle), "stop");

    /// <summary>Stop all sources, tear down the graph, release every handle.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var n in _nodes.Where(n => n.IsSource))
        {
            NativeMethods.cvedix_src_stop(n.Handle);
            NativeMethods.cvedix_node_detach_recursively(n.Handle);
        }
        foreach (var n in _nodes) NativeMethods.cvedix_node_destroy(n.Handle);
        _nodes.Clear();
        _retainedCallbacks.Clear();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Node Register(IntPtr handle, string op, bool isSource)
    {
        if (handle == IntPtr.Zero) throw new CvedixException(op);
        var node = new Node(handle, isSource);
        _nodes.Add(node);
        return node;
    }

    private static void Check(int status, string op)
    {
        if (status != 0) throw new CvedixException($"{op} (status {status})");
    }
}
