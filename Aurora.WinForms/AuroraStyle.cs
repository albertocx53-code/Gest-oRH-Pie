using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Aurora.WinForms;

internal static class AuroraStyle
{
    public static readonly Color Ink = Color.FromArgb(237, 240, 249);
    public static readonly Color Muted = Color.FromArgb(168, 177, 198);
    public static readonly Color Accent = Color.FromArgb(173, 161, 246);
    public static readonly Color Primary = Color.FromArgb(100, 80, 194);
    public static readonly Color Paper = Color.FromArgb(25, 30, 46);
    public static readonly Color Border = Color.FromArgb(53, 61, 81);
    public static readonly Color Tint = Color.FromArgb(52, 49, 81);
    public static readonly Color Field = Color.FromArgb(33, 40, 59);

    public static Font Font(float size, bool strong = false) => new(strong ? "Segoe UI Semibold" : "Segoe UI", size, GraphicsUnit.Point);

    public static Label Label(string text, float size, bool strong = false, Color? color = null) => new()
    {
        Text = text, Font = Font(size, strong), ForeColor = color ?? Ink, BackColor = Color.Transparent,
        AutoSize = true, UseMnemonic = false, Margin = Padding.Empty
    };

    public static void Put(Control parent, Control child, int x, int y)
    {
        child.Location = new(x, y);
        parent.Controls.Add(child);
    }

    public static GraphicsPath Shape(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        var d = 2 * radius;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void Icon(Graphics g, string name, float centerX, float centerY, Color color, float size = 20)
    {
        var state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(centerX - size / 2, centerY - size / 2);
        g.ScaleTransform(size / 20, size / 20);
        using var p = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        switch (name)
        {
            case "home":
                g.DrawLines(p, [new(2, 9), new(10, 2), new(18, 9), new(16, 9), new(16, 17), new(4, 17), new(4, 9), new(2, 9)]);
                g.DrawRectangle(p, 8, 12, 4, 5);
                break;
            case "people":
                g.DrawEllipse(p, 4, 3, 6, 6); g.DrawArc(p, 1.5f, 11, 11, 12, 180, 180);
                g.DrawArc(p, 10.5f, 4, 6, 6, 260, 160); g.DrawArc(p, 9, 11, 9.5f, 12, 270, 90);
                break;
            case "time":
                g.DrawEllipse(p, 2, 2, 16, 16); g.DrawLines(p, [new(10, 5), new(10, 10), new(13, 12)]);
                break;
            case "hire":
                g.DrawRectangle(p, 2, 6, 16, 11); g.DrawLines(p, [new(7, 6), new(7, 3), new(13, 3), new(13, 6)]);
                g.DrawLine(p, 2, 10, 18, 10); g.DrawLine(p, 10, 9, 10, 12);
                break;
            case "calendar":
                g.DrawRectangle(p, 2, 4, 16, 14); g.DrawLine(p, 2, 8, 18, 8);
                g.DrawLine(p, 6, 2, 6, 6); g.DrawLine(p, 14, 2, 14, 6); g.DrawLine(p, 6, 12, 9, 12);
                break;
            case "pay":
                g.DrawRectangle(p, 2, 4, 16, 12); g.DrawEllipse(p, 7, 7, 6, 6);
                break;
            case "plus": g.DrawLine(p, 10, 4, 10, 16); g.DrawLine(p, 4, 10, 16, 10); break;
            case "prev": g.DrawLines(p, [new(13, 4), new(7, 10), new(13, 16)]); break;
            case "next": g.DrawLines(p, [new(7, 4), new(13, 10), new(7, 16)]); break;
        }
        g.Restore(state);
    }

    public static void DrawBrand(Graphics graphics, Rectangle bounds)
    {
        var state = graphics.Save();
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var scale = Math.Min(bounds.Width, bounds.Height) / 41f;
        graphics.TranslateTransform(bounds.Left + bounds.Width / 2f - 20.5f * scale,
            bounds.Top + bounds.Height / 2f - 20.5f * scale);
        graphics.ScaleTransform(scale, scale);

        using var tile = Shape(new(1.8f, 1.8f, 37.4f, 37.4f), 14);
        using var violet = new LinearGradientBrush(new RectangleF(0, 0, 41, 41), Color.FromArgb(42, 39, 93), Color.FromArgb(85, 54, 153), 58);
        graphics.FillPath(violet, tile);

        graphics.TranslateTransform(4, 4);
        using var right = new GraphicsPath();
        right.AddBezier(22, 10, 24, 10, 24, 13, 25, 16);
        right.AddBezier(25, 16, 26, 21, 27, 25, 30, 26);
        right.AddBezier(30, 26, 27, 29, 23, 28, 21, 24);
        right.AddLine(21, 24, 18, 17);
        right.CloseFigure();
        using var lilac = new LinearGradientBrush(new RectangleF(17, 10, 14, 19), Color.FromArgb(239, 220, 255), Color.FromArgb(182, 137, 250), 74);
        graphics.FillPath(lilac, right);

        using var crossbar = new GraphicsPath();
        crossbar.AddBezier(10, 21, 14, 17, 19, 16, 24, 18);
        crossbar.AddLine(24, 18, 25, 21);
        crossbar.AddBezier(25, 21, 20, 19, 16, 20, 11, 23);
        crossbar.CloseFigure();
        using var lavender = new SolidBrush(Color.FromArgb(208, 181, 255));
        graphics.FillPath(lavender, crossbar);

        using var left = new GraphicsPath();
        left.AddBezier(3, 25, 8, 24, 12, 13, 17, 7);
        left.AddBezier(17, 7, 20, 3, 24, 3, 28, 5);
        left.AddBezier(28, 5, 24, 5, 22, 8, 19, 13);
        left.AddLine(19, 13, 12, 25);
        left.AddBezier(12, 25, 10, 29, 6, 29, 3, 25);
        left.CloseFigure();
        using var pearl = new LinearGradientBrush(new RectangleF(3, 4, 26, 25), Color.FromArgb(246, 245, 255), Color.FromArgb(163, 207, 255), 90);
        graphics.FillPath(pearl, left);
        graphics.Restore(state);
    }
}

// O fundo é calculado de novo só quando a janela muda de tamanho.
internal sealed class AuroraBackdrop : Panel
{
    private Bitmap? background;

    public AuroraBackdrop()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        background?.Dispose();
        background = null;
        base.OnSizeChanged(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) background?.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Width < 1 || Height < 1) return;
        background ??= CreateBackground();
        e.Graphics.DrawImageUnscaled(background, 0, 0);
    }

    private Bitmap CreateBackground()
    {
        var bitmap = new Bitmap(Width, Height);
        using var g = Graphics.FromImage(bitmap);
        using var baseFill = new LinearGradientBrush(ClientRectangle, Color.FromArgb(14, 18, 31), Color.FromArgb(21, 19, 37), 23);
        g.FillRectangle(baseFill, ClientRectangle);
        Glow(g, new(-270, -315, Width * 1.08f, Height * 1.44f), Color.FromArgb(43, 81, 133));
        Glow(g, new(Width * .29f, -Height * .71f, Width, Height * 1.29f), Color.FromArgb(81, 56, 140));
        Glow(g, new(Width * .61f, Height * .39f, Width * .68f, Height * .91f), Color.FromArgb(78, 47, 104));
        return bitmap;
    }

    private static void Glow(Graphics g, RectangleF bounds, Color color)
    {
        using var shape = new GraphicsPath();
        shape.AddEllipse(bounds);
        using var glow = new PathGradientBrush(shape)
        {
            CenterColor = Color.FromArgb(195, color), SurroundColors = [Color.FromArgb(0, color)],
            Blend = new() { Positions = [0, .37f, .7f, 1], Factors = [0, .19f, .67f, 1] }
        };
        g.FillPath(glow, shape);
    }
}

internal sealed class Surface : Panel
{
    private readonly Color fill;
    private readonly Color border;
    private readonly int radius;

    public Surface(Color fill, Color border, int radius = 18)
    {
        this.fill = fill;
        this.border = border;
        this.radius = radius;
        BackColor = Color.Transparent;
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width >= 2 * radius + 3 && Height >= 2 * radius + 3)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = AuroraStyle.Shape(new(0.8f, 0.8f, Width - 1.6f, Height - 1.6f), radius);
            using var brush = new SolidBrush(fill);
            using var pen = new Pen(border, 1.15f);
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        }
        base.OnPaint(e);
    }
}

internal sealed class RoundedGrid : DataGridView
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (ClientSize.Width < 2 || ClientSize.Height < 2) return;
        var radius = Math.Min(10f * DeviceDpi / 96, Math.Min(ClientSize.Width, ClientSize.Height) / 2f);
        using var corners = AuroraStyle.Shape(ClientRectangle, radius);
        // Extend the mask past the clip so antialiasing affects only the rounded edge.
        corners.AddRectangle(Rectangle.Inflate(ClientRectangle, 2, 2));
        using var surface = new SolidBrush(AuroraStyle.Paper);
        var previous = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(surface, corners);
        e.Graphics.SmoothingMode = previous;
    }
}

// Mantemos o clique e o teclado do Button nativo. Aqui só muda o desenho.
internal sealed class AuroraButton : Button
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string Glyph { get; init; } = "";
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Primary { get; init; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Selected { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Navigation { get; init; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool CalendarDay { get; init; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool Marker { get; init; }
    private bool hot;
    private bool down;

    public AuroraButton(string text)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        Text = text;
        BackColor = Color.Transparent;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = AuroraStyle.Font(10, true);
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { hot = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { hot = false; down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var textColor = Primary ? Color.White : Selected ? AuroraStyle.Accent : AuroraStyle.Ink;
        if (!Enabled) textColor = AuroraStyle.Muted;
        using var path = AuroraStyle.Shape(new(1, 1, Width - 2, Height - 2), Navigation ? 11 : 10);
        if (Primary || !Navigation || Selected || hot)
        {
            var fill = Primary ? (hot ? Color.FromArgb(115, 96, 211) : AuroraStyle.Primary)
                : Selected ? AuroraStyle.Tint : hot ? AuroraStyle.Tint : AuroraStyle.Field;
            if (down) fill = ControlPaint.Dark(fill, .1f);
            using var brush = new SolidBrush(fill);
            e.Graphics.FillPath(brush, path);
            if (!Primary && !Navigation)
            {
                using var pen = new Pen(AuroraStyle.Border);
                e.Graphics.DrawPath(pen, path);
            }
        }
        if (Selected && Navigation)
        {
            using var pen = new Pen(AuroraStyle.Accent, 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            e.Graphics.DrawLine(pen, 3, 12, 3, Height - 12);
        }
        const TextFormatFlags textFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        if (Navigation)
        {
            AuroraStyle.Icon(e.Graphics, Glyph, 23, Height / 2f, textColor);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(40, 0, Width - 46, Height), textColor, textFlags);
        }
        else if (CalendarDay)
        {
            DrawCalendarDay(e.Graphics, textColor);
        }
        else if (Glyph.Length == 0)
        {
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(4, 0, Width - 8, Height), textColor, textFlags | TextFormatFlags.HorizontalCenter);
        }
        else if (Text.Length == 0)
        {
            AuroraStyle.Icon(e.Graphics, Glyph, Width / 2f, Height / 2f, textColor, 13);
        }
        else
        {
            const int iconSize = 16;
            const int gap = 7;
            var measured = TextRenderer.MeasureText(e.Graphics, Text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
            var textWidth = Math.Min(measured, Math.Max(1, Width - 16 - iconSize - gap));
            var left = (Width - iconSize - gap - textWidth) / 2;
            AuroraStyle.Icon(e.Graphics, Glyph, left + iconSize / 2f, Height / 2f, textColor, iconSize);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(left + iconSize + gap, 0, textWidth, Height), textColor, textFlags);
        }
        if (Focused && ShowFocusCues)
        {
            using var pen = new Pen(Primary ? Color.White : AuroraStyle.Accent, 1.5f) { DashStyle = DashStyle.Dot };
            e.Graphics.DrawPath(pen, path);
        }
    }

    private void DrawCalendarDay(Graphics graphics, Color color)
    {
        var center = Width / 2f;
        var scale = DeviceDpi / 96f;
        using var number = new GraphicsPath();
        number.AddString(Text, Font.FontFamily, (int)Font.Style, Font.SizeInPoints * graphics.DpiY / 72f, PointF.Empty, StringFormat.GenericTypographic);
        var ink = number.GetBounds();
        using var position = new Matrix();
        position.Translate(center - ink.Left - ink.Width / 2, Height / 2f - 1.5f * scale - ink.Top - ink.Height / 2);
        number.Transform(position);
        using var foreground = new SolidBrush(color);
        graphics.FillPath(foreground, number);
        if (Marker)
        {
            using var dot = new SolidBrush(Primary ? Color.White : AuroraStyle.Accent);
            var diameter = 3f * scale;
            graphics.FillEllipse(dot, center - diameter / 2, Height / 2f + 6.5f * scale, diameter, diameter);
        }
    }
}
