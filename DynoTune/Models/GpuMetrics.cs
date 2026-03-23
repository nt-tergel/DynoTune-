namespace DynoTune.Models;

public class GpuMetrics
{
    public string Name { get; set; } = string.Empty;
    public double UsagePercent { get; set; }
    public double TemperatureC { get; set; }
    public double? HotspotTemperatureC { get; set; }
    public double CoreClockMHz { get; set; }
    public double MemoryClockMHz { get; set; }
    public double? VoltageMv { get; set; }
    public double PowerW { get; set; }
    public int FanRpm { get; set; }
    public double? FanPercent { get; set; }
    public double? VramUsageMb { get; set; }
    public bool IsThrottling { get; set; }
}