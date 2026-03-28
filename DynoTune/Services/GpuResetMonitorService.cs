using System.Diagnostics.Eventing.Reader;
using DynoTune.Models;

namespace DynoTune.Services;

/// <summary>
/// Observes Display / 4101 "driver recovered" events in the System log as a proxy for GPU driver resets.
/// </summary>
public class GpuResetMonitorService
{
    private static readonly string DisplayXPath = @"
            *[
                System[
                    Provider[@Name='Display']
                    and (EventID=4101)
                ]
            ]";

    public IReadOnlyList<GpuDriverResetEvent> GetRecentDriverResets(int maxCount = 50)
    {
        var results = new List<GpuDriverResetEvent>();

        var logQuery = new EventLogQuery("System", PathType.LogName, DisplayXPath)
        {
            ReverseDirection = true
        };

        try
        {
            using var reader = new EventLogReader(logQuery);

            for (EventRecord? evt = reader.ReadEvent();
                 evt != null && results.Count < maxCount;
                 evt = reader.ReadEvent())
            {
                try
                {
                    results.Add(MapRecord(evt));
                }
                finally
                {
                    evt.Dispose();
                }
            }
        }
        catch (Exception)
        {
        }

        return results;
    }

    public int CountSinceUtc(DateTime sinceUtc, int maxEventsToScan = 5000)
    {
        int count = 0;
        int scanned = 0;

        var logQuery = new EventLogQuery("System", PathType.LogName, DisplayXPath)
        {
            ReverseDirection = true
        };

        try
        {
            using var reader = new EventLogReader(logQuery);

            for (EventRecord? evt = reader.ReadEvent();
                 evt != null && scanned < maxEventsToScan;
                 evt = reader.ReadEvent(), scanned++)
            {
                try
                {
                    if (!evt.TimeCreated.HasValue)
                    {
                        continue;
                    }

                    if (evt.TimeCreated.Value.ToUniversalTime() < sinceUtc)
                    {
                        break;
                    }

                    count++;
                }
                finally
                {
                    evt.Dispose();
                }
            }
        }
        catch (Exception)
        {
        }

        return count;
    }

    private static GpuDriverResetEvent MapRecord(EventRecord evt)
    {
        string message;
        try
        {
            message = evt.FormatDescription() ?? string.Empty;
        }
        catch (EventLogException)
        {
            message = string.Empty;
        }

        return new GpuDriverResetEvent
        {
            TimeCreated = evt.TimeCreated,
            Id = evt.Id,
            Provider = evt.ProviderName ?? string.Empty,
            Level = evt.LevelDisplayName ?? string.Empty,
            Message = message
        };
    }
}
