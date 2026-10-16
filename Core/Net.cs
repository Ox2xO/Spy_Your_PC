using System.Text.Json;

namespace BotController1.Core;

public static class Net
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(120) };

    public static string GetString(string url)
    {
        var resp = Client.GetAsync(url).GetAwaiter().GetResult();
        return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    public static JsonElement GetJson(string url)
    {
        using var doc = JsonDocument.Parse(GetString(url));
        return doc.RootElement.Clone();
    }
}
