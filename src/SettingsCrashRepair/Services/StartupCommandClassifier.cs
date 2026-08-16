using SettingsCrashRepair.Models;

namespace SettingsCrashRepair.Services;

public static class StartupCommandClassifier
{
    public static StartupValueState Classify(
        string? rawValue,
        string expectedExecutablePath,
        out string correctValue)
    {
        correctValue = BuildCorrectValue(expectedExecutablePath);

        if (rawValue is null)
        {
            return StartupValueState.Missing;
        }

        if (rawValue.Equals(correctValue, StringComparison.OrdinalIgnoreCase))
        {
            return StartupValueState.Healthy;
        }

        var knownMalformed = BuildKnownMalformedValue(expectedExecutablePath);
        if (rawValue.Equals(knownMalformed, StringComparison.OrdinalIgnoreCase))
        {
            return StartupValueState.KnownMalformed;
        }

        return StartupValueState.Unexpected;
    }

    public static string BuildCorrectValue(string executablePath) =>
        $"\"{executablePath}\" --startup";

    public static string BuildKnownMalformedValue(string executablePath) =>
        $"\"\\\"{executablePath}\\\" --startup\"";
}
