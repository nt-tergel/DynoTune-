namespace DynoTune.Models;

public class LogRecord
{
    public DateTime Timestamp { get; set; }
    public string ActiveProfile { get; set; } = "Default";

    public double CpuUsagePercent { get; set; }
    public double? CpuTemperatureC { get; set; }
    public double CpuClockMHz { get; set; }
    public double? CpuPowerW { get; set; }

    public string GpuName { get; set; } = string.Empty;
    public double GpuUsagePercent { get; set; }
    public double GpuTemperatureC { get; set; }
    public double GpuCoreClockMHz { get; set; }
    public double GpuMemoryClockMHz { get; set; }
    public double GpuPowerW { get; set; }
    public int GpuFanRpm { get; set; }

    public double MemoryUsedGB { get; set; }
    public double MemoryTotalGB { get; set; }
    public double? SystemPowerW { get; set; }
    public double? AmbientTemperatureC { get; set; }
}
