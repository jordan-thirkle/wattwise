using LibreHardwareMonitor.Hardware;

namespace WattWise.Backend.Services;

public class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private readonly ILogger<HardwareMonitorService> _logger;
    private double _cpuPower;
    private double _gpuPower;
    private double _cpuTemp;
    private double _gpuTemp;
    private double _cpuLoad;
    private double _gpuLoad;
    private bool _isInitialized;

    public HardwareMonitorService(ILogger<HardwareMonitorService> logger)
    {
        _logger = logger;
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
        };
    }

    public void Initialize()
    {
        try
        {
            _computer.Open();
            _computer.Accept(new UpdateVisitor());
            _isInitialized = true;
            _logger.LogInformation("Hardware monitor initialized");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Hardware monitor init failed, using estimated values: {Msg}", ex.Message);
        }
    }

    public void Update()
    {
        if (!_isInitialized) return;

        try
        {
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();

                foreach (var sensor in hardware.Sensors)
                {
                    if (!sensor.Value.HasValue) continue;

                    switch (hardware.HardwareType)
                    {
                        case HardwareType.Cpu:
                            ReadCpuSensor(sensor);
                            break;
                        case HardwareType.GpuNvidia:
                        case HardwareType.GpuAmd:
                            ReadGpuSensor(sensor);
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Sensor update error: {Msg}", ex.Message);
        }
    }

    private void ReadCpuSensor(ISensor sensor)
    {
        switch (sensor.SensorType)
        {
            case SensorType.Power when sensor.Name.Contains("Package"):
                _cpuPower = (double)sensor.Value!;
                break;
            case SensorType.Temperature when sensor.Name.Contains("Package") || sensor.Name.Contains("CPU"):
                _cpuTemp = (double)sensor.Value!;
                break;
            case SensorType.Load when sensor.Name.Contains("Total"):
                _cpuLoad = (double)sensor.Value!;
                break;
        }
    }

    private void ReadGpuSensor(ISensor sensor)
    {
        switch (sensor.SensorType)
        {
            case SensorType.Power when sensor.Name.Contains("GPU") || sensor.Name.Contains("Power"):
                _gpuPower = (double)sensor.Value!;
                break;
            case SensorType.Temperature when sensor.Name.Contains("GPU") || sensor.Name.Contains("Core"):
                _gpuTemp = (double)sensor.Value!;
                break;
            case SensorType.Load when sensor.Name.Contains("GPU") || sensor.Name.Contains("Core"):
                _gpuLoad = (double)sensor.Value!;
                break;
        }
    }

    public (double cpu, double gpu, double cpuTemp, double gpuTemp, double cpuLoad, double gpuLoad) GetReadings()
    {
        return (_cpuPower, _gpuPower, _cpuTemp, _gpuTemp, _cpuLoad, _gpuLoad);
    }

    public void Dispose() => _computer.Close();

    private class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware) sub.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
