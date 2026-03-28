namespace DynoTune.Models;

/// <summary>
/// Aggregate stability observations for a time window (e.g. app or experiment session).
/// Counts are from Windows event logs, not hardware counters.
/// </summary>
public class StabilitySnapshot
{
    public DateTime WindowStartUtc { get; set; }
    public DateTime CapturedAtUtc { get; set; }

    /// <summary>All tracked WHEA-Logger events (IDs 17, 18, 19, 46) in the window.</summary>
    public int WheaErrorCount { get; set; }

    public int FatalWheaCount { get; set; }
    public int CorrectedWheaCount { get; set; }
    public int OtherWheaCount { get; set; }

    public int GpuDriverResetCount { get; set; }

    public IReadOnlyList<WheaEventRecord> WheaEvents { get; set; } = Array.Empty<WheaEventRecord>();
    public IReadOnlyList<GpuDriverResetEvent> GpuDriverResets { get; set; } = Array.Empty<GpuDriverResetEvent>();
}
