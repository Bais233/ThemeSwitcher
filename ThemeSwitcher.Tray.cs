// ThemeSwitcher.Tray.cs — 系统托盘图标与右键菜单（partial class 的一部分）
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Microsoft.Win32;

static partial class ThemeSwitcher
{
    static NotifyIcon tray;

    // 统一界面字体(可在设置里自定义); 避免默认字体中文回退发虚
    internal static Font UiFont = new Font("Microsoft YaHei UI", 9F);

    internal static void ApplyUiFont(string familyName, float size)
    {
        Font next;
        try { next = new Font(familyName, size); }
        catch { return; }
        if (next.Name != familyName) { next.Dispose(); return; } // 系统无此字体则忽略
        UiFont = next;
        if (tray != null && tray.ContextMenuStrip != null)
            tray.ContextMenuStrip.Font = UiFont;
    }

    static bool menuDark;

    static ToolStripMenuItem CreateItem(string glyph, string text, Action click)
    {
        var item = new ToolStripMenuItem(text, GlyphIcon(glyph, !currentLight), (s, e) => click());
        item.Tag = glyph; // 记住图标, 换主题时重绘
        return item;
    }

    // 菜单跟随当前主题: 深色模式=深色菜单, 浅色模式=白菜单, 都是原生质感
    static void ApplyMenuTheme(ContextMenuStrip menu, bool dark)
    {
        menu.Renderer = new ToolStripProfessionalRenderer(new ThemeColors(dark)) { RoundedEdges = false };
        Color text = dark ? Color.FromArgb(235, 235, 235) : Color.FromArgb(32, 32, 32);
        foreach (ToolStripItem it in menu.Items)
        {
            it.ForeColor = text;
            var glyph = it.Tag as string;
            if (glyph != null) it.Image = GlyphIcon(glyph, dark);
        }
        menuDark = dark;
    }

    // 用 Segoe MDL2 Assets 字体渲染 16x16 图标(深浅两套颜色)
    static Bitmap GlyphIcon(string glyph, bool dark)
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Brush brush = dark ? Brushes.White : Brushes.Black;
            g.DrawString(glyph, new Font("Segoe MDL2 Assets", 9.5F), brush,
                new RectangleF(0, -1, 16, 18), new StringFormat { Alignment = StringAlignment.Center });
        }
        return bmp;
    }

    static void TrayRun()
    {
        Application.EnableVisualStyles();

        tray = new NotifyIcon();
        tray.Icon = LoadAppIcon();
        tray.Text = "ThemeSwitcher";
        tray.Visible = true;

        var menu = new ContextMenuStrip
        {
            Font = UiFont,
            ShowImageMargin = true,
            Renderer = new ToolStripProfessionalRenderer(new ThemeColors(!currentLight)) { RoundedEdges = false }
        };
        menu.Items.Add(CreateItem("\uE706", "切换到浅色模式", () => SetTheme(true)));
        menu.Items.Add(CreateItem("\uE708", "切换到深色模式", () => SetTheme(false)));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateItem("\uE713", "设置…", ShowSettings));
        menu.Items.Add(CreateItem("\uE7E8", "退出", () =>
        {
            tray.Visible = false;
            Application.Exit();
        }));
        foreach (ToolStripItem it in menu.Items) it.Padding = new Padding(0, 5, 0, 5);
        menuDark = !currentLight;
        tray.ContextMenuStrip = menu;

        // 弹出时若主题已变化, 按当前主题重新配色(深浅两套原生样式)
        menu.Opening += (s, e) =>
        {
            bool dark = !currentLight;
            if (dark != menuDark) ApplyMenuTheme(menu, dark);
        };

        // 悬停提示跟随模式
        var timer = new System.Windows.Forms.Timer { Interval = 2000 };
        timer.Tick += (s, e) =>
        {
            tray.Text = "ThemeSwitcher — " + (currentLight ? "浅色模式" : "深色模式");
        };
        timer.Start();

        Application.Run(); // 消息循环, 直到菜单选"退出"
    }

    static Icon LoadAppIcon()
    {
        try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch { return SystemIcons.Application; }
    }
}

// 原生菜单配色(深浅两套, 实色无模糊, 响应快)
internal class ThemeColors : ProfessionalColorTable
{
    readonly bool dark;
    public ThemeColors(bool dark) { this.dark = dark; UseSystemColors = false; }

    readonly Color bg       = Color.FromArgb(32, 32, 32);
    readonly Color hoverD   = Color.FromArgb(45, 45, 45);
    readonly Color borderD  = Color.FromArgb(60, 60, 60);
    readonly Color sepD     = Color.FromArgb(50, 50, 50);
    readonly Color hoverL   = Color.FromArgb(240, 240, 240);
    readonly Color borderL  = Color.FromArgb(204, 204, 204);
    readonly Color sepL     = Color.FromArgb(226, 226, 226);

    public override Color ToolStripDropDownBackground { get { return dark ? bg : Color.White; } }
    public override Color MenuBorder { get { return dark ? borderD : borderL; } }
    public override Color MenuItemBorder { get { return Color.Transparent; } }
    public override Color MenuItemSelected { get { return dark ? hoverD : hoverL; } }
    public override Color MenuItemSelectedGradientBegin { get { return dark ? hoverD : hoverL; } }
    public override Color MenuItemSelectedGradientEnd { get { return dark ? hoverD : hoverL; } }
    public override Color MenuItemPressedGradientBegin { get { return dark ? hoverD : hoverL; } }
    public override Color MenuItemPressedGradientEnd { get { return dark ? hoverD : hoverL; } }
    public override Color ImageMarginGradientBegin { get { return dark ? bg : Color.White; } }
    public override Color ImageMarginGradientMiddle { get { return dark ? bg : Color.White; } }
    public override Color ImageMarginGradientEnd { get { return dark ? bg : Color.White; } }
    public override Color SeparatorDark { get { return dark ? sepD : sepL; } }
    public override Color SeparatorLight { get { return dark ? sepD : sepL; } }
    public override Color ToolStripBorder { get { return dark ? borderD : borderL; } }
    public override Color ToolStripGradientBegin { get { return dark ? bg : Color.White; } }
    public override Color ToolStripGradientMiddle { get { return dark ? bg : Color.White; } }
    public override Color ToolStripGradientEnd { get { return dark ? bg : Color.White; } }
}

// ---- 开机自启(HKCU Run 键) ----
partial class ThemeSwitcher
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValue = "ThemeSwitcher";

    internal static bool GetAutostart()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
            return key != null && key.GetValue(RunValue) != null;
    }

    internal static void SetAutostart(bool enable)
    {
        using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            if (enable) key.SetValue(RunValue, "\"" + Application.ExecutablePath + "\"");
            else key.DeleteValue(RunValue, false);
        }
    }
}
