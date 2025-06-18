
using Azure;
using Azure.AI.Vision.Face;
using DotNetEnv;

using Microsoft.Extensions.Configuration;

using SkiaSharp;
using System.Drawing;
using System.Drawing.Imaging;

DotNetEnv.Env.Load();

string aiSvcKey = Environment.GetEnvironmentVariable("AIServicesKey");
string aiSvcEndpoint = Environment.GetEnvironmentVariable("AIServicesEndpoint");


string imageFile = "images/faces4.jpg";
if (args.Length > 0)
{
    imageFile = args[0];
}

FaceClient faceClient = new FaceClient(


    new Uri(aiSvcEndpoint),
    new AzureKeyCredential(aiSvcKey));

// Specify facial features to be retrieved
FaceAttributeType[] features = new FaceAttributeType[]
{
    FaceAttributeType.Detection01.HeadPose,
    FaceAttributeType.Detection01.Occlusion,
    FaceAttributeType.Detection01.Accessories
};

// Use client to detect faces in an image
// Get faces
using (var imageData = File.OpenRead(imageFile))
{
    var response = faceClient.Detect(
        BinaryData.FromStream(imageData),
        FaceDetectionModel.Detection01,
        FaceRecognitionModel.Recognition01,
        returnFaceId: false,
        returnFaceAttributes: features);

    IReadOnlyList<FaceDetectionResult> detectedFaces = response.Value;

    if (detectedFaces.Count() > 0)
    {
        Console.WriteLine($"{detectedFaces.Count()} faces detected.");

        int faceCount = 0;
        foreach (var face in detectedFaces)
        {
            faceCount++;
            Console.WriteLine($"\nFace number {faceCount}");

            // Get face properties
            Console.WriteLine($" - Head Pose (Yaw): {face.FaceAttributes.HeadPose.Yaw}");
            Console.WriteLine($" - Head Pose (Pitch): {face.FaceAttributes.HeadPose.Pitch}");
            Console.WriteLine($" - Head Pose (Roll): {face.FaceAttributes.HeadPose.Roll}");
            Console.WriteLine($" - Forehead occluded: {face.FaceAttributes.Occlusion.ForeheadOccluded}");
            Console.WriteLine($" - Eye occluded: {face.FaceAttributes.Occlusion.EyeOccluded}");
            Console.WriteLine($" - Mouth occluded: {face.FaceAttributes.Occlusion.MouthOccluded}");
            Console.WriteLine($" - Accessories:");
            foreach (AccessoryItem accessory in face.FaceAttributes.Accessories)
            {
                Console.WriteLine($"   - {accessory.Type}");
            }
        }
        // Annotate faces in the image
        AnnotateFaces(imageFile, detectedFaces);

    }

}
// Annotate faces in the image
void AnnotateFaces(string imageFile, IReadOnlyList<FaceDetectionResult> faces)
{
    using var bitmap = SKBitmap.Decode(imageFile);
    using var canvas = new SKCanvas(bitmap);

    var paint = new SKPaint
    {
        Color = SKColors.Red,
        StrokeWidth = 3,
        Style = SKPaintStyle.Stroke
    };

    var textPaint = new SKPaint
    {
        Color = SKColors.Red,
        TextSize = 24,
        IsAntialias = true
    };

    foreach (var face in faces)
    {
        var rect = ToSKRect(face.FaceRectangle);
        canvas.DrawRect(rect, paint);
        canvas.DrawText($"Yaw: {face.FaceAttributes.HeadPose.Yaw}", rect.Left, rect.Top - 10, textPaint);
    }

    Directory.CreateDirectory("output");
    string outputFile = Path.Combine("output", Path.GetFileName(imageFile));
    using var image = File.OpenWrite(outputFile);
    bitmap.Encode(image, SKEncodedImageFormat.Jpeg, 100);
    Console.WriteLine($"Annotated image saved to {outputFile}");
}

SKRect ToSKRect(FaceRectangle rect)
{
    return new SKRect(rect.Left, rect.Top, rect.Left + rect.Width, rect.Top + rect.Height);
}