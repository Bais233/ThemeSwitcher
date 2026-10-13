// ThemeSwitcher.Tray.cs — 系统托盘图标与右键菜单（partial class 的一部分）
using System;
using System.Drawing;
using System.Windows.Forms;

static partial class ThemeSwitcher
{
    static NotifyIcon tray;

    static void TrayRun()
    {
        Application.EnableVisualStyles();

        tray = new NotifyIcon();
        tray.Icon = LoadAppIcon();
        tray.Text = "ThemeSwitcher";
        tray.Visible = true;

        var menu = new ContextMenuStrip();
        menu.Items.Add("切换到浅色模式", null, (s, e) => SetTheme(true));
        menu.Items.Add("切换到深色模式", null, (s, e) => SetTheme(false));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (s, e) =>
        {
            tray.Visible = false;
            Application.Exit();
        });
        tray.ContextMenuStrip = menu;

        // 定时刷新提示文字(后台线程只改 currentLight, 这里轮询避免跨线程 UI 调用)
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
