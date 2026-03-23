namespace DynoTune.Models;

public class PerformanceMetrics
{
    public double? FpsAverage { get; set; }
    public double? FpsOnePercentLow { get; set; }
    public double? RenderTimeSeconds { get; set; }
    public double? CompileTimeSeconds { get; set; }
}
