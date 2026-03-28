namespace DynoTune.Models;

/// <summary>
/// Session-level stability summary. WHEA and GPU reset figures may be filled from Windows event log observations.
/// </summary>
public class StabilityMetrics
{
    public int CrashCount { get; set; }

    /// <summary>Total WHEA-Logger events observed (IDs 17, 18, 19, 46).</summary>
    public int WheaOrMceErrorCount { get; set; }

    public int FatalWheaCount { get; set; }
    public int CorrectedWheaCount { get; set; }
    public int GpuDriverResetCount { get; set; }

    public bool IsStable =>
        CrashCount == 0 &&
        WheaOrMceErrorCount == 0 &&
        GpuDriverResetCount == 0;
}
