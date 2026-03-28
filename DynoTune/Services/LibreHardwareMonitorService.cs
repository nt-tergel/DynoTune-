using System.Management;
using DynoTune.Models;
using LibreHardwareMonitor.Hardware;

namespace DynoTune.Services;

public class LibreHardwareMonitorService
{
    // Priority order for CPU temperature sensor name matching.
    // Earlier entries win when multiple valid candidates are found.
    private static readonly string[] CpuTempPriorityNames =
    [
        "Tctl", "Tdie", "CPU Package", "Package",
        "CPU Socket", "Socket", "CPU", "TMPIN0", "TMPIN1", "TMPIN2",
        "Temp1", "Temp2", "Temp3"
    ];

    private readonly Computer _computer;
    private readonly UpdateVisitor _updateVisitor = new();
    private bool _hasDumpedSensorInventory;

    public LibreHardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsMotherboardEnabled = true,
            IsMemoryEnabled = false,
            IsGpuEnabled = false,
            IsStorageEnabled = false,
            IsNetworkEnabled = false,
            IsControllerEnabled = true
        };
    }

    public void Initialize()
    {
        _computer.Open();
    }

    public void Shutdown()
    {
        _computer.Close();
    }

    public CpuMetrics GetCpuMetrics()
    {
        _computer.Accept(_updateVisitor);

        double usagePercent = 0;
        double clockMHz = 0;
        double? powerW = null;
        double? packagePowerW = null;
        bool thermalThrottle = false;
        bool powerThrottle = false;
        bool usageFound = false;
        bool clockFound = false;
        double clockSum = 0;
        int clockCount = 0;
        bool packagePowerFound = false;
        bool anyPowerFound = false;

        foreach (IHardware hardware in _computer.Hardware)
        {
            // Only collect non-temperature metrics from CPU node.
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                void ProcessCpuSensor(ISensor sensor)
                {
                    if (sensor.Value is null)
                    {
                        return;
                    }

                    float value = sensor.Value.Value;
                    string sensorName = sensor.Name ?? string.Empty;

                    bool isNonLoadMetric =
                        sensor.SensorType == SensorType.Clock ||
                        sensor.SensorType == SensorType.Power ||
                        sensor.SensorType == SensorType.Factor;
                    if (isNonLoadMetric && value <= 0)
                    {
                        return;
                    }

                    if (sensor.SensorType == SensorType.Load)
                    {
                        if (sensorName.Contains("Total", StringComparison.OrdinalIgnoreCase))
                        {
                            usagePercent = value;
                            usageFound = true;
                        }
                        else if (!usageFound && value > usagePercent)
                        {
                            usagePercent = value;
                        }
                    }
                    else if (sensor.SensorType == SensorType.Clock)
                    {
                        if (sensorName.Contains("Core", StringComparison.OrdinalIgnoreCase))
                        {
                            clockSum += value;
                            clockCount++;
                            clockFound = true;
                        }
                        else if (!clockFound && value > clockMHz)
                        {
                            clockMHz = value;
                        }
                    }
                    else if (sensor.SensorType == SensorType.Power)
                    {
                        anyPowerFound = true;
                        if (sensorName.Contains("Package", StringComparison.OrdinalIgnoreCase))
                        {
                            packagePowerW = value;
                            packagePowerFound = true;
                            powerW = value;
                        }
                        else if (!packagePowerFound)
                        {
                            powerW = (powerW ?? 0) + value;
                        }
                    }
                    else if (sensor.SensorType == SensorType.Factor)
                    {
                        if (sensorName.Contains("Thermal", StringComparison.OrdinalIgnoreCase))
                        {
                            thermalThrottle = value > 0;
                        }
                        else if (sensorName.Contains("Power", StringComparison.OrdinalIgnoreCase))
                        {
                            powerThrottle = value > 0;
                        }
                    }
                }

                ProcessHardwareSensorsRecursive(hardware, ProcessCpuSensor);
            }
        }

        if (!anyPowerFound)
        {
            packagePowerW = null;
            powerW = null;
        }

        if (clockCount > 0)
        {
            clockMHz = clockSum / clockCount;
        }

        // WMI fallback for clock: Win32_Processor.CurrentClockSpeed works without admin.
        if (clockMHz <= 0)
        {
            clockMHz = TryGetCpuClockMhzViaWmi();
        }

        // Temperature uses a separate multi-hardware scan with fallback priority.
        double? temperatureC = FindBestCpuTemperature();

        return new CpuMetrics
        {
            UsagePercent = usagePercent,
            TemperatureC = temperatureC,
            ClockMHz = clockMHz,
            PowerW = powerW,
            PackagePowerW = packagePowerW,
            IsThermallyThrottling = thermalThrottle,
            IsPowerThrottling = powerThrottle
        };
    }

    // Scans CPU, Motherboard, and SuperIO hardware for the best CPU temperature candidate.
    // Priority order: Ryzen internal > CPU-named > Socket/Package-named > TMPIN/Temp fallbacks.
    private double? FindBestCpuTemperature()
    {
        // Collect all valid temperature sensor candidates across CPU + motherboard nodes.
        var candidates = new List<(ISensor Sensor, int Priority)>();

        foreach (IHardware hardware in _computer.Hardware)
        {
            bool isThermalHardware =
                hardware.HardwareType == HardwareType.Cpu ||
                hardware.HardwareType == HardwareType.Motherboard ||
                hardware.HardwareType.ToString().Contains("Super", StringComparison.OrdinalIgnoreCase) ||
                hardware.HardwareType.ToString().Contains("Embedded", StringComparison.OrdinalIgnoreCase);

            if (!isThermalHardware)
            {
                continue;
            }

            CollectTempCandidatesRecursive(hardware, candidates);
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        // Return highest-priority (lowest priority number) candidate.
        candidates.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        return candidates[0].Sensor.Value.HasValue ? (double?)candidates[0].Sensor.Value!.Value : null;
    }

    private static void CollectTempCandidatesRecursive(
        IHardware hardware,
        List<(ISensor, int)> candidates)
    {
        foreach (ISensor sensor in hardware.Sensors)
        {
            if (sensor.SensorType != SensorType.Temperature)
            {
                continue;
            }

            if (!sensor.Value.HasValue)
            {
                continue;
            }

            float value = sensor.Value.Value;

            // Reject physically implausible readings.
            if (value < 5 || value > 120)
            {
                continue;
            }

            string name = sensor.Name ?? string.Empty;
            int priority = GetCpuTempPriority(name);

            // Only include sensors that match a known CPU-like pattern.
            if (priority < int.MaxValue)
            {
                candidates.Add((sensor, priority));
            }
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            CollectTempCandidatesRecursive(subHardware, candidates);
        }
    }

    private static int GetCpuTempPriority(string sensorName)
    {
        for (int i = 0; i < CpuTempPriorityNames.Length; i++)
        {
            if (sensorName.Contains(CpuTempPriorityNames[i], StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static double TryGetCpuClockMhzViaWmi()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT CurrentClockSpeed FROM Win32_Processor");

            double total = 0;
            int count = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["CurrentClockSpeed"] is uint speed && speed > 0)
                {
                    total += speed;
                    count++;
                }
            }

            return count > 0 ? total / count : 0;
        }
        catch
        {
            return 0;
        }
    }

    public void DumpSensorInventoryOnce()
    {
        if (_hasDumpedSensorInventory)
        {
            return;
        }

        _computer.Accept(_updateVisitor);
        _hasDumpedSensorInventory = true;

        System.Diagnostics.Debug.WriteLine("===== LHM Sensor Inventory (one-time) =====");
        foreach (IHardware hardware in _computer.Hardware)
        {
            DumpHardwareRecursive(hardware, 0);
        }
        System.Diagnostics.Debug.WriteLine("===========================================");
    }

    private static void DumpHardwareRecursive(IHardware hardware, int depth)
    {
        string indent = new string(' ', depth * 2);
        System.Diagnostics.Debug.WriteLine($"{indent}[HW] {hardware.HardwareType} :: {hardware.Name}");

        foreach (ISensor sensor in hardware.Sensors)
        {
            string sensorName = sensor.Name ?? string.Empty;
            string value = sensor.Value?.ToString() ?? "null";
            System.Diagnostics.Debug.WriteLine($"{indent}  [SENSOR] {sensor.SensorType} :: {sensorName} = {value}");
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            DumpHardwareRecursive(subHardware, depth + 1);
        }
    }

    private static void ProcessHardwareSensorsRecursive(IHardware hardware, Action<ISensor> processSensor)
    {
        foreach (ISensor sensor in hardware.Sensors)
        {
            processSensor(sensor);
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            ProcessHardwareSensorsRecursive(subHardware, processSensor);
        }
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            foreach (IHardware hardware in computer.Hardware)
            {
                hardware.Accept(this);
            }
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }
        }

        public void VisitParameter(IParameter parameter)
        {
        }

        public void VisitSensor(ISensor sensor)
        {
        }
    }
}
