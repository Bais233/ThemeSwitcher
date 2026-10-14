// ThemeSwitcher.Tray.cs — 系统托盘图标与右键菜单（partial class 的一部分）
using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

static partial class ThemeSwitcher
{
    static NotifyIcon tray;
    static ContextMenuStrip menu;
    static bool menuIsDark;

    // 统一界面字体(可在设置里自定义); 避免默认字体中文回退发虚
    internal static Font UiFont = new Font("Microsoft YaHei UI", 9F);

    internal static void ApplyUiFont(string familyName, float size)
    {
        Font next;
        try { next = new Font(familyName, size); }
        catch { return; }
        if (next.Name != familyName) { next.Dispose(); return; } // 系统无此字体则忽略
        // 注意: 旧字体可能仍被菜单引用, 不主动 Dispose, 交给 GC
        UiFont = next;
        if (tray != null && tray.ContextMenuStrip != null)
            tray.ContextMenuStrip.Font = UiFont;
    }

    static void TrayRun()
    {
        Application.EnableVisualStyles();

        tray = new NotifyIcon();
        tray.Icon = LoadAppIcon();
        tray.Text = "ThemeSwitcher";
        tray.Visible = true;

        menu = new ContextMenuStrip { Font = UiFont, ShowImageMargin = false };
        menu.Items.Add(MenuText(true), null, (s, e) => SetTheme(true));
        menu.Items.Add(MenuText(false), null, (s, e) => SetTheme(false));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("⚙  设置…", null, (s, e) => ShowSettings());
        menu.Items.Add("退出", null, (s, e) =>
        {
            tray.Visible = false;
            Application.Exit();
        });
        foreach (ToolStripItem it in menu.Items) it.Padding = new Padding(2, 7, 2, 7);
        StyleMenu(); // 按当前主题配色
        tray.ContextMenuStrip = menu;

        // 定时: 悬停提示跟随模式; 菜单配色跟随主题
        var timer = new System.Windows.Forms.Timer { Interval = 2000 };
        timer.Tick += (s, e) =>
        {
            tray.Text = "ThemeSwitcher — " + (currentLight ? "浅色模式" : "深色模式");
            if (menuIsDark == currentLight) StyleMenu();
        };
        timer.Start();

        Application.Run(); // 消息循环, 直到菜单选"退出"
    }

    static string MenuText(bool light) { return (light ? "☀  " : "🌙  ") + "切换到" + (light ? "浅色" : "深色") + "模式"; }

    // 菜单配色跟随当前主题(深色模式=深灰菜单, 浅色模式=白菜单)
    static void StyleMenu()
    {
        bool dark = !currentLight;
        menu.Renderer = new ToolStripProfessionalRenderer(new ThemeMenuColors(dark)) { RoundedEdges = false };
        Color text = dark ? Color.FromArgb(235, 235, 235) : Color.FromArgb(32, 32, 32);
        foreach (ToolStripItem it in menu.Items) it.ForeColor = text;
        menuIsDark = dark;
    }

    static Icon LoadAppIcon()
    {
        try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch { return SystemIcons.Application; }
    }
}

// 现代扁平配色的菜单渲染表
internal class ThemeMenuColors : ProfessionalColorTable
{
    readonly bool dark;
    public ThemeMenuColors(bool dark) { this.dark = dark; UseSystemColors = false; }

    Color Bg       { get { return dark ? Color.FromArgb(32, 32, 32)   : Color.White; } }
    Color Hover    { get { return dark ? Color.FromArgb(55, 55, 55)   : Color.FromArgb(240, 240, 240); } }
    Color Border   { get { return dark ? Color.FromArgb(63, 63, 63)   : Color.FromArgb(229, 229, 229); } }
    Color Separator{ get { return dark ? Color.FromArgb(48, 48, 48)   : Color.FromArgb(235, 235, 235); } }

    public override Color ToolStripDropDownBackground { get { return Bg; } }
    public override Color MenuBorder { get { return Border; } }
    public override Color MenuItemBorder { get { return Color.Transparent; } }
    public override Color MenuItemSelected { get { return Hover; } }
    public override Color MenuItemSelectedGradientBegin { get { return Hover; } }
    public override Color MenuItemSelectedGradientEnd { get { return Hover; } }
    public override Color MenuItemPressedGradientBegin { get { return Hover; } }
    public override Color MenuItemPressedGradientEnd { get { return Hover; } }
    public override Color ImageMarginGradientBegin { get { return Bg; } }
    public override Color ImageMarginGradientMiddle { get { return Bg; } }
    public override Color ImageMarginGradientEnd { get { return Bg; } }
    public override Color SeparatorDark { get { return Separator; } }
    public override Color SeparatorLight { get { return Separator; } }
    public override Color ToolStripBorder { get { return Bg; } }
    public override Color ToolStripGradientBegin { get { return Bg; } }
    public override Color ToolStripGradientMiddle { get { return Bg; } }
    public override Color ToolStripGradientEnd { get { return Bg; } }
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
