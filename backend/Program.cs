using WattWise.Backend.Models;
using WattWise.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "WattWise", "wattwise.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddSingleton<HistoryStore>(sp =>
    new HistoryStore(dbPath, sp.GetRequiredService<ILogger<HistoryStore>>()));
builder.Services.AddSingleton<CostCalculator>();
builder.Services.AddSingleton<HardwareMonitorService>();
builder.Services.AddSingleton<OptimizationEngine>();
builder.Services.AddSingleton<LicenseService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();

var store = app.Services.GetRequiredService<HistoryStore>();
var calc = app.Services.GetRequiredService<CostCalculator>();
var hw = app.Services.GetRequiredService<HardwareMonitorService>();
var opt = app.Services.GetRequiredService<OptimizationEngine>();
var license = app.Services.GetRequiredService<LicenseService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

var settings = LoadSettings(store, calc);

hw.Initialize();

// GET /api/current — live sensor readings
app.MapGet("/api/current", () =>
{
    hw.Update();
    var (cpuW, gpuW, cpuT, gpuT, cpuL, gpuL) = hw.GetReadings();
    var totalW = calc.CalculateTotalWatts(cpuW, gpuW);
    var costHr = calc.CalculateCostPerHour(totalW);
    var summary = store.GetCostSummary(settings);
    var idle = opt.IsSystemIdle(cpuL, gpuL, totalW);
    var tier = license.GetTier();

    var snap = new SensorSnapshot
    {
        CpuPowerWatts = Math.Round(cpuW, 1),
        GpuPowerWatts = Math.Round(gpuW, 1),
        TotalSystemWatts = Math.Round(totalW, 1),
        CostPerHour = Math.Round(costHr, 4),
        CpuTemp = Math.Round(cpuT, 1),
        GpuTemp = Math.Round(gpuT, 1),
        CpuLoad = Math.Round(cpuL, 1),
        GpuLoad = Math.Round(gpuL, 1),
        IsIdle = idle
    };

    Task.Run(() => store.InsertSnapshot(snap));

    return new CurrentStatus
    {
        CpuPowerWatts = snap.CpuPowerWatts,
        GpuPowerWatts = snap.GpuPowerWatts,
        TotalSystemWatts = snap.TotalSystemWatts,
        CostPerHour = snap.CostPerHour,
        CpuTemp = snap.CpuTemp,
        GpuTemp = snap.GpuTemp,
        CpuLoad = snap.CpuLoad,
        GpuLoad = snap.GpuLoad,
        IsIdle = snap.IsIdle,
        Timestamp = snap.Timestamp,
        TodayCost = summary.TodayCost,
        WeekCost = summary.WeekCost,
        MonthEstimate = summary.MonthEstimate,
        YearEstimate = summary.YearEstimate,
        StandingChargeDaily = summary.StandingChargeDaily,
        Tier = tier
    };
});

// GET /api/summary — cost aggregates (free: today only)
app.MapGet("/api/summary", () =>
{
    var summary = store.GetCostSummary(settings);
    if (!license.IsPro())
    {
        return new CostSummary
        {
            TodayCost = summary.TodayCost,
            StandingChargeDaily = summary.StandingChargeDaily,
            TotalKwhToday = summary.TotalKwhToday,
            AvgWattsToday = summary.AvgWattsToday,
            PeakWattsToday = summary.PeakWattsToday,
            UpgradeHint = "Upgrade to Pro for weekly & monthly estimates"
        };
    }
    return summary;
});

// GET /api/history?days=7 — hourly history (free: max 1 day)
app.MapGet("/api/history", (int days) =>
{
    var clamped = license.IsPro() ? days : Math.Min(days, 1);
    return store.GetHistory(clamped);
});

// GET /api/sparkline?minutes=60 — recent data points (free: max 60 min)
app.MapGet("/api/sparkline", (int minutes) =>
{
    var clamped = license.IsPro() ? minutes : Math.Min(minutes, 60);
    var data = store.GetRecentSnapshots(clamped);
    return data.Select(d => new { t = d.Timestamp, w = d.TotalSystemWatts, c = d.CostPerHour, idle = d.IsIdle });
});

// GET /api/suggestions — optimization tips (free: empty with hint)
app.MapGet("/api/suggestions", () =>
{
    if (!license.IsPro())
    {
        return Results.Ok(new { suggestions = Array.Empty<Suggestion>(), upgradeHint = "Unlock the Optimization Engine with Pro — £4.99 one-time" });
    }
    var summary = store.GetCostSummary(settings);
    return Results.Ok(new { suggestions = opt.Analyze(summary, settings), upgradeHint = (string?)null });
});

// GET /api/settings — current settings
app.MapGet("/api/settings", () => settings);

// POST /api/settings — update settings
app.MapPost("/api/settings", (SettingsDto newSettings) =>
{
    settings = newSettings;
    calc.RatePencePerKwh = newSettings.RatePencePerKwh;
    calc.StandingChargePencePerDay = newSettings.StandingChargePencePerDay;
    calc.BaseSystemWatts = newSettings.BaseSystemWatts;
    store.SaveSetting("rate", newSettings.RatePencePerKwh.ToString());
    store.SaveSetting("standing", newSettings.StandingChargePencePerDay.ToString());
    store.SaveSetting("baseWatts", newSettings.BaseSystemWatts.ToString());
    store.SaveSetting("currency", newSettings.Currency);
    store.SaveSetting("interval", newSettings.UpdateIntervalMs.ToString());
    store.SaveSetting("retention", newSettings.DataRetentionDays.ToString());
    return Results.Ok(settings);
});

// GET /api/health — sidecar health check
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

// GET /api/license/status
app.MapGet("/api/license/status", () => license.GetStatus());

// POST /api/license/validate
app.MapPost("/api/license/validate", async (LicenseValidateRequest req) =>
{
    var result = await license.ValidateKey(req.Key);
    return Results.Ok(result);
});

// POST /api/license/trial — start 7-day trial (free users only)
app.MapPost("/api/license/trial", () =>
{
    var status = license.StartTrial();
    return Results.Ok(status);
});

// GET /api/permissions — admin elevation status
app.MapGet("/api/permissions", () =>
{
    for (int i = 0; i < 10; i++)
    {
        hw.Update();
        var (cw, gw, _, _, _, _) = hw.GetReadings();
        if (cw > 0 || gw > 0) break;
        Thread.Sleep(500);
    }
    hw.Update();
    var (cpuW, gpuW, _, _, _, _) = hw.GetReadings();
    bool hasAccess = cpuW > 0 || gpuW > 0;
    bool isAdmin = false;
    try
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        isAdmin = new System.Security.Principal.WindowsPrincipal(identity)
            .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
    catch { }
    return Results.Ok(new { hasAccess, isAdmin, needsElevation = !hasAccess });
});

// Background: hourly aggregation
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromMinutes(5));
        try { store.AggregateHourly(); }
        catch (Exception ex) { logger.LogDebug("Aggregation error: {Msg}", ex.Message); }
    }
});

// Background: daily purge
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromHours(24));
        try { store.PurgeOldData(settings.DataRetentionDays); }
        catch (Exception ex) { logger.LogDebug("Purge error: {Msg}", ex.Message); }
    }
});

logger.LogInformation("WattWise sensor backend starting on http://localhost:45892");
app.Run("http://localhost:45892");

static SettingsDto LoadSettings(HistoryStore store, CostCalculator calc)
{
    var s = new SettingsDto();
    var rate = store.GetSetting("rate");
    if (rate != null && double.TryParse(rate, out var r)) { s = s with { RatePencePerKwh = r }; calc.RatePencePerKwh = r; }
    var standing = store.GetSetting("standing");
    if (standing != null && double.TryParse(standing, out var st)) { s = s with { StandingChargePencePerDay = st }; calc.StandingChargePencePerDay = st; }
    var baseW = store.GetSetting("baseWatts");
    if (baseW != null && int.TryParse(baseW, out var bw)) { s = s with { BaseSystemWatts = bw }; calc.BaseSystemWatts = bw; }
    var curr = store.GetSetting("currency");
    if (curr != null) s = s with { Currency = curr };
    var interval = store.GetSetting("interval");
    if (interval != null && int.TryParse(interval, out var iv)) s = s with { UpdateIntervalMs = iv };
    var retention = store.GetSetting("retention");
    if (retention != null && int.TryParse(retention, out var ret)) s = s with { DataRetentionDays = ret };
    var firstLaunch = store.GetSetting("first_launch");
    if (firstLaunch != null) s = s with { FirstLaunch = firstLaunch };
    var licenseKey = store.GetSetting("license_key");
    if (licenseKey != null) s = s with { LicenseKey = licenseKey };
    var licenseStatus = store.GetSetting("license_status");
    if (licenseStatus != null) s = s with { LicenseStatus = licenseStatus };
    var licenseEmail = store.GetSetting("license_email");
    if (licenseEmail != null) s = s with { LicenseEmail = licenseEmail };
    return s;
}

record LicenseValidateRequest(string Key);
