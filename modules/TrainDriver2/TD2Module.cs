using System.Drawing;
using System.Text.RegularExpressions;
using Tesseract;

namespace HaslerConnect.TrainDriver2;

public class TD2Module : GameModule
{
    private TesseractEngine engine;
    private int[] imageDimensions = [497, 0, 429, 31]; // start X, start Y, width, height
    private int lastSpeed = 0;

    Bitmap CaptureScreenRegion(int x, int y, int width, int height)
    {
        Bitmap bmp = new Bitmap(width, height);
        Graphics g = Graphics.FromImage(bmp);
        g.CopyFromScreen(x, y, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
        return bmp;
    }
    
    string CleanText(string rawText) 
        => rawText.Replace("O", "0")  // Fix common zero misrecognition
            .Replace(" ", "");  // Remove spaces
    
    string ExtractSpeed(string cleanedText) 
    {
        var match = Regex.Match(cleanedText, @"(\d+)km/h");
        return match.Success ? match.Groups[1].Value : "0";
    }
    
    public override void Initialize()
    {
        engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
    }
    
    public override bool ReadyForRead()
    {
        // Actually moved to GetSpeed method, to prevent double image takes
        return true;
    }
    
    public override int GetSpeed()
    {
        Bitmap screenshot = CaptureScreenRegion(imageDimensions[0], imageDimensions[1], imageDimensions[2], imageDimensions[3]);
        var page = engine.Process(screenshot);
        string result = page.GetText();
        page.Dispose();
        screenshot.Dispose();
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
                Console.WriteLine("RETURNING LAST SPEED!");
                return lastSpeed;
            }
        }
        
        return 0;
    }

    public override void Tick()
    {
        
    }
}