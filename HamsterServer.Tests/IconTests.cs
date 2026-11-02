using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

namespace HamsterServer.Tests;

[TestClass]
public sealed class IconTests
{
    private static IEnumerable<(string Name, string Data)> AllGlyphs() =>
        typeof(UiTheme.Glyph)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (field.Name, (string)field.GetRawConstantValue()!));

    [TestMethod]
    public void ToReverseLogo_TurnsOnlyTheServerBarsIvory()
    {
        var ico = Path.Combine(FindRepositoryRoot(), "HamsterServer", "server.ico");
        foreach (var size in new[] { 16, 24, 32, 48, 64 })
        {
            using var icon = new Icon(ico, size, size);
            using var colour = icon.ToBitmap();
            using var reverse = icon.ToBitmap();
            UiTheme.ToReverseLogo(reverse);

            int inkBars = 0, ivoryBars = 0, changedAbove = 0, ledsChanged = 0;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color before = colour.GetPixel(x, y), after = reverse.GetPixel(x, y);
                    Assert.AreEqual(before.A, after.A, $"{size}px: alpha changed at {x},{y}");
                    bool isInk = before.A > 128 && Near(before, UiTheme.BrandInk);
                    if (y < size * 0.58)
                    {
                        if (before != after) changedAbove++;
                        continue;
                    }
                    if (isInk) inkBars++;
                    if (isInk && Near(after, UiTheme.BrandIvory)) ivoryBars++;
                    if (before.A > 128 && Math.Max(before.R, Math.Max(before.G, before.B)) - Math.Min(before.R, Math.Min(before.G, before.B)) > 80
                        && before != after) ledsChanged++;
                }
            }
            Assert.IsTrue(inkBars > 0, $"{size}px: the frame has no server bars to recolour");
            Assert.AreEqual(inkBars, ivoryBars, $"{size}px: some bar pixels stayed ink");
            Assert.AreEqual(0, changedAbove, $"{size}px: the eyes or nose were recoloured");
            Assert.AreEqual(0, ledsChanged, $"{size}px: an LED was recoloured");
        }

        static bool Near(Color a, Color b) =>
            Math.Abs(a.R - b.R) <= 12 && Math.Abs(a.G - b.G) <= 12 && Math.Abs(a.B - b.B) <= 12;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "HamsterServer.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("HamsterServer.slnx not found.");
    }

    [TestMethod]
    public void SvgPath_ParsesAbsoluteRelativeAndShorthandCommands()
    {
        using var square = SvgPath.Parse("M1 1h4v4h-4z");
        AssertBounds(square, 1, 1, 4, 4);

        using var lines = SvgPath.Parse("M0 0L10 0 10 5");   // implicit lineto repetition
        AssertBounds(lines, 0, 0, 10, 5);

        using var packed = SvgPath.Parse("M1.5.5l-1-.5");    // "1.5.5" is two numbers
        AssertBounds(packed, 0.5F, 0, 1, 0.5F);
    }

    [TestMethod]
    public void SvgPath_ConvertsArcsToTheRightCircle()
    {
        // Two half-circle arcs: the construction every round glyph in the set uses.
        using var circle = SvgPath.Parse("M12 3.5a8.5 8.5 0 1 0 0 17a8.5 8.5 0 1 0 0-17z");
        AssertBounds(circle, 3.5F, 3.5F, 17, 17, tolerance: 0.05F);

        // Packed arc flags, and radii too small to reach the end point (scaled up by the spec).
        using var packedFlags = SvgPath.Parse("M0 0a1 1 0 0110 0");
        AssertBounds(packedFlags, 0, -5, 10, 5, tolerance: 0.05F);
    }

    [TestMethod]
    public void SvgPath_RejectsMalformedData()
    {
        Assert.ThrowsExactly<FormatException>(() => SvgPath.Parse("10 10"));
        Assert.ThrowsExactly<FormatException>(() => SvgPath.Parse("M1 2 X3 4"));
        Assert.ThrowsExactly<FormatException>(() => SvgPath.Parse("M1 2z 3 4"));
        Assert.ThrowsExactly<FormatException>(() => SvgPath.Parse("M0 0a1 1 0 2 0 1 1"));
    }

    [TestMethod]
    public void EveryGlyph_StaysInsideTheTwoPixelSafeMargin()
    {
        foreach (var (name, data) in AllGlyphs())
        {
            using var path = SvgPath.Parse(data);
            var bounds = path.GetBounds();
            Assert.IsTrue(bounds.Width > 0 || bounds.Height > 0, $"{name} draws nothing.");
            // 2 px margin on the 24 px grid, minus a little for the settings cog's tick ends.
            Assert.IsTrue(bounds.Left >= 1.5F && bounds.Top >= 1.5F &&
                          bounds.Right <= 22.5F && bounds.Bottom <= 22.5F,
                $"{name} leaves the safe area: {bounds}");
        }
    }

    [TestMethod]
    public void EveryGlyph_RendersVisiblyAndUnclippedAtEverySize()
    {
        foreach (var size in new[] { 16, 20, 24, 32, 48 })
        {
            foreach (var (name, data) in AllGlyphs())
            {
                using var bitmap = UiTheme.CreateGlyph(data, Color.Black, size);
                Assert.AreEqual(size, bitmap.Width);
                // Minimize is one short dash — about half a row of ink at 16 px — so the floor is
                // half the icon width: enough to catch a glyph that failed to draw at all.
                Assert.IsTrue(CountInk(bitmap) > size / 2, $"{name} at {size} px is almost empty.");
                Assert.IsFalse(TouchesEdge(bitmap), $"{name} at {size} px is clipped by the bitmap edge.");
            }
        }
    }

    /// <summary>
    /// Writes a contact sheet of the whole set when HAMSTER_GLYPH_SHEET names a .png path — for
    /// reviewing a glyph change by eye. Does nothing (and passes) otherwise.
    /// </summary>
    [TestMethod]
    public void WriteGlyphContactSheet_WhenRequested()
    {
        var target = Environment.GetEnvironmentVariable("HAMSTER_GLYPH_SHEET");
        if (string.IsNullOrWhiteSpace(target)) return;

        var glyphs = AllGlyphs().ToArray();
        int[] sizes = [16, 24, 32, 48];
        const int cell = 64, label = 120;
        using var sheet = new Bitmap(label + sizes.Length * cell, glyphs.Length * cell, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(sheet);
        g.Clear(Color.White);
        using var font = new Font("Segoe UI", 9F);
        for (int row = 0; row < glyphs.Length; row++)
        {
            g.DrawString(glyphs[row].Name, font, Brushes.Black, 4, row * cell + cell / 2 - 8);
            for (int col = 0; col < sizes.Length; col++)
            {
                using var icon = UiTheme.CreateGlyph(glyphs[row].Data, Color.FromArgb(22, 24, 29), sizes[col]);
                g.DrawImageUnscaled(icon, label + col * cell + (cell - sizes[col]) / 2, row * cell + (cell - sizes[col]) / 2);
            }
        }
        sheet.Save(target, ImageFormat.Png);
    }

    private static void AssertBounds(GraphicsPath path, float x, float y, float w, float h, float tolerance = 0.001F)
    {
        var b = path.GetBounds();
        Assert.AreEqual(x, b.X, tolerance, "x");
        Assert.AreEqual(y, b.Y, tolerance, "y");
        Assert.AreEqual(w, b.Width, tolerance, "width");
        Assert.AreEqual(h, b.Height, tolerance, "height");
    }

    private static int CountInk(Bitmap bitmap)
    {
        int ink = 0;
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).A > 64) ink++;
        return ink;
    }

    private static bool TouchesEdge(Bitmap bitmap)
    {
        int last = bitmap.Width - 1;
        for (int i = 0; i <= last; i++)
        {
            if (bitmap.GetPixel(i, 0).A > 128 || bitmap.GetPixel(i, last).A > 128 ||
                bitmap.GetPixel(0, i).A > 128 || bitmap.GetPixel(last, i).A > 128)
                return true;
        }
        return false;
    }
}
