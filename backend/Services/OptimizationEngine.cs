using WattWise.Backend.Models;

namespace WattWise.Backend.Services;

public class OptimizationEngine
{
    private readonly HistoryStore _store;
    private readonly CostCalculator _calc;

    public OptimizationEngine(HistoryStore store, CostCalculator calc)
    {
        _store = store;
        _calc = calc;
    }

    public List<Suggestion> Analyze(CostSummary summary, SettingsDto settings)
    {
        var suggestions = new List<Suggestion>();

        if (summary.IdleCostToday > 0.01)
        {
            suggestions.Add(new Suggestion
            {
                Id = "idle-waste",
                Title = "Reduce idle power waste",
                Description = $"Your PC idled for {summary.IdleHoursToday}h today drawing ~{summary.AvgWattsToday:F0}W, costing £{summary.IdleCostToday:F3}. Enable aggressive hibernate to save £{(summary.IdleCostToday * 30):F2}/month.",
                EstimatedMonthlySaving = Math.Round(summary.IdleCostToday * 30, 2),
                ActionType = "powercfg",
                ActionCommand = "powercfg /setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 29f6c1db-86da-48c5-9fdb-f2b67b1f44da 900"
            });
        }

        if (summary.AvgWattsToday > 200)
        {
            suggestions.Add(new Suggestion
            {
                Id = "undervolt",
                Title = "Consider undervolting GPU",
                Description = "Your average draw is high. GPU undervolting can reduce power by 15-25% with minimal FPS loss. Estimated saving: £5-10/month.",
                EstimatedMonthlySaving = 7.5,
                ActionType = "info",
                ActionCommand = "https://github.com/LibreHardwareMonitor/LibreHardwareMonitor"
            });
        }

        var recentData = _store.GetRecentSnapshots(1440);
        if (recentData.Count > 0)
        {
            var idleSpikes = recentData.Count(s => s.IsIdle && s.TotalSystemWatts > 100);
            if (idleSpikes > 30)
            {
                suggestions.Add(new Suggestion
                {
                    Id = "background-apps",
                    Title = "Background apps draining power",
                    Description = $"Detected {idleSpikes} high-power moments during idle. Check for background processes (RGB software, updaters) that keep CPU awake.",
                    EstimatedMonthlySaving = Math.Round(idleSpikes * 0.001, 2),
                    ActionType = "info",
                    ActionCommand = "taskmgr"
                });
            }
        }

        if (settings.RatePencePerKwh > 20)
        {
            suggestions.Add(new Suggestion
            {
                Id = "tariff-check",
                Title = "Check your energy tariff",
                Description = $"At {settings.RatePencePerKwh}p/kWh, you're above the UK average. A switch to a cheaper tariff could save £20-50/year on PC usage alone.",
                EstimatedMonthlySaving = 3.0,
                ActionType = "link",
                ActionCommand = "https://www.moneysavingexpert.com/cheapenergyclub"
            });
        }

        return suggestions;
    }

    public bool IsSystemIdle(double cpuLoad, double gpuLoad, double totalWatts)
    {
        return cpuLoad < 10 && gpuLoad < 5 && totalWatts < 120;
    }
}
