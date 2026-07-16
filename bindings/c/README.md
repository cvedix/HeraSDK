# CVEDIX C Binding

Dùng trực tiếp `libcvedix_capi.so` + header [cvedix_c_api.h](../../include/cvedix/capi/cvedix_c_api.h).

## Build sample

```bash
cd bindings/c
mkdir -p build && cd build
cmake .. && make
```

## Chạy

```bash
export LD_LIBRARY_PATH=$PWD/../../../lib/x86_64:$LD_LIBRARY_PATH
./c_pipeline_sample video.mp4 yolo11n.onnx labels.txt
```

## API tóm tắt

```c
cvedix_init(CVEDIX_LOG_INFO);

cvedix_node_t* src = cvedix_file_src_create("src", 0, "video.mp4", 0.5f, 1, NULL);
cvedix_node_t* det = cvedix_yolo_detector_create("det", "model.onnx", CVEDIX_YOLO11, "labels.txt", 0.45f, 0.5f);
cvedix_node_t* app = cvedix_app_des_create("app", 0);

cvedix_app_des_set_callback(app, on_result, NULL);   // nhận detection mỗi frame
cvedix_node_attach_to(det, &src, 1);
cvedix_node_attach_to(app, &det, 1);
cvedix_src_start(src);
```

Quy tắc:

- Mọi hàm `*_create` trả `NULL` khi lỗi → đọc `cvedix_last_error()`
- Hàm trả `int`: `0` = OK, âm = lỗi (xem `cvedix_status_t`)
- Callback chạy trên thread nội bộ của pipeline; mảng `detections` chỉ hợp lệ trong callback — copy nếu cần giữ
- Thứ tự dọn dẹp: `cvedix_src_stop` → `cvedix_node_detach_recursively(src)` → `cvedix_node_destroy` từng node
