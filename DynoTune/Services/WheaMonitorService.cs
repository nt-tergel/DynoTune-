using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using DynoTune.Models;

namespace DynoTune.Services;

/// <summary>
/// Reads WHEA hardware error events from the System log (Microsoft-Windows-WHEA-Logger).
/// </summary>
public class WheaMonitorService
{
    private const string WheaProvider = "Microsoft-Windows-WHEA-Logger";

    private static readonly string WheaXPath = $@"
            *[
                System[
                    Provider[@Name='{WheaProvider}']
                    and (EventID=17 or EventID=18 or EventID=19 or EventID=46)
                ]
            ]";

    public IReadOnlyList<WheaEventRecord> GetRecentWheaEvents(int maxCount = 50)
    {
        var results = new List<WheaEventRecord>();

        var logQuery = new EventLogQuery("System", PathType.LogName, WheaXPath)
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
            // Event log access can fail without admin rights or on restricted profiles.
        }

        return results;
    }

    /// <summary>
    /// Counts WHEA events with TimeCreated &gt;= <paramref name="sinceUtc"/>. Walks newest-first and stops when older events are reached.
    /// </summary>
    public (int Total, int Fatal, int Corrected, int Other) CountSinceUtc(DateTime sinceUtc, int maxEventsToScan = 5000)
    {
        int total = 0, fatal = 0, corrected = 0, other = 0;
        int scanned = 0;

        var logQuery = new EventLogQuery("System", PathType.LogName, WheaXPath)
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
                    DateTime? created = evt.TimeCreated;
                    if (!created.HasValue)
                    {
                        continue;
                    }

                    if (created.Value.ToUniversalTime() < sinceUtc)
                    {
                        break;
                    }

                    switch (evt.Id)
                    {
                        case 18:
                            fatal++;
                            total++;
                            break;
                        case 17:
                        case 19:
                            corrected++;
                            total++;
                            break;
                        case 46:
                            other++;
                            total++;
                            break;
                    }
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

        return (total, fatal, corrected, other);
    }

    private static WheaEventRecord MapRecord(EventRecord evt)
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

        var record = new WheaEventRecord
        {
            TimeCreated = evt.TimeCreated,
            Id = evt.Id,
            Provider = evt.ProviderName ?? string.Empty,
            Level = evt.LevelDisplayName ?? string.Empty,
            Message = message
        };

        EnrichFromMessage(message, record);
        if (evt is EventLogRecord elr)
        {
            TryEnrichFromXml(elr, record);
        }

        return record;
    }

    private static void EnrichFromMessage(string message, WheaEventRecord record)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        record.Component ??= MatchLabel(message, @"Reported by component");
        record.ErrorSource ??= MatchLabel(message, @"Error source");
        record.ErrorType ??= MatchLabel(message, @"Error type");
        record.ProcessorApicId ??= MatchLabel(message, @"Processor APIC ID");
        record.ProcessorApicId ??= MatchLabel(message, @"APIC Id");
    }

    private static string? MatchLabel(string text, string label)
    {
        var m = Regex.Match(
            text,
            $@"{label}\s*:\s*(?<v>.+?)(?:\r|\n|$)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
        return m.Success ? m.Groups["v"].Value.Trim() : null;
    }

    private static void TryEnrichFromXml(EventLogRecord elr, WheaEventRecord record)
    {
        string xml;
        try
        {
            xml = elr.ToXml();
        }
        catch (EventLogException)
        {
            return;
        }

        record.Component ??= MatchXmlData(xml, "Component");
        record.ErrorSource ??= MatchXmlData(xml, "ErrorSource");
        record.ErrorType ??= MatchXmlData(xml, "ErrorType");
        record.ProcessorApicId ??= MatchXmlData(xml, "APICId");
        record.ProcessorApicId ??= MatchXmlData(xml, "ApicId");
    }

    private static string? MatchXmlData(string xml, string dataName)
    {
        var m = Regex.Match(
            xml,
            $@"<Data\s+Name=['""]{Regex.Escape(dataName)}['""]>\s*(?<v>[^<]*)</Data>",
            RegexOptions.IgnoreCase);
        return m.Success ? m.Groups["v"].Value.Trim() : null;
    }
}
