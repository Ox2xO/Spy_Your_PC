using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BotController1.Telegram;

public class TelegramClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(180) };
    private static readonly Dictionary<string, string> Mime = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".mp4"] = "video/mp4",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime",
        [".wmv"] = "video/x-ms-wmv",
        [".mkv"] = "video/x-matroska",
        [".flv"] = "video/x-flv",
    };

    private readonly string _token;

    public TelegramClient(string token) => _token = token;

    private string Url(string method) => $"https://api.telegram.org/bot{_token}/{method}";

    private static void EnsureOk(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.GetProperty("ok").GetBoolean())
            throw new InvalidOperationException(
                doc.RootElement.TryGetProperty("description", out var d) ? d.GetString() : body);
    }

    public JsonElement GetUpdates(long offset)
    {
        var body = Http.GetStringAsync(Url($"getUpdates?offset={offset}&timeout=10"))
            .GetAwaiter().GetResult();
        EnsureOk(body);
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("result").Clone();
    }

    public void SendMessage(long chatId, string text, string parseMode = null, int? replyTo = null)
    {
        var data = new Dictionary<string, string> { ["chat_id"] = chatId.ToString(), ["text"] = text };
        if (parseMode != null)
            data["parse_mode"] = parseMode;
        if (replyTo.HasValue)
            data["reply_to_message_id"] = replyTo.Value.ToString();
        var body = Http.PostAsync(Url("sendMessage"), new FormUrlEncodedContent(data))
            .GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
        EnsureOk(body);
    }

    public void ReplyTo(TgMessage message, string text, string parseMode = null) =>
        SendMessage(message.ChatId, text, parseMode, message.MessageId);

    public void SendPhoto(long chatId, string filePath, string caption = null, int? replyTo = null) =>
        SendFile("photo", "sendPhoto", chatId, filePath, caption, replyTo);

    public void SendDocument(long chatId, string filePath, string caption = null, int? replyTo = null) =>
        SendFile("document", "sendDocument", chatId, filePath, caption, replyTo);

    private void SendFile(string field, string method, long chatId, string filePath,
        string caption, int? replyTo)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(chatId.ToString()), "chat_id");
        if (caption != null)
            form.Add(new StringContent(caption), "caption");
        if (replyTo.HasValue)
            form.Add(new StringContent(replyTo.Value.ToString()), "reply_to_message_id");

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var streamContent = new StreamContent(stream);
        var ext = Path.GetExtension(filePath);
        streamContent.Headers.ContentType =
            new MediaTypeHeaderValue(Mime.TryGetValue(ext, out var mime) ? mime : "application/octet-stream");
        form.Add(streamContent, field, Path.GetFileName(filePath));

        var body = Http.PostAsync(Url(method), form)
            .GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
        EnsureOk(body);
    }
}
