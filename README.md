# CVEDIX SDK — Bộ công cụ phát triển AI Video Analytics

<div align="center">
  <img src="assets/cvedix_sdk_architecture.png" alt="Kiến trúc CVEDIX SDK" width="800"/>
</div>

> Xây dựng pipeline phân tích video AI thời gian thực mà không cần chạm vào source code của core runtime.

---

## SDK bao gồm những gì

| Thư mục | Mô tả |
|---------|-------|
| `include/` | Header C++ công khai để tích hợp SDK (+ `include/cvedix/capi/` — header C API) |
| `lib/` | Thư viện dựng sẵn (`.so`) theo từng kiến trúc phần cứng |
| `bindings/` | Bindings đa ngôn ngữ: **C**, **Java (JNA)**, **C# (.NET P/Invoke)** — xem [bindings/README.md](bindings/README.md) |
| `samples/` | Ứng dụng mẫu chọn lọc kèm hướng dẫn từng bước |
| `docs/` | Tổng quan kiến trúc, danh mục node, các pattern pipeline |
| `cmake/` | Tích hợp CMake (`find_package(cvedix)`) |
| `mcp/` | MCP Server hỗ trợ lập trình bằng AI (Cursor, VSCode, Antigravity) |

---

## Bắt đầu nhanh

### 1. Cài đặt SDK

```bash
# Thiết lập biến môi trường
export CVEDIX_SDK_DIR=/duong/dan/toi/cvedix-sdk
export LD_LIBRARY_PATH=$CVEDIX_SDK_DIR/lib/x86_64:$LD_LIBRARY_PATH
```

### 2. Build pipeline đầu tiên

```bash
cd samples
mkdir build && cd build
cmake -DCVEDIX_SDK_DIR=../../ ..
make -j$(nproc)
./01_basic_pipeline
```

### 3. Lập trình với AI (MCP)

Kết nối CVEDIX MCP server vào công cụ AI coding của bạn để được hỗ trợ dựng pipeline theo ngữ cảnh.

```bash
cd mcp
npm install && npm run build
```

**Cursor / VSCode** — Thêm vào `.cursor/mcp.json` hoặc settings của VS Code:

```json
{
  "mcpServers": {
    "cvedix-sdk": {
      "command": "node",
      "args": ["/duong/dan/tuyet-doi/toi/cvedix-sdk/mcp/build/index.js"]
    }
  }
}
```

Sau đó yêu cầu AI: *"Tạo pipeline phát hiện khuôn mặt có làm mờ và xuất ra web"*

---

## Kiến trúc SDK

```
Ứng dụng của bạn
    │
    ├── #include <cvedix/nodes/src/cvedix_file_src_node.h>
    ├── #include <cvedix/nodes/infers/cvedix_yolo_detector_node.h>
    ├── #include <cvedix/nodes/osd/cvedix_osd_node.h>
    │
    ▼
┌─────────────────────────────────┐
│  libcvedix_core.so              │  ← Core runtime dựng sẵn
│  + libtrt_yolov11.so (plugin)   │  ← Plugin backend TensorRT
│  + libonnx_yolov11.so (plugin)  │  ← Plugin backend ONNX
└─────────────────────────────────┘
```

Không dùng C++? Lớp **C API** (`libcvedix_capi.so`) cung cấp ABI ổn định cho **Java**, **C#** và **C** — xem [bindings/README.md](bindings/README.md).

## Ví dụ tối giản (C++)

```cpp
#include <cvedix/nodes/src/cvedix_file_src_node.h>
#include <cvedix/nodes/infers/cvedix_yolo_detector_node.h>
#include <cvedix/nodes/osd/cvedix_osd_node.h>
#include <cvedix/nodes/des/cvedix_screen_des_node.h>

int main() {
    CVEDIX_LOGGER_INIT();

    auto src = std::make_shared<cvedix_nodes::cvedix_file_src_node>(
        "src", 0, "video.mp4", 1.0);
    auto det = std::make_shared<cvedix_nodes::cvedix_yolo_detector_node>(
        "det", "yolo11n.engine", cvedix_nodes::YoloVersion::YOLO11,
        "labels.txt", 0.45f, 0.5f);
    auto osd = std::make_shared<cvedix_nodes::cvedix_osd_node>("osd");
    auto screen = std::make_shared<cvedix_nodes::cvedix_screen_des_node>("screen", 0);

    det->attach_to({src});      // nối pipeline: src → det → osd → screen
    osd->attach_to({det});
    screen->attach_to({osd});

    src->start();               // chạy
    std::cin.get();
    src->detach_recursively();  // dọn dẹp
}
```

---

## Tài liệu

- [**Hướng dẫn bắt đầu nhanh**](docs/QUICKSTART.md)
- [**Danh mục Node**](docs/NODE_CATALOG.md) — 80+ node kèm tham số cấu hình
- [**Pipeline Patterns**](docs/PIPELINE_PATTERNS.md) — Các topology thông dụng
- [**Bindings đa ngôn ngữ**](bindings/README.md) — Java, C#, C

---

## Yêu cầu hệ thống

| Yêu cầu | Phiên bản |
|---------|-----------|
| Chuẩn C++ | C++17 trở lên |
| Trình biên dịch | GCC ≥ 7.5 |
| OpenCV | ≥ 4.6 |
| GStreamer | 1.14.5+ |

Tùy chọn theo backend suy luận: CUDA + TensorRT, ONNX Runtime, OpenVINO.

Bindings: JDK 17+ với Maven (Java), .NET 8 SDK (C#).

---

## Giấy phép

Phần mềm độc quyền — CVEDIX. Liên hệ account manager để biết thông tin cấp phép.
