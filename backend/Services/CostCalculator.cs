namespace WattWise.Backend.Services;

public class CostCalculator
{
    public double RatePencePerKwh { get; set; } = 23.09;
    public double StandingChargePencePerDay { get; set; } = 64.29;
    public int BaseSystemWatts { get; set; } = 45;

    public double CalculateTotalWatts(double cpuWatts, double gpuWatts)
    {
        return Math.Max(0, cpuWatts + gpuWatts + BaseSystemWatts);
    }

    public double CalculateCostPerHour(double totalWatts)
    {
        var kwh = totalWatts / 1000.0;
        return kwh * (RatePencePerKwh / 100.0);
    }

    public double CalculateKwh(double totalWatts, double hours)
    {
        return totalWatts / 1000.0 * hours;
    }

    public string FormatCost(double cost)
    {
        return $"£{cost:F3}";
    }
}
