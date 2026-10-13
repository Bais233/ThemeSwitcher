// ThemeSwitcher.Sun.cs — 日出日落获取与切换时刻计算（partial class 的一部分）
using System;

static partial class ThemeSwitcher
{
    // 从 sunrise-sunset.org 取指定日期的日出/日落(UTC ISO)，转成本地时间
    // 必须显式传本地日期: API 默认按 UTC 日取数, UTC+8 的凌晨会拿到昨天的数据导致早上不切浅色
    static DateTime GetSunEvent(string latlon, bool sunrise)
    {
        string date = DateTime.Now.ToString("yyyy-MM-dd");
        string json = HttpGet("https://api.sunrise-sunset.org/json?lat="
            + latlon.Substring(0, latlon.IndexOf(','))
            + "&lng=" + latlon.Substring(latlon.IndexOf(',') + 1)
            + "&date=" + date + "&formatted=0");
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

        // 应用用户设置的日出/日落偏移
        if (rise.HasValue) rise = rise.Value.AddMinutes(AppSettings.SunriseOffsetMin);
        if (set.HasValue) set = set.Value.AddMinutes(AppSettings.SunsetOffsetMin);

        if (rise.HasValue && now < rise.Value) { light = true; return rise.Value; }
        if (set.HasValue && now < set.Value) { light = false; return set.Value; }

        // 今天的事件都过了: 过午夜再取新数据
        light = false;
        return now.Date.AddDays(1).AddMinutes(1);
    }
}
