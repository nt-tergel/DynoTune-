namespace DynoTune.Models;

/// <summary>
/// One WHEA hardware error row as observed in the System event log (not raw CPU MCE registers).
/// </summary>
public class WheaEventRecord
{
    public DateTime? TimeCreated { get; set; }
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Best-effort parse from the formatted message or event XML.</summary>
    public string? Component { get; set; }

    public string? ErrorSource { get; set; }
    public string? ErrorType { get; set; }
    public string? ProcessorApicId { get; set; }
}
