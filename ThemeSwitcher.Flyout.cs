// ThemeSwitcher.Flyout.cs — 亚克力圆角弹出菜单(仿 TranslucentTB)与窗口模糊/圆角辅助
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class WindowFx
{
    [StructLayout(LayoutKind.Sequential)]
    struct ACCENT_POLICY { public int AccentState; public int AccentFlags; public uint GradientColor; public int AnimationId; }

    [StructLayout(LayoutKind.Sequential)]
    struct WINCOMPATTRDATA { public int Attribute; public IntPtr Data; public int SizeOfData; }

    [DllImport("user32.dll")]
    static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WINCOMPATTRDATA data);

    const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;
    const int WCA_ACCENT_POLICY = 19;

    // 给窗口启用 Win10 亚克力模糊; gradient 为 ABGR(含透明度), 如 0xE01E1E1E
    public static void EnableAcrylic(Form f, uint gradient)
    {
        try
        {
            var policy = new ACCENT_POLICY { AccentState = ACCENT_ENABLE_ACRYLICBLURBEHIND, GradientColor = gradient };
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(policy));
            Marshal.StructureToPtr(policy, ptr, false);
            var data = new WINCOMPATTRDATA { Attribute = WCA_ACCENT_POLICY, Data = ptr, SizeOfData = Marshal.SizeOf(policy) };
            SetWindowCompositionAttribute(f.Handle, ref data);
            Marshal.FreeHGlobal(ptr);
        }
        catch { /* 模糊失败就纯色, 不致命 */ }
    }

    // 圆角路径
    public static GraphicsPath RoundedPath(Size size, float radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(0, 0, d, d, 180, 90);
        path.AddArc(size.Width - d - 1, 0, d, d, 270, 90);
        path.AddArc(size.Width - d - 1, size.Height - d - 1, d, d, 0, 90);
        path.AddArc(0, size.Height - d - 1, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    // 圆角 Region
    public static Region RoundedRegion(Size size, float radius)
    {
        using (var path = RoundedPath(size, radius))
            return new Region(path);
    }

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int HTCAPTION = 0x2;

    // 让无边框窗口可拖动
    public static void DragWindow(Form f)
    {
        ReleaseCapture();
        SendMessage(f.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
    }
}

// TranslucentTB 风格的弹出菜单: 亚克力深色圆角 + MDL2 图标 + 悬停高亮
internal class FlyoutMenu : Form
{
    class Entry
    {
        public string Glyph, Text;
        public Action Click;
        public bool Separator;
    }

    readonly System.Collections.Generic.List<Entry> entries = new System.Collections.Generic.List<Entry>();
    int hover = -1;

    const int ItemH = 38;
    const int SepH = 11;
    const int Pad = 8;
    const int WidthPx = 250;

    static readonly Font TextFont = new Font("Microsoft YaHei UI", 9.5F);
    static readonly Font GlyphFont = new Font("Segoe MDL2 Assets", 11F);
    static readonly Font GlyphFontSmall = new Font("Segoe MDL2 Assets", 8F);

    public FlyoutMenu()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(30, 30, 30);
        Size = new Size(WidthPx, Pad * 2 + MeasureHeight());
        DoubleBuffered = true;
        KeyPreview = true;
        KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Hide(); };
        Deactivate += (s, e) => Hide();
        MouseMove += OnMouseMove;
        MouseLeave += (s, e) => { hover = -1; Invalidate(); };
        MouseClick += OnMouseClick;
        Paint += OnPaint;
    }

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ClassStyle |= 0x20000; return cp; } // CS_DROPSHADOW
    }

    int MeasureHeight()
    {
        int h = 0;
        foreach (var it in entries) h += it.Separator ? SepH : ItemH;
        return h;
    }

    public void AddItem(string glyph, string text, Action click)
    {
        entries.Add(new Entry { Glyph = glyph, Text = text, Click = click });
        UpdateSize();
    }

    public void AddSeparator()
    {
        entries.Add(new Entry { Separator = true });
        UpdateSize();
    }

    void UpdateSize()
    {
        Size = new Size(WidthPx, Pad * 2 + MeasureHeight());
        Region = WindowFx.RoundedRegion(Size, 10);
        Invalidate();
    }

    public void ShowAt(Point cursor)
    {
        var wa = Screen.FromPoint(cursor).WorkingArea;
        int x = cursor.X - WidthPx - 8;               // 托盘在右下: 菜单往左弹
        if (x < wa.Left + 4) x = cursor.X + 8;
        int y = cursor.Y - Height - 8;
        if (y < wa.Top + 4) y = wa.Bottom - Height - 8;
        Location = new Point(x, y);
        hover = -1;
        WindowFx.EnableAcrylic(this, 0xE01E1E1E);
        Show();
        Activate();
    }

    Rectangle EntryRect(int index)
    {
        int y = Pad;
        for (int i = 0; i < index; i++) y += entries[i].Separator ? SepH : ItemH;
        return new Rectangle(Pad, y, WidthPx - Pad * 2, entries[index].Separator ? SepH : ItemH);
    }

    void OnMouseMove(object s, MouseEventArgs e)
    {
        int old = hover; hover = -1;
        for (int i = 0; i < entries.Count; i++)
        {
            if (!entries[i].Separator && EntryRect(i).Contains(e.Location)) { hover = i; break; }
        }
        if (hover != old) Invalidate();
    }

    void OnMouseClick(object s, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || hover < 0) return;
        var act = entries[hover].Click;
        Hide();
        if (act != null) act();
    }

    void OnPaint(object s, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        for (int i = 0; i < entries.Count; i++)
        {
            var r = EntryRect(i);
            if (entries[i].Separator)
            {
                using (var pen = new Pen(Color.FromArgb(70, 255, 255, 255)))
                    g.DrawLine(pen, r.Left + 14, r.Top + SepH / 2, r.Right - 14, r.Top + SepH / 2);
                continue;
            }

            if (i == hover)
            using (var path = WindowFx.RoundedPath(r.Size, 6))
            using (var brush = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                g.FillPath(brush, path);

            // 图标 (Segoe MDL2 Assets)
            if (!string.IsNullOrEmpty(entries[i].Glyph))
            {
                var gf = entries[i].Glyph.Length > 1 ? GlyphFontSmall : GlyphFont;
                var sz = g.MeasureString(entries[i].Glyph, gf);
                g.DrawString(entries[i].Glyph, gf, Brushes.White,
                    r.Left + 12, r.Top + (ItemH - sz.Height) / 2);
            }

            // 文字
            var tsz = g.MeasureString(entries[i].Text, TextFont);
            g.DrawString(entries[i].Text, TextFont, Brushes.White,
                r.Left + 44, r.Top + (ItemH - tsz.Height) / 2 + 1);
        }
    }
}
