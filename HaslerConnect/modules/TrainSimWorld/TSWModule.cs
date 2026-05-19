using System.Text.Json;

namespace HaslerConnect.modules.TrainSimWorld;

public class TSWModule : GameModule
{
    private HttpClient? client;
    private JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "get/CurrentDrivableActor.Function.HUD_GetSpeed");
    
    public override void Initialize()
    {
        string commKeyPath = @"%UserProfile%\Documents\My Games\TrainSimWorld6\Saved\Config\CommAPIKey.txt";
        if (!File.Exists(commKeyPath))
        {
            throw new FileNotFoundException("CommAPI Key not found! Be sure you have \"-HTTPAPI\" in the TSW launch arguments and you launched it at least once.");
        }
        string commKey = File.ReadAllText(commKeyPath);

        client = new HttpClient();
        client.DefaultRequestHeaders.Add("DTGCommKey", commKey);
        client.BaseAddress = new Uri("http://127.0.0.1:31270/");
    }

    public override bool ReadyForRead()
    {
        // Actually moved to GetSpeed method, to prevent double API requests
        return true;
    }

    public override int GetSpeed()
    {
        if (client == null)
        {
            throw new NullReferenceException("HttpClient is null! There was a problem somewhere!");
        }
        var response = client.Send(request);
        response.EnsureSuccessStatusCode();

        using var stream = response.Content.ReadAsStream();
        var deserializedResponse = JsonSerializer.Deserialize<JsonElement>(stream, jsonOptions);
        if (deserializedResponse.TryGetProperty("errorCode", out var errorCode))
        {
            throw new Exception("Exception when calling request: " + errorCode.ToString() + " : " + deserializedResponse.GetProperty("errorMessage").ToString());
        }

        if (deserializedResponse.TryGetProperty("Result", out var result))
        {
            if (result.ToString().Equals("Success"))
            {
                int.TryParse(deserializedResponse.GetProperty("Values").GetProperty("Speed (ms)").ToString(), out int speedMs);
                var speedKmh = (int)Math.Floor(speedMs * 3.6);
                //if (Program.Debug) Console.WriteLine(speedKMH);
                return speedKmh;
            }
        }
        
        return 0;
    }

    public override void Tick()
    {
        
    }
}