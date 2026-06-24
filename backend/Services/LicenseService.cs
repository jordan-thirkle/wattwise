using System.Net.Http.Json;
using WattWise.Backend.Models;

namespace WattWise.Backend.Services;

public class LicenseService
{
    private readonly HistoryStore _store;
    private readonly HttpClient _http;
    private const string ProductPermalink = "wattwise";
    private const int TrialDays = 7;

    public LicenseService(HistoryStore store)
    {
        _store = store;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public LicenseStatus GetStatus()
    {
        var firstLaunch = _store.GetSetting("first_launch");
        if (firstLaunch == null)
        {
            firstLaunch = DateTime.UtcNow.ToString("O");
            _store.SaveSetting("first_launch", firstLaunch);
            _store.SaveSetting("license_status", "trial");
        }

        var status = _store.GetSetting("license_status") ?? "trial";
        var key = _store.GetSetting("license_key") ?? "";
        var email = _store.GetSetting("license_email") ?? "";

        DateTime.TryParse(firstLaunch, out var launchDate);
        var trialEnd = launchDate.AddDays(TrialDays);
        var daysRemaining = Math.Max(0, (int)(trialEnd - DateTime.UtcNow).TotalDays);

        if (status == "active")
        {
            return new LicenseStatus { State = "active", DaysRemaining = -1, Email = email, TrialEnd = trialEnd.ToString("O") };
        }

        if (DateTime.UtcNow > trialEnd)
        {
            _store.SaveSetting("license_status", "expired");
            return new LicenseStatus { State = "expired", DaysRemaining = 0, Email = email, TrialEnd = trialEnd.ToString("O") };
        }

        return new LicenseStatus { State = "trial", DaysRemaining = daysRemaining, Email = "", TrialEnd = trialEnd.ToString("O") };
    }

    public bool IsLocked()
    {
        var status = GetStatus();
        return status.State == "expired";
    }

    public async Task<ValidateResult> ValidateKey(string key)
    {
        try
        {
            var url = $"https://api.gumroad.com/v2/licenses/verify?product_permalink={ProductPermalink}&license_key={Uri.EscapeDataString(key)}";
            var response = await _http.GetAsync(url);
            var json = await response.Content.ReadFromJsonAsync<GumroadResponse>();

            if (json?.Success == true)
            {
                _store.SaveSetting("license_key", key);
                _store.SaveSetting("license_status", "active");
                _store.SaveSetting("license_email", json.Purchase?.Email ?? "");
                return new ValidateResult { Valid = true, Message = "Licence activated successfully." };
            }
            else
            {
                return new ValidateResult { Valid = false, Message = json?.Message ?? "Invalid licence key." };
            }
        }
        catch (Exception)
        {
            // Network error — trust local state if already active
            var currentStatus = _store.GetSetting("license_status");
            if (currentStatus == "active")
            {
                return new ValidateResult { Valid = true, Message = "Using cached licence.", Cached = true };
            }
            return new ValidateResult { Valid = false, Message = "Cannot reach licence server. Check your internet connection." };
        }
    }

    public SensorSnapshot? GetLastSnapshot()
    {
        var recent = _store.GetRecentSnapshots(1);
        return recent.FirstOrDefault();
    }
}

// Gumroad API response models
public class GumroadResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public GumroadPurchase? Purchase { get; set; }
}

public class GumroadPurchase
{
    public string? Email { get; set; }
}
