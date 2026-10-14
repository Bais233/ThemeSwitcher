// ThemeSwitcher.Tray.cs — 系统托盘图标与右键菜单（partial class 的一部分）
using System;
using System.Drawing;
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

        // 自绘亚克力弹出菜单(仿 TranslucentTB)
        var flyout = new FlyoutMenu();
        flyout.AddItem("\uE706", "切换到浅色模式", () => SetTheme(true));   // MDL2 太阳
        flyout.AddItem("\uE708", "切换到深色模式", () => SetTheme(false));  // MDL2 月亮
        flyout.AddSeparator();
        flyout.AddItem("\uE713", "设置…", ShowSettings);                     // MDL2 齿轮
        flyout.AddItem("\uE7E8", "退出", () =>
        {
            tray.Visible = false;
            Application.Exit();
        });

        tray.MouseUp += (s, e) =>
        {
            if (e.Button == MouseButtons.Right) flyout.ShowAt(Cursor.Position);
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
