// ThemeSwitcher.Sun.cs — 日出日落获取与切换时刻计算（partial class 的一部分）
using System;

static partial class ThemeSwitcher
{
    // 从 sunrise-sunset.org 取当天日出/日落(UTC ISO)，转成本地时间
    static DateTime GetSunEvent(string latlon, bool sunrise)
    {
        string json = HttpGet("https://api.sunrise-sunset.org/json?lat="
            + latlon.Substring(0, latlon.IndexOf(','))
            + "&lng=" + latlon.Substring(latlon.IndexOf(',') + 1) + "&formatted=0");
        string field = sunrise ? "sunrise" : "sunset";
        int i = json.IndexOf("\"" + field + "\":\"") + field.Length + 4;
        int j = json.IndexOf('"', i);
        DateTime utc = DateTime.Parse(json.Substring(i, j - i), null,
            System.Globalization.DateTimeStyles.AssumeUniversal);
        return utc.ToLocalTime();
    }

    // 计算下一次切换时刻: 未日出→等到日出切浅色; 未日落→等到日落切深色
    static DateTime NextEvent(DateTime now, string latlon, out bool light)
    {
        DateTime? rise = null, set = null;
        try
        {
            rise = GetSunEvent(latlon, true);
            set = GetSunEvent(latlon, false);
        }
        catch { /* 网络失败时稍后重试 */ }

        if (rise.HasValue && now < rise.Value) { light = true; return rise.Value; }
        if (set.HasValue && now < set.Value) { light = false; return set.Value; }

        // 今天的事件都过了: 过午夜再取新数据
        light = false;
        return now.Date.AddDays(1).AddMinutes(1);
    }
}
