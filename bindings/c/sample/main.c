/**
 * @file main.c
 * @brief C binding sample: File → YOLO → SORT tracker → App callback.
 *
 * Build:
 *   cd sdk/bindings/c && mkdir -p build && cd build
 *   cmake -DCVEDIX_SDK_DIR=../../.. .. && make
 *
 * Run:
 *   export LD_LIBRARY_PATH=$CVEDIX_SDK_DIR/lib/x86_64:$LD_LIBRARY_PATH
 *   ./c_pipeline_sample <video.mp4> <model.onnx> [labels.txt]
 */
#include <stdio.h>
#include <stdlib.h>

#include <cvedix_c_api.h>

static void on_result(int32_t frame_index, int32_t channel,
                      int32_t frame_w, int32_t frame_h,
                      const cvedix_detection_t* dets, int32_t count,
                      void* user_data) {
    (void)channel; (void)frame_w; (void)frame_h; (void)user_data;
    if (frame_index % 30 != 0) return; /* log ~1 lần/giây với video 30fps */
    printf("frame %d: %d object(s)\n", frame_index, count);
    for (int32_t i = 0; i < count; ++i) {
        printf("  [%s] score=%.2f track=%d box=(%d,%d,%d,%d)\n",
               dets[i].label, dets[i].score, dets[i].track_id,
               dets[i].x, dets[i].y, dets[i].width, dets[i].height);
    }
}

int main(int argc, char** argv) {
    if (argc < 3) {
        fprintf(stderr, "usage: %s <video.mp4> <model.onnx> [labels.txt]\n", argv[0]);
        return 1;
    }
    const char* video = argv[1];
    const char* model = argv[2];
    const char* labels = argc > 3 ? argv[3] : NULL;

    if (cvedix_init(CVEDIX_LOG_INFO) != CVEDIX_OK) {
        fprintf(stderr, "init failed: %s\n", cvedix_last_error());
        return 1;
    }
    printf("cvedix core version: %s\n", cvedix_version());

    /* ── Tạo node ── */
    cvedix_node_t* src = cvedix_file_src_create("src", 0, video, 0.5f, 1, NULL);
    cvedix_node_t* det = cvedix_yolo_detector_create("det", model, CVEDIX_YOLO11,
                                                     labels, 0.45f, 0.5f);
    cvedix_node_t* trk = cvedix_sort_tracker_create("trk");
    cvedix_node_t* app = cvedix_app_des_create("app", 0);

    if (!src || !det || !trk || !app) {
        fprintf(stderr, "node creation failed: %s\n", cvedix_last_error());
        return 1;
    }

    cvedix_app_des_set_callback(app, on_result, NULL);

    /* ── Nối pipeline: src → det → trk → app ── */
    cvedix_node_attach_to(det, &src, 1);
    cvedix_node_attach_to(trk, &det, 1);
    cvedix_node_attach_to(app, &trk, 1);

    /* ── Chạy ── */
    cvedix_src_start(src);
    printf("pipeline running — press Enter to stop\n");
    getchar();

    /* ── Dừng & dọn ── */
    cvedix_src_stop(src);
    cvedix_node_detach_recursively(src);
    cvedix_node_destroy(app);
    cvedix_node_destroy(trk);
    cvedix_node_destroy(det);
    cvedix_node_destroy(src);
    return 0;
}
