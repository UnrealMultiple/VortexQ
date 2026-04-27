using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Vortex.Bot.Utility;

public sealed class SystemMonitor : IDisposable
{
    #region 基础字段和属性
    private Timer _timer;
    private NetworkUsageData _prevNetworkData;
    private CpuUsageData _prevCpuData;
    private long _prevNetworkTimestamp;

    public float CpuUsagePercent { get; private set; }
    public float MemoryUsagePercent { get; private set; }
    public long TotalPhysicalMemory { get; private set; }
    public long UsedPhysicalMemory { get; private set; }
    public long AvailablePhysicalMemory { get; private set; }
    public float NetworkUploadKbps { get; private set; }
    public float NetworkDownloadKbps { get; private set; }
    #endregion

    #region 初始化与核心监控逻辑
    public SystemMonitor(int updateIntervalMs = 1000)
    {
        InitializeMemory();
        _prevCpuData = GetCpuData();
        _prevNetworkTimestamp = Stopwatch.GetTimestamp();
        _prevNetworkData = new NetworkUsageData();
        _timer = new Timer(UpdateMetrics, null, 0, updateIntervalMs);
    }

    private void UpdateMetrics(object? state)
    {
        try
        {
            UpdateCpuUsage();
            UpdateMemoryUsage();
            UpdateNetworkUsage();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[监控错误] {ex.Message}");
        }
    }
    #endregion

    #region CPU监控实现
    private struct CpuUsageData
    {
        public ulong TotalTime;
        public ulong IdleTime;
    }

    private void UpdateCpuUsage()
    {
        var current = GetCpuData();
        var totalDiff = current.TotalTime - _prevCpuData.TotalTime;
        var idleDiff = current.IdleTime - _prevCpuData.IdleTime;

        if (totalDiff > 0 && idleDiff <= totalDiff)
        {
            CpuUsagePercent = (float)((totalDiff - idleDiff) * 100.0 / totalDiff);
            CpuUsagePercent = Math.Clamp(CpuUsagePercent, 0, 100);
        }
        _prevCpuData = current;
    }

    private CpuUsageData GetCpuData()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return GetWindowsCpuData();
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? GetLinuxCpuData()
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? GetMacCpuData() : throw new PlatformNotSupportedException();
    }

    [StructLayout(LayoutKind.Sequential)]
    struct FILETIME { public uint dwLowDateTime; public uint dwHighDateTime; }

    [DllImport("kernel32.dll")]
    static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);

    private static CpuUsageData GetWindowsCpuData()
    {
        GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);
        return new CpuUsageData
        {
            IdleTime = ((ulong)idle.dwHighDateTime << 32) | idle.dwLowDateTime,
            TotalTime = (((ulong)kernel.dwHighDateTime << 32) | kernel.dwLowDateTime) +
                       (((ulong)user.dwHighDateTime << 32) | user.dwLowDateTime)
        };
    }

    private static CpuUsageData GetLinuxCpuData()
    {
        var lines = File.ReadAllLines("/proc/stat");
        var values = lines.First(l => l.StartsWith("cpu ")).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return new CpuUsageData
        {
            TotalTime = values.Skip(1).Take(3).Select(ulong.Parse).Aggregate((a, b) => a + b),
            IdleTime = ulong.Parse(values[4])
        };
    }

    [DllImport("libSystem.dylib")]
    static extern int sysctlbyname(string name, IntPtr ptr, ref uint size, IntPtr newp, uint newlen);

    private static CpuUsageData GetMacCpuData()
    {
        const string name = "kern.cp_time";
        var size = (uint)Marshal.SizeOf<CpuUsageData>();
        var ptr = Marshal.AllocHGlobal((int)size);
        try
        {
            return sysctlbyname(name, ptr, ref size, IntPtr.Zero, 0) == 0
                ? Marshal.PtrToStructure<CpuUsageData>(ptr)
                : throw new Win32Exception();
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }
    #endregion

    #region 内存监控实现
    private void InitializeMemory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) InitWindowsMemory();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) InitLinuxMemory();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) InitMacMemory();
    }

    private void UpdateMemoryUsage()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) UpdateWindowsMemory();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) UpdateLinuxMemory();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) UpdateMacMemory();

        UsedPhysicalMemory = TotalPhysicalMemory - AvailablePhysicalMemory;
        if (TotalPhysicalMemory > 0)
            MemoryUsagePercent = (float)(UsedPhysicalMemory * 100.0 / TotalPhysicalMemory);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
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
        public MEMORYSTATUSEX() => dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    private void InitWindowsMemory()
    {
        var memStatus = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(memStatus))
        {
            TotalPhysicalMemory = (long)memStatus.ullTotalPhys;
            AvailablePhysicalMemory = (long)memStatus.ullAvailPhys;
        }
    }

    private void UpdateWindowsMemory()
    {
        var memStatus = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(memStatus))
            AvailablePhysicalMemory = (long)memStatus.ullAvailPhys;
    }

    private void InitLinuxMemory()
    {
        try
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            TotalPhysicalMemory = GetMemValue(lines, "MemTotal") * 1024;
            AvailablePhysicalMemory = GetMemValue(lines, "MemAvailable") * 1024;
        }
        catch (Exception ex) { Debug.WriteLine($"[Linux内存初始化错误] {ex.Message}"); }
    }

    private void UpdateLinuxMemory()
    {
        try
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            var memAvailable = lines.FirstOrDefault(l => l.StartsWith("MemAvailable"));
            if (memAvailable != null)
            {
                AvailablePhysicalMemory = GetMemValue(lines, "MemAvailable") * 1024;
            }
            else
            {
                AvailablePhysicalMemory = (GetMemValue(lines, "MemFree") +
                                         GetMemValue(lines, "Buffers") +
                                         GetMemValue(lines, "Cached")) * 1024;
            }
        }
        catch (Exception ex) { Debug.WriteLine($"[Linux内存更新错误] {ex.Message}"); }
    }

    [DllImport("libSystem.dylib")]
    static extern int sysctlbyname(string name, out long value, ref IntPtr size, IntPtr newp, uint newlen);

    private void InitMacMemory()
    {
        try
        {
            IntPtr len = sizeof(long);
            if (sysctlbyname("hw.memsize", out long total, ref len, IntPtr.Zero, 0) == 0)
                TotalPhysicalMemory = total;
            UpdateMacMemory();
        }
        catch (Exception ex) { Debug.WriteLine($"[macOS内存初始化错误] {ex.Message}"); }
    }

    private void UpdateMacMemory()
    {
        try
        {
            var psi = new ProcessStartInfo("vm_stat") { RedirectStandardOutput = true };
            using var process = Process.Start(psi);
            var output = process?.StandardOutput.ReadToEnd();
            process?.WaitForExit();

            if (string.IsNullOrEmpty(output)) return;

            var lines = output.Split('\n');
            var free = GetMacMemValue(lines, "Pages free");
            var speculative = GetMacMemValue(lines, "Pages speculative");
            var pageSize = GetPageSize();
            AvailablePhysicalMemory = (free + speculative) * pageSize;
        }
        catch (Exception ex) { Debug.WriteLine($"[macOS内存更新错误] {ex.Message}"); }
    }

    private static long GetMemValue(string[] lines, string key)
    {
        var line = lines.FirstOrDefault(l => l.StartsWith(key));
        if (line == null) return 0;

        var parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length < 2 ? 0 : long.TryParse(parts[1], out long value) ? value : 0;
    }

    private static long GetMacMemValue(string[] lines, string key)
    {
        var line = lines.FirstOrDefault(l => l.Contains(key));
        if (line == null) return 0;

        var parts = line.Split(':');
        if (parts.Length < 2) return 0;

        var valueStr = parts[1].Trim().Split('.')[0];
        return long.TryParse(valueStr, out long value) ? value : 0;
    }

    private static long GetPageSize()
    {
        IntPtr len = sizeof(long);
        _ = sysctlbyname("hw.pagesize", out long size, ref len, IntPtr.Zero, 0);
        return size;
    }
    #endregion

    #region 网络监控实现
    private struct NetworkUsageData { public long ReceivedBytes; public long SentBytes; }

    private void UpdateNetworkUsage()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => !n.Description.Contains("Virtual"))
                .Where(n => !n.Description.Contains("Pseudo"))
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            var current = new NetworkUsageData
            {
                ReceivedBytes = interfaces.Sum(n => n.GetIPStatistics().BytesReceived),
                SentBytes = interfaces.Sum(n => n.GetIPStatistics().BytesSent)
            };

            var timestamp = Stopwatch.GetTimestamp();
            var elapsed = (timestamp - _prevNetworkTimestamp) / (double)Stopwatch.Frequency;

            if (elapsed > 0.5 && _prevNetworkData.ReceivedBytes != 0)
            {
                NetworkDownloadKbps = (float)((current.ReceivedBytes - _prevNetworkData.ReceivedBytes) / elapsed / 1024);
                NetworkUploadKbps = (float)((current.SentBytes - _prevNetworkData.SentBytes) / elapsed / 1024);
            }

            _prevNetworkData = current;
            _prevNetworkTimestamp = timestamp;
        }
        catch (Exception ex) { Debug.WriteLine($"[网络监控错误] {ex.Message}"); }
    }
    #endregion

    #region 资源清理
    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
    #endregion
}
