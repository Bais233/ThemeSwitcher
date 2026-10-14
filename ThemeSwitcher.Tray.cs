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

    // 用 Segoe MDL2 Assets 字体渲染 16x16 图标
    static Bitmap GlyphIcon(string glyph)
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.DrawString(glyph, new Font("Segoe MDL2 Assets", 9.5F), Brushes.White,
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
            Renderer = new ToolStripProfessionalRenderer(new NativeDarkColors()) { RoundedEdges = false }
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
        tray.ContextMenuStrip = menu;

        // 悬停提示跟随模式
        var timer = new System.Windows.Forms.Timer { Interval = 2000 };
        timer.Tick += (s, e) =>
        {
            tray.Text = "ThemeSwitcher — " + (currentLight ? "浅色模式" : "深色模式");
        };
        timer.Start();

        Application.Run(); // 消息循环, 直到菜单选"退出"
    }

    static ToolStripMenuItem CreateItem(string glyph, string text, Action click)
    {
        return new ToolStripMenuItem(text, GlyphIcon(glyph), (s, e) => click());
    }

    static Icon LoadAppIcon()
    {
        try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch { return SystemIcons.Application; }
    }
}

// 原生深色菜单配色(实色, 无模糊, 响应快)
internal class NativeDarkColors : ProfessionalColorTable
{
    readonly Color bg = Color.FromArgb(32, 32, 32);
    readonly Color hover = Color.FromArgb(45, 45, 45);
    readonly Color border = Color.FromArgb(60, 60, 60);
    readonly Color sep = Color.FromArgb(50, 50, 50);

    public NativeDarkColors() { UseSystemColors = false; }

    public override Color ToolStripDropDownBackground { get { return bg; } }
    public override Color MenuBorder { get { return border; } }
    public override Color MenuItemBorder { get { return Color.Transparent; } }
    public override Color MenuItemSelected { get { return hover; } }
    public override Color MenuItemSelectedGradientBegin { get { return hover; } }
    public override Color MenuItemSelectedGradientEnd { get { return hover; } }
    public override Color MenuItemPressedGradientBegin { get { return hover; } }
    public override Color MenuItemPressedGradientEnd { get { return hover; } }
    public override Color ImageMarginGradientBegin { get { return bg; } }
    public override Color ImageMarginGradientMiddle { get { return bg; } }
    public override Color ImageMarginGradientEnd { get { return bg; } }
    public override Color SeparatorDark { get { return sep; } }
    public override Color SeparatorLight { get { return sep; } }
    public override Color ToolStripBorder { get { return border; } }
    public override Color ToolStripGradientBegin { get { return bg; } }
    public override Color ToolStripGradientMiddle { get { return bg; } }
    public override Color ToolStripGradientEnd { get { return bg; } }
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
