namespace BotController1.Core;

public static class BotState
{
    public static volatile bool IsRunning;
    public static volatile string CurrentTask;
    public static SystemInfoData SystemInfoCache;
}
