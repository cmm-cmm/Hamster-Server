using HamsterServer.Core;

namespace HamsterServer.Tests;

[TestClass]
public sealed class LogEntryFormatterTests
{
    [TestMethod]
    public void Parse_AssignsSemanticKindsAndPreservesOriginalText()
    {
        const string text = "[12:34:56] [Apache] [OK] Port 443 ready → https://localhost:443/app";

        var segments = LogEntryFormatter.Parse(text);

        Assert.AreEqual(text, string.Concat(segments.Select(segment => segment.Text)));
        CollectionAssert.AreEqual(
            new[]
            {
                LogSegmentKind.Timestamp,
                LogSegmentKind.Normal,
                LogSegmentKind.Apache,
                LogSegmentKind.Normal,
                LogSegmentKind.Success,
                LogSegmentKind.Normal,
                LogSegmentKind.Link
            },
            segments.Select(segment => segment.Kind).ToArray());
    }

    [TestMethod]
    public void Parse_RecognizesSeveritySourceAndEndpointCaseInsensitively()
    {
        const string text = "[php] [warn] Retry 127.0.0.1:9000 [ERROR] [INFO]";

        var kinds = LogEntryFormatter.Parse(text).Select(segment => segment.Kind).ToArray();

        CollectionAssert.Contains(kinds, LogSegmentKind.Php);
        CollectionAssert.Contains(kinds, LogSegmentKind.Warning);
        CollectionAssert.Contains(kinds, LogSegmentKind.Link);
        CollectionAssert.Contains(kinds, LogSegmentKind.Error);
        CollectionAssert.Contains(kinds, LogSegmentKind.Information);
    }

    [TestMethod]
    public void Parse_ReturnsOneNormalSegmentForPlainText()
    {
        const string text = "Apache stopped.";

        var segment = LogEntryFormatter.Parse(text).Single();

        Assert.AreEqual(text, segment.Text);
        Assert.AreEqual(LogSegmentKind.Normal, segment.Kind);
    }

    [TestMethod]
    public void ParseSpans_ReferencesOriginalTextWithoutChangingContent()
    {
        const string text = "[10:20:30] [PHP] Listening at localhost:9000";

        var spans = LogEntryFormatter.ParseSpans(text);

        Assert.AreEqual(text, string.Concat(spans.Select(span => text.Substring(span.Start, span.Length))));
        Assert.IsTrue(spans.Any(span => span.Kind == LogSegmentKind.Timestamp));
        Assert.IsTrue(spans.Any(span => span.Kind == LogSegmentKind.Php));
        Assert.IsTrue(spans.Any(span => span.Kind == LogSegmentKind.Link));
    }

    [TestMethod]
    public void NormalizeForDisplay_KeepsSemanticOffsetsAlignedAcrossCrLfLines()
    {
        const string text = "[10:20:30] [Apache] first\r\n[10:20:31] [WARN] second";

        var normalized = LogEntryFormatter.NormalizeForDisplay(text);
        var spans = LogEntryFormatter.ParseSpans(normalized);

        Assert.DoesNotContain('\r', normalized);
        Assert.AreEqual(text.ReplaceLineEndings("\n"), normalized);
        Assert.AreEqual(
            normalized,
            string.Concat(spans.Select(span => normalized.Substring(span.Start, span.Length))));
        Assert.IsTrue(spans.All(span => span.Start >= 0 && span.Start + span.Length <= normalized.Length));
    }
}
