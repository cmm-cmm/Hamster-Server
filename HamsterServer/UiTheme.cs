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

    // ── Log severity ─────────────────────────────────────────────────────────
    public static readonly Color LogNormal = SystemColors.WindowText;
    public static readonly Color LogInfo = Color.FromArgb(0, 0, 160);
    public static readonly Color LogOk = StatusRunning;
    public static readonly Color LogWarn = StatusTransition;
    public static readonly Color LogError = StatusError;

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
    public static Bitmap CreateStatusDot(Color fill, int size = 16)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int d = Math.Max(6, size - 6);
        int off = (size - d) / 2;
        using var brush = new SolidBrush(fill);
        using var pen = new Pen(ControlPaint.Dark(fill, 0.15f));
        g.FillEllipse(brush, off, off, d, d);
        g.DrawEllipse(pen, off, off, d, d);
        return bmp;
    }

    /// <summary>
    /// An icon-font glyph tinted <paramref name="color"/>. Falls back to a status dot when the
    /// icon font is unavailable, so the image list keeps its indices either way.
    /// </summary>
    public static Bitmap CreateGlyph(string glyph, Color color, int size = 16)
    {
        if (IconFontName is null) return CreateStatusDot(color, size);

        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        using var font = new Font(IconFontName, size * 0.62F, FontStyle.Regular, GraphicsUnit.Pixel);
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
