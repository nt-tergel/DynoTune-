namespace DynoTune.Models;

public class ExperimentRunRequest
{
    public string WorkloadName { get; set; } = string.Empty;
    public WorkloadClass WorkloadClass { get; set; } = WorkloadClass.Mixed;
    public ExperimentProfileType ProfileType { get; set; } = ExperimentProfileType.Baseline;
    public int WarmupSeconds { get; set; } = 30;
    public int DurationSeconds { get; set; } = 300;
    public int SamplingIntervalSeconds { get; set; } = 1;
}
