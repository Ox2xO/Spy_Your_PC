using System.Globalization;
using BotController1.Telegram;

namespace BotController1;

internal static class Program
{
    public const string BotToken = "BOT TOKAN";
    public static readonly long[] AllowedChatIds = { ChatID_L };
    public const int MaxImages = 20;
    public const int MaxVideos = 10;
    public const int MaxImageSizeMb = 10;
    public const int MaxVideoSizeMb = 100;

    private static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        Console.WriteLine($"Bot controller starting at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        if (AllowedChatIds.Length == 0 || AllowedChatIds[0] == 123456789)
            Console.WriteLine("⚠️ WARNING: No valid chat IDs configured. Update the ALLOWED_CHAT_IDS in the script.");

        while (true)
        {
            try
            {
                Console.WriteLine("Bot is now running. Press Ctrl+C to stop.");
                var router = new Router(new TelegramClient(BotToken));
                router.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in bot polling: {e}");
                Thread.Sleep(10000);
            }
        }
    }
}
