using System.Text.Json;
using SettingsCrashRepair.Models;

namespace SettingsCrashRepair.Services;

public static class ReportExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static async Task ExportAsync(
        DiagnosticSnapshot snapshot,
        string path,
        CancellationToken cancellationToken = default)
    {
        var report = new
        {
            reportFormat = "SettingsCrashRepair-Sanitized-1",
            generatedAt = DateTimeOffset.Now,
            privacy = new
            {
                sanitized = true,
                omitted = new[]
                {
                    "user name",
                    "SID",
                    "computer name",
                    "full file paths",
                    "raw registry values",
                    "unrelated startup entries",
                    "complete event records"
                }
            },
            environment = new
            {
                windowsVersion = snapshot.WindowsVersion,
                toolVersion = snapshot.ToolVersion,
                claudeVersion = snapshot.ClaudeVersion,
                settingsExecutableVersion = snapshot.SettingsExecutableVersion
            },
            claudeStartup = new
            {
                state = snapshot.Claude.State.ToString(),
                snapshot.Claude.Summary,
                snapshot.Claude.ExecutableExists,
                rawValueIncluded = false
            },
            settingsApp = new
            {
                snapshot.SettingsAppx.PackageFound,
                snapshot.SettingsAppx.ManifestExists,
                snapshot.SettingsAppx.Status,
                snapshot.SettingsAppx.PackageVersion,
                inspectionSucceeded = snapshot.SettingsAppx.Error is null
            },
            crashEvidence = snapshot.Events.Evidence.Select(x => new
            {
                x.ModuleName,
                x.Count,
                x.LatestOccurrence,
                x.ExceptionCode,
                x.FaultOffset
            }),
            eventScanSucceeded = snapshot.Events.Error is null,
            startupPageLiveVerification = new
            {
                state = snapshot.LiveVerification.State.ToString(),
                snapshot.LiveVerification.Summary,
                snapshot.LiveVerification.SettingsProcessWasAlive,
                eventMonitorSucceeded = snapshot.LiveVerification.NewEvents.Error is null,
                newCrashCount = snapshot.LiveVerification.NewEvents.Evidence.Sum(x => x.Count)
            },
            conclusion = BuildConclusion(snapshot)
        };

        var json = JsonSerializer.Serialize(report, Options);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private static string BuildConclusion(DiagnosticSnapshot snapshot)
    {
        if (snapshot.Claude.State == StartupValueState.KnownMalformed)
        {
            return "Confirmed known malformed Claude startup entry. Repair is available.";
        }

        if (snapshot.LiveVerification.State == LiveVerificationState.CrashConfirmed)
        {
            return "Startup page crash was confirmed during this scan's live verification window.";
        }

        if (snapshot.Events.StorageSense is not null)
        {
            return "A new Windows Settings component crash event was observed during this scan.";
        }

        return "No currently actionable known issue was confirmed by this scan.";
    }
}
