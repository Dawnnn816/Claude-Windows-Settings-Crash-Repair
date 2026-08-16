namespace SettingsCrashRepair.Models;

public sealed record CrashEvidence(
    string ModuleName,
    int Count,
    DateTimeOffset? LatestOccurrence,
    string ExceptionCode,
    string FaultOffset);

public sealed record EventScanResult(
    IReadOnlyList<CrashEvidence> Evidence,
    string? Error = null)
{
    public CrashEvidence? Startup => Evidence.FirstOrDefault(x =>
        x.ModuleName.Equals("SettingsHandlers_Startup.dll", StringComparison.OrdinalIgnoreCase));

    public CrashEvidence? StorageSense => Evidence.FirstOrDefault(x =>
        x.ModuleName.Equals("SettingsHandlers_StorageSense.dll", StringComparison.OrdinalIgnoreCase));
}

public sealed record SettingsAppxInspection(
    bool ManifestExists,
    bool PackageFound,
    string Status,
    string? PackageVersion,
    string? Error = null);

public enum LiveVerificationState
{
    NotRun,
    Passed,
    CrashConfirmed,
    Inconclusive,
    LaunchFailed
}

public sealed record LiveVerificationResult(
    LiveVerificationState State,
    string Summary,
    EventScanResult NewEvents,
    bool SettingsProcessWasAlive,
    string? Error = null);

public sealed record ScanProgress(int Percent, string Message);

public sealed record DiagnosticSnapshot(
    DateTimeOffset ScannedAt,
    StartupInspection Claude,
    EventScanResult Events,
    SettingsAppxInspection SettingsAppx,
    string WindowsVersion,
    string ToolVersion,
    string? ClaudeVersion,
    string? SettingsExecutableVersion,
    LiveVerificationResult LiveVerification);
