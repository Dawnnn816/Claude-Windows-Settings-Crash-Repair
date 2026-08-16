using SettingsCrashRepair.Models;
using SettingsCrashRepair.Services;

var expectedPath = @"C:\Users\Example\AppData\Local\AnthropicClaude\claude.exe";
var tests = new (string Name, string? Value, StartupValueState Expected)[]
{
    ("missing", null, StartupValueState.Missing),
    ("healthy", $"\"{expectedPath}\" --startup", StartupValueState.Healthy),
    ("known malformed", $"\"\\\"{expectedPath}\\\" --startup\"", StartupValueState.KnownMalformed),
    ("unquoted", $"{expectedPath} --startup", StartupValueState.Unexpected),
    ("different argument", $"\"{expectedPath}\" --silent", StartupValueState.Unexpected),
    ("different executable", "\"C:\\Temp\\claude.exe\" --startup", StartupValueState.Unexpected),
    ("extra whitespace", $"\"{expectedPath}\"  --startup", StartupValueState.Unexpected),
    ("malformed prefix only", $"\\\"{expectedPath}\\\" --startup", StartupValueState.Unexpected)
};

var failed = 0;
foreach (var test in tests)
{
    var actual = StartupCommandClassifier.Classify(test.Value, expectedPath, out var correct);
    if (actual != test.Expected)
    {
        Console.Error.WriteLine($"FAIL {test.Name}: expected {test.Expected}, got {actual}");
        failed++;
    }

    var expectedCorrect = $"\"{expectedPath}\" --startup";
    if (!correct.Equals(expectedCorrect, StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"FAIL {test.Name}: correction was not deterministic");
        failed++;
    }
}

if (failed > 0)
{
    Console.Error.WriteLine($"{failed} assertion(s) failed.");
    return 1;
}

Console.WriteLine($"PASS: {tests.Length} startup command classification tests.");

if (args.Contains("--integration", StringComparer.OrdinalIgnoreCase))
{
    var backupStore = new BackupStore();
    var claudeService = new ClaudeStartupService(backupStore);
    var eventScanner = new EventLogScanner();
    var appxService = new SettingsAppxService();
    var diagnosticService = new DiagnosticService(
        claudeService,
        eventScanner,
        appxService,
        new StartupPageVerifier(eventScanner));
    var runLiveVerification = args.Contains("--live", StringComparer.OrdinalIgnoreCase);
    var snapshot = await diagnosticService.ScanAsync(performLiveVerification: runLiveVerification);

    Console.WriteLine($"INTEGRATION ClaudeState={snapshot.Claude.State}");
    Console.WriteLine($"INTEGRATION SettingsPackageFound={snapshot.SettingsAppx.PackageFound}");
    Console.WriteLine($"INTEGRATION SettingsStatus={snapshot.SettingsAppx.Status}");
    Console.WriteLine($"INTEGRATION EventScanError={snapshot.Events.Error ?? "<none>"}");
    Console.WriteLine($"INTEGRATION StartupCrashCount={snapshot.Events.Startup?.Count ?? 0}");
    Console.WriteLine($"INTEGRATION StorageSenseCrashCount={snapshot.Events.StorageSense?.Count ?? 0}");
    Console.WriteLine($"INTEGRATION LiveVerificationState={snapshot.LiveVerification.State}");
    Console.WriteLine($"INTEGRATION LiveNewStartupCrashCount={snapshot.LiveVerification.NewEvents.Startup?.Count ?? 0}");
    Console.WriteLine($"INTEGRATION LiveVerificationError={snapshot.LiveVerification.Error ?? "<none>"}");

    var reportPath = Path.Combine(Path.GetTempPath(), $"SettingsCrashRepair-test-{Guid.NewGuid():N}.json");
    try
    {
        await ReportExporter.ExportAsync(snapshot, reportPath);
        var report = await File.ReadAllTextAsync(reportPath);
        var forbiddenValues = new[]
        {
            Environment.UserName,
            Environment.MachineName,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            snapshot.Claude.ExpectedExecutablePath,
            snapshot.Claude.RawValue ?? string.Empty
        }.Where(x => !string.IsNullOrWhiteSpace(x));

        var leaked = forbiddenValues.FirstOrDefault(value =>
            report.Contains(value, StringComparison.OrdinalIgnoreCase));
        if (leaked is not null)
        {
            Console.Error.WriteLine("FAIL: sanitized report contains a forbidden local value.");
            return 1;
        }

        Console.WriteLine("PASS: sanitized report excludes local identity, paths, and raw registry data.");
    }
    finally
    {
        if (File.Exists(reportPath))
        {
            File.Delete(reportPath);
        }
    }
}

return 0;
