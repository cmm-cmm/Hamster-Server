using System.Drawing.Drawing2D;
using System.Globalization;

namespace HamsterServer;

/// <summary>
/// Turns SVG path data (the <c>d</c> attribute) into a <see cref="GraphicsPath"/>, so the icon set
/// can be drawn by GDI+ without an SVG library or an icon font.
/// <para>
/// Supports the whole path grammar: M L H V C S Q T A Z in absolute and relative form, implicit
/// command repetition, packed numbers (<c>1.5.5</c> is two numbers) and packed arc flags. Arcs are
/// converted to cubic Béziers per SVG 1.1 appendix F.6.
/// </para>
/// </summary>
internal static class SvgPath
{
    public static GraphicsPath Parse(string data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var path = new GraphicsPath(FillMode.Winding);
        try
        {
            Build(path, data);
            return path;
        }
        catch
        {
            path.Dispose();
            throw;
        }
    }

    private static void Build(GraphicsPath path, string data)
    {
        var reader = new Reader(data);
        PointF current = default, figureStart = default;
        PointF lastCubic = default, lastQuad = default;
        char command = '\0', previous = '\0';

        while (reader.SkipSeparators())
        {
            if (reader.TryReadCommand(out char next)) command = next;
            else if (command == '\0') throw reader.Error("path data must start with a command");
            else if (command is 'Z' or 'z') throw reader.Error("Z takes no arguments");

            bool relative = char.IsLower(command);
            PointF Rel(PointF p) => relative ? new PointF(current.X + p.X, current.Y + p.Y) : p;

            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                    {
                        var p = Rel(reader.ReadPoint());
                        path.StartFigure();
                        current = figureStart = p;
                        // Further coordinate pairs after a moveto are implicit linetos.
                        command = relative ? 'l' : 'L';
                        previous = 'M';
                        continue;
                    }
                case 'L':
                    {
                        var p = Rel(reader.ReadPoint());
                        path.AddLine(current, p);
                        current = p;
                        break;
                    }
                case 'H':
                    {
                        float x = reader.ReadNumber();
                        var p = new PointF(relative ? current.X + x : x, current.Y);
                        path.AddLine(current, p);
                        current = p;
                        break;
                    }
                case 'V':
                    {
                        float y = reader.ReadNumber();
                        var p = new PointF(current.X, relative ? current.Y + y : y);
                        path.AddLine(current, p);
                        current = p;
                        break;
                    }
                case 'C':
                    {
                        var c1 = Rel(reader.ReadPoint());
                        var c2 = Rel(reader.ReadPoint());
                        var p = Rel(reader.ReadPoint());
                        path.AddBezier(current, c1, c2, p);
                        lastCubic = c2;
                        current = p;
                        break;
                    }
                case 'S':
                    {
                        var c1 = previous is 'C' or 'S' ? Reflect(lastCubic, current) : current;
                        var c2 = Rel(reader.ReadPoint());
                        var p = Rel(reader.ReadPoint());
                        path.AddBezier(current, c1, c2, p);
                        lastCubic = c2;
                        current = p;
                        break;
                    }
                case 'Q':
                    {
                        var q = Rel(reader.ReadPoint());
                        var p = Rel(reader.ReadPoint());
                        AddQuadratic(path, current, q, p);
                        lastQuad = q;
                        current = p;
                        break;
                    }
                case 'T':
                    {
                        var q = previous is 'Q' or 'T' ? Reflect(lastQuad, current) : current;
                        var p = Rel(reader.ReadPoint());
                        AddQuadratic(path, current, q, p);
                        lastQuad = q;
                        current = p;
                        break;
                    }
                case 'A':
                    {
                        float rx = reader.ReadNumber();
                        float ry = reader.ReadNumber();
                        float rotation = reader.ReadNumber();
                        bool largeArc = reader.ReadFlag();
                        bool sweep = reader.ReadFlag();
                        var p = Rel(reader.ReadPoint());
                        AddArc(path, current, rx, ry, rotation, largeArc, sweep, p);
                        current = p;
                        break;
                    }
                case 'Z':
                    path.CloseFigure();
                    current = figureStart;
                    break;
                default:
                    throw reader.Error($"unknown command '{command}'");
            }
            previous = char.ToUpperInvariant(command);
        }
    }

    private static PointF Reflect(PointF control, PointF about) =>
        new(2 * about.X - control.X, 2 * about.Y - control.Y);

    private static void AddQuadratic(GraphicsPath path, PointF p0, PointF q, PointF p1)
    {
        var c1 = new PointF(p0.X + 2f / 3f * (q.X - p0.X), p0.Y + 2f / 3f * (q.Y - p0.Y));
        var c2 = new PointF(p1.X + 2f / 3f * (q.X - p1.X), p1.Y + 2f / 3f * (q.Y - p1.Y));
        path.AddBezier(p0, c1, c2, p1);
    }

    /// <summary>SVG elliptical arc → cubic Béziers, at most a quarter turn each (SVG 1.1 F.6.5–F.6.6).</summary>
    private static void AddArc(GraphicsPath path, PointF p0, float rxIn, float ryIn, float rotationDeg,
                               bool largeArc, bool sweep, PointF p1)
    {
        if (p0 == p1) return;
        double rx = Math.Abs(rxIn), ry = Math.Abs(ryIn);
        if (rx == 0 || ry == 0)
        {
            path.AddLine(p0, p1);
            return;
        }

        double phi = rotationDeg * Math.PI / 180.0, cos = Math.Cos(phi), sin = Math.Sin(phi);
        double dx = (p0.X - p1.X) / 2.0, dy = (p0.Y - p1.Y) / 2.0;
        double x1 = cos * dx + sin * dy, y1 = -sin * dx + cos * dy;

        // Radii too small to reach the end point are scaled up, as the spec requires.
        double lambda = x1 * x1 / (rx * rx) + y1 * y1 / (ry * ry);
        if (lambda > 1)
        {
            double s = Math.Sqrt(lambda);
            rx *= s;
            ry *= s;
        }

        double rx2 = rx * rx, ry2 = ry * ry;
        double numerator = rx2 * ry2 - rx2 * y1 * y1 - ry2 * x1 * x1;
        double denominator = rx2 * y1 * y1 + ry2 * x1 * x1;
        double coef = denominator == 0 ? 0 : Math.Sqrt(Math.Max(0, numerator / denominator));
        if (largeArc == sweep) coef = -coef;
        double cxp = coef * rx * y1 / ry, cyp = coef * -ry * x1 / rx;
        double cx = cos * cxp - sin * cyp + (p0.X + p1.X) / 2.0;
        double cy = sin * cxp + cos * cyp + (p0.Y + p1.Y) / 2.0;

        double theta = Angle(1, 0, (x1 - cxp) / rx, (y1 - cyp) / ry);
        double delta = Angle((x1 - cxp) / rx, (y1 - cyp) / ry, (-x1 - cxp) / rx, (-y1 - cyp) / ry);
        if (!sweep && delta > 0) delta -= 2 * Math.PI;
        else if (sweep && delta < 0) delta += 2 * Math.PI;

        int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(delta) / (Math.PI / 2) - 1e-9));
        double step = delta / segments;
        double k = 4.0 / 3.0 * Math.Tan(step / 4);

        PointF Map(double ux, double uy) =>
            new((float)(cx + rx * ux * cos - ry * uy * sin), (float)(cy + rx * ux * sin + ry * uy * cos));

        var from = p0;
        for (int i = 0; i < segments; i++)
        {
            double a = theta + i * step, b = a + step;
            double ca = Math.Cos(a), sa = Math.Sin(a), cb = Math.Cos(b), sb = Math.Sin(b);
            var c1 = Map(ca - k * sa, sa + k * ca);
            var c2 = Map(cb + k * sb, sb - k * cb);
            var to = i == segments - 1 ? p1 : Map(cb, sb);
            path.AddBezier(from, c1, c2, to);
            from = to;
        }
    }

    private static double Angle(double ux, double uy, double vx, double vy) =>
        Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);

    private sealed class Reader(string data)
    {
        private int _pos;

        public FormatException Error(string message) =>
            new($"Invalid SVG path data at {_pos}: {message}. Data: \"{data}\"");

        /// <returns><c>false</c> at the end of the data.</returns>
        public bool SkipSeparators()
        {
            while (_pos < data.Length && (char.IsWhiteSpace(data[_pos]) || data[_pos] == ',')) _pos++;
            return _pos < data.Length;
        }

        public bool TryReadCommand(out char command)
        {
            command = data[_pos];
            if (!"MmLlHhVvCcSsQqTtAaZz".Contains(command)) return false;
            _pos++;
            return true;
        }

        public PointF ReadPoint() => new(ReadNumber(), ReadNumber());

        public float ReadNumber()
        {
            if (!SkipSeparators()) throw Error("expected a number");
            int start = _pos;
            if (data[_pos] is '+' or '-') _pos++;
            bool digits = false, dot = false;
            while (_pos < data.Length)
            {
                char c = data[_pos];
                if (char.IsAsciiDigit(c)) { digits = true; _pos++; }
                else if (c == '.' && !dot) { dot = true; _pos++; }
                else break;
            }
            if (!digits) throw Error("expected a number");
            if (_pos < data.Length && data[_pos] is 'e' or 'E')
            {
                int mark = _pos++;
                if (_pos < data.Length && data[_pos] is '+' or '-') _pos++;
                if (_pos < data.Length && char.IsAsciiDigit(data[_pos]))
                    while (_pos < data.Length && char.IsAsciiDigit(data[_pos])) _pos++;
                else _pos = mark;   // an 'e' that starts no exponent belongs to the next token
            }
            return float.Parse(data.AsSpan(start, _pos - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        // Arc flags are a single 0 or 1 and may be packed with what follows ("a1 1 0 011 1").
        public bool ReadFlag()
        {
            if (!SkipSeparators()) throw Error("expected an arc flag");
            char c = data[_pos++];
            return c switch
            {
                '0' => false,
                '1' => true,
                _ => throw Error("arc flags must be 0 or 1")
            };
        }
    }
}
