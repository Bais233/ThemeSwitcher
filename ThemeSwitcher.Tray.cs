// ThemeSwitcher.Tray.cs — 系统托盘图标与右键菜单（partial class 的一部分）
using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

static partial class ThemeSwitcher
{
    static NotifyIcon tray;
    static ToolStripMenuItem infoLocation, infoNext, autostartItem;

    static void TrayRun()
    {
        Application.EnableVisualStyles();

        tray = new NotifyIcon();
        tray.Icon = LoadAppIcon();
        tray.Text = "ThemeSwitcher";
        tray.Visible = true;

        var menu = new ContextMenuStrip();

        // 状态信息(只读展示, 由定时器刷新)
        infoLocation = new ToolStripMenuItem("定位：…") { Enabled = false };
        infoNext = new ToolStripMenuItem("下次切换：…") { Enabled = false };
        menu.Items.Add(infoLocation);
        menu.Items.Add(infoNext);
        menu.Items.Add(new ToolStripSeparator());

        // 立即切换
        menu.Items.Add("切换到浅色模式", null, (s, e) => SetTheme(true));
        menu.Items.Add("切换到深色模式", null, (s, e) => SetTheme(false));
        menu.Items.Add("重新获取日出日落", null, (s, e) => refreshEpoch++);

        // 定位
        menu.Items.Add("更改定位…", null, (s, e) => PromptLocation());
        menu.Items.Add(new ToolStripSeparator());

        // 设置
        menu.Items.Add("设置…", null, (s, e) => ShowSettings());

        // 开机自启(注册表 Run 键)
        autostartItem = new ToolStripMenuItem("开机自启");
        autostartItem.Checked = GetAutostart();
        autostartItem.Click += (s, e) => { ToggleAutostart(); autostartItem.Checked = GetAutostart(); };
        menu.Items.Add(autostartItem);

        menu.Items.Add("打开项目主页", null, (s, e) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "https://github.com/Bais233/ThemeSwitcher") { UseShellExecute = true }); } catch { }
        });
        menu.Items.Add("退出", null, (s, e) =>
        {
            tray.Visible = false;
            Application.Exit();
        });
        tray.ContextMenuStrip = menu;

        // 定时刷新状态显示(后台线程只改 volatile 字段, 这里轮询避免跨线程 UI 调用)
        var timer = new System.Windows.Forms.Timer { Interval = 2000 };
        timer.Tick += (s, e) =>
        {
            infoLocation.Text = "定位：" + locationDisplay;
            if (nextSwitchTime == DateTime.MaxValue)
                infoNext.Text = "下次切换：获取中…";
            else
                infoNext.Text = string.Format("下次切换：{0:HH:mm} → {1}",
                    nextSwitchTime, nextSwitchLight ? "浅色" : "深色");
            tray.Text = "ThemeSwitcher — " + (currentLight ? "浅色模式" : "深色模式");
        };
        timer.Start();

        Application.Run(); // 消息循环, 直到菜单选"退出"
    }

    // 输入框: 更改定位(支持 地名 或 "纬度,经度")
    static void PromptLocation()
    {
        using (var form = new Form())
        {
            form.Text = "更改定位";
            form.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.ClientSize = new Size(360, 110);
            form.MaximizeBox = false; form.MinimizeBox = false;
            form.ShowInTaskbar = false;

            var label = new Label
            {
                Text = "输入城市名（如：上海 / New York）或经纬度（如：31.23,121.47）：",
                Location = new Point(10, 10), AutoSize = true
            };
            var box = new TextBox { Location = new Point(10, 35), Width = 340 };
            var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Location = new Point(190, 70), Width = 75 };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(275, 70), Width = 75 };
            form.Controls.AddRange(new Control[] { label, box, ok, cancel });
            form.AcceptButton = ok; form.CancelButton = cancel;

            if (form.ShowDialog() != DialogResult.OK) return;
            string input = box.Text.Trim();
            if (input.Length == 0) return;

            try
            {
                // 纯数字 -> 视为 "纬度,经度"
                double lat, lon;
                if (input.Contains(",") &&
                    double.TryParse(input.Split(',')[0].Trim(), out lat) &&
                    double.TryParse(input.Split(',')[1].Trim(), out lon))
                {
                    ChangeLocation(lat + "," + lon, lat + "," + lon);
                }
                else
                {
                    string ll = Geocode(input);
                    if (ll.Length == 0) throw new Exception("geocode failed");
                    ChangeLocation(ll, input + " (" + ll + ")");
                }
                tray.ShowBalloonTip(2000, "ThemeSwitcher", "定位已切换：" + locationDisplay, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                tray.ShowBalloonTip(3000, "定位失败", ex.Message, ToolTipIcon.Error);
            }
        }
    }

    // ---- 开机自启(HKCU Run 键) ----
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValue = "ThemeSwitcher";

    static bool GetAutostart()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
            return key != null && key.GetValue(RunValue) != null;
    }

    static void ToggleAutostart()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            if (GetAutostart()) key.DeleteValue(RunValue, false);
            else key.SetValue(RunValue, "\"" + Application.ExecutablePath + "\"");
        }
    }

    static Icon LoadAppIcon()
    {
        try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch { return SystemIcons.Application; }
    }
}
