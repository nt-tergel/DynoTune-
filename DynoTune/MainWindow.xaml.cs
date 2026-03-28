using Microsoft.UI.Xaml;
using DynoTune.Models;
using DynoTune.Services;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DynoTune;

public sealed partial class MainWindow : Window
{
    private readonly AmdAdlxService _gpuService = new();
    private readonly LibreHardwareMonitorService _cpuService = new();
    private readonly MonitoringService _monitoringService;
    private readonly LoggingService _loggingService;
    private readonly StabilityMonitorService _stabilityMonitor = new();
    private readonly DateTime _sessionStartUtc = DateTime.UtcNow;
    private readonly DispatcherTimer _loggingTimer = new();
    private bool _isShuttingDown;

    public MainWindow()
    {
        this.InitializeComponent();

        _monitoringService = new MonitoringService(_cpuService, _gpuService);
        _loggingService = new LoggingService(_monitoringService);

        InitializeTimedLogging();
        Closed += MainWindow_Closed;
    }

    private static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void InitializeTimedLogging()
    {
        bool isAdmin = IsRunningAsAdmin();
        Debug.WriteLine($"Running as admin: {isAdmin}");
        if (!isAdmin)
        {
            Debug.WriteLine("WARNING: Not running as administrator. CPU temp/clock/power sensors may be unavailable. Run Visual Studio as administrator for full telemetry.");
        }

        bool gpuInitialized = _gpuService.Initialize();
        Debug.WriteLine($"ADLX init: {gpuInitialized}");
        _gpuService.DumpSensorInventoryOnce();

        _cpuService.Initialize();
        _cpuService.DumpSensorInventoryOnce();

        _loggingTimer.Interval = TimeSpan.FromSeconds(1);
        _loggingTimer.Tick += LoggingTimer_Tick;
        _loggingTimer.Start();
    }

    private void LoggingTimer_Tick(object? sender, object e)
    {
        if (_isShuttingDown)
        {
            return;
        }

        try
        {
            var snapshot = _monitoringService.GetCurrentSnapshot();
            var record = _loggingService.CreateRecordFromSnapshot(snapshot, "Stock");
            _loggingService.AddRecord(record);

            Debug.WriteLine("===== Telemetry Snapshot =====");
            Debug.WriteLine($"Timestamp: {snapshot.Timestamp:O}");

            Debug.WriteLine("[CPU]");
            Debug.WriteLine($"UsagePercent: {snapshot.Cpu.UsagePercent:F1}%");
            Debug.WriteLine($"TemperatureC: {(snapshot.Cpu.TemperatureC.HasValue ? $"{snapshot.Cpu.TemperatureC.Value:F1} C" : "N/A")}");
            Debug.WriteLine($"ClockMHz: {snapshot.Cpu.ClockMHz:F1} MHz");
            Debug.WriteLine($"PowerW: {(snapshot.Cpu.PowerW.HasValue ? $"{snapshot.Cpu.PowerW.Value:F2} W" : "N/A")}");
            Debug.WriteLine($"PackagePowerW: {(snapshot.Cpu.PackagePowerW.HasValue ? $"{snapshot.Cpu.PackagePowerW.Value:F2} W" : "N/A")}");
            Debug.WriteLine($"IsThermallyThrottling: {snapshot.Cpu.IsThermallyThrottling}");
            Debug.WriteLine($"IsPowerThrottling: {snapshot.Cpu.IsPowerThrottling}");

            Debug.WriteLine("[GPU]");
            Debug.WriteLine($"Name: {snapshot.Gpu.Name}");
            Debug.WriteLine($"UsagePercent: {snapshot.Gpu.UsagePercent:F1}%");
            Debug.WriteLine($"TemperatureC: {snapshot.Gpu.TemperatureC:F1} C");
            Debug.WriteLine($"HotspotTemperatureC: {(snapshot.Gpu.HotspotTemperatureC.HasValue ? $"{snapshot.Gpu.HotspotTemperatureC.Value:F1} C" : "N/A")}");
            Debug.WriteLine($"CoreClockMHz: {snapshot.Gpu.CoreClockMHz:F1} MHz");
            Debug.WriteLine($"MemoryClockMHz: {snapshot.Gpu.MemoryClockMHz:F1} MHz");
            Debug.WriteLine($"VoltageMv: {(snapshot.Gpu.VoltageMv.HasValue ? $"{snapshot.Gpu.VoltageMv.Value:F0} mV" : "N/A")}");
            Debug.WriteLine($"PowerW: {snapshot.Gpu.PowerW:F2} W");
            Debug.WriteLine($"FanRpm: {snapshot.Gpu.FanRpm}");
            Debug.WriteLine($"FanPercent: {(snapshot.Gpu.FanPercent.HasValue ? $"{snapshot.Gpu.FanPercent.Value:F1}%" : "N/A")}");
            Debug.WriteLine($"VramUsageMb: {(snapshot.Gpu.VramUsageMb.HasValue ? $"{snapshot.Gpu.VramUsageMb.Value:F0} MB" : "N/A")}");
            Debug.WriteLine($"IsThrottling: {snapshot.Gpu.IsThrottling}");

            Debug.WriteLine("[SYSTEM]");
            Debug.WriteLine($"MemoryUsedGB: {snapshot.MemoryUsedGB:F2} GB");
            Debug.WriteLine($"MemoryTotalGB: {snapshot.MemoryTotalGB:F2} GB");
            Debug.WriteLine($"SystemPowerW: {(snapshot.SystemPowerW.HasValue ? $"{snapshot.SystemPowerW.Value:F2} W" : "N/A")}");
            Debug.WriteLine($"AmbientTemperatureC: {(snapshot.AmbientTemperatureC.HasValue ? $"{snapshot.AmbientTemperatureC.Value:F1} C" : "N/A")}");
            Debug.WriteLine("==============================");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Telemetry tick failed: {ex.Message}");
        }
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _isShuttingDown = true;
        _loggingTimer.Stop();

        try
        {
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DynoTune",
                "logs");
            Directory.CreateDirectory(logDir);

            string csvPath = Path.Combine(logDir, $"telemetry-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

            await _loggingService.SaveToCsvAsync(csvPath);
            Debug.WriteLine($"CSV exported: {csvPath}");

            string stabilityPath = Path.Combine(logDir, $"stability-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            await SaveStabilitySessionLogAsync(stabilityPath);
            Debug.WriteLine($"Stability log exported: {stabilityPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CSV export failed: {ex.Message}");
        }

        _gpuService.Shutdown();
        _cpuService.Shutdown();
    }

    private async Task SaveStabilitySessionLogAsync(string filePath)
    {
        StabilitySnapshot snapshot = _stabilityMonitor.GetSnapshotSince(_sessionStartUtc);

        var sb = new StringBuilder();
        sb.AppendLine("DynoTune stability session log");
        sb.AppendLine("Counts are from the Windows System event log (WHEA-Logger and Display 4101), not raw hardware registers.");
        sb.AppendLine();
        sb.Append("Window start (UTC): ").AppendLine(snapshot.WindowStartUtc.ToString("O", CultureInfo.InvariantCulture));
        sb.Append("Captured at (UTC): ").AppendLine(snapshot.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        sb.AppendLine();
        sb.AppendLine("WheaErrorCount (event IDs 17,18,19,46): " + snapshot.WheaErrorCount.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("FatalWheaCount (18): " + snapshot.FatalWheaCount.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("CorrectedWheaCount (17, 19): " + snapshot.CorrectedWheaCount.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("OtherWheaCount (46): " + snapshot.OtherWheaCount.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("GpuDriverResetCount (Display / 4101): " + snapshot.GpuDriverResetCount.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine();
        sb.AppendLine("--- WHEA events in window (sample, may be truncated) ---");
        foreach (WheaEventRecord e in snapshot.WheaEvents)
        {
            sb.AppendLine();
            string timeStr = e.TimeCreated.HasValue
                ? e.TimeCreated.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
                : "(null)";
            sb.Append("  Time (UTC): ").AppendLine(timeStr);
            sb.AppendLine("  Id: " + e.Id.ToString(CultureInfo.InvariantCulture) + "  Level: " + e.Level);
            sb.AppendLine("  Provider: " + e.Provider);
            if (!string.IsNullOrEmpty(e.Component))
            {
                sb.AppendLine("  Component: " + e.Component);
            }

            if (!string.IsNullOrEmpty(e.ErrorSource))
            {
                sb.AppendLine("  Error source: " + e.ErrorSource);
            }

            if (!string.IsNullOrEmpty(e.ErrorType))
            {
                sb.AppendLine("  Error type: " + e.ErrorType);
            }

            if (!string.IsNullOrEmpty(e.ProcessorApicId))
            {
                sb.AppendLine("  Processor APIC ID: " + e.ProcessorApicId);
            }

            sb.AppendLine("  Message: " + e.Message);
        }

        sb.AppendLine();
        sb.AppendLine("--- GPU driver recovery events in window (sample, may be truncated) ---");
        foreach (GpuDriverResetEvent e in snapshot.GpuDriverResets)
        {
            sb.AppendLine();
            string timeStr = e.TimeCreated.HasValue
                ? e.TimeCreated.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
                : "(null)";
            sb.Append("  Time (UTC): ").AppendLine(timeStr);
            sb.AppendLine("  Id: " + e.Id.ToString(CultureInfo.InvariantCulture) + "  Level: " + e.Level);
            sb.AppendLine("  Provider: " + e.Provider);
            sb.AppendLine("  Message: " + e.Message);
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }
}