package com.cvedix.sdk.sample;

import com.cvedix.sdk.CvedixLibrary;
import com.cvedix.sdk.CvedixPipeline;

/**
 * Java binding sample: File → YOLO → SORT tracker → App callback.
 *
 * Run:
 *   export LD_LIBRARY_PATH=$CVEDIX_SDK_DIR/lib/x86_64:$LD_LIBRARY_PATH
 *   mvn -q exec:java -Dexec.args="video.mp4 model.onnx labels.txt"
 */
public final class PipelineSample {

    public static void main(String[] args) throws Exception {
        if (args.length < 2) {
            System.err.println("usage: PipelineSample <video.mp4> <model.onnx> [labels.txt]");
            System.exit(1);
        }
        String video = args[0];
        String model = args[1];
        String labels = args.length > 2 ? args[2] : null;

        System.out.println("cvedix core version: " + CvedixPipeline.coreVersion());

        try (CvedixPipeline pipeline = new CvedixPipeline()) {
            CvedixPipeline.Node src = pipeline.fileSource("src", 0, video, 0.5f, true);
            CvedixPipeline.Node det = pipeline.yoloDetector("det", model, CvedixLibrary.YOLO11, labels);
            CvedixPipeline.Node trk = pipeline.sortTracker("trk");
            CvedixPipeline.Node app = pipeline.appDestination("app", 0, (frame, dets) -> {
                if (frame.frameIndex() % 30 != 0) return; // log ~1 lần/giây
                System.out.printf("frame %d: %d object(s)%n", frame.frameIndex(), dets.size());
                dets.forEach(d -> System.out.println("  " + d));
            });

            det.attachTo(src);
            trk.attachTo(det);
            app.attachTo(trk);

            pipeline.start(src);
            System.out.println("pipeline running — press Enter to stop");
            System.in.read();
        } // close() dừng source, tháo pipeline, giải phóng handle
    }
}
