namespace DynoTune.Models;

public class StabilityMetrics
{
    public int CrashCount { get; set; }
    public int WheaOrMceErrorCount { get; set; }
    public int GpuDriverResetCount { get; set; }
    public bool IsStable => CrashCount == 0 && WheaOrMceErrorCount == 0 && GpuDriverResetCount == 0;
}
