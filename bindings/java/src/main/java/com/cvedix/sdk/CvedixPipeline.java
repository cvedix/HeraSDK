package com.cvedix.sdk;

import com.sun.jna.Pointer;

import java.util.ArrayList;
import java.util.List;

/**
 * High-level builder-style wrapper over the CVEDIX C API.
 *
 * <pre>{@code
 * CvedixPipeline pipeline = new CvedixPipeline();
 * CvedixPipeline.Node src = pipeline.fileSource("src", 0, "video.mp4", 0.5f, true);
 * CvedixPipeline.Node det = pipeline.yoloDetector("det", "model.onnx", CvedixLibrary.YOLO11, "labels.txt");
 * CvedixPipeline.Node app = pipeline.appDestination("app", 0, (frame, dets) -> {
 *     dets.forEach(System.out::println);
 * });
 * det.attachTo(src);
 * app.attachTo(det);
 * pipeline.start(src);
 * ...
 * pipeline.close();  // stop + detach + destroy everything
 * }</pre>
 */
public final class CvedixPipeline implements AutoCloseable {

    private static final CvedixLibrary LIB = CvedixLibrary.INSTANCE;

    /** Result listener for app destinations. */
    @FunctionalInterface
    public interface ResultListener {
        void onFrame(FrameInfo frame, List<Detection> detections);
    }

    /** Per-frame metadata. */
    public record FrameInfo(int frameIndex, int channel, int width, int height) {}

    /** Handle to one pipeline node. */
    public final class Node {
        final Pointer handle;
        private final boolean isSource;

        private Node(Pointer handle, boolean isSource) {
            this.handle = handle;
            this.isSource = isSource;
        }

        /** Attach this node downstream of the given nodes. */
        public Node attachTo(Node... upstreams) {
            Pointer[] ptrs = new Pointer[upstreams.length];
            for (int i = 0; i < upstreams.length; i++) ptrs[i] = upstreams[i].handle;
            check(LIB.cvedix_node_attach_to(handle, ptrs, ptrs.length), "attach_to");
            return this;
        }
    }

    private final List<Node> nodes = new ArrayList<>();
    // JNA callbacks must stay strongly referenced or they get GC'd while native code holds them.
    private final List<CvedixLibrary.ResultCallback> retainedCallbacks = new ArrayList<>();
    private boolean closed = false;

    public CvedixPipeline() {
        this(CvedixLibrary.LOG_INFO);
    }

    public CvedixPipeline(int logLevel) {
        check(LIB.cvedix_init(logLevel), "init");
    }

    public static String coreVersion() {
        return LIB.cvedix_version();
    }

    /* ── Node factories ────────────────────────────────────────────────── */

    public Node fileSource(String name, int channel, String path, float resizeRatio, boolean loop) {
        return register(LIB.cvedix_file_src_create(name, channel, path, resizeRatio, loop ? 1 : 0, null),
                "fileSource", true);
    }

    public Node rtspSource(String name, int channel, String url, float resizeRatio) {
        return register(LIB.cvedix_rtsp_src_create(name, channel, url, resizeRatio, null),
                "rtspSource", true);
    }

    public Node yoloDetector(String name, String modelPath, int yoloVersion, String labelsPath) {
        return yoloDetector(name, modelPath, yoloVersion, labelsPath, 0.45f, 0.5f);
    }

    public Node yoloDetector(String name, String modelPath, int yoloVersion, String labelsPath,
                             float confThreshold, float nmsThreshold) {
        return register(LIB.cvedix_yolo_detector_create(name, modelPath, yoloVersion,
                labelsPath, confThreshold, nmsThreshold), "yoloDetector", false);
    }

    public Node sortTracker(String name) {
        return register(LIB.cvedix_sort_tracker_create(name), "sortTracker", false);
    }

    public Node byteTracker(String name) {
        return register(LIB.cvedix_bytetrack_tracker_create(name, 0.5f, 0.6f, 0.8f, 30, 30),
                "byteTracker", false);
    }

    public Node osd(String name) {
        return register(LIB.cvedix_osd_create(name, null), "osd", false);
    }

    public Node rtspDestination(String name, int channel, int port) {
        return register(LIB.cvedix_rtsp_des_create(name, channel, port, null, 1024, 1),
                "rtspDestination", false);
    }

    public Node fileDestination(String name, int channel, String saveDir) {
        return register(LIB.cvedix_file_des_create(name, channel, saveDir, null, 2, 1024, 1),
                "fileDestination", false);
    }

    /** Browser MJPEG debug output at {@code http://localhost:port}. */
    public Node webDestination(String name, int channel, int port) {
        return register(LIB.cvedix_web_des_create(name, channel, port, 75),
                "webDestination", false);
    }

    /** App destination delivering per-frame detections to {@code listener}. */
    public Node appDestination(String name, int channel, ResultListener listener) {
        Node node = register(LIB.cvedix_app_des_create(name, channel), "appDestination", false);
        CvedixLibrary.ResultCallback cb = (frameIndex, ch, w, h, detPtr, count, user) -> {
            List<Detection> out = new ArrayList<>(count);
            if (detPtr != null && count > 0) {
                CvedixLibrary.Detection first = new CvedixLibrary.Detection(detPtr);
                CvedixLibrary.Detection[] arr = (CvedixLibrary.Detection[]) first.toArray(count);
                for (CvedixLibrary.Detection d : arr) out.add(new Detection(d));
            }
            listener.onFrame(new FrameInfo(frameIndex, ch, w, h), out);
        };
        retainedCallbacks.add(cb);
        check(LIB.cvedix_app_des_set_callback(node.handle, cb, null), "set_callback");
        return node;
    }

    /* ── Lifecycle ─────────────────────────────────────────────────────── */

    public void start(Node source) {
        check(LIB.cvedix_src_start(source.handle), "start");
    }

    public void stop(Node source) {
        check(LIB.cvedix_src_stop(source.handle), "stop");
    }

    /** Stop all sources, tear down the graph, release every handle. */
    @Override
    public void close() {
        if (closed) return;
        closed = true;
        for (Node n : nodes) {
            if (n.isSource) {
                LIB.cvedix_src_stop(n.handle);
                LIB.cvedix_node_detach_recursively(n.handle);
            }
        }
        for (Node n : nodes) LIB.cvedix_node_destroy(n.handle);
        nodes.clear();
        retainedCallbacks.clear();
    }

    /* ── Helpers ───────────────────────────────────────────────────────── */

    private Node register(Pointer handle, String op, boolean isSource) {
        if (handle == null) throw new CvedixException(op);
        Node node = new Node(handle, isSource);
        nodes.add(node);
        return node;
    }

    private static void check(int status, String op) {
        if (status != 0) throw new CvedixException(op + " (status " + status + ")");
    }
}
