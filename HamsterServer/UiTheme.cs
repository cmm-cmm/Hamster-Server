namespace HamsterServer;

/// <summary>
/// Centralized design system. Hamster Server uses the native Windows Forms look: controls keep
/// their system-drawn appearance (<see cref="SystemColors"/>, visual styles) and nothing is
/// custom-painted. Only the few places where the OS has no opinion — service status text,
/// log severity, config syntax highlighting — get explicit colors, and they live here so the
/// forms stay consistent. Do not hard-code <c>Color.FromArgb</c> in a form; add it here.
/// </summary>
internal static class UiTheme
{
    // ── Status colors ────────────────────────────────────────────────────────
    // Used for the Services list and the log. Chosen to stay readable on
    // SystemColors.Window in both the light and high-contrast Windows themes.
    public static readonly Color StatusRunning = Color.FromArgb(0, 128, 0);
    public static readonly Color StatusTransition = Color.FromArgb(180, 95, 0);
    public static readonly Color StatusError = Color.FromArgb(192, 0, 0);
    public static readonly Color StatusStopped = SystemColors.GrayText;

    // ── Taskbar / tray status dot ────────────────────────────────────────────
    // The same four states as above, but drawn as a 4-6 px dot burned into the window icon
    // (BuildStatusIcon composites it at 16/32/48 px). At that size the list colors above read as
    // muddy brown-grey against whatever wallpaper or taskbar tint is behind them, so these are
    // lifted in saturation. They are not interchangeable with Status* — those have to stay legible
    // as text on SystemColors.Window, these have to stay legible as a dot on an arbitrary backdrop.
    public static readonly Color TaskbarDotRunning = Color.FromArgb(39, 174, 96);
    public static readonly Color TaskbarDotTransition = Color.FromArgb(243, 156, 18);
    public static readonly Color TaskbarDotError = Color.FromArgb(231, 76, 60);
    public static readonly Color TaskbarDotStopped = Color.FromArgb(149, 165, 166);

    // ── Log severity ─────────────────────────────────────────────────────────
    public static readonly Color LogNormal = SystemColors.WindowText;
    public static readonly Color LogInfo = Color.FromArgb(0, 0, 160);
    public static readonly Color LogOk = StatusRunning;
    public static readonly Color LogWarn = StatusTransition;
    public static readonly Color LogError = StatusError;
    public static readonly Color LogTimestamp = SystemColors.GrayText;
    // Apache had shared #005AB4 with AccentAction / AccentHttp, so "Apache" and "a neutral action"
    // looked the same in the Tools menu. #9C2777 was picked by measurement, not taste: 7.1:1 on
    // white (log text), and the largest worst-case CIE76 ΔE (13) to every other token here under
    // normal vision and simulated protan / deutan / tritan vision among 14 candidates — the teal
    // #00796B first proposed came out at ΔE 4 against StatusStopped for red-green deficiencies.
    public static readonly Color LogApache = Color.FromArgb(156, 39, 119);
    public static readonly Color LogPhp = Color.FromArgb(112, 48, 160);
    public static readonly Color LogLink = Color.FromArgb(0, 102, 204);

    // ── Config syntax highlighting (httpd.conf / php.ini, light editor) ──────
    // Visual Studio classic palette — readable on SystemColors.Window.
    public static readonly Color SyntaxDefault = SystemColors.WindowText;
    public static readonly Color SyntaxComment = Color.FromArgb(0, 128, 0);
    public static readonly Color SyntaxSection = Color.FromArgb(163, 21, 21);
    public static readonly Color SyntaxKeyword = Color.FromArgb(0, 0, 255);
    public static readonly Color SyntaxValue = Color.FromArgb(64, 64, 64);
    /// <summary>Quoted values — paths and patterns, the parts most often eyeballed for typos.</summary>
    public static readonly Color SyntaxString = Color.FromArgb(163, 21, 21);
    public static readonly Color SyntaxNumber = Color.FromArgb(128, 0, 128);

    // ── Validation ───────────────────────────────────────────────────────────
    public static readonly Color FieldError = Color.FromArgb(255, 235, 235);
    public static readonly Color FieldNormal = SystemColors.Window;

    // ── Fonts ────────────────────────────────────────────────────────────────
    /// <summary>Monospace font for log and config editors, with a guaranteed fallback.</summary>
    public static Font CreateMonospaceFont(float size = 9F)
    {
        foreach (var name in new[] { "Cascadia Mono", "Consolas", "Courier New" })
        {
            try
            {
                var font = new Font(name, size);
                if (font.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return font;
                font.Dispose();
            }
            catch { /* try the next candidate */ }
        }
        return new Font(FontFamily.GenericMonospace, size);
    }

    // ── Semantic accents ─────────────────────────────────────────────────────
    // For icons only — never for control backgrounds, which stay system-drawn.
    public static readonly Color AccentHttp = Color.FromArgb(0, 90, 180);
    public static readonly Color AccentHttps = Color.FromArgb(0, 128, 0);
    public static readonly Color AccentNeutral = Color.FromArgb(90, 90, 90);
    public static readonly Color AccentAction = Color.FromArgb(0, 90, 180);

    /// <summary>Background for a field whose value is valid but worth a second look.</summary>
    public static readonly Color FieldWarning = Color.FromArgb(255, 248, 220);

    // ── Icons ────────────────────────────────────────────────────────────────
    // Windows ships an icon font; drawing its glyphs into a bitmap gives crisp, correctly
    // shaped icons we can tint per meaning, without hand-painting any control.

    /// <summary>
    /// Device-pixel size every glyph and status dot is rendered at: 16 px at 100 %, scaled with the
    /// primary monitor's DPI and snapped to an even number so the icon font's strokes stay on pixel
    /// boundaries (16 / 20 / 24 / 28 / 32 …).
    /// <para>
    /// The bitmaps used to be a fixed 16 px. Under PerMonitorV2 a ToolStrip scales its
    /// <c>ImageScalingSize</c> with the DPI, so at 200 % it stretched those 16 px glyphs to 32 —
    /// blurry — and plain Buttons and ImageLists simply showed them half-size next to doubled text.
    /// </para>
    /// <para>
    /// Measured once, at startup: moving the window to a monitor with another scale keeps these
    /// sizes and lets Windows resample, which is the same trade-off every other WinForms icon makes.
    /// </para>
    /// </summary>
    public static int IconSize { get; } = ComputeIconSize();

    private static int ComputeIconSize()
    {
        float dpi = 96F;
        try
        {
            using var screen = Graphics.FromHwnd(IntPtr.Zero);
            dpi = screen.DpiX;
        }
        catch { /* no desktop (service session, test host): keep 100 % */ }

        int size = (int)Math.Round(16 * dpi / 96F / 2F) * 2;
        return Math.Clamp(size, 16, 64);
    }

    /// <summary>The <see cref="IconSize"/> square, for ImageList.ImageSize and ToolStrip.ImageScalingSize.</summary>
    public static Size IconBox => new(IconSize, IconSize);

    /// <summary>
    /// Makes tool strips show the glyphs at the size they were rendered, instead of resampling
    /// them to a DPI-scaled 16 px box.
    /// </summary>
    public static void ApplyIconScaling(params ToolStrip[] strips)
    {
        foreach (var strip in strips) strip.ImageScalingSize = IconBox;
    }

    /// <summary>
    /// The icon set, as SVG path data on a 24 \u00D7 24 grid: 2 px safe margin, 1.5 px stroke, round caps
    /// and joins, never filled. <see cref="CreateGlyph"/> strokes them in the colour of their meaning.
    /// <para>
    /// These replaced the Segoe Fluent Icons / Segoe MDL2 Assets codepoints: the set no longer
    /// depends on which icon font the machine has (Windows 10 lacks Fluent, Server Core lacks both,
    /// and the old fallback turned every icon into the same dot), and each glyph can be adjusted.
    /// The design sheet that defines them is described in doc/ICONS_AND_LOGO.md. A dot is drawn as a
    /// near-zero segment (<c>h.01</c>) that the round cap turns into a circle.
    /// </para>
    /// </summary>
    public static class Glyph
    {
        public const string Play = "M8 5.5v13l10.5-6.5z";
        public const string Stop = "M6.5 6.5h11v11h-11z";
        public const string Refresh = "M19.5 12a7.5 7.5 0 1 1-2.2-5.3 M19.5 4v4.5H15";
        public const string Settings =
            "M12 9.2a2.8 2.8 0 1 0 0 5.6a2.8 2.8 0 1 0 0-5.6z M12 5.5a6.5 6.5 0 1 0 0 13a6.5 6.5 0 1 0 0-13z " +
            "M12 2.8v2.7 M12 18.5v2.7 M2.8 12h2.7 M18.5 12h2.7 M5.5 5.5l1.9 1.9 M16.6 16.6l1.9 1.9 " +
            "M5.5 18.5l1.9-1.9 M16.6 7.4l1.9-1.9";
        public const string World =
            "M12 3.5a8.5 8.5 0 1 0 0 17a8.5 8.5 0 1 0 0-17z M3.5 12h17 " +
            "M12 3.5c2.5 2.3 3.5 5.3 3.5 8.5s-1 6.2-3.5 8.5 M12 3.5c-2.5 2.3-3.5 5.3-3.5 8.5s1 6.2 3.5 8.5";
        public const string Folder =
            "M3.5 7a1.5 1.5 0 0 1 1.5-1.5h4l2 2h8a1.5 1.5 0 0 1 1.5 1.5v8.5a1.5 1.5 0 0 1-1.5 1.5H5a1.5 1.5 0 0 1-1.5-1.5z";
        public const string Server =
            "M4.5 4.5h15v6h-15z M4.5 13.5h15v6h-15z M7.5 7.5h4 M7.5 16.5h4 M16 7.5h.01 M16 16.5h.01";
        public const string Add = "M12 5v14 M5 12h14";
        public const string Edit = "M15.5 4.5l4 4L9 19H5v-4z M13.5 6.5l4 4";
        public const string Delete = "M5 7h14 M9.5 7V4.5h5V7 M7 7l.8 12.5h8.4L17 7 M10.5 10.5v6 M13.5 10.5v6";
        public const string Lock = "M6.5 11h11v8.5h-11z M8.5 11V8a3.5 3.5 0 0 1 7 0v3";
        public const string Unlock = "M6.5 11h11v8.5h-11z M8.5 11V8a3.5 3.5 0 0 1 6.8-1.2";
        public const string Clear = "M4 6.5h10 M4 10.5h10 M4 14.5h6 M14.5 14.5l5 5 M19.5 14.5l-5 5";
        public const string Warning = "M12 4l9 16H3z M12 10v4.5 M12 17.5h.01";
        public const string Link =
            "M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1 M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1";
        public const string Minimize = "M5 13h14";
        public const string Exit = "M12 3.5v8 M7.2 6.3a7.5 7.5 0 1 0 9.6 0";
        public const string Info = "M12 3.5a8.5 8.5 0 1 0 0 17a8.5 8.5 0 1 0 0-17z M12 11v5.5 M12 7.8h.01";
        public const string Download = "M12 4v11 M7.5 10.5L12 15l4.5-4.5 M5 19.5h14";
        public const string Code = "M8.5 7l-5 5 5 5 M15.5 7l5 5-5 5 M13.5 5l-3 14";
        public const string Page = "M6.5 3.5h7l4 4v13h-11z M13.5 3.5v4h4 M9 12h6 M9 15.5h6";
        // Two opposed arrows: a port is where traffic goes in and out. Deliberately not Server \u2014
        // that one belongs to Apache itself, and sharing it made the two menus look alike.
        public const string Ports = "M4 9h14 M14.5 5.5L18 9l-3.5 3.5 M20 15H6 M9.5 11.5L6 15l3.5 3.5";
        public const string Accept = "M5 12.5l4.5 4.5L19 7.5";
        public const string Star = "M12 3.8l2.5 5.2 5.7.8-4.1 4 1 5.7L12 16.8l-5.1 2.7 1-5.7-4.1-4 5.7-.8z";
        public const string SelectAll =
            "M4 6.5l1.5 1.5 3-3 M11 6.5h9 M4 12.5l1.5 1.5 3-3 M11 12.5h9 M4 18.5l1.5 1.5 3-3 M11 18.5h9";
        public const string ClearSelection = "M4.5 4.5h5v5h-5z M4.5 14.5h5v5h-5z M12.5 7h7 M12.5 17h7";
        public const string Wrench =
            "M15 4.5a4.5 4.5 0 0 0-4.2 6.1l-6.3 6.3a1.8 1.8 0 0 0 2.6 2.6l6.3-6.3a4.5 4.5 0 0 0 6.1-4.2" +
            "l-2.7 2.7-2.6-.9-.9-2.6 2.7-2.7a4.5 4.5 0 0 0-1-1z";
    }

    /// <summary>Stroke width of every glyph, in units of the 24 px design grid.</summary>
    public const float GlyphStroke = 1.5F;

    /// <summary>
    /// Puts a glyph on a plain <see cref="Button"/>. AutoSize is normally required rather than
    /// cosmetic: the hand-written designer widths leave no room for a 16 px image, so a fixed
    /// width clips the caption instead of widening the button.
    /// <para>
    /// Pass <paramref name="autoSize"/> = false for a button anchored Left|Right inside a
    /// TableLayoutPanel cell — AutoSize overrides that stretch and the button would snap to its
    /// text width, breaking a column that was laid out to line up with the rows above it. Such a
    /// button needs its cell widened by hand instead.
    /// </para>
    /// </summary>
    public static void ApplyGlyph(Button button, string glyph, Color color, bool autoSize = true)
    {
        button.Image = CreateGlyph(glyph, color);
        button.ImageAlign = ContentAlignment.MiddleLeft;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
        if (!autoSize) return;
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    /// <summary>A filled status dot — used where the meaning is a state, not an action.</summary>
    /// <param name="size">Device pixels; <c>0</c> (the default) means <see cref="IconSize"/>.</param>
    public static Bitmap CreateStatusDot(Color fill, int size = 0)
    {
        if (size <= 0) size = IconSize;
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Proportions of the original 16 px dot (3 px inset each side, 1 px outline), kept at
        // every size so a 32 px dot reads as the same mark rather than a bigger blob.
        int inset = Math.Max(3, size * 3 / 16);
        int d = Math.Max(6, size - 2 * inset);
        int off = (size - d) / 2;
        using var brush = new SolidBrush(fill);
        using var pen = new Pen(ControlPaint.Dark(fill, 0.15f), Math.Max(1F, size / 16F));
        g.FillEllipse(brush, off, off, d, d);
        g.DrawEllipse(pen, off, off, d, d);
        return bmp;
    }

    /// <summary>
    /// A <see cref="Glyph"/> (SVG path data on the 24 px grid) stroked in <paramref name="color"/>
    /// at <paramref name="size"/> device pixels.
    /// </summary>
    /// <param name="size">Device pixels; <c>0</c> (the default) means <see cref="IconSize"/>.</param>
    public static Bitmap CreateGlyph(string glyph, Color color, int size = 0)
    {
        if (size <= 0) size = IconSize;
        float scale = size / 24F;

        using var path = SvgPath.Parse(glyph);
        using (var toPixels = new System.Drawing.Drawing2D.Matrix())
        {
            toPixels.Scale(scale, scale);
            path.Transform(toPixels);
        }

        var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        // 1.5 grid units is exactly 1 px at 16 px, which anti-aliasing spreads over two pixel rows
        // and reads as faded next to the label; a floor of 1.25 px keeps the 100 % icons as dark
        // as the font glyphs they replaced. From 20 px up the grid value wins.
        using var pen = new Pen(color, Math.Max(1.25F, GlyphStroke * scale))
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        g.DrawPath(pen, path);
        return bmp;
    }

    /// <summary>
    /// The readable form of <paramref name="color"/> on a selected row. Windows paints the
    /// selection with <see cref="SystemColors.Highlight"/>, where the dark status colors fall
    /// below usable contrast, so the three meaningful ones are lightened toward the selection's
    /// own text color and everything else simply uses it.
    /// </summary>
    public static Color OnHighlight(Color color) =>
        color == StatusRunning || color == StatusTransition || color == StatusError
            ? Blend(color, SystemColors.HighlightText, 0.62f)
            : SystemColors.HighlightText;

    private static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
        from.A,
        from.R + (int)((to.R - from.R) * amount),
        from.G + (int)((to.G - from.G) * amount),
        from.B + (int)((to.B - from.B) * amount));

    /// <summary>
    /// Paints one cell of a Details-view <see cref="ListView"/> the way Windows would, with one
    /// difference: a selected row keeps its per-cell status color instead of being flattened to
    /// the system selection color. Windows discards that color on the selected row — which is
    /// the row the user is acting on, so the state of the service they are about to start or
    /// stop was the one state the colors never described.
    /// </summary>
    /// <remarks>
    /// This is the only owner-drawn control in the app, and it stays as close to the system
    /// rendering as it can: system colors, the same icon placement, the same grid lines. Wire it
    /// up with <c>OwnerDraw = true</c>, <c>DrawColumnHeader</c> set to <c>e.DrawDefault = true</c>,
    /// an empty <c>DrawItem</c> handler, and <c>DrawSubItem</c> pointing here.
    /// </remarks>
    public static void DrawStatusListSubItem(ListView list, DrawListViewSubItemEventArgs e)
    {
        if (e.Item is not { } item || e.SubItem is not { } sub) return;

        bool selected = item.Selected;
        using (var back = new SolidBrush(selected ? SystemColors.Highlight : list.BackColor))
            e.Graphics.FillRectangle(back, e.Bounds);

        var cell = e.Bounds;
        if (e.ColumnIndex == 0 && list.SmallImageList is { } icons &&
            item.ImageIndex >= 0 && item.ImageIndex < icons.Images.Count)
        {
            var size = icons.ImageSize;
            icons.Draw(e.Graphics, cell.Left + 4, cell.Top + (cell.Height - size.Height) / 2, item.ImageIndex);
            int shift = size.Width + 2;   // matches the gap the system leaves after the icon
            cell = new Rectangle(cell.Left + shift, cell.Top, cell.Width - shift, cell.Height);
        }

        var align = e.ColumnIndex < list.Columns.Count
            ? list.Columns[e.ColumnIndex].TextAlign
            : HorizontalAlignment.Left;
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix
                  | align switch
                  {
                      HorizontalAlignment.Right => TextFormatFlags.Right,
                      HorizontalAlignment.Center => TextFormatFlags.HorizontalCenter,
                      _ => TextFormatFlags.Left
                  };
        var fore = selected ? OnHighlight(sub.ForeColor) : sub.ForeColor;
        TextRenderer.DrawText(e.Graphics, sub.Text, sub.Font, Rectangle.Inflate(cell, -4, 0), fore, flags);

        // Owner drawing replaces the default item painting, grid lines included — redraw them.
        if (list.GridLines)
        {
            using var pen = new Pen(SystemColors.ControlLight);
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom - 1);
        }
    }

    /// <summary>Status dot color for a service state; keep in step with the Status column text.</summary>
    public static Color StatusColor(bool running, bool transitioning, bool error) =>
        error ? StatusError
        : transitioning ? StatusTransition
        : running ? StatusRunning
        : StatusStopped;

    // ── Layout metrics ───────────────────────────────────────────────────────
    public const int LabelColumnWidth = 150;
    public const int DialogPadding = 10;
    public const int GroupPadding = 10;
    public const int RowHeight = 30;
}
