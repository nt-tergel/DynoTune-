namespace DynoTune.Models;

public class WorkloadClassificationResult
{
    public WorkloadClass WorkloadClass { get; set; } = WorkloadClass.Idle;
    public double ConfidencePercent { get; set; }
    public string Reason { get; set; } = string.Empty;
}
