// ThemeSwitcher — 按日出日落自动切换 Windows 深色/浅色模式（网络版）
// 编译: build.bat  (用 Windows 自带的 csc.exe)
// 用法: ThemeSwitcher.exe [坐标或地名]，无参数则按 IP 定位
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
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
        currentLight = light;
    }

    // 供托盘显示的当前状态
    internal static volatile bool currentLight = true;
    internal static volatile string locationDisplay = "定位中…";   // 托盘菜单显示用
    internal static DateTime nextSwitchTime = DateTime.MaxValue;   // 下次切换时刻
    internal static bool nextSwitchLight;                          // 下次切换到的模式
    internal static volatile int refreshEpoch;                     // +1 表示要求重新拉取日出日落/重算

    // 运行时切换定位(托盘菜单调用); 直接生效, 后台线程会自动重算
    internal static void ChangeLocation(string latlon, string display)
    {
        latlonCurrent = latlon;
        locationDisplay = display;
        refreshEpoch++;
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

    [STAThread]
    static void Main(string[] args)
    {
        // 定位优先级: 数字坐标 > 地名 > IP 自动定位（实现在 ThemeSwitcher.Location.cs）
        string latlon;
        double dummy;
        if (args.Length >= 2 && double.TryParse(args[0], out dummy) && double.TryParse(args[1], out dummy))
        {
            latlon = args[0] + "," + args[1];
            locationDisplay = latlon;
        }
        else if (args.Length >= 1)
        {
            latlon = Geocode(string.Join(" ", args));
            locationDisplay = string.Join(" ", args) + " (" + latlon + ")";
        }
        else
        {
            latlon = DetectLocation();
            locationDisplay = "IP 定位 (" + latlon + ")";
        }
        latlonCurrent = latlon;

        // 切换逻辑跑在后台线程
        var worker = new Thread(() => RunLoop());
        worker.IsBackground = true;
        worker.Start();

        TrayRun(); // 托盘图标 + 消息循环(阻塞直到退出)
    }

    static string latlonCurrent = "";

    static void RunLoop()
    {
        int epoch = refreshEpoch;
        while (true)
        {
            bool light;
            DateTime next;

            // 计算下次切换时刻(失败时 30 分钟后重试)
            try
            {
                next = NextEvent(DateTime.Now, latlonCurrent, out light);
            }
            catch
            {
                nextSwitchTime = DateTime.Now.AddMinutes(30);
                nextSwitchLight = currentLight;
                Thread.Sleep(30000);
                continue;
            }
            if (epoch != refreshEpoch) { epoch = refreshEpoch; continue; } // 定位/刷新已变更, 重算
            nextSwitchTime = next;
            nextSwitchLight = light;

            // 等到切换时刻; 期间定位/刷新变更或时钟被改则提前跳出重算
            while (true)
            {
                if (epoch != refreshEpoch) break;
                double ms = (next - DateTime.Now).TotalMilliseconds;
                if (ms <= 0) { ms = 0; }
                else if (ms > 3600000) ms = 3600000; // 分片睡眠, 改系统时间后也能较快纠正
                if (ms == 0)
                {
                    if (DateTime.Now >= next) break;
                    continue;
                }
                Thread.Sleep((int)ms);
            }
            if (epoch != refreshEpoch) { epoch = refreshEpoch; continue; }

            if (DateTime.Now >= next) SetTheme(light);
            epoch = refreshEpoch;
        }
    }
}
