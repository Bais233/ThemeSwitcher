// ThemeSwitcher.Settings.cs — 设置存取与设置窗口（partial class 的一部分）
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

// 应用设置, 存 %APPDATA%\ThemeSwitcher\settings.ini
internal static class AppSettings
{
    public static bool SwitchApps = true;      // 切换应用主题 (AppsUseLightTheme)
    public static bool SwitchSystem = true;    // 切换系统/任务栏主题 (SystemUsesLightTheme)
    public static int SunriseOffsetMin;        // 日出偏移(分钟, 正=推迟切浅色)
    public static int SunsetOffsetMin;         // 日落偏移(分钟, 正=推迟切深色)

    static string FilePath
    {
        get
        {
            string dir = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "ThemeSwitcher");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.ini");
        }
    }

    public static void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            foreach (var line in File.ReadAllLines(FilePath))
            {
                int i = line.IndexOf('=');
                if (i <= 0) continue;
                string k = line.Substring(0, i).Trim();
                string v = line.Substring(i + 1).Trim();
                bool b; int n;
                switch (k)
                {
                    case "SwitchApps": if (bool.TryParse(v, out b)) SwitchApps = b; break;
                    case "SwitchSystem": if (bool.TryParse(v, out b)) SwitchSystem = b; break;
                    case "SunriseOffsetMin": if (int.TryParse(v, out n)) SunriseOffsetMin = n; break;
                    case "SunsetOffsetMin": if (int.TryParse(v, out n)) SunsetOffsetMin = n; break;
                }
            }
        }
        catch { /* 设置损坏时用默认值 */ }
    }

    public static void Save()
    {
        File.WriteAllLines(FilePath, new[]
        {
            "SwitchApps=" + SwitchApps,
            "SwitchSystem=" + SwitchSystem,
            "SunriseOffsetMin=" + SunriseOffsetMin,
            "SunsetOffsetMin=" + SunsetOffsetMin,
        });
    }
}

static partial class ThemeSwitcher
{
    // 设置窗口; 保存后刷新重算
    internal static void ShowSettings()
    {
        using (var form = new Form())
        {
            form.Text = "ThemeSwitcher 设置";
            form.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.ClientSize = new Size(400, 210);
            form.MaximizeBox = false; form.MinimizeBox = false;
            form.ShowInTaskbar = false;

            var chkApps = new CheckBox
            {
                Text = "切换应用主题（UWP / 资源管理器等 AppsUseLightTheme）",
                Location = new Point(12, 12), AutoSize = true,
                Checked = AppSettings.SwitchApps
            };
            var chkSystem = new CheckBox
            {
                Text = "切换系统主题（任务栏 / 开始菜单 SystemUsesLightTheme）",
                Location = new Point(12, 40), AutoSize = true,
                Checked = AppSettings.SwitchSystem
            };

            var lblSunrise = new Label { Text = "日出偏移(分钟，正=推迟切浅色)：", Location = new Point(12, 78), AutoSize = true };
            var numSunrise = new NumericUpDown { Location = new Point(240, 74), Width = 70, Minimum = -720, Maximum = 720, Value = AppSettings.SunriseOffsetMin };

            var lblSunset = new Label { Text = "日落偏移(分钟，正=推迟切深色)：", Location = new Point(12, 108), AutoSize = true };
            var numSunset = new NumericUpDown { Location = new Point(240, 104), Width = 70, Minimum = -720, Maximum = 720, Value = AppSettings.SunsetOffsetMin };

            var note = new Label
            {
                Text = "注：两项主题都不勾选时，到点不修改系统设置。",
                Location = new Point(12, 138), AutoSize = true,
                ForeColor = SystemColors.GrayText
            };

            var ok = new Button { Text = "保存", DialogResult = DialogResult.OK, Location = new Point(225, 170), Width = 80 };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(315, 170), Width = 80 };
            form.Controls.AddRange(new Control[] {
                chkApps, chkSystem, lblSunrise, numSunrise, lblSunset, numSunset, note, ok, cancel });
            form.AcceptButton = ok; form.CancelButton = cancel;

            if (form.ShowDialog() == DialogResult.OK)
            {
                AppSettings.SwitchApps = chkApps.Checked;
                AppSettings.SwitchSystem = chkSystem.Checked;
                AppSettings.SunriseOffsetMin = (int)numSunrise.Value;
                AppSettings.SunsetOffsetMin = (int)numSunset.Value;
                AppSettings.Save();
                refreshEpoch++; // 重新计算下次切换时刻
                if (tray != null)
                    tray.ShowBalloonTip(2000, "ThemeSwitcher", "设置已保存并生效", ToolTipIcon.Info);
            }
        }
    }
}
