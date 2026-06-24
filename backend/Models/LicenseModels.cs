namespace WattWise.Backend.Models;

public record LicenseStatus
{
    public string State { get; init; } = "free"; // free | trial | active
    public string Tier { get; init; } = "free";   // free | pro
    public int DaysRemaining { get; init; }
    public string Email { get; init; } = "";
    public string TrialEnd { get; init; } = "";
    public string? UpgradeHint { get; init; }
}

public record ValidateResult
{
    public bool Valid { get; init; }
    public string Message { get; init; } = "";
    public bool Cached { get; init; }
}
