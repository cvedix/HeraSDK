using Cvedix.Sdk;

// C# binding sample: File → YOLO → SORT tracker → App callback.
//
// Run:
//   export LD_LIBRARY_PATH=$CVEDIX_SDK_DIR/lib/x86_64:$LD_LIBRARY_PATH
//   dotnet run -- video.mp4 model.onnx labels.txt

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: PipelineSample <video.mp4> <model.onnx> [labels.txt]");
    return 1;
}
string video = args[0];
string model = args[1];
string? labels = args.Length > 2 ? args[2] : null;

Console.WriteLine($"cvedix core version: {CvedixPipeline.CoreVersion}");

using var pipeline = new CvedixPipeline();

var src = pipeline.FileSource("src", 0, video, 0.5f, loop: true);
var det = pipeline.YoloDetector("det", model, NativeMethods.Yolo11, labels);
var trk = pipeline.SortTracker("trk");
var app = pipeline.AppDestination("app", 0, (frame, dets) =>
{
    if (frame.FrameIndex % 30 != 0) return; // log ~1 lần/giây
    Console.WriteLine($"frame {frame.FrameIndex}: {dets.Count} object(s)");
    foreach (var d in dets) Console.WriteLine($"  {d}");
});

det.AttachTo(src);
trk.AttachTo(det);
app.AttachTo(trk);

pipeline.Start(src);
Console.WriteLine("pipeline running — press Enter to stop");
Console.ReadLine();
return 0;
