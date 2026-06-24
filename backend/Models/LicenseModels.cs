namespace WattWise.Backend.Models;

public record LicenseStatus
{
    public string State { get; init; } = "trial"; // trial | active | expired
    public int DaysRemaining { get; init; }
    public string Email { get; init; } = "";
    public string TrialEnd { get; init; } = "";
}

public record ValidateResult
{
    public bool Valid { get; init; }
    public string Message { get; init; } = "";
    public bool Cached { get; init; }
}
