// ThemeSwitcher.Sun.cs — 日出日落获取与切换时刻计算（partial class 的一部分）
using System;

static partial class ThemeSwitcher
{
    // 从 sunrise-sunset.org 取指定日期的日出/日落(UTC ISO)，转成本地时间
    // 必须显式传本地日期: API 默认按 UTC 日取数, UTC+8 的凌晨会拿到昨天的数据导致早上不切浅色
    static DateTime GetSunEvent(string latlon, bool sunrise, int dayOffset)
    {
        string date = DateTime.Now.AddDays(dayOffset).ToString("yyyy-MM-dd");
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
    // 今天的事件都已过完时直接查明天的, 免得界面上出现"00:01 → 深色"这种没意义的时刻
    static DateTime NextEvent(DateTime now, string latlon, out bool light)
    {
        for (int day = 0; day <= 1; day++)
        {
            DateTime? rise = null, set = null;
            try
            {
                rise = GetSunEvent(latlon, true, day);
                set = GetSunEvent(latlon, false, day);
            }
            catch { /* 网络失败走兜底 */ }
            if (!rise.HasValue && !set.HasValue) break;

            if (rise.HasValue)
            {
                DateTime when = rise.Value.AddMinutes(AppSettings.SunriseOffsetMin);
                if (now < when) { light = true; return when; }
            }
            if (set.HasValue)
            {
                DateTime when = set.Value.AddMinutes(AppSettings.SunsetOffsetMin);
                if (now < when) { light = false; return when; }
            }
        }

        // 拿不到数据: 过午夜再重算
        light = false;
        return now.Date.AddDays(1).AddMinutes(1);
    }
}