namespace DynoTune.Models;

public class TuningProfile
{
    public string Name { get; set; } = string.Empty;
    public WorkloadClass TargetWorkload { get; set; } = WorkloadClass.Mixed;

    public int? GpuPowerLimitPercent { get; set; }
    public int? GpuMaxClockMHz { get; set; }
    public int? GpuVoltageMv { get; set; }

    public int? CpuPptW { get; set; }
    public int? CpuTdcA { get; set; }
    public int? CpuEdcA { get; set; }

    public List<FanCurvePoint> GpuFanCurve { get; set; } = new();
    public SafetyLimits SafetyLimits { get; set; } = new();
}
