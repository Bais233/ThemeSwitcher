// ThemeSwitcher — 按日出日落自动切换 Windows 深色/浅色模式（网络版）
// 编译: build.bat  (用 Windows 自带的 csc.exe)
// 用法: ThemeSwitcher.exe [坐标或地名]，无参数则按 IP 定位
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

static partial class ThemeSwitcher
{
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam,
        uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

    const uint WM_SETTINGCHANGE = 0x001A;
    static readonly IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;

    static void SetTheme(bool light)
    {
        using (var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true))
        {
            key.SetValue("AppsUseLightTheme", light ? 1 : 0, RegistryValueKind.DWord);
            key.SetValue("SystemUsesLightTheme", light ? 1 : 0, RegistryValueKind.DWord);
        }
        UIntPtr result;
        // 广播让应用与系统主题立即生效
        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero,
            "ImmersiveColorSet", 2, 1000, out result);
    }

    static string HttpGet(string url)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        using (var wc = new WebClient())
        {
            wc.Headers["User-Agent"] = "ThemeSwitcher/1.0";
            return wc.DownloadString(url);
        }
    }

    static void Main(string[] args)
    {
        // 定位优先级: 数字坐标 > 地名 > IP 自动定位（实现在 ThemeSwitcher.Location.cs）
        string latlon;
        double dummy;
        if (args.Length >= 2 && double.TryParse(args[0], out dummy) && double.TryParse(args[1], out dummy))
            latlon = args[0] + "," + args[1];
        else if (args.Length >= 1)
            latlon = Geocode(string.Join(" ", args));
        else
            latlon = DetectLocation();

        while (true)
        {
            bool light;
            DateTime next = NextEvent(DateTime.Now, latlon, out light);

            // 分片睡眠(最长 1 小时), 改系统时间后也能较快纠正
            while (DateTime.Now < next)
            {
                double ms = (next - DateTime.Now).TotalMilliseconds;
                if (ms > 3600000) ms = 3600000;
                if (ms < 1) break;
                Thread.Sleep((int)ms);
            }

            SetTheme(light);
        }
    }
}
