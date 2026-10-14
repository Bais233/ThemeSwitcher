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
    public static string FontName = "Microsoft YaHei UI"; // 界面字体
    public static float FontSize = 9F;                    // 界面字号
    public static bool SwitchCursor;                      // 切换时同步切换鼠标指针方案
    public static string CursorLightScheme = "";          // 浅色指针方案
    public static string CursorDarkScheme = "";           // 深色指针方案

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
                    case "FontName": if (v.Length > 0) FontName = v; break;
                    case "FontSize": { float f; if (float.TryParse(v, out f) && f >= 6 && f <= 24) FontSize = f; break; }
                    case "SwitchCursor": if (bool.TryParse(v, out b)) SwitchCursor = b; break;
                    case "CursorLightScheme": CursorLightScheme = v; break;
                    case "CursorDarkScheme": CursorDarkScheme = v; break;
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
            "FontName=" + FontName,
            "FontSize=" + FontSize,
            "SwitchCursor=" + SwitchCursor,
            "CursorLightScheme=" + CursorLightScheme,
            "CursorDarkScheme=" + CursorDarkScheme,
        });
    }
}

static partial class ThemeSwitcher
{
    // 深色扁平化: 递归处理控件颜色(注意 csc 是 C#5, 不用模式匹配)
    static void StyleDark(Control c)
    {
        Label lb = c as Label;
        if (lb != null) { lb.ForeColor = Color.FromArgb(215, 215, 215); lb.BackColor = Color.Transparent; }
        CheckBox cb2 = c as CheckBox;
        if (cb2 != null) { cb2.ForeColor = Color.FromArgb(215, 215, 215); cb2.BackColor = Color.Transparent; }
        LinkLabel lk = c as LinkLabel;
        if (lk != null) { lk.LinkColor = Color.FromArgb(120, 180, 255); lk.BackColor = Color.Transparent; }
        GroupBox gb = c as GroupBox;
        if (gb != null) { gb.ForeColor = Color.FromArgb(160, 160, 160); gb.BackColor = Color.Transparent; }
        Button bt = c as Button;
        if (bt != null)
        {
            bt.FlatStyle = FlatStyle.Flat;
            bt.FlatAppearance.BorderSize = 0;
            bt.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 65, 65);
            bt.BackColor = Color.FromArgb(50, 50, 50);
            bt.ForeColor = Color.FromArgb(230, 230, 230);
        }
        TextBox tb = c as TextBox;
        if (tb != null)
        {
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.BackColor = Color.FromArgb(48, 48, 48);
            tb.ForeColor = Color.FromArgb(230, 230, 230);
        }
        NumericUpDown nup = c as NumericUpDown;
        if (nup != null)
        {
            nup.BorderStyle = BorderStyle.FixedSingle;
            nup.BackColor = Color.FromArgb(48, 48, 48);
            nup.ForeColor = Color.FromArgb(230, 230, 230);
        }
        ComboBox cmb = c as ComboBox;
        if (cmb != null)
        {
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = Color.FromArgb(48, 48, 48);
            cmb.ForeColor = Color.FromArgb(230, 230, 230);
        }
        foreach (Control ch in c.Controls) StyleDark(ch);
    }

    // 解析定位输入: "纬度,经度" 或 地名(Nominatim); 返回 false 表示解析失败
    static bool TryParseLocationInput(string input, out string latlon, out string display)
    {
        latlon = null; display = null;
        input = (input ?? "").Trim();
        if (input.Length == 0) return false;

        double lat, lon;
        if (input.Contains(",") &&
            double.TryParse(input.Split(',')[0].Trim(), out lat) &&
            double.TryParse(input.Split(',')[1].Trim(), out lon))
        {
            latlon = lat + "," + lon;
            display = latlon;
            return true;
        }

        try
        {
            latlon = Geocode(input);
            if (latlon == null || latlon.Length == 0 || latlon == ",") return false;
            display = input + " (" + latlon + ")";
            return true;
        }
        catch { return false; }
    }

    // 设置窗口(托盘菜单"设置…"打开); 亚克力深色圆角风格
    internal static void ShowSettings()
    {
        using (var form = new Form())
        {
            form.Text = "ThemeSwitcher 设置";
            form.FormBorderStyle = FormBorderStyle.None;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.ClientSize = new Size(460, 482);
            form.MaximizeBox = false; form.MinimizeBox = false;
            form.ShowInTaskbar = false;
            form.Font = UiFont;
            form.BackColor = Color.FromArgb(30, 30, 30);
            form.KeyPreview = true;
            form.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) form.Close(); };
            form.Shown += (s, e) =>
            {
                form.Region = WindowFx.RoundedRegion(form.Size, 10);
                WindowFx.EnableAcrylic(form, 0xE01E1E1E);
            };

            // 自绘标题栏: 可拖动 + 关闭按钮
            var header = new Panel { Location = new Point(0, 0), Size = new Size(460, 36), BackColor = Color.Transparent };
            var title = new Label { Text = "ThemeSwitcher 设置", Location = new Point(14, 9), AutoSize = true, ForeColor = Color.FromArgb(220, 220, 220) };
            var btnClose = new Button
            {
                Text = "\uE711", Location = new Point(424, 4), Size = new Size(32, 28),
                FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(200, 200, 200),
                BackColor = Color.Transparent,
                Font = new Font("Segoe MDL2 Assets", 9F)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 60, 60, 60);
            btnClose.Click += (s, e) => form.Close();
            header.Controls.AddRange(new Control[] { title, btnClose });
            header.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) WindowFx.DragWindow(form); };
            title.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) WindowFx.DragWindow(form); };

            // 内容层: 所有控件相对内容层定位(原坐标 + 顶部 36px 标题栏)
            var content = new Panel { Location = new Point(1, 37), Size = new Size(458, 444), BackColor = Color.Transparent };

            // ---- 状态 ----
            var grpState = new GroupBox { Text = "状态", Location = new Point(10, 10), Size = new Size(420, 72) };
            var lblLocation = new Label { Location = new Point(12, 22), AutoSize = true, Text = "定位：" + locationDisplay };
            var lblNext = new Label { Location = new Point(12, 46), AutoSize = true, Text = "下次切换：…" };
            grpState.Controls.Add(lblLocation);
            grpState.Controls.Add(lblNext);

            // ---- 定位 ----
            var grpLocation = new GroupBox { Text = "定位", Location = new Point(10, 88), Size = new Size(420, 72) };
            var txtLocation = new TextBox { Location = new Point(12, 22), Width = 320, Text = latlonCurrent };
            var btnLocation = new Button { Text = "应用", Location = new Point(340, 20), Width = 68 };
            var lblLocationHint = new Label
            {
                Location = new Point(12, 50), AutoSize = true, ForeColor = SystemColors.GrayText,
                Text = "城市名（如：上海 / New York）或经纬度（如：31.23,121.47）"
            };
            grpLocation.Controls.AddRange(new Control[] { txtLocation, btnLocation, lblLocationHint });

            // ---- 主题切换 ----
            var grpTheme = new GroupBox { Text = "「深色模式」切换哪些项", Location = new Point(10, 166), Size = new Size(420, 70) };
            var chkApps = new CheckBox
            {
                Text = "应用主题（UWP / 资源管理器等 AppsUseLightTheme）",
                Location = new Point(12, 20), AutoSize = true, Checked = AppSettings.SwitchApps
            };
            var chkSystem = new CheckBox
            {
                Text = "系统主题（任务栏 / 开始菜单 SystemUsesLightTheme）",
                Location = new Point(12, 44), AutoSize = true, Checked = AppSettings.SwitchSystem
            };
            grpTheme.Controls.AddRange(new Control[] { chkApps, chkSystem });

            // ---- 时机偏移 ----
            var grpOffset = new GroupBox { Text = "切换时机偏移（分钟）", Location = new Point(10, 242), Size = new Size(420, 54) };
            var lblSunrise = new Label { Text = "日出（正=推迟浅色）：", Location = new Point(12, 24), AutoSize = true };
            var numSunrise = new NumericUpDown { Location = new Point(150, 21), Width = 70, Minimum = -720, Maximum = 720, Value = AppSettings.SunriseOffsetMin };
            var lblSunset = new Label { Text = "日落（正=推迟深色）：", Location = new Point(240, 24), AutoSize = true };
            var numSunset = new NumericUpDown { Location = new Point(378, 21), Width = 32, Minimum = -720, Maximum = 720, Value = AppSettings.SunsetOffsetMin };
            grpOffset.Controls.AddRange(new Control[] { lblSunrise, numSunrise, lblSunset, numSunset });

            // ---- 界面字体 ----
            var grpFont = new GroupBox { Text = "界面字体", Location = new Point(10, 302), Size = new Size(420, 56) };
            var cmbFont = new ComboBox
            {
                Location = new Point(12, 22), Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Items = { }
            };
            foreach (var fam in FontFamily.Families) cmbFont.Items.Add(fam.Name);
            int idx = cmbFont.Items.IndexOf(AppSettings.FontName);
            if (idx < 0) { cmbFont.Items.Add(AppSettings.FontName); idx = cmbFont.Items.Count - 1; }
            cmbFont.SelectedIndex = idx;
            var numFontSize = new NumericUpDown
            {
                Location = new Point(244, 21), Width = 60,
                Minimum = 7, Maximum = 18, DecimalPlaces = 1, Increment = 0.5M,
                Value = (decimal)AppSettings.FontSize
            };
            var lblFontPreview = new Label
            {
                Text = "预览 Aa 中文 123", Location = new Point(316, 22), AutoSize = true,
                UseCompatibleTextRendering = true // Label 用 GDI+ 才能预览非 GDI 字体效果
            };
            Action updatePreview = () =>
            {
                try { lblFontPreview.Font = new Font((string)cmbFont.SelectedItem, (float)numFontSize.Value); } catch { }
            };
            cmbFont.SelectedIndexChanged += (s, e) => updatePreview();
            numFontSize.ValueChanged += (s, e) => updatePreview();
            updatePreview();
            grpFont.Controls.AddRange(new Control[] { cmbFont, numFontSize, lblFontPreview });

            // ---- 鼠标指针 ----
            var grpCursor = new GroupBox { Text = "鼠标指针", Location = new Point(10, 364), Size = new Size(420, 80) };
            var chkCursor = new CheckBox
            {
                Text = "切换主题时同步切换指针方案",
                Location = new Point(12, 20), AutoSize = true,
                Checked = AppSettings.SwitchCursor
            };
            var schemes = GetCursorSchemes();
            var cmbLightCur = new ComboBox
            {
                Location = new Point(12, 46), Width = 190, DropDownStyle = ComboBoxStyle.DropDownList
            };
            var cmbDarkCur = new ComboBox
            {
                Location = new Point(228, 46), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var sc in schemes) { cmbLightCur.Items.Add(sc); cmbDarkCur.Items.Add(sc); }
            Action<ComboBox, string> selectScheme = (cb, name) =>
            {
                int i = cb.Items.IndexOf(string.IsNullOrEmpty(name) ? DefaultSchemeName : name);
                cb.SelectedIndex = i >= 0 ? i : 0;
            };
            selectScheme(cmbLightCur, AppSettings.CursorLightScheme);
            selectScheme(cmbDarkCur, AppSettings.CursorDarkScheme);
            Action updateCursorUi = () =>
            {
                cmbLightCur.Enabled = cmbDarkCur.Enabled = chkCursor.Checked;
            };
            chkCursor.CheckedChanged += (s, e) => updateCursorUi();
            updateCursorUi();
            grpCursor.Controls.AddRange(new Control[] { chkCursor, cmbLightCur, cmbDarkCur });

            // ---- 底部行 ----
            var chkAutostart = new CheckBox
            {
                Text = "开机自启", Location = new Point(12, 452), AutoSize = true,
                Checked = GetAutostart()
            };
            var lnkHomepage = new LinkLabel { Text = "项目主页", Location = new Point(110, 456), AutoSize = true };
            lnkHomepage.LinkClicked += (s, e) =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "https://github.com/Bais233/ThemeSwitcher") { UseShellExecute = true }); } catch { }
            };
            var btnRefresh = new Button { Text = "重新获取日出日落", Location = new Point(200, 449), Width = 135 };
            var btnSaveClose = new Button { Text = "保存并关闭", Location = new Point(348, 449), Width = 82 };

            // 定位应用
            btnLocation.Click += (s, e) =>
            {
                string ll, disp;
                if (TryParseLocationInput(txtLocation.Text, out ll, out disp))
                {
                    ChangeLocation(ll, disp);
                    lblLocation.Text = "定位：" + locationDisplay;
                    txtLocation.Text = ll;
                    MessageBox.Show(form, "定位已生效，正在重新计算切换时间。", "ThemeSwitcher",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(form, "定位解析失败，请检查输入（需联网解析地名）。", "ThemeSwitcher",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            // 刷新日出日落
            btnRefresh.Click += (s, e) => refreshEpoch++;

            // 保存并关闭
            btnSaveClose.Click += (s, e) =>
            {
                AppSettings.SwitchApps = chkApps.Checked;
                AppSettings.SwitchSystem = chkSystem.Checked;
                AppSettings.SunriseOffsetMin = (int)numSunrise.Value;
                AppSettings.SunsetOffsetMin = (int)numSunset.Value;
                AppSettings.FontName = (string)cmbFont.SelectedItem;
                AppSettings.FontSize = (float)numFontSize.Value;
                AppSettings.SwitchCursor = chkCursor.Checked;
                AppSettings.CursorLightScheme = (string)cmbLightCur.SelectedItem;
                AppSettings.CursorDarkScheme = (string)cmbDarkCur.SelectedItem;
                AppSettings.Save();
                SetAutostart(chkAutostart.Checked);
                ApplyUiFont(AppSettings.FontName, AppSettings.FontSize);
                // 指针方案立即应用一次, 便于看到效果
                if (AppSettings.SwitchCursor)
                    ApplyCursorScheme(currentLight ? AppSettings.CursorLightScheme : AppSettings.CursorDarkScheme);
                refreshEpoch++;
                form.Close();
            };

            form.Controls.AddRange(new Control[] { header, content });

            // 深色扁平化样式: 递归处理所有控件
            foreach (Control root in content.Controls) StyleDark(root);

            // 窗口打开期间实时刷新"下次切换"显示
            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (s, e) =>
            {
                if (nextSwitchTime == DateTime.MaxValue)
                    lblNext.Text = "下次切换：获取中…";
                else
                    lblNext.Text = string.Format("下次切换：{0:HH:mm} → {1}", nextSwitchTime, nextSwitchLight ? "浅色" : "深色");
            };
            timer.Start();
            form.FormClosed += (s, e) => timer.Dispose();

            form.ShowDialog();
        }
    }
}
