using System.Linq;
using DynoTune.Models;

namespace DynoTune.Services;

/// <summary>
/// Combines WHEA and GPU driver recovery event observation for session stability reporting.
/// Intended for periodic checks (e.g. every 10–30 s), not per-second telemetry.
/// </summary>
public class StabilityMonitorService
{
    private readonly WheaMonitorService _whea;
    private readonly GpuResetMonitorService _gpuResets;

    public StabilityMonitorService()
        : this(new WheaMonitorService(), new GpuResetMonitorService())
    {
    }

    public StabilityMonitorService(WheaMonitorService whea, GpuResetMonitorService gpuResets)
    {
        _whea = whea;
        _gpuResets = gpuResets;
    }

    /// <summary>
    /// Builds a snapshot for events at or after <paramref name="sinceUtc"/>.
    /// </summary>
    public StabilitySnapshot GetSnapshotSince(DateTime sinceUtc, int maxDetailEvents = 50, int maxScan = 5000)
    {
        (int total, int fatal, int corrected, int other) = _whea.CountSinceUtc(sinceUtc, maxScan);
        int gpuResets = _gpuResets.CountSinceUtc(sinceUtc, maxScan);

        IReadOnlyList<WheaEventRecord> wheaDetails = FilterSince(_whea.GetRecentWheaEvents(Math.Max(maxDetailEvents * 4, 200)), sinceUtc)
            .Take(maxDetailEvents)
            .ToList();

        IReadOnlyList<GpuDriverResetEvent> gpuDetails = FilterGpuSince(_gpuResets.GetRecentDriverResets(Math.Max(maxDetailEvents * 4, 200)), sinceUtc)
            .Take(maxDetailEvents)
            .ToList();

        return new StabilitySnapshot
        {
            WindowStartUtc = sinceUtc,
            CapturedAtUtc = DateTime.UtcNow,
            WheaErrorCount = total,
            FatalWheaCount = fatal,
            CorrectedWheaCount = corrected,
            OtherWheaCount = other,
            GpuDriverResetCount = gpuResets,
            WheaEvents = wheaDetails,
            GpuDriverResets = gpuDetails
        };
    }

    /// <summary>
    /// Maps a snapshot into <see cref="StabilityMetrics"/> for experiment summaries.
    /// </summary>
    public static StabilityMetrics ToMetrics(StabilitySnapshot snapshot)
    {
        return new StabilityMetrics
        {
            WheaOrMceErrorCount = snapshot.WheaErrorCount,
            FatalWheaCount = snapshot.FatalWheaCount,
            CorrectedWheaCount = snapshot.CorrectedWheaCount,
            GpuDriverResetCount = snapshot.GpuDriverResetCount
        };
    }

    private static IEnumerable<WheaEventRecord> FilterSince(IReadOnlyList<WheaEventRecord> items, DateTime sinceUtc)
    {
        foreach (WheaEventRecord e in items)
        {
            if (!e.TimeCreated.HasValue)
            {
                continue;
            }

            if (e.TimeCreated.Value.ToUniversalTime() >= sinceUtc)
            {
                yield return e;
            }
        }
    }

    private static IEnumerable<GpuDriverResetEvent> FilterGpuSince(IReadOnlyList<GpuDriverResetEvent> items, DateTime sinceUtc)
    {
        foreach (GpuDriverResetEvent e in items)
        {
            if (!e.TimeCreated.HasValue)
            {
                continue;
            }

            if (e.TimeCreated.Value.ToUniversalTime() >= sinceUtc)
            {
                yield return e;
            }
        }
    }
}
