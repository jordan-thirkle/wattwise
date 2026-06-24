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
        // First launch — default to free
        var firstLaunch = _store.GetSetting("first_launch");
        if (firstLaunch == null)
        {
            firstLaunch = DateTime.UtcNow.ToString("O");
            _store.SaveSetting("first_launch", firstLaunch);
            _store.SaveSetting("license_status", "free");
        }

        var status = _store.GetSetting("license_status") ?? "free";
        var key = _store.GetSetting("license_key") ?? "";
        var email = _store.GetSetting("license_email") ?? "";

        // Active licence — always honoured
        if (status == "active")
        {
            return new LicenseStatus
            {
                State = "active",
                Tier = "pro",
                DaysRemaining = -1,
                Email = email,
                TrialEnd = ""
            };
        }

        // Trial — check expiry, auto-revert to free if expired
        if (status == "trial")
        {
            var trialStart = _store.GetSetting("trial_start");
            if (trialStart != null && DateTime.TryParse(trialStart, out var ts))
            {
                var trialEnd = ts.AddDays(TrialDays);
                var daysRemaining = Math.Max(0, (int)(trialEnd - DateTime.UtcNow).TotalDays);

                if (DateTime.UtcNow > trialEnd)
                {
                    // Trial expired — revert to free
                    _store.SaveSetting("license_status", "free");
                    _store.SaveSetting("trial_start", "");
                    return new LicenseStatus
                    {
                        State = "free",
                        Tier = "free",
                        DaysRemaining = 0,
                        Email = "",
                        TrialEnd = ""
                    };
                }

                return new LicenseStatus
                {
                    State = "trial",
                    Tier = "pro",
                    DaysRemaining = daysRemaining,
                    Email = "",
                    TrialEnd = trialEnd.ToString("O")
                };
            }
            // Corrupt trial state — reset to free
            _store.SaveSetting("license_status", "free");
        }

        // Free tier
        return new LicenseStatus
        {
            State = "free",
            Tier = "free",
            DaysRemaining = 0,
            Email = "",
            TrialEnd = ""
        };
    }

    public string GetTier()
    {
        return GetStatus().Tier;
    }

    public bool IsPro()
    {
        return GetTier() == "pro";
    }

    public LicenseStatus StartTrial()
    {
        var current = GetStatus();
        if (current.State == "trial" || current.State == "active")
        {
            return current;
        }

        var now = DateTime.UtcNow.ToString("O");
        _store.SaveSetting("trial_start", now);
        _store.SaveSetting("license_status", "trial");

        return new LicenseStatus
        {
            State = "trial",
            Tier = "pro",
            DaysRemaining = TrialDays,
            Email = "",
            TrialEnd = DateTime.UtcNow.AddDays(TrialDays).ToString("O")
        };
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
                _store.SaveSetting("trial_start", "");
                return new ValidateResult { Valid = true, Message = "Licence activated successfully." };
            }
            else
            {
                return new ValidateResult { Valid = false, Message = json?.Message ?? "Invalid licence key." };
            }
        }
        catch (Exception)
        {
            var currentStatus = _store.GetSetting("license_status");
            if (currentStatus == "active")
            {
                return new ValidateResult { Valid = true, Message = "Using cached licence.", Cached = true };
            }
            return new ValidateResult { Valid = false, Message = "Cannot reach licence server. Check your internet connection." };
        }
    }
}

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
