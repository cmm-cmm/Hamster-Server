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
public readonly record struct LogTextSpan(int Start, int Length, LogSegmentKind Kind);

/// <summary>Splits a log entry into semantic spans without altering its persisted text.</summary>
public static partial class LogEntryFormatter
{
    /// <summary>
    /// RichTextBox stores every line break as LF even when AppendText receives CRLF. Normalize
    /// before calculating spans so selection offsets continue to match the displayed text.
    /// </summary>
    public static string NormalizeForDisplay(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.Contains('\r') ? text.ReplaceLineEndings("\n") : text;
    }

    public static IReadOnlyList<LogTextSegment> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return ParseSpans(text)
            .Select(span => new LogTextSegment(text.Substring(span.Start, span.Length), span.Kind))
            .ToArray();
    }

    /// <summary>
    /// Returns offsets into the original entry so UI callers can apply formatting without
    /// allocating a substring for every semantic token.
    /// </summary>
    public static IReadOnlyList<LogTextSpan> ParseSpans(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0) return [];

        var spans = new List<LogTextSpan>();
        int position = 0;
        foreach (Match match in SemanticTokenPattern().Matches(text))
        {
            if (match.Index > position)
                AddSpan(spans, position, match.Index - position, LogSegmentKind.Normal);

            AddSpan(spans, match.Index, match.Length, GetKind(match));
            position = match.Index + match.Length;
        }

        if (position < text.Length)
            AddSpan(spans, position, text.Length - position, LogSegmentKind.Normal);

        return spans;
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

    private static void AddSpan(
        List<LogTextSpan> spans, int start, int length, LogSegmentKind kind)
    {
        if (length == 0) return;
        if (spans.Count > 0 && spans[^1].Kind == kind && spans[^1].Start + spans[^1].Length == start)
        {
            var previous = spans[^1];
            spans[^1] = previous with { Length = previous.Length + length };
            return;
        }

        spans.Add(new LogTextSpan(start, length, kind));
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
