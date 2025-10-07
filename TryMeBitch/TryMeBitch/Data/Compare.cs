using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using static System.Net.Mime.MediaTypeNames;
using TryMeBitch.Models;

namespace TryMeBitch.Data
{
    public class FrameDiffHeatmapWithAreas
    {
        private readonly MRTDbContext _db;
        public FrameDiffHeatmapWithAreas(MRTDbContext db)
        {
            _db = db;
        }
        public static AreaMetadata LoadMetadata(string metadataPath)
        {
            string json = File.ReadAllText(metadataPath);
            return JsonSerializer.Deserialize<AreaMetadata>(json);
        }

        public void GenerateFrameDiffHeatmap(
            string inputVideo,
            string outputVideo,
            string metadataPath,
            string ffmpegPath = "ffmpeg.exe")
        {
            var metadata = LoadMetadata(metadataPath);
            bool flag = true;
            string tempDir = "TempDiffFrames";
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            using var capture = new VideoCapture(inputVideo);
            if (!capture.IsOpened)
            {
                Console.WriteLine("Cannot open video: " + inputVideo);
                return;
            }

            double fps = capture.Get(CapProp.Fps);
            if (fps <= 0) fps = 25;

            Mat prevColor = capture.QueryFrame();
            if (prevColor == null || prevColor.IsEmpty)
            {
                Console.WriteLine("No frames to read.");
                return;
            }
            Mat prevGray = new Mat();
            CvInvoke.CvtColor(prevColor, prevGray, ColorConversion.Bgr2Gray);

            int frameIndex = 1;
            Console.WriteLine("Processing...");

            while (true)
            {
                Mat currColor = capture.QueryFrame();
                if (currColor == null || currColor.IsEmpty)
                    break;

                frameIndex++;
                Console.WriteLine($" Frame {frameIndex}");

                Mat currGray = new Mat();
                CvInvoke.CvtColor(currColor, currGray, ColorConversion.Bgr2Gray);

                Mat diff = new Mat();
                CvInvoke.AbsDiff(prevGray, currGray, diff);
                CvInvoke.GaussianBlur(diff, diff, new Size(3, 3), 0);

                Mat overlay = currColor.Clone();
                
                foreach (var area in metadata.Areas)
                {
                    Mat areaMask = Mat.Zeros(diff.Rows, diff.Cols, Emgu.CV.CvEnum.DepthType.Cv8U, 1);

                    if (area.Shape == "rectangle")
                    {
                        CvInvoke.Rectangle(areaMask,
                            new Rectangle(area.X, area.Y, area.Width, area.Height),
                            new MCvScalar(255), -1);
                    }
                    else if (area.Shape == "polygon" && area.Points != null)
                    {
                        var pts = new List<Point>();
                        foreach (var p in area.Points)
                            pts.Add(new Point(p[0], p[1]));

                        using (var vvp = new Emgu.CV.Util.VectorOfVectorOfPoint())
                        using (var vp = new Emgu.CV.Util.VectorOfPoint(pts.ToArray()))
                        {
                            vvp.Push(vp);
                            CvInvoke.FillPoly(areaMask, vvp, new MCvScalar(255));
                        }

                        Mat areaDiff = new Mat();
                        CvInvoke.BitwiseAnd(diff, diff, areaDiff, areaMask);

                        Mat thresh = new Mat();
                        CvInvoke.Threshold(areaDiff, thresh, area.Threshold, 255, ThresholdType.Binary);

                        Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
                        CvInvoke.MorphologyEx(thresh, thresh, MorphOp.Close, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

                        int markedPixels = CvInvoke.CountNonZero(thresh);
                        int totalPixels = CvInvoke.CountNonZero(areaMask);
                        double motionCoverage = (double)markedPixels / totalPixels;

                        // Normalize + color map
                        CvInvoke.Normalize(thresh, thresh, 0.5, 255, NormType.MinMax);
                        Mat heat = new Mat();
                        CvInvoke.ApplyColorMap(thresh, heat, ColorMapType.Jet);

                        Mat maskedHeat = new Mat();
                        heat.CopyTo(maskedHeat, areaMask);

                        CvInvoke.AddWeighted(overlay, 1.0, maskedHeat, 0.8, 0.9, overlay);

                        // Draw the polygon outline
                        using (var vp2 = new Emgu.CV.Util.VectorOfPoint(pts.ToArray()))
                        {
                            CvInvoke.Polylines(overlay, vp2, true, new MCvScalar(0, 255, 0), 2);
                        }

                        // Put text inside polygon - at first point plus offset
                        CvInvoke.PutText(overlay,
                            $"{area.Name}: {motionCoverage:P1}",
                            new Point(pts[0].X + 5, pts[0].Y + 20),
                            FontFace.HersheySimplex, 0.6,
                            new Bgr(255, 255, 255).MCvScalar, 2);

                        if (motionCoverage > area.Threshold && flag)
                        {
                            flag = false;
                            
                            if (inputVideo.Contains("K4.mp4"))
                            {
                                _db.Issues.Add(new Issues
                                {
                                    id = new Guid(),
                                    Author = "CCTV Station",
                                    station = "Clementi",
                                    title = "Overcrowded Surge",
                                    summary = $"A Overcrowdedness surge has been detected at Clementi area {area.Name}. Please Prepare Manpower. Adjacent Stations please assist.",
                                    Severity = "High",
                                    status = "Open",
                                    timestamp = DateTime.Now
                                });
                                _db.SaveChangesAsync();
                                var msg = new Models.TwilioService();
                                msg.Alert($"Overcrowded Surge @ Clementi: area '{area.Name}'. Requesting Personnel.", "1234679");

                            }
                            if (inputVideo.Contains("K6.mp4"))
                            {
                                _db.Issues.Add(new Issues
                                {
                                    id = new Guid(),
                                    Author = "CCTV Station",
                                    station = "Clementi",
                                    title = "Commuter has been detected on the tracks",
                                    summary = $"A Commuter has been detected on the tracks @ {area.Name}. Requesting Immediate Personnel ",
                                    Severity = "Critical",
                                    status = "Open",
                                    timestamp = DateTime.Now
                                });
                                _db.SaveChangesAsync();
                                var msg = new Models.TwilioService();
                                msg.Alert($"Commuter On Track @ Clementi: area '{area.Name}'. Requesting Immediate Personnel.", "123456789");
                            }
                               
                           
                        }
                        areaMask.Dispose();
                        areaDiff.Dispose();
                        thresh.Dispose();
                        heat.Dispose();
                        maskedHeat.Dispose();
                    }

                }

                string fname = Path.Combine(tempDir, $"frame_{frameIndex:D5}.png");
                CvInvoke.Imwrite(fname, overlay);

                prevGray.Dispose();
                prevGray = currGray;
                currColor.Dispose();
            }

            Console.WriteLine("Creating output video...");
            string args = $"-framerate {fps} -i \"{tempDir}/frame_%05d.png\" -c:v libx264 -pix_fmt yuv420p \"{outputVideo}\"";
            var psi = new ProcessStartInfo(ffmpegPath, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            Console.WriteLine(proc.ExitCode == 0 ? "Done." : $"FFmpeg error {proc.ExitCode}");
            Directory.Delete(tempDir, true);
        }

        public class AreaMetadata
        {
            public List<Area> Areas { get; set; }
        }

        public class Area
        {
            public string Name { get; set; }
            public string Shape { get; set; } // "rectangle" or "polygon"
            public int X { get; set; }        // for rectangle
            public int Y { get; set; }        // for rectangle
            public int Width { get; set; }    // for rectangle
            public int Height { get; set; }   // for rectangle
            public List<List<int>> Points { get; set; } // for polygon
            public double Threshold { get; set; }
        }
        public void PlatformProcess(string inputVid, string outputVid, string metadataFile, string ffmpeg = "ffmpeg.exe")
        {
            if (File.Exists(outputVid))
            {
                Console.WriteLine("Output already exists. Skipping.");
                return;
            }

            if (!File.Exists(ffmpeg))
            {
                Console.WriteLine("ffmpeg.exe not found at " + ffmpeg);
                return;
            }

            GenerateFrameDiffHeatmap(inputVid, outputVid, metadataFile, ffmpeg);
        }
    }
}
