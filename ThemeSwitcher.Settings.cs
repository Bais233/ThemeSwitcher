// ThemeSwitcher.Settings.cs — 设置存取与设置窗口（partial class 的一部分）
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

// 应用设置, 存 %APPDATA%\ThemeSwitcher\settings.ini
internal static partial class AppSettings
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
    // Win10 1809+ 深色标题栏
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    static extern void DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    static void EnableDarkTitleBar(Form form)
    {
        int v = 1;
        try
        {
            DwmSetWindowAttribute(form.Handle, 20, ref v, 4); // DWMWA_USE_IMMERSIVE_DARK_MODE
            DwmSetWindowAttribute(form.Handle, 19, ref v, 4); // 老版本 19041 之前用 19
        }
        catch { }
    }

    // 深色扁平化: 递归处理控件颜色(注意 csc 是 C#5, 不用模式匹配)
    static void StyleDark(Control c)
    {
        Label lb = c as Label;
        if (lb != null) { lb.ForeColor = Color.FromArgb(215, 215, 215); lb.BackColor = Color.FromArgb(32, 32, 32); }
        CheckBox cb2 = c as CheckBox;
        if (cb2 != null) { cb2.ForeColor = Color.FromArgb(215, 215, 215); cb2.BackColor = Color.FromArgb(32, 32, 32); }
        LinkLabel lk = c as LinkLabel;
        if (lk != null) { lk.LinkColor = Color.FromArgb(120, 180, 255); lk.BackColor = Color.FromArgb(32, 32, 32); }
        GroupBox gb = c as GroupBox;
        if (gb != null) { gb.ForeColor = Color.FromArgb(160, 160, 160); gb.BackColor = Color.FromArgb(32, 32, 32); }
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

    // 下次切换时间文案(设置窗口与托盘共用)
    static string FormatNextSwitch()
    {
        if (nextSwitchTime == DateTime.MaxValue) return "下次切换：获取中…";
        return string.Format("下次切换：{0:HH:mm} → {1}", nextSwitchTime, nextSwitchLight ? "浅色" : "深色");
    }

    static int ClampInt(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }

    static decimal ClampDecimal(double v, decimal lo, decimal hi)
    {
        decimal d = (decimal)v;
        return d < lo ? lo : (d > hi ? hi : d);
    }

    // ---- 原生深色支持 ----
    // Win10 1809+ 的 uxtheme 未公开接口: 让按钮/复选框/下拉框/编辑框由系统深色主题绘制,
    // 而不是手工刷一层深灰(那样怎么看都是假的)。接口不可用时调用方退回手工配色。
    enum PreferredTheme { Default = 0, Light = 1, Dark = 2 }

    [System.Runtime.InteropServices.DllImport("uxtheme.dll", EntryPoint = "#135")]
    static extern int SetPreferredAppMode(PreferredTheme mode);

    [System.Runtime.InteropServices.DllImport("uxtheme.dll", EntryPoint = "#133")]
    static extern int AllowDarkModeForWindow(IntPtr hwnd, bool allow);

    [System.Runtime.InteropServices.DllImport("uxtheme.dll", EntryPoint = "#132")]
    static extern int FlushMenuThemes();

    [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);

    static bool TryEnableNativeDark(bool dark)
    {
        try
        {
            SetPreferredAppMode(dark ? PreferredTheme.Dark : PreferredTheme.Light);
            FlushMenuThemes();
            return true;
        }
        catch { return false; }    // 老系统或接口变动: 退回手工配色
    }

    static void TouchHandles(Control c)
    {
        IntPtr h = c.Handle;      // 提前建句柄, 主题在首次绘制前生效, 避免先白后黑闪一下
        foreach (Control ch in c.Controls) TouchHandles(ch);
    }

    // 原生深色下只调整文字颜色, 背景/边框交给系统主题绘制(手工刷背景会破坏原生质感)
    static void ApplyDarkText(Control c)
    {
        if (c is TextBox || c is NumericUpDown || c is ComboBox) c.ForeColor = Color.FromArgb(240, 240, 240);
        GroupBox gb = c as GroupBox;
        if (gb != null) gb.ForeColor = Color.FromArgb(238, 238, 238);
        LinkLabel lk = c as LinkLabel;
        if (lk != null) { lk.LinkColor = Color.FromArgb(130, 185, 255); lk.ActiveLinkColor = Color.FromArgb(170, 210, 255); }
        foreach (Control ch in c.Controls) ApplyDarkText(ch);
    }
    static void ApplyNativeDark(Control c)
    {
        try
        {
            AllowDarkModeForWindow(c.Handle, true);
            SetWindowTheme(c.Handle, "DarkMode_Explorer", null);
        }
        catch { }
        foreach (Control ch in c.Controls) ApplyNativeDark(ch);
    }

    // 设置窗口(托盘菜单"设置…"打开)
    // 布局按当前字体实测计算, 不写死像素: 换字体/换 DPI/换缩放都不会裁切控件
    internal static void ShowSettings()
    {
        using (var form = new SettingsForm())
        {
            form.Text = "ThemeSwitcher 设置";
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.AutoScaleMode = AutoScaleMode.None;   // 尺寸已实测, 不让 WinForms 再缩放一次
            form.Font = UiFont;
            form.KeyPreview = true;
            form.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) form.Close(); };

            bool dark = !currentLight;
            bool nativeDark = dark && TryEnableNativeDark(true);   // 深色: 让通用控件走系统原生深色主题
            if (!dark) TryEnableNativeDark(false);
            if (dark)
            {
                form.BackColor = Color.FromArgb(32, 32, 32);
                form.ForeColor = Color.FromArgb(240, 240, 240);
                form.Shown += (s, e) => EnableDarkTitleBar(form);
            }

            // ---- 度量单位 ----
            Font f = UiFont;
            int lh = TextRenderer.MeasureText("Ag中文", f).Height;   // 文本行高
            int pad = Math.Max(4, lh / 3);          // 常规间距
            int rowH = lh + pad;                    // 行步进
            int ctlH = lh + pad * 2;                // 控件高度
            int capH = lh + 6;                      // 组标题区高度
            int inner = pad + 6;                    // 组内边距
            int m = pad + 6;                        // 窗口边距
            int gap = pad + 4;                      // 组间距
            Func<string, int> tw = s => TextRenderer.MeasureText(s, f).Width;
            Func<string, int> btnW = delegate(string s)
            {
                using (var p = new Button()) { p.Font = f; p.Text = s; return p.GetPreferredSize(Size.Empty).Width; }
            };
            Func<string, int> chkW = delegate(string s)
            {
                using (var p = new CheckBox()) { p.Font = f; p.Text = s; return p.GetPreferredSize(Size.Empty).Width; }
            };

            // ---- 状态 ----
            string sLoc = "定位：" + locationDisplay;
            string sNext = FormatNextSwitch();
            var lblLocation = new Label { Text = sLoc, AutoSize = true };
            var lblNext = new Label { Text = sNext, AutoSize = true };

            // ---- 定位 ----
            var txtLocation = new TextBox { Text = latlonCurrent };
            var btnLocation = new Button { Text = "应用" };
            var lblHint = new Label { Text = "城市名（如：上海 / New York）或经纬度（如：31.23,121.47）", AutoSize = true };

            // ---- 深色模式切换范围 ----
            var chkApps = new CheckBox { AutoSize = true, Text = "应用主题（UWP / 资源管理器等 AppsUseLightTheme）", Checked = AppSettings.SwitchApps };
            var chkSystem = new CheckBox { AutoSize = true, Text = "系统主题（任务栏 / 开始菜单 SystemUsesLightTheme）", Checked = AppSettings.SwitchSystem };

            // ---- 切换时机偏移 ----
            var lblSunrise = new Label { Text = "日出（正=推迟浅色）：", AutoSize = true };
            var lblSunset = new Label { Text = "日落（正=推迟深色）：", AutoSize = true };
            var numSunrise = new NumericUpDown { Minimum = -720, Maximum = 720 };
            var numSunset = new NumericUpDown { Minimum = -720, Maximum = 720 };
            numSunrise.Value = ClampInt(AppSettings.SunriseOffsetMin, -720, 720);
            numSunset.Value = ClampInt(AppSettings.SunsetOffsetMin, -720, 720);

            // ---- 界面字体 ----
            var cmbFont = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var fam in FontFamily.Families) cmbFont.Items.Add(fam.Name);
            int fontIdx = cmbFont.Items.IndexOf(AppSettings.FontName);
            if (fontIdx < 0) { cmbFont.Items.Add(AppSettings.FontName); fontIdx = cmbFont.Items.Count - 1; }
            cmbFont.SelectedIndex = fontIdx;
            var numFontSize = new NumericUpDown { Minimum = 6, Maximum = 24, DecimalPlaces = 1, Increment = 0.5M };
            numFontSize.Value = ClampDecimal(AppSettings.FontSize, 6M, 24M);
            var lblFontPreview = new Label { Text = "预览 Aa 中文 123", AutoSize = true };
            Action updatePreview = () =>
            {
                try { lblFontPreview.Font = new Font((string)cmbFont.SelectedItem, (float)numFontSize.Value); }
                catch { }
            };
            updatePreview();

            // ---- 鼠标指针 ----
            var chkCursor = new CheckBox { AutoSize = true, Text = "切换主题时同步切换指针方案", Checked = AppSettings.SwitchCursor };
            var lblCurLight = new Label { Text = "浅色：", AutoSize = true };
            var lblCurDark = new Label { Text = "深色：", AutoSize = true };
            var cmbLightCur = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            var cmbDarkCur = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var sc in GetCursorSchemes()) { cmbLightCur.Items.Add(sc); cmbDarkCur.Items.Add(sc); }
            Action<ComboBox, string> pickScheme = (cb, name) =>
            {
                if (cb.Items.Count == 0) return;
                int i = cb.Items.IndexOf(string.IsNullOrEmpty(name) ? DefaultSchemeName : name);
                cb.SelectedIndex = i >= 0 ? i : 0;
            };
            pickScheme(cmbLightCur, AppSettings.CursorLightScheme);
            pickScheme(cmbDarkCur, AppSettings.CursorDarkScheme);

            // ---- 底部 ----
            var chkAutostart = new CheckBox { AutoSize = true, Text = "开机自启", Checked = GetAutostart() };
            var lnkHomepage = new LinkLabel { Text = "项目主页", AutoSize = true };
            var btnRefresh = new Button { Text = "重新获取日出日落" };
            var btnSaveClose = new Button { Text = "保存并关闭" };

            // ---- 各组的固有宽度需求 ----
            int spinW = Math.Max(lh * 4, tw("-720") + lh * 2);
            int sizeSpinW = Math.Max(lh * 3, tw("24.0") + lh * 2);
            int fontComboW = lh * 11;
            int curComboW = lh * 9;
            btnLocation.Size = new Size(btnW("应用"), ctlH);
            btnRefresh.Size = new Size(btnW("重新获取日出日落"), ctlH);
            btnSaveClose.Size = new Size(btnW("保存并关闭"), ctlH);
            int wState = Math.Max(tw(sLoc), tw(sNext));
            int wLocation = tw(lblHint.Text);
            int wTheme = Math.Max(chkW(chkApps.Text), chkW(chkSystem.Text));
            int wOffset = tw(lblSunrise.Text) + spinW + tw(lblSunset.Text) + spinW + pad * 2;
            int wFont = fontComboW + pad + sizeSpinW + pad + lblFontPreview.PreferredWidth + lh;
            int wCursor = Math.Max(chkW(chkCursor.Text),
                tw(lblCurLight.Text) + curComboW + tw(lblCurDark.Text) + curComboW + pad * 2);
            int wBottom = chkW("开机自启") + tw("项目主页") + btnRefresh.Width + btnSaveClose.Width + pad * 5;

            int contentW = wState;
            contentW = Math.Max(contentW, wLocation);
            contentW = Math.Max(contentW, wTheme);
            contentW = Math.Max(contentW, wOffset);
            contentW = Math.Max(contentW, wFont);
            contentW = Math.Max(contentW, wCursor);
            contentW = Math.Max(contentW, wBottom);
            contentW = Math.Max(contentW, lh * 20);           // 最小可用宽度
            int groupW = contentW + inner * 2;

            // ---- 各组高度 ----
            int hState = rowH * 2, hLocation = rowH * 2, hTheme = rowH * 2;
            int hOffset = rowH, hFont = rowH, hCursor = rowH * 2;
            int grpH = capH + hState + inner;
            int clientW = groupW + m * 2;
            int clientH = m * 2 + (capH + hState + inner) + (capH + hLocation + inner) +
                          (capH + hTheme + inner) + (capH + hOffset + inner) +
                          (capH + hFont + inner) + (capH + hCursor + inner) + gap * 5 + ctlH;

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            bool scroll = false;
            if (clientW > wa.Width - 16) { clientW = wa.Width - 16; scroll = true; }
            if (clientH > wa.Height - 16) { clientH = wa.Height - 16; scroll = true; }
            form.ClientSize = new Size(clientW, clientH);
            if (scroll) form.AutoScroll = true;

            // ---- 摆放 ----
            int gy = m;
            var grpState = new GroupBox { Text = "状态", Location = new Point(m, gy), Size = new Size(groupW, capH + hState + inner) };
            lblLocation.Location = new Point(inner, capH);
            lblNext.Location = new Point(inner, capH + rowH);
            grpState.Controls.AddRange(new Control[] { lblLocation, lblNext });
            gy += grpState.Height + gap;

            var grpLocation = new GroupBox { Text = "定位", Location = new Point(m, gy), Size = new Size(groupW, capH + hLocation + inner) };
            int editW = groupW - inner * 2 - pad - btnLocation.Width;
            txtLocation.Size = new Size(editW, txtLocation.Height);
            txtLocation.Location = new Point(inner, capH + (rowH - txtLocation.Height) / 2);
            btnLocation.Location = new Point(groupW - inner - btnLocation.Width, capH + (rowH - btnLocation.Height) / 2);
            lblHint.Location = new Point(inner, capH + rowH + (rowH - lblHint.Height) / 2);
            grpLocation.Controls.AddRange(new Control[] { txtLocation, btnLocation, lblHint });
            gy += grpLocation.Height + gap;

            var grpTheme = new GroupBox { Text = "「深色模式」切换哪些项", Location = new Point(m, gy), Size = new Size(groupW, capH + hTheme + inner) };
            chkApps.Location = new Point(inner, capH + (rowH - chkApps.Height) / 2);
            chkSystem.Location = new Point(inner, capH + rowH + (rowH - chkSystem.Height) / 2);
            grpTheme.Controls.AddRange(new Control[] { chkApps, chkSystem });
            gy += grpTheme.Height + gap;

            var grpOffset = new GroupBox { Text = "切换时机偏移（分钟）", Location = new Point(m, gy), Size = new Size(groupW, capH + hOffset + inner) };
            int ox = inner;
            lblSunrise.Location = new Point(ox, capH + (rowH - lblSunrise.Height) / 2);
            ox += tw(lblSunrise.Text);
            numSunrise.Size = new Size(spinW, ctlH);
            numSunrise.Location = new Point(ox, capH + (rowH - numSunrise.Height) / 2);
            ox += spinW + pad * 2;
            lblSunset.Location = new Point(ox, capH + (rowH - lblSunset.Height) / 2);
            numSunset.Size = new Size(spinW, ctlH);
            numSunset.Location = new Point(groupW - inner - spinW, capH + (rowH - numSunset.Height) / 2);
            grpOffset.Controls.AddRange(new Control[] { lblSunrise, numSunrise, lblSunset, numSunset });
            gy += grpOffset.Height + gap;

            var grpFont = new GroupBox { Text = "界面字体", Location = new Point(m, gy), Size = new Size(groupW, capH + hFont + inner) };
            cmbFont.Size = new Size(fontComboW, ctlH);
            cmbFont.Location = new Point(inner, capH + (rowH - cmbFont.Height) / 2);
            numFontSize.Size = new Size(sizeSpinW, ctlH);
            numFontSize.Location = new Point(inner + fontComboW + pad, capH + (rowH - numFontSize.Height) / 2);
            lblFontPreview.Location = new Point(inner + fontComboW + pad + sizeSpinW + pad, capH + (rowH - lblFontPreview.Height) / 2);
            grpFont.Controls.AddRange(new Control[] { cmbFont, numFontSize, lblFontPreview });
            gy += grpFont.Height + gap;

            var grpCursor = new GroupBox { Text = "鼠标指针", Location = new Point(m, gy), Size = new Size(groupW, capH + hCursor + inner) };
            chkCursor.Location = new Point(inner, capH + (rowH - chkCursor.Height) / 2);
            int cy = capH + rowH;
            lblCurLight.Location = new Point(inner, cy + (rowH - lblCurLight.Height) / 2);
            cmbLightCur.Size = new Size(curComboW, ctlH);
            cmbLightCur.Location = new Point(inner + tw(lblCurLight.Text), cy + (rowH - cmbLightCur.Height) / 2);
            cmbDarkCur.Size = new Size(curComboW, ctlH);
            cmbDarkCur.Location = new Point(groupW - inner - curComboW, cy + (rowH - cmbDarkCur.Height) / 2);
            lblCurDark.Location = new Point(cmbDarkCur.Left - tw(lblCurDark.Text) - pad, cy + (rowH - lblCurDark.Height) / 2);
            grpCursor.Controls.AddRange(new Control[] { chkCursor, lblCurLight, cmbLightCur, lblCurDark, cmbDarkCur });
            gy += grpCursor.Height + gap;

            int by = clientH - m - ctlH;
            chkAutostart.Location = new Point(m, by + (ctlH - chkAutostart.Height) / 2);
            lnkHomepage.Location = new Point(m + chkW("开机自启") + pad, by + (ctlH - lnkHomepage.Height) / 2);
            btnSaveClose.Location = new Point(clientW - m - btnSaveClose.Width, by);
            btnRefresh.Location = new Point(btnSaveClose.Left - pad - btnRefresh.Width, by);

            form.Controls.AddRange(new Control[] {
                grpState, grpLocation, grpTheme, grpOffset, grpFont, grpCursor,
                chkAutostart, lnkHomepage, btnRefresh, btnSaveClose });
            if (dark && !nativeDark) foreach (Control root in form.Controls) StyleDark(root);
            lblHint.ForeColor = dark ? Color.FromArgb(168, 168, 168) : SystemColors.GrayText;
            if (nativeDark) { TouchHandles(form); ApplyNativeDark(form); ApplyDarkText(form); }

            // ---- 事件 ----
            lnkHomepage.LinkClicked += (s, e) =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "https://github.com/Bais233/ThemeSwitcher") { UseShellExecute = true }); } catch { }
            };
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
            btnRefresh.Click += (s, e) => refreshEpoch++;
            cmbFont.SelectedIndexChanged += (s, e) => updatePreview();
            numFontSize.ValueChanged += (s, e) => updatePreview();
            chkCursor.CheckedChanged += (s, e) => { cmbLightCur.Enabled = cmbDarkCur.Enabled = chkCursor.Checked; };
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
                if (AppSettings.SwitchCursor)
                    ApplyCursorScheme(currentLight ? AppSettings.CursorLightScheme : AppSettings.CursorDarkScheme);
                refreshEpoch++;
                form.Close();
            };
            form.AcceptButton = btnSaveClose;

            // ---- 窗口打开期间实时刷新"下次切换" ----
            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (s, e) => { string txt = FormatNextSwitch(); if (txt != lblNext.Text) lblNext.Text = txt; };
            timer.Start();
            form.FormClosed += (s, e) => timer.Dispose();

            form.ShowDialog();
        }
    }
}

// 设置文件时间戳: 另一个实例(命令行 --settings)保存后, 托盘实例据此热加载
internal static partial class AppSettings
{
    public static DateTime Timestamp
    {
        get { return File.Exists(FilePath) ? File.GetLastWriteTime(FilePath) : DateTime.MinValue; }
    }
}

// 双缓冲设置窗体: 不双缓冲时深色背景逐控件重绘会闪, 打开像"现场渲染"
class SettingsForm : Form
{
    public SettingsForm()
    {
        DoubleBuffered = true;
    }
}
