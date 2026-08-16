using Microsoft.Win32;

namespace SettingsCrashRepair.Models;

public enum StartupValueState
{
    Missing,
    Healthy,
    KnownMalformed,
    Unexpected,
    ReadError
}

public sealed record StartupInspection(
    StartupValueState State,
    string Summary,
    string? RawValue,
    string ExpectedExecutablePath,
    string CorrectValue,
    bool ExecutableExists,
    RegistryValueKind? ValueKind,
    string? Error = null);

public sealed record ClaudeBackup(
    DateTimeOffset CreatedAt,
    string KeyPath,
    string ValueName,
    string OriginalValue,
    string RepairedValue,
    RegistryValueKind ValueKind,
    string FormatVersion = "1");

public sealed record OperationResult(bool Success, string Message);
