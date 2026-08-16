using System.Text.Json;
using SettingsCrashRepair.Models;

namespace SettingsCrashRepair.Services;

public sealed class BackupStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string BackupDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SettingsCrashRepair",
        "backups");

    public async Task<string> SaveAsync(ClaudeBackup backup, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(BackupDirectory);
        var fileName = $"claude-run-{backup.CreatedAt:yyyyMMdd-HHmmss-fff}.json";
        var path = Path.Combine(BackupDirectory, fileName);
        var json = JsonSerializer.Serialize(backup, JsonOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
        return path;
    }

    public async Task<(ClaudeBackup? Backup, string? Path)> LoadLatestAsync(
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(BackupDirectory))
        {
            return (null, null);
        }

        var path = Directory.EnumerateFiles(BackupDirectory, "claude-run-*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (path is null)
        {
            return (null, null);
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        var backup = JsonSerializer.Deserialize<ClaudeBackup>(json, JsonOptions);
        return (backup, path);
    }
}
