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
    public static readonly Color LogApache = Color.FromArgb(0, 90, 180);
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

    /// <summary>Glyph codepoints from Segoe Fluent Icons / Segoe MDL2 Assets.</summary>
    public static class Glyph
    {
        public const string Play = "\uE768";
        public const string Stop = "\uE71A";
        public const string Refresh = "\uE72C";
        public const string Settings = "\uE713";
        public const string World = "\uE774";
        public const string Folder = "\uE8B7";
        public const string Server = "\uE839";
        public const string Add = "\uE710";
        public const string Edit = "\uE70F";
        public const string Delete = "\uE74D";
        public const string Lock = "\uE72E";
        public const string Unlock = "\uE785";
        public const string Clear = "\uE894";
        public const string Warning = "\uE7BA";
        public const string Link = "\uE71B";
        public const string Minimize = "\uE921";
        public const string Exit = "\uE7E8";
        public const string Info = "\uE946";
        public const string Download = "\uE896";
        public const string Code = "\uE943";
        public const string Page = "\uE7C3";
        // Two opposed arrows: a port is where traffic goes in and out. Deliberately not Server \u2014
        // that one belongs to Apache itself, and sharing it made the two menus look alike.
        public const string Ports = "\uE8AB";
        public const string Accept = "\uE8FB";
        public const string Star = "\uE734";
        public const string SelectAll = "\uE8B3";
        public const string ClearSelection = "\uE8E6";
        public const string Wrench = "\uE90F";
    }

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

    private static readonly string? IconFontName = ResolveIconFont();

    private static string? ResolveIconFont()
    {
        foreach (var name in new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
        {
            try
            {
                using var probe = new Font(name, 10F);
                if (probe.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return name;
            }
            catch { /* try the next candidate */ }
        }
        return null;   // fall back to a plain dot
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
    /// An icon-font glyph tinted <paramref name="color"/>. Falls back to a status dot when the
    /// icon font is unavailable, so the image list keeps its indices either way.
    /// </summary>
    /// <param name="size">Device pixels; <c>0</c> (the default) means <see cref="IconSize"/>.</param>
    public static Bitmap CreateGlyph(string glyph, Color color, int size = 0)
    {
        if (size <= 0) size = IconSize;
        if (IconFontName is null) return CreateStatusDot(color, size);

        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        // 0.875 rather than the em-size the icon font is drawn for: at 1.0 several glyphs with
        // full-height detail get clipped by the cell (Server loses its base, Star its point,
        // Page and Wrench a corner). Below ~0.8 the strokes thin out and the glyph reads as
        // washed out next to the label beside it — 0.62 was the original value and looked faded.
        using var font = new Font(IconFontName, size * 0.875F, FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(glyph, font, brush, new RectangleF(0, 0, size, size), format);
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
