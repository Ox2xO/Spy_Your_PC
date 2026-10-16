using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;
using System.Windows.Forms;

namespace BotController1.Core;

public static class Capture
{
    public static string Screenshot()
    {
        try
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            using var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(bitmap))
                g.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
            var path = Path.Combine(Path.GetTempPath(),
                $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            bitmap.Save(path, ImageFormat.Png);
            return path;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error capturing screenshot: {e}");
            return null;
        }
    }

    public static string Webcam()
    {
        try
        {
            using var cap = new VideoCapture(0);
            if (!cap.IsOpened())
            {
                Console.WriteLine("Failed to open webcam");
                return null;
            }
            using var frame = new Mat();
            var ok = cap.Read(frame);
            if (!ok || frame.Empty())
            {
                Console.WriteLine("Failed to capture webcam image");
                return null;
            }
            var path = Path.Combine(Path.GetTempPath(),
                $"webcam_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
            Cv2.ImWrite(path, frame);
            return path;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error capturing webcam: {e}");
            return null;
        }
    }

    public static List<string> WebcamMulti(int count)
    {
        var files = new List<string>();
        try
        {
            using var cap = new VideoCapture(0);
            if (!cap.IsOpened())
            {
                Console.WriteLine("Failed to open webcam");
                return files;
            }
            for (var i = 0; i < count; i++)
            {
                Thread.Sleep(2000);
                using var frame = new Mat();
                var ok = cap.Read(frame);
                if (ok && !frame.Empty())
                {
                    var path = Path.Combine(Path.GetTempPath(),
                        $"webcam_{DateTime.Now:yyyyMMdd_HHmmss}_{i}.jpg");
                    Cv2.ImWrite(path, frame);
                    files.Add(path);
                }
                else
                {
                    Console.WriteLine($"Failed to capture webcam photo {i + 1}");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error capturing webcam photos: {e}");
        }
        return files;
    }
}
