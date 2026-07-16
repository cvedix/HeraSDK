package com.cvedix.sdk;

import com.sun.jna.Callback;
import com.sun.jna.Library;
import com.sun.jna.Native;
import com.sun.jna.Pointer;
import com.sun.jna.Structure;

/**
 * Raw JNA mapping of {@code cvedix_c_api.h} (libcvedix_capi.so).
 *
 * <p>Prefer the high-level {@link CvedixPipeline} API. Use this interface
 * directly only when you need functions not yet wrapped.</p>
 */
public interface CvedixLibrary extends Library {

    CvedixLibrary INSTANCE = Native.load("cvedix_capi", CvedixLibrary.class);

    /* ── Log levels ── */
    int LOG_ERROR = 1;
    int LOG_WARN = 2;
    int LOG_INFO = 3;
    int LOG_DEBUG = 4;

    /* ── YOLO versions ── */
    int YOLO11 = 0;
    int YOLO26 = 1;
    int YOLO12 = 2;
    int RF_DETR = 3;

    /** Mirrors {@code cvedix_detection_t}. */
    @Structure.FieldOrder({"x", "y", "width", "height", "classId", "score", "trackId", "label"})
    class Detection extends Structure {
        public int x;
        public int y;
        public int width;
        public int height;
        public int classId;
        public float score;
        public int trackId;
        public byte[] label = new byte[64];

        public Detection() {}

        public Detection(Pointer p) {
            super(p);
            read();
        }

        public String labelString() {
            int end = 0;
            while (end < label.length && label[end] != 0) end++;
            return new String(label, 0, end, java.nio.charset.StandardCharsets.UTF_8);
        }
    }

    /** Mirrors {@code cvedix_result_callback_t}. */
    interface ResultCallback extends Callback {
        void invoke(int frameIndex, int channel, int frameWidth, int frameHeight,
                    Pointer detections, int count, Pointer userData);
    }

    /* ── Runtime ── */
    int cvedix_init(int logLevel);
    String cvedix_version();
    String cvedix_last_error();

    /* ── Nodes ── */
    Pointer cvedix_file_src_create(String name, int channel, String filePath,
                                   float resizeRatio, int loop, String gstDecoder);
    Pointer cvedix_rtsp_src_create(String name, int channel, String rtspUrl,
                                   float resizeRatio, String gstDecoder);
    Pointer cvedix_yolo_detector_create(String name, String modelPath, int yoloVersion,
                                        String labelsPath, float confThreshold, float nmsThreshold);
    Pointer cvedix_sort_tracker_create(String name);
    Pointer cvedix_bytetrack_tracker_create(String name, float trackThresh, float highThresh,
                                            float matchThresh, int trackBuffer, int frameRate);
    Pointer cvedix_osd_create(String name, String font);
    Pointer cvedix_rtsp_des_create(String name, int channel, int port,
                                   String streamName, int bitrateKbps, int drawOsd);
    Pointer cvedix_file_des_create(String name, int channel, String saveDir, String namePrefix,
                                   int maxMinutesPerFile, int bitrateKbps, int drawOsd);
    Pointer cvedix_web_des_create(String name, int channel, int port, int jpegQuality);
    Pointer cvedix_app_des_create(String name, int channel);

    /* ── Callback ── */
    int cvedix_app_des_set_callback(Pointer appDes, ResultCallback callback, Pointer userData);

    /* ── Pipeline ── */
    int cvedix_node_attach_to(Pointer node, Pointer[] upstreams, int count);
    int cvedix_node_detach(Pointer node);
    int cvedix_node_detach_recursively(Pointer node);
    int cvedix_src_start(Pointer src);
    int cvedix_src_stop(Pointer src);
    int cvedix_node_destroy(Pointer node);
}
