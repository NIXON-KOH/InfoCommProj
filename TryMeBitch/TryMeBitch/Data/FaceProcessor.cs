using Emgu.CV;
using Emgu.CV.Dnn;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System;
using TryMeBitch.Models;
using System.Drawing;
using System.IO;
using Emgu.CV.Util;
using System.Diagnostics;
using System.Text.Json;
using System.Net.Http.Json;

namespace TryMeBitch.Data
{
    public class FaceProcessor
    {
        private double RECOGNITION_THRESHOLD = 3850;

        private readonly string _modelPath;
        private readonly string _configPath;
        private readonly string _wantedFacePath;
        private readonly MRTDbContext _db;
        

        public FaceProcessor(string modelPath, string configPath, string wantedFacePath, MRTDbContext db)
        {
            _modelPath = modelPath;
            _configPath = configPath;
            _wantedFacePath = wantedFacePath;
            _db = db;
        }
        private async void ExtractAndSaveWantedFace(Image<Bgr, byte> wantedImage, Rectangle faceRect)
        {
            var apiKey = "xxx";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

            using var http = new HttpClient();

            // Fallback: full image if no faceRect
            if (faceRect == Rectangle.Empty)
                faceRect = new Rectangle(0, 0, wantedImage.Width, wantedImage.Height);

            var grayFace = wantedImage.Copy(faceRect).Resize(100, 100, Inter.Cubic);

            string outputPath = Path.Combine(AppContext.BaseDirectory, "Wanted.png");
            grayFace.Save(outputPath); // overwrite by default

            Debug.WriteLine($"Saved wanted face to: {outputPath}");

            byte[] imageBytes = File.ReadAllBytes(outputPath);
            string base64 = Convert.ToBase64String(imageBytes);

            var requestBody = new
                                {
                                    contents = new[]
                          {
                            new {
                                parts = new object[]
                                {
                                    new { text =  "This image shows CCTV footage with a red square highlighting a person of interest. " +
                                                "Provide a precise, law-enforcement-style description of the person inside the red square, Keep it within 100 Characters" },
                                    new {
                                        inline_data = new {
                                            mime_type = "image/png",
                                            data = base64
                                        }
                                    }
                                }
                            }
                        }
                                };

            var response = await http.PostAsJsonAsync(url, requestBody);

            response.EnsureSuccessStatusCode();

            var resultJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Raw Response: " + resultJson);

            // Deserialize to get the text
            using var doc = JsonDocument.Parse(resultJson);
            var text = doc.RootElement
                          .GetProperty("candidates")[0]
                          .GetProperty("content")
                          .GetProperty("parts")[0]
                          .GetProperty("text")
                          .GetString();
            Debug.WriteLine("Generated description: " + text);
            _db.Issues.Add(new Issues
            {
                id = new Guid(),
                Author = "CCTV Station",
                station = "Clementi",
                title = "Wanted Person Detected",
                summary = text,
                Severity = "High",
                status = "Open",
                timestamp = DateTime.Now
            });
            await _db.SaveChangesAsync();
            var msg = new Models.TwilioService();
            msg.Alert($"Wanted Person Detected @ Clementi. Requesting Security.\n Description: {text}","12346789");
        }

        public void ProcessVideo(string inputVideoPath, string outputVideoPath)
        {
            if (File.Exists(outputVideoPath))
            {
                Console.WriteLine("Output already exists. Skipping.");
                return;
            }

            // Create unique temp path next to output
            string tempVideoPath = Path.Combine(Path.GetDirectoryName(outputVideoPath)!, $"temp_{Guid.NewGuid()}.avi");

            using var capture = new VideoCapture(inputVideoPath);
            if (!capture.IsOpened)
                throw new FileNotFoundException("Cannot open input video", inputVideoPath);

            var faceNet = DnnInvoke.ReadNetFromCaffe(_configPath, _modelPath);
            if (faceNet.Empty)
                throw new FileNotFoundException("Failed to load face detection model");

            var recognizer = TrainRecognizer(faceNet);

            int frameWidth = (int)capture.Get(CapProp.FrameWidth);
            int frameHeight = (int)capture.Get(CapProp.FrameHeight);
            double fps = capture.Get(CapProp.Fps);

            // === Write processed frames to temp AVI ===
            using (var writer = new VideoWriter(
                tempVideoPath,
                VideoWriter.Fourcc('M', 'J', 'P', 'G'),
                fps,
                new Size(frameWidth, frameHeight),
                true))
            {
                var frame = new Mat();
                while (true)
                {
                    capture.Read(frame);
                    if (frame.IsEmpty) break;

                    ProcessFrame(frame, faceNet, recognizer);
                    writer.Write(frame);
                }

                // Important: auto-dispose at end of using block
            }

            Console.WriteLine("Finished writing temp video: " + tempVideoPath);

            // === Transcode temp AVI to final MP4 ===
            try
            {
                var ffmpeg = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-y -i \"{tempVideoPath}\" -c:v libx264 -preset medium -c:a aac \"{outputVideoPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                ffmpeg.Start();

                // Non-blocking read for logs
                string error = ffmpeg.StandardError.ReadToEnd();
                string output = ffmpeg.StandardOutput.ReadToEnd();

                ffmpeg.WaitForExit();

                Console.WriteLine("FFmpeg stdout: " + output);
                Console.WriteLine("FFmpeg stderr: " + error);

                if (!File.Exists(outputVideoPath))
                    throw new Exception("FFmpeg failed: output file not created.");

                Console.WriteLine("FFmpeg transcoding done: " + outputVideoPath);
            }
            finally
            {
                // 🔑 Always delete temp even if FFmpeg failed
                if (File.Exists(tempVideoPath))
                {
                    File.Delete(tempVideoPath);
                    Console.WriteLine("Temp file deleted: " + tempVideoPath);
                }
            }
        }


        private EigenFaceRecognizer TrainRecognizer(Net faceNet)
        {
            var trainingImages = new VectorOfMat();
            var trainingLabels = new VectorOfInt();

            var wantedImage = new Image<Bgr, byte>(_wantedFacePath);

            var inputBlob = DnnInvoke.BlobFromImage(wantedImage.Mat, 1.0, new Size(300, 300), new MCvScalar(104, 177, 123), false, false);
            faceNet.SetInput(inputBlob);
            var detection = faceNet.Forward();
            var data = (float[,,,])detection.GetData();

            Rectangle faceRect = Rectangle.Empty;
            for (int i = 0; i < data.GetLength(2); i++)
            {
                float confidence = data[0, 0, i, 2];
                if (confidence > 0.5)
                {
                    int x1 = (int)(data[0, 0, i, 3] * wantedImage.Width);
                    int y1 = (int)(data[0, 0, i, 4] * wantedImage.Height);
                    int x2 = (int)(data[0, 0, i, 5] * wantedImage.Width);
                    int y2 = (int)(data[0, 0, i, 6] * wantedImage.Height);

                    faceRect = new Rectangle(new Point(x1, y1), new Size(x2 - x1, y2 - y1));
                    break;
                }
            }

            if (faceRect == Rectangle.Empty)
                faceRect = new Rectangle(0, 0, wantedImage.Width, wantedImage.Height);
            // Extract, save, and encode the wanted face
            ExtractAndSaveWantedFace(wantedImage, faceRect);

            var grayFace = wantedImage.Convert<Gray, byte>().Copy(faceRect).Resize(100, 100, Inter.Cubic);
            CvInvoke.EqualizeHist(grayFace, grayFace);

            trainingImages.Push(grayFace.Mat);
            trainingLabels.Push(new int[] { 1 });

            var brighter = grayFace.Clone();
            brighter._GammaCorrect(1.2);
            trainingImages.Push(brighter.Mat);
            trainingLabels.Push(new int[] { 1 });

            var darker = grayFace.Clone();
            darker._GammaCorrect(0.8);
            trainingImages.Push(darker.Mat);
            trainingLabels.Push(new int[] { 1 });

            var recognizer = new EigenFaceRecognizer(80, double.PositiveInfinity);
            recognizer.Train(trainingImages, trainingLabels);

            return recognizer;
        }

        private void ProcessFrame(Mat frame, Net faceNet, EigenFaceRecognizer recognizer)
        {
            var inputBlob = DnnInvoke.BlobFromImage(frame, 1.0, new Size(300, 300), new MCvScalar(104, 177, 123), false, false);
            faceNet.SetInput(inputBlob);
            var detection = faceNet.Forward();
            var data = (float[,,,])detection.GetData();

            int frameWidth = frame.Width;
            int frameHeight = frame.Height;

            for (int i = 0; i < data.GetLength(2); i++)
            {
                float confidence = data[0, 0, i, 2];
                if (confidence < 0.5) continue;

                int x1 = (int)(data[0, 0, i, 3] * frameWidth);
                int y1 = (int)(data[0, 0, i, 4] * frameHeight);
                int x2 = (int)(data[0, 0, i, 5] * frameWidth);
                int y2 = (int)(data[0, 0, i, 6] * frameHeight);

                var faceRect = new Rectangle(new Point(x1, y1), new Size(x2 - x1, y2 - y1));

                if (faceRect.Width <= 0 || faceRect.Height <= 0 ||
                    faceRect.X < 0 || faceRect.Y < 0 ||
                    faceRect.Right > frameWidth || faceRect.Bottom > frameHeight)
                    continue;

                using (var image = frame.ToImage<Bgr, byte>())
                {
                    var faceGray = image.Convert<Gray, byte>().Copy(faceRect).Resize(100, 100, Inter.Cubic);
                    CvInvoke.EqualizeHist(faceGray, faceGray);

                    var result = recognizer.Predict(faceGray.Mat);

                    bool isWanted = result.Label == 1 && result.Distance < RECOGNITION_THRESHOLD;
                    var color = isWanted ? new MCvScalar(0, 0, 255) : new MCvScalar(0, 255, 0);

                    CvInvoke.Rectangle(frame, faceRect, color, 2);

                    if (isWanted)
                    {
                        double confidencePercent = Math.Clamp(100 * (1 - result.Distance / RECOGNITION_THRESHOLD), 0, 100);
                        string text = $"WANTED ({confidencePercent:F1}%)";

                        CvInvoke.PutText(frame, text, new Point(faceRect.X, faceRect.Y - 10),
                            FontFace.HersheyComplex, 0.8, color, 2);
                    }
                }
            }
        }
    }
}
