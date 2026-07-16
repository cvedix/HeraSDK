# CVEDIX Language Bindings

Bindings cho các ngôn ngữ ngoài C++, xây trên **C API ổn định** (`libcvedix_capi.so` + `cvedix_c_api.h`).

```
┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│  Java (JNA) │  │ C# P/Invoke │  │      C      │
└──────┬──────┘  └──────┬──────┘  └──────┬──────┘
       └────────────────┼────────────────┘
                ┌───────▼────────┐
                │ libcvedix_capi │   C ABI — không đổi khi core refactor
                └───────┬────────┘
                ┌───────▼────────┐
                │ libcvedix_core │   C++ runtime (80+ nodes)
                └────────────────┘
```

| Binding | Thư mục | Công nghệ | Yêu cầu |
|---------|---------|-----------|---------|
| C | [c/](c/) | Header thuần C99 | GCC/Clang |
| Java | [java/](java/) | JNA 5.x | JDK 11+, Maven |
| C# | [csharp/](csharp/) | P/Invoke | .NET 8 |

## Cách hoạt động của luồng cập nhật

1. CoreAI thay đổi → build lại (`make build`) → `libcvedix_capi.so` mới
2. Chạy `scripts/package_sdk.sh` từ thư mục core → copy `.so` + header vào SDK này
3. Bindings **không cần sửa** trừ khi `cvedix_c_api.h` thêm hàm mới (ABI chỉ thêm, không sửa/xóa)

## Phạm vi C API v1

Node được hỗ trợ: `file_src`, `rtsp_src`, `yolo_detector` (ONNX/TensorRT/RKNN auto-detect), `sort_tracker`, `bytetrack_tracker`, `osd`, `rtsp_des`, `file_des`, `web_des`, `app_des` (callback kết quả detection về ứng dụng).

Cần thêm node khác → mở rộng `capi/` trong repo core, bindings chỉ cần khai báo thêm hàm tương ứng.

## Runtime

Mọi binding đều cần các thư viện native trong `LD_LIBRARY_PATH`:

```bash
export LD_LIBRARY_PATH=$CVEDIX_SDK_DIR/lib/x86_64:$LD_LIBRARY_PATH
```
