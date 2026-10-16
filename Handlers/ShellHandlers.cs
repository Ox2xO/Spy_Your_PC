using System.Diagnostics;
using System.Text;
using BotController1.Core;
using BotController1.Telegram;

namespace BotController1.Handlers;

public class ShellHandlers : HandlerBase
{
    public ShellHandlers(TelegramClient tg) : base(tg)
    {
    }

    public void Pwd(TgMessage message)
    {
        if (!Authorize(message))
            return;
        Tg.SendMessage(message.ChatId, $"📂 Current directory: {Directory.GetCurrentDirectory()}");
    }

    public void Cmd(TgMessage message)
    {
        if (!Authorize(message))
            return;
        string command;
        try
        {
            command = Arg(message.Text);
        }
        catch (IndexOutOfRangeException)
        {
            Tg.SendMessage(message.ChatId, "❌ Please specify a command.");
            return;
        }
        var result = GetOutput(command);
        Tg.SendMessage(message.ChatId, $"💻 Command output:\n{result}");
    }

    public void OpenUrl(TgMessage message)
    {
        if (!Authorize(message))
            return;
        string url;
        try
        {
            url = Arg(message.Text);
        }
        catch (IndexOutOfRangeException)
        {
            Tg.SendMessage(message.ChatId, "❌ Please specify a URL.");
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
        }
        Tg.SendMessage(message.ChatId, $"🌐 Opened URL: {url}");
    }

    public void Ls(TgMessage message)
    {
        if (!Authorize(message))
            return;
        var entries = Directory.GetFileSystemEntries(Directory.GetCurrentDirectory())
            .Select(Path.GetFileName).ToArray();
        Tg.SendMessage(message.ChatId, $"📂 Files and folders:\n{string.Join("\n", entries)}");
    }

    public void Cd(TgMessage message)
    {
        if (!Authorize(message))
            return;
        try
        {
            Directory.SetCurrentDirectory(Arg(message.Text));
            Tg.SendMessage(message.ChatId, $"📂 Changed directory to: {Directory.GetCurrentDirectory()}");
        }
        catch (Exception e)
        {
            Tg.SendMessage(message.ChatId, $"❌ Error: {e.Message}");
        }
    }

    public void Download(TgMessage message)
    {
        if (!Authorize(message))
            return;
        try
        {
            var filePath = Arg(message.Text);
            Tg.SendDocument(message.ChatId, filePath);
        }
        catch (Exception e)
        {
            Tg.SendMessage(message.ChatId, $"❌ Error: {e.Message}");
        }
    }

    public void RmDir(TgMessage message)
    {
        if (!Authorize(message))
            return;
        try
        {
            var directory = Arg(message.Text);
            Directory.Delete(directory, true);
            Tg.SendMessage(message.ChatId, $"🗑️ Deleted folder: {directory}");
        }
        catch (Exception e)
        {
            Tg.SendMessage(message.ChatId, $"❌ Error: {e.Message}");
        }
    }

    public void KillProcess(TgMessage message)
    {
        if (!Authorize(message))
            return;
        string processName;
        try
        {
            processName = Arg(message.Text);
        }
        catch (IndexOutOfRangeException)
        {
            Tg.SendMessage(message.ChatId, "❌ Please specify a process name.");
            return;
        }
        RunSilent($"taskkill /f /im {processName}");
        Tg.SendMessage(message.ChatId, $"✅ Process {processName} killed.");
    }

    public void Reboot(TgMessage message)
    {
        if (!Authorize(message))
            return;
        RunSilent("shutdown /r /t 0");
        Tg.SendMessage(message.ChatId, "🔄 Rebooting system...");
    }

    public void Shutdown(TgMessage message)
    {
        if (!Authorize(message))
            return;
        RunSilent("shutdown /s /t 0");
        Tg.SendMessage(message.ChatId, "🛑 Shutting down system...");
    }

    private static void RunSilent(string command)
    {
        var psi = new ProcessStartInfo("cmd.exe", "/c " + command)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        try
        {
            using var p = Process.Start(psi);
            p?.WaitForExit();
        }
        catch
        {
        }
    }

    private static string GetOutput(string command)
    {
        var psi = new ProcessStartInfo("cmd.exe", "/c " + command)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        var sb = new StringBuilder();
        using var p = Process.Start(psi);
        if (p == null)
            return "";
        var stdoutDone = new ManualResetEvent(false);
        var stderrDone = new ManualResetEvent(false);
        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) stdoutDone.Set();
            else lock (sb) sb.AppendLine(e.Data);
        };
        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) stderrDone.Set();
            else lock (sb) sb.AppendLine(e.Data);
        };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();
        stdoutDone.WaitOne(1000);
        stderrDone.WaitOne(1000);
        var text = sb.ToString().Replace("\r\n", "\n");
        if (text.EndsWith("\n"))
            text = text.Substring(0, text.Length - 1);
        return text;
    }
}
