namespace DynoTune.Models;

public class ThermalMetrics
{
    public double CpuAverageTemperatureC { get; set; }
    public double CpuPeakTemperatureC { get; set; }
    public double GpuAverageTemperatureC { get; set; }
    public double GpuPeakTemperatureC { get; set; }
    public double ThrottlingSeconds { get; set; }
}
