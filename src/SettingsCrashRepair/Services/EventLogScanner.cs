using System.Globalization;
using System.Xml.Linq;
using SettingsCrashRepair.Models;

namespace SettingsCrashRepair.Services;

public sealed class EventLogScanner
{
    private static readonly string[] TargetModules =
    [
        "SettingsHandlers_Startup.dll",
        "SettingsHandlers_StorageSense.dll"
    ];

    public async Task<EventScanResult> ScanAsync(
        DateTimeOffset? since = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = since is null
                ? "*[System[(EventID=1000)]]"
                : "*[System[(EventID=1000) and TimeCreated[timediff(@SystemTime) <= 300000]]]";
            var result = await ProcessRunner.RunAsync(
                "wevtutil.exe",
                ["qe", "Application", $"/q:{query}", "/f:xml", since is null ? "/c:400" : "/c:100", "/rd:true"],
                cancellationToken);

            if (result.ExitCode != 0)
            {
                return new EventScanResult([], CleanError(result.StandardError));
            }

            var eventXml = StripXmlDeclarations(result.StandardOutput);
            var document = XDocument.Parse($"<Events>{eventXml}</Events>");
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
            var matches = new List<(string Module, DateTimeOffset? Time, string Code, string Offset)>();

            foreach (var eventElement in document.Descendants(ns + "Event"))
            {
                var data = eventElement.Descendants(ns + "EventData")
                    .Elements(ns + "Data")
                    .Select(x => x.Value)
                    .ToList();

                if (data.Count < 7 ||
                    !data[0].Equals("SystemSettings.exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var module = TargetModules.FirstOrDefault(target =>
                    data[3].Equals(target, StringComparison.OrdinalIgnoreCase));
                if (module is null)
                {
                    continue;
                }

                var timeText = eventElement.Descendants(ns + "TimeCreated")
                    .FirstOrDefault()?.Attribute("SystemTime")?.Value;
                DateTimeOffset? time = DateTimeOffset.TryParse(
                    timeText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var parsedTime)
                    ? parsedTime
                    : null;

                matches.Add((module, time, data[6], data.Count > 7 ? data[7] : "未知"));
            }

            var evidence = matches
                .Where(match => since is null ||
                    (match.Time is not null && match.Time.Value >= since.Value.ToUniversalTime()))
                .GroupBy(x => x.Module, StringComparer.OrdinalIgnoreCase)
                .Select(group => new CrashEvidence(
                    group.Key,
                    group.Count(),
                    group.Max(x => x.Time),
                    group.First().Code,
                    group.First().Offset))
                .OrderBy(x => x.ModuleName)
                .ToList();

            return new EventScanResult(evidence);
        }
        catch (Exception ex)
        {
            return new EventScanResult([], CleanError(ex.Message));
        }
    }

    private static string CleanError(string error)
    {
        var firstLine = error.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstLine) ? "无法读取应用程序事件日志。" : firstLine.Trim();
    }

    private static string StripXmlDeclarations(string xml)
    {
        const string declarationStart = "<?xml";
        var remaining = xml;
        var builder = new System.Text.StringBuilder(xml.Length);

        while (true)
        {
            var declaration = remaining.IndexOf(declarationStart, StringComparison.OrdinalIgnoreCase);
            if (declaration < 0)
            {
                builder.Append(remaining);
                break;
            }

            builder.Append(remaining.AsSpan(0, declaration));
            var declarationEnd = remaining.IndexOf("?>", declaration, StringComparison.Ordinal);
            if (declarationEnd < 0)
            {
                builder.Append(remaining.AsSpan(declaration));
                break;
            }

            remaining = remaining[(declarationEnd + 2)..];
        }

        return builder.ToString();
    }
}
