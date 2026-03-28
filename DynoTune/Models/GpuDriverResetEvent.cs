namespace DynoTune.Models;

/// <summary>
/// Display driver recovery (TDR) as observed in the System log (Event ID 4101).
/// </summary>
public class GpuDriverResetEvent
{
    public DateTime? TimeCreated { get; set; }
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
