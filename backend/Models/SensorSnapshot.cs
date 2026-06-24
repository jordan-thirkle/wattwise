namespace WattWise.Backend.Models;

public record SensorSnapshot
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public double CpuPowerWatts { get; init; }
    public double GpuPowerWatts { get; init; }
    public double TotalSystemWatts { get; init; }
    public double CostPerHour { get; init; }
    public double CpuTemp { get; init; }
    public double GpuTemp { get; init; }
    public double CpuLoad { get; init; }
    public double GpuLoad { get; init; }
    public bool IsIdle { get; init; }
}

public record CurrentStatus
{
    public double CpuPowerWatts { get; init; }
    public double GpuPowerWatts { get; init; }
    public double TotalSystemWatts { get; init; }
    public double CostPerHour { get; init; }
    public double CpuTemp { get; init; }
    public double GpuTemp { get; init; }
    public double CpuLoad { get; init; }
    public double GpuLoad { get; init; }
    public bool IsIdle { get; init; }
    public DateTime Timestamp { get; init; }
    public double TodayCost { get; init; }
    public double WeekCost { get; init; }
    public double MonthEstimate { get; init; }
    public double YearEstimate { get; init; }
    public double StandingChargeDaily { get; init; }
    public bool Locked { get; init; }
}

public record HistoryEntry
{
    public DateTime Timestamp { get; init; }
    public double AvgWatts { get; init; }
    public double TotalKwh { get; init; }
    public double Cost { get; init; }
    public double CpuAvgWatts { get; init; }
    public double GpuAvgWatts { get; init; }
    public double PeakWatts { get; init; }
}

public record CostSummary
{
    public double TodayCost { get; init; }
    public double YesterdayCost { get; init; }
    public double WeekCost { get; init; }
    public double MonthEstimate { get; init; }
    public double YearEstimate { get; init; }
    public double StandingChargeDaily { get; init; }
    public double TotalKwhToday { get; init; }
    public double AvgWattsToday { get; init; }
    public double PeakWattsToday { get; init; }
    public int IdleHoursToday { get; init; }
    public double IdleCostToday { get; init; }
}

public record Suggestion
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public double EstimatedMonthlySaving { get; init; }
    public string ActionType { get; init; } = ""; // powercfg, settings, display
    public string ActionCommand { get; init; } = "";
}

public record SettingsDto
{
    public double RatePencePerKwh { get; init; } = 23.09;
    public double StandingChargePencePerDay { get; init; } = 64.29;
    public string Currency { get; init; } = "GBP";
    public int BaseSystemWatts { get; init; } = 45;
    public int UpdateIntervalMs { get; init; } = 1000;
    public int DataRetentionDays { get; init; } = 90;
    public string? FirstLaunch { get; init; }
    public string? LicenseKey { get; init; }
    public string? LicenseStatus { get; init; } = "trial";
    public string? LicenseEmail { get; init; }
}
