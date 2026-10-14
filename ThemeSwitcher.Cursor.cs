// ThemeSwitcher.Cursor.cs — 鼠标指针方案切换（partial class 的一部分）
// 原理: 写 HKCU\Control Panel\Cursors 的各指针值, 再 SystemParametersInfo(SPI_SETCURSORS) 立即生效
using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Runtime.InteropServices;

static partial class ThemeSwitcher
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

    const uint SPI_SETCURSORS = 0x0057;
    const uint SPIF_UPDATEINIFILE = 0x01;
    const uint SPIF_SENDCHANGE = 0x02;

    const string CursorsKeyPath = @"Control Panel\Cursors";
    const string SchemesKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Control Panel\Cursors\Schemes";
    const string DefaultSchemeName = "Windows 默认（系统方案）";

    // 方案字符串里的指针顺序 -> 注册表值名
    static readonly string[] CursorValueNames =
    {
        "Arrow", "Help", "AppStarting", "Wait", "Crosshair", "IBeam", "NWPen",
        "No", "SizeNS", "SizeWE", "SizeNWSE", "SizeNESW", "UpArrow", "Hand"
    };

    // 列出本机已安装的指针方案名(含"Windows 默认")
    internal static List<string> GetCursorSchemes()
    {
        var list = new List<string> { DefaultSchemeName };
        using (var key = Registry.CurrentUser.OpenSubKey(SchemesKeyPath))
        {
            if (key != null)
                foreach (var name in key.GetValueNames())
                    if (!string.IsNullOrEmpty(name)) list.Add(name);
        }
        return list;
    }

    // 应用指定方案; schemeName 为空/默认方案时恢复系统默认指针
    internal static void ApplyCursorScheme(string schemeName)
    {
        try
        {
            string[] files = new string[CursorValueNames.Length]; // 默认全空 = 系统默认
            if (!string.IsNullOrEmpty(schemeName) && schemeName != DefaultSchemeName)
            {
                using (var sk = Registry.CurrentUser.OpenSubKey(SchemesKeyPath))
                {
                    string data = sk != null ? (sk.GetValue(schemeName) as string) : null;
                    if (data != null)
                    {
                        var parts = data.Split(',');
                        for (int i = 0; i < CursorValueNames.Length && i < parts.Length; i++)
                            files[i] = parts[i].Trim().Trim('"');
                    }
                }
            }

            using (var key = Registry.CurrentUser.CreateSubKey(CursorsKeyPath))
            {
                for (int i = 0; i < CursorValueNames.Length; i++)
                {
                    if (string.IsNullOrEmpty(files[i])) key.DeleteValue(CursorValueNames[i], false);
                    else key.SetValue(CursorValueNames[i], files[i]);
                }
                key.SetValue("Scheme Source", 0, RegistryValueKind.DWord); // 0 = 自定义/用户定义
                key.SetValue("", schemeName ?? ""); // (默认) 记录当前方案名
            }

            // 通知系统立即重载指针
            SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }
        catch { /* 指针切换失败不影响主题切换 */ }
    }
}
