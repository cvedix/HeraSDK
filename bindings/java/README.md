# CVEDIX Java Binding (JNA)

Java wrapper cho CVEDIX runtime qua [JNA](https://github.com/java-native-access/jna) — không cần compile JNI.

## Yêu cầu

- JDK 17+, Maven 3.6+
- `libcvedix_capi.so` + `libcvedix_core.so` trong `LD_LIBRARY_PATH` (có sẵn trong `lib/x86_64/` của SDK)

## Build & chạy sample

```bash
cd bindings/java
mvn -q package

export LD_LIBRARY_PATH=$PWD/../../lib/x86_64:$LD_LIBRARY_PATH
mvn -q exec:java -Dexec.args="video.mp4 yolo11n.onnx labels.txt"
```

## Dùng trong project của bạn

```java
try (CvedixPipeline pipeline = new CvedixPipeline()) {
    var src = pipeline.fileSource("src", 0, "video.mp4", 0.5f, true);
    var det = pipeline.yoloDetector("det", "model.onnx", CvedixLibrary.YOLO11, "labels.txt");
    var app = pipeline.appDestination("app", 0, (frame, dets) -> {
        dets.forEach(System.out::println);   // Detection: label, score, trackId, bbox
    });

    det.attachTo(src);
    app.attachTo(det);
    pipeline.start(src);
    // ...
} // tự động stop + giải phóng
```

## Lưu ý

- Callback được gọi từ **thread native của pipeline** — không block lâu, không đụng UI thread trực tiếp
- `CvedixPipeline` giữ strong-reference cho JNA callback (tránh bị GC khi native còn dùng)
- Lỗi native ném `CvedixException` kèm message từ `cvedix_last_error()`
- API cấp thấp đầy đủ: xem [CvedixLibrary.java](src/main/java/com/cvedix/sdk/CvedixLibrary.java)
