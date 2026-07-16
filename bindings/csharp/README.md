# CVEDIX C# Binding (P/Invoke)

C# wrapper cho CVEDIX runtime qua P/Invoke (.NET 8).

## Yêu cầu

- .NET SDK 8.0+
- `libcvedix_capi.so` + `libcvedix_core.so` trong `LD_LIBRARY_PATH` (có sẵn trong `lib/x86_64/` của SDK)

## Build & chạy sample

```bash
cd bindings/csharp
dotnet build

export LD_LIBRARY_PATH=$PWD/../../lib/x86_64:$LD_LIBRARY_PATH
cd Cvedix.Sdk.Sample
dotnet run -- video.mp4 yolo11n.onnx labels.txt
```

## Dùng trong project của bạn

```csharp
using Cvedix.Sdk;

using var pipeline = new CvedixPipeline();
var src = pipeline.FileSource("src", 0, "video.mp4", 0.5f, loop: true);
var det = pipeline.YoloDetector("det", "model.onnx", NativeMethods.Yolo11, "labels.txt");
var app = pipeline.AppDestination("app", 0, (frame, dets) =>
{
    foreach (var d in dets) Console.WriteLine(d);  // Detection: Label, Score, TrackId, bbox
});

det.AttachTo(src);
app.AttachTo(det);
pipeline.Start(src);
```

## Lưu ý

- Callback được gọi từ **thread native của pipeline** — marshal về UI thread nếu cần
- `CvedixPipeline` giữ reference cho delegate (tránh GC thu hồi khi native còn giữ function pointer)
- Lỗi native ném `CvedixException` kèm message từ `cvedix_last_error()`
- API cấp thấp đầy đủ: xem [NativeMethods.cs](Cvedix.Sdk/NativeMethods.cs)
