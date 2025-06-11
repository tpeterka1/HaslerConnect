using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Tesseract;

namespace HaslerConnect.TrainDriver2;

public class TD2Module : GameModule
{
    private TesseractEngine? engine;
    private int[] imageDimensions = [497, 0, 429, 31]; // start X, start Y, width, height
    private int lastSpeed = 0;

    private static Bitmap CaptureScreenRegion(int x, int y, int width, int height)
    {
        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        Graphics g = Graphics.FromImage(bmp);
        g.CopyFromScreen(x, y, 0, 0, bmp.Size, CopyPixelOperation.SourceInvert);
        return bmp;
    }
    
    // Previous way (kinda good, but shit on blue backgrounds)
    //Bitmap ClearImage(Bitmap bmp)
    //{
    //    Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
    //    BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
    //
    //    int bytesPerPixel = 4;
    //    int byteCount = data.Stride * bmp.Height;
    //    byte[] pixels = new byte[byteCount];
    //    Marshal.Copy(data.Scan0, pixels, 0, byteCount);
    //
    //    for (int y = 0; y < bmp.Height; y++)
    //    {
    //        for (int x = 0; x < bmp.Width; x++)
    //        {
    //            int i = y * data.Stride + x * bytesPerPixel;
    //            byte b = pixels[i];
    //            byte g = pixels[i + 1];
    //            byte r = pixels[i + 2];
    //
    //            // Vivid blue threshold: adjust as needed
    //            if (r < 60 && g > 140 && b > 140)
    //            {
    //                // Set to black
    //                pixels[i] = 0;      // B
    //                pixels[i + 1] = 0;  // G
    //                pixels[i + 2] = 0;  // R
    //                pixels[i + 3] = 255;
    //            }
    //            else
    //            {
    //                // Set to white
    //                pixels[i] = 255;
    //                pixels[i + 1] = 255;
    //                pixels[i + 2] = 255;
    //                pixels[i + 3] = 255;
    //            }
    //        }
    //    }
    //
    //    Marshal.Copy(pixels, 0, data.Scan0, byteCount);
    //    bmp.UnlockBits(data);
    //
    //    bmp.Save("image.png", System.Drawing.Imaging.ImageFormat.Png); // Use PNG for lossless save
    //    return bmp;
    //}
    
    private static Bitmap RemoveSmallComponents(Bitmap bmp)
    {
        BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int bytesPerPixel = 4;
        int stride = data.Stride;
        int width = bmp.Width;
        int height = bmp.Height;
        byte[] pixels = new byte[stride * height];
        Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        int minSize = 20;

        bool[,] visited = new bool[width, height];
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * stride + x * bytesPerPixel;
                // Check for black pixel and not visited
                if (!visited[x, y] && pixels[i] == 0 && pixels[i + 1] == 0 && pixels[i + 2] == 0)
                {
                    // Flood fill to get component
                    Queue<(int, int)> q = new Queue<(int, int)>();
                    List<(int, int)> component = new List<(int, int)>();
                    q.Enqueue((x, y));
                    visited[x, y] = true;

                    while (q.Count > 0)
                    {
                        var (cx, cy) = q.Dequeue();
                        component.Add((cx, cy));
                        for (int dir = 0; dir < 4; dir++)
                        {
                            int nx = cx + dx[dir];
                            int ny = cy + dy[dir];
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                                !visited[nx, ny])
                            {
                                int ni = ny * stride + nx * bytesPerPixel;
                                if (pixels[ni] == 0 && pixels[ni + 1] == 0 && pixels[ni + 2] == 0)
                                {
                                    q.Enqueue((nx, ny));
                                    visited[nx, ny] = true;
                                }
                            }
                        }
                    }

                    // Remove small components
                    if (component.Count < minSize)
                    {
                        foreach (var (px, py) in component)
                        {
                            int pi = py * stride + px * bytesPerPixel;
                            pixels[pi] = 255;
                            pixels[pi + 1] = 255;
                            pixels[pi + 2] = 255;
                            pixels[pi + 3] = 255;
                        }
                    }
                }
            }
        }

        Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        bmp.UnlockBits(data);
        return bmp;
    }
    
    private static Bitmap ClearImage(Bitmap bmp32)
    {
        Rectangle rect = new Rectangle(0, 0, bmp32.Width, bmp32.Height);
        BitmapData data = bmp32.LockBits(rect, ImageLockMode.ReadWrite, bmp32.PixelFormat);

        int bytesPerPixel = 4;
        int byteCount = data.Stride * bmp32.Height;
        byte[] pixels = new byte[byteCount];
        Marshal.Copy(data.Scan0, pixels, 0, byteCount);

        // HSV threshold values for blue
        int hueMin = 170;
        int hueMax = 200;
        int satMin = 120;
        int valMin = 130;
        
        //int hueMin = 170;
        //int hueMax = 220;
        //int satMin = 80;
        //int valMin = 80;

        // Helper function to convert RGB to HSV
        (int H, int S, int V) RgbToHsv(byte r, byte g, byte b)
        {
            float rf = r / 255f;
            float gf = g / 255f;
            float bf = b / 255f;

            float max = Math.Max(rf, Math.Max(gf, bf));
            float min = Math.Min(rf, Math.Min(gf, bf));
            float delta = max - min;

            float h = 0f;
            if (delta == 0) h = 0;
            else if (max == rf) h = 60 * (((gf - bf) / delta) % 6);
            else if (max == gf) h = 60 * (((bf - rf) / delta) + 2);
            else if (max == bf) h = 60 * (((rf - gf) / delta) + 4);
            if (h < 0) h += 360;

            float s = (max == 0) ? 0 : (delta / max);
            float v = max;

            return ((int)h, (int)(s * 255), (int)(v * 255));
        }

        // First pass: threshold blue pixels
        for (int y = 0; y < bmp32.Height; y++)
        {
            for (int x = 0; x < bmp32.Width; x++)
            {
                int i = y * data.Stride + x * bytesPerPixel;
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];

                var (H, S, V) = RgbToHsv(r, g, b);

                if (H >= hueMin && H <= hueMax && S >= satMin && V >= valMin)
                {
                    // Blue text pixel: set black with full opacity
                    pixels[i] = 0;
                    pixels[i + 1] = 0;
                    pixels[i + 2] = 0;
                    pixels[i + 3] = 255;
                }
                else
                {
                    // Background pixel: set white with full opacity
                    pixels[i] = 255;
                    pixels[i + 1] = 255;
                    pixels[i + 2] = 255;
                    pixels[i + 3] = 255;
                }
            }
        }

        Marshal.Copy(pixels, 0, data.Scan0, byteCount);
        bmp32.UnlockBits(data);

        // Remove small pixels from the image (denoising)
        Bitmap processed = RemoveSmallComponents(bmp32);

        //processed.Save("image.png", System.Drawing.Imaging.ImageFormat.Png);
    
        return processed;
    }
    
    private static string CleanText(string rawText) 
        => rawText.Replace("O", "0")  // Fix common zero misrecognition
            .Replace("S", "5")
            .Replace("G", "6")
            .Replace(" ", "");  // Remove spaces
    
    private static string ExtractSpeed(string cleanedText) 
    {
        var match = Regex.Match(cleanedText, @"(\d+)km");
        return match.Success ? match.Groups[1].Value : "Nothing found";
    }
    
    public override void Initialize()
    {
        engine = new TesseractEngine(@".\tessdata", "engrestrict_best", EngineMode.Default);
    }
    
    public override bool ReadyForRead()
    {
        // Actually moved to GetSpeed method, to prevent double image takes
        return true;
    }
    
    public override int GetSpeed()
    {
        Bitmap screenshot = CaptureScreenRegion(imageDimensions[0], imageDimensions[1], imageDimensions[2], imageDimensions[3]);
        Bitmap filtered = ClearImage(screenshot);
        var page = engine.Process(filtered);
        screenshot.Dispose();
        filtered.Dispose();
        string result = page.GetText();
        page.Image.Dispose();
        page.Dispose();
        Console.WriteLine(result);
        if (result.Contains("Current speed"))
        {
            string cleaned = CleanText(result);
            string speedOcr = ExtractSpeed(cleaned);

            if (Int32.TryParse(speedOcr, out int speed))
            {
                lastSpeed = speed;
                return speed;
            }
            else
            {
                //Console.WriteLine("RETURNING LAST SPEED!");
                return lastSpeed;
            }
        }
        
        return 0;
    }

    public override void Tick()
    {
        
    }
}