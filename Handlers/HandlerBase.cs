using BotController1.Telegram;

namespace BotController1.Handlers;

public abstract class HandlerBase
{
    protected readonly TelegramClient Tg;

    protected HandlerBase(TelegramClient tg) => Tg = tg;

    protected bool Authorize(TgMessage message, bool logIncident = false)
    {
        if (Array.IndexOf(Program.AllowedChatIds, message.ChatId) >= 0)
            return true;
        Tg.ReplyTo(message, logIncident
            ? "⛔ Access denied. This incident has been logged."
            : "⛔ Access denied.");
        if (logIncident)
            Console.WriteLine($"Unauthorized access attempt from chat ID: {message.ChatId}");
        return false;
    }

    protected static string Arg(string text)
    {
        var idx = text.IndexOf(' ');
        if (idx < 0)
            throw new IndexOutOfRangeException("list index out of range");
        return text.Substring(idx + 1);
    }

    protected static string BrowserFrom(string text, string prefix) =>
        text.Substring(1).Replace(prefix, "").Replace("_", "");

    protected static string Title(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
}
