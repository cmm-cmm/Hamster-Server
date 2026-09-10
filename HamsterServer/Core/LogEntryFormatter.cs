using System.Text.RegularExpressions;

namespace HamsterServer.Core;

public enum LogSegmentKind
{
    Normal,
    Timestamp,
    Information,
    Success,
    Warning,
    Error,
    Apache,
    Php,
    Link
}

public readonly record struct LogTextSegment(string Text, LogSegmentKind Kind);

/// <summary>Splits a log entry into semantic spans without altering its persisted text.</summary>
public static partial class LogEntryFormatter
{
    public static IReadOnlyList<LogTextSegment> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0) return [];

        var segments = new List<LogTextSegment>();
        int position = 0;
        foreach (Match match in SemanticTokenPattern().Matches(text))
        {
            if (match.Index > position)
                AddSegment(segments, text[position..match.Index], LogSegmentKind.Normal);

            AddSegment(segments, match.Value, GetKind(match));
            position = match.Index + match.Length;
        }

        if (position < text.Length)
            AddSegment(segments, text[position..], LogSegmentKind.Normal);

        return segments;
    }

    private static LogSegmentKind GetKind(Match match)
    {
        if (match.Groups["timestamp"].Success) return LogSegmentKind.Timestamp;
        if (match.Groups["link"].Success || match.Groups["endpoint"].Success)
            return LogSegmentKind.Link;

        return match.Value.ToUpperInvariant() switch
        {
            "[INFO]" => LogSegmentKind.Information,
            "[OK]" => LogSegmentKind.Success,
            "[WARN]" => LogSegmentKind.Warning,
            "[ERROR]" => LogSegmentKind.Error,
            "[APACHE]" => LogSegmentKind.Apache,
            "[PHP]" => LogSegmentKind.Php,
            _ => LogSegmentKind.Normal
        };
    }

    private static void AddSegment(
        List<LogTextSegment> segments, string text, LogSegmentKind kind)
    {
        if (text.Length == 0) return;
        if (segments.Count > 0 && segments[^1].Kind == kind)
        {
            var previous = segments[^1];
            segments[^1] = previous with { Text = previous.Text + text };
            return;
        }

        segments.Add(new LogTextSegment(text, kind));
    }

    [GeneratedRegex(
        @"(?<timestamp>\[(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d\])|" +
        @"(?<level>\[(?:ERROR|WARN|OK|INFO)\])|" +
        @"(?<source>\[(?:Apache|PHP)\])|" +
        @"(?<link>https?://[^\s]+)|" +
        @"(?<endpoint>\b(?:localhost|127\.0\.0\.1):\d{1,5}\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SemanticTokenPattern();
}
