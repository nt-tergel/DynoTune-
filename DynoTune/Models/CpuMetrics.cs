namespace DynoTune.Models;

public class CpuMetrics
{
    public double UsagePercent { get; set; }
    public double TemperatureC { get; set; }
    public double ClockMHz { get; set; }
    public double PowerW { get; set; }
    public double? PackagePowerW { get; set; }
    public bool IsThermallyThrottling { get; set; }
    public bool IsPowerThrottling { get; set; }
}