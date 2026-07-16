# CVEDIX SDK — AI Video Analytics Development Kit

<div align="center">
  <img src="assets/cvedix_sdk_architecture.png" alt="CVEDIX SDK Architecture" width="800"/>
</div>

> Build real-time AI video analytics pipelines without touching the core runtime source code.

---

## What's Inside

| Directory | Description |
|-----------|-------------|
| `include/` | Public C++ headers for SDK integration (+ `include/cvedix/capi/` — C API header) |
| `lib/` | Pre-built shared libraries (`.so`) per architecture |
| `bindings/` | Language bindings: **C**, **Java (JNA)**, **C# (.NET P/Invoke)** — see [bindings/README.md](bindings/README.md) |
| `samples/` | Curated sample applications with step-by-step guides |
| `docs/` | Architecture overview, node catalog, pipeline patterns |
| `cmake/` | CMake integration (`find_package(cvedix)`) |
| `mcp/` | MCP Server for AI-assisted development (Cursor, VSCode, Antigravity) |

---

## Quick Start

### 1. Install SDK

```bash
# Option A: Use install script
./scripts/install.sh

# Option B: Manual — set environment
export CVEDIX_SDK_DIR=/path/to/cvedix-sdk
export LD_LIBRARY_PATH=$CVEDIX_SDK_DIR/lib/x86_64:$LD_LIBRARY_PATH
```

### 2. Build Your First Pipeline

```bash
cd samples
mkdir build && cd build
cmake -DCVEDIX_SDK_DIR=../../ ..
make -j$(nproc)
./01_basic_pipeline
```

### 3. Use AI-Assisted Development (MCP)

Connect the CVEDIX MCP server to your AI coding tool for context-aware pipeline development.

```bash
cd mcp
npm install && npm run build
```

**Cursor / VSCode** — Add to `.cursor/mcp.json` or VS Code settings:

```json
{
  "mcpServers": {
    "cvedix-sdk": {
      "command": "node",
      "args": ["/absolute/path/to/cvedix-sdk/mcp/build/index.js"]
    }
  }
}
```

Then ask your AI: *"Create a face detection pipeline with blur and web output"*

---

## SDK Architecture

```
Your Application
    │
    ├── #include <cvedix/nodes/src/cvedix_file_src_node.h>
    ├── #include <cvedix/nodes/infers/cvedix_yolo_detector_node.h>
    ├── #include <cvedix/nodes/osd/cvedix_osd_node.h>
    │
    ▼
┌─────────────────────────────────┐
│  libcvedix_core.so              │  ← Pre-built core runtime
│  + libtrt_yolov11.so (plugin)   │  ← TensorRT backend plugin
│  + libonnx_yolov11.so (plugin)  │  ← ONNX backend plugin
└─────────────────────────────────┘
```

## Minimal Example

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

    det->attach_to({src});
    osd->attach_to({det});
    screen->attach_to({osd});

    src->start();
    std::cin.get();
    src->detach_recursively();
}
```

---

## Documentation

- [**Quick Start Guide**](docs/QUICKSTART.md)
- [**Node Catalog**](docs/NODE_CATALOG.md) — 80+ nodes with config reference
- [**Pipeline Patterns**](docs/PIPELINE_PATTERNS.md) — Common topologies
- [**API Reference**](docs/API_REFERENCE.md) — Config structs and methods

---

## Requirements

| Requirement | Version |
|-------------|---------|
| C++ Standard | C++17 or higher |
| Compiler | GCC ≥ 7.5 |
| OpenCV | ≥ 4.6 |
| GStreamer | 1.14.5+ |

Optional: CUDA + TensorRT, ONNX Runtime, OpenVINO (depending on inference backend)

---

## License

Proprietary — CVEDIX. Contact your account manager for licensing.
