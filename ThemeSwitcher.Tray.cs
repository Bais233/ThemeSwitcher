// ThemeSwitcher.Tray.cs — 系统托盘图标与右键菜单（partial class 的一部分）
using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

static partial class ThemeSwitcher
{
    static NotifyIcon tray;

    // 统一使用系统 UI 字体(微软雅黑 UI), 避免默认字体中文回退发虚
    internal static readonly Font UiFont = new Font("Microsoft YaHei UI", 9F);

    static void TrayRun()
    {
        Application.EnableVisualStyles();

        tray = new NotifyIcon();
        tray.Icon = LoadAppIcon();
        tray.Text = "ThemeSwitcher";
        tray.Visible = true;

        var menu = new ContextMenuStrip { Font = UiFont, ShowImageMargin = false };
        menu.Items.Add("切换到浅色模式", null, (s, e) => SetTheme(true));
        menu.Items.Add("切换到深色模式", null, (s, e) => SetTheme(false));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("设置…", null, (s, e) => ShowSettings());
        menu.Items.Add("退出", null, (s, e) =>
        {
            tray.Visible = false;
            Application.Exit();
        });
        tray.ContextMenuStrip = menu;

        // 悬停提示跟随当前模式
        var timer = new System.Windows.Forms.Timer { Interval = 2000 };
        timer.Tick += (s, e) =>
        {
            tray.Text = "ThemeSwitcher — " + (currentLight ? "浅色模式" : "深色模式");
        };
        timer.Start();

        Application.Run(); // 消息循环, 直到菜单选"退出"
    }

    // ---- 开机自启(HKCU Run 键) ----
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

    static Icon LoadAppIcon()
    {
        try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch { return SystemIcons.Application; }
    }
}
