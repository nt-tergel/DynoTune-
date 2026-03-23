namespace DynoTune.Models;

public class SafetyLimits
{
    public double MaxCpuTemperatureC { get; set; } = 90;
    public double MaxGpuTemperatureC { get; set; } = 85;
    public double MaxGpuHotspotTemperatureC { get; set; } = 100;
    public int MaxFanPercent { get; set; } = 100;
    public double MinPerformancePercent { get; set; } = 95;
}
