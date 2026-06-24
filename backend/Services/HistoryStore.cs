using Microsoft.Data.Sqlite;
using WattWise.Backend.Models;

namespace WattWise.Backend.Services;

public class HistoryStore : IDisposable
{
    private readonly SqliteConnection _db;
    private readonly ILogger<HistoryStore> _logger;

    public HistoryStore(string dbPath, ILogger<HistoryStore> logger)
    {
        _logger = logger;
        _db = new SqliteConnection($"Data Source={dbPath}");
        _db.Open();
        CreateTables();
    }

    private void CreateTables()
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS snapshots (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp TEXT NOT NULL,
                cpu_power REAL NOT NULL,
                gpu_power REAL NOT NULL,
                total_power REAL NOT NULL,
                cost_per_hour REAL NOT NULL,
                cpu_temp REAL NOT NULL,
                gpu_temp REAL NOT NULL,
                cpu_load REAL NOT NULL,
                gpu_load REAL NOT NULL,
                is_idle INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_snapshots_ts ON snapshots(timestamp);
            CREATE TABLE IF NOT EXISTS hourly (
                hour TEXT PRIMARY KEY,
                avg_watts REAL NOT NULL,
                total_kwh REAL NOT NULL,
                cost REAL NOT NULL,
                cpu_avg_watts REAL NOT NULL,
                gpu_avg_watts REAL NOT NULL,
                peak_watts REAL NOT NULL,
                idle_minutes INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
        """;
        cmd.ExecuteNonQuery();
    }

    public void InsertSnapshot(SensorSnapshot snap)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            INSERT INTO snapshots (timestamp, cpu_power, gpu_power, total_power, cost_per_hour, cpu_temp, gpu_temp, cpu_load, gpu_load, is_idle)
            VALUES ($ts, $cp, $gp, $tp, $ch, $ct, $gt, $cl, $gl, $ii)
        """;
        cmd.Parameters.AddWithValue("$ts", snap.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("$cp", snap.CpuPowerWatts);
        cmd.Parameters.AddWithValue("$gp", snap.GpuPowerWatts);
        cmd.Parameters.AddWithValue("$tp", snap.TotalSystemWatts);
        cmd.Parameters.AddWithValue("$ch", snap.CostPerHour);
        cmd.Parameters.AddWithValue("$ct", snap.CpuTemp);
        cmd.Parameters.AddWithValue("$gt", snap.GpuTemp);
        cmd.Parameters.AddWithValue("$cl", snap.CpuLoad);
        cmd.Parameters.AddWithValue("$gl", snap.GpuLoad);
        cmd.Parameters.AddWithValue("$ii", snap.IsIdle ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void AggregateHourly()
    {
        var hour = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:00:00");
        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO hourly (hour, avg_watts, total_kwh, cost, cpu_avg_watts, gpu_avg_watts, peak_watts, idle_minutes)
            SELECT
                strftime('%Y-%m-%dT%H:00:00', timestamp) as hr,
                AVG(total_power),
                SUM(total_power) / 3600000.0,
                SUM(cost_per_hour) / 3600.0,
                AVG(cpu_power),
                AVG(gpu_power),
                MAX(total_power),
                SUM(CASE WHEN is_idle = 1 THEN 1 ELSE 0 END)
            FROM snapshots
            WHERE strftime('%Y-%m-%dT%H:00:00', timestamp) = $hour
        """;
        cmd.Parameters.AddWithValue("$hour", hour);
        cmd.ExecuteNonQuery();
    }

    public CostSummary GetCostSummary(SettingsDto settings)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var weekAgo = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");

        double todayKwh = 0, todayCost = 0, todayAvgW = 0, todayPeakW = 0;
        double yesterdayCost = 0, weekCost = 0;
        int idleMinToday = 0;

        using var cmd = _db.CreateCommand();

        cmd.CommandText = "SELECT COALESCE(SUM(total_kwh),0), COALESCE(SUM(cost),0), COALESCE(AVG(avg_watts),0), COALESCE(MAX(peak_watts),0), COALESCE(SUM(idle_minutes),0) FROM hourly WHERE hour >= $today";
        cmd.Parameters.AddWithValue("$today", today + "T00:00:00");
        using (var reader = cmd.ExecuteReader())
        {
            if (reader.Read())
            {
                todayKwh = reader.GetDouble(0);
                todayCost = reader.GetDouble(1);
                todayAvgW = reader.GetDouble(2);
                todayPeakW = reader.GetDouble(3);
                idleMinToday = reader.GetInt32(4);
            }
        }

        cmd.CommandText = "SELECT COALESCE(SUM(cost),0) FROM hourly WHERE hour >= $y AND hour < $t";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("$y", yesterday + "T00:00:00");
        cmd.Parameters.AddWithValue("$t", today + "T00:00:00");
        using (var reader = cmd.ExecuteReader())
        {
            if (reader.Read()) yesterdayCost = reader.GetDouble(0);
        }

        cmd.CommandText = "SELECT COALESCE(SUM(cost),0) FROM hourly WHERE hour >= $w";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("$w", weekAgo + "T00:00:00");
        using (var reader = cmd.ExecuteReader())
        {
            if (reader.Read()) weekCost = reader.GetDouble(0);
        }

        var monthEst = todayCost > 0 ? todayCost * 30 : 0;
        var yearEst = todayCost > 0 ? todayCost * 365 : 0;

        return new CostSummary
        {
            TodayCost = Math.Round(todayCost, 4),
            YesterdayCost = Math.Round(yesterdayCost, 4),
            WeekCost = Math.Round(weekCost, 4),
            MonthEstimate = Math.Round(monthEst, 2),
            YearEstimate = Math.Round(yearEst, 2),
            StandingChargeDaily = settings.StandingChargePencePerDay / 100.0,
            TotalKwhToday = Math.Round(todayKwh, 4),
            AvgWattsToday = Math.Round(todayAvgW, 1),
            PeakWattsToday = Math.Round(todayPeakW, 1),
            IdleHoursToday = idleMinToday / 60,
            IdleCostToday = Math.Round(idleMinToday / 60.0 * todayAvgW / 1000.0 * settings.RatePencePerKwh / 100.0, 4)
        };
    }

    public List<HistoryEntry> GetHistory(int days)
    {
        var entries = new List<HistoryEntry>();
        var since = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");

        using var cmd = _db.CreateCommand();
        cmd.CommandText = """
            SELECT hour, avg_watts, total_kwh, cost, cpu_avg_watts, gpu_avg_watts, peak_watts
            FROM hourly
            WHERE hour >= $since
            ORDER BY hour DESC
        """;
        cmd.Parameters.AddWithValue("$since", since + "T00:00:00");

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new HistoryEntry
            {
                Timestamp = DateTime.Parse(reader.GetString(0)),
                AvgWatts = Math.Round(reader.GetDouble(1), 1),
                TotalKwh = Math.Round(reader.GetDouble(2), 6),
                Cost = Math.Round(reader.GetDouble(3), 6),
                CpuAvgWatts = Math.Round(reader.GetDouble(4), 1),
                GpuAvgWatts = Math.Round(reader.GetDouble(5), 1),
                PeakWatts = Math.Round(reader.GetDouble(6), 1)
            });
        }
        return entries;
    }

    public List<SensorSnapshot> GetRecentSnapshots(int minutes = 60)
    {
        var entries = new List<SensorSnapshot>();
        var since = DateTime.UtcNow.AddMinutes(-minutes).ToString("O");

        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT timestamp, cpu_power, gpu_power, total_power, cost_per_hour, cpu_temp, gpu_temp, cpu_load, gpu_load, is_idle FROM snapshots WHERE timestamp >= $s ORDER BY timestamp";
        cmd.Parameters.AddWithValue("$s", since);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new SensorSnapshot
            {
                Timestamp = DateTime.Parse(reader.GetString(0)),
                CpuPowerWatts = reader.GetDouble(1),
                GpuPowerWatts = reader.GetDouble(2),
                TotalSystemWatts = reader.GetDouble(3),
                CostPerHour = reader.GetDouble(4),
                CpuTemp = reader.GetDouble(5),
                GpuTemp = reader.GetDouble(6),
                CpuLoad = reader.GetDouble(7),
                GpuLoad = reader.GetDouble(8),
                IsIdle = reader.GetInt32(9) == 1
            });
        }
        return entries;
    }

    public void PurgeOldData(int retentionDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays).ToString("O");
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "DELETE FROM snapshots WHERE timestamp < $c";
        cmd.Parameters.AddWithValue("$c", cutoff);
        var deleted = cmd.ExecuteNonQuery();
        if (deleted > 0) _logger.LogInformation("Purged {Count} old snapshots", deleted);
    }

    public void SaveSetting(string key, string value)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO settings (key, value) VALUES ($k, $v)";
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$v", value);
        cmd.ExecuteNonQuery();
    }

    public string? GetSetting(string key)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = $k";
        cmd.Parameters.AddWithValue("$k", key);
        var result = cmd.ExecuteScalar();
        return result?.ToString();
    }

    public void Dispose() => _db?.Dispose();
}
