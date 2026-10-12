// ThemeSwitcher.Location.cs — 定位与 JSON 解析（partial class 的一部分）
using System;
using System.Net;

static partial class ThemeSwitcher
{
    // 按 IP 定位(ip-api.com, 免费无 key), 返回 "lat,lon"
    static string DetectLocation()
    {
        string json = HttpGet("http://ip-api.com/json/?fields=status,lat,lon");
        if (!json.Contains("\"status\":\"success\"")) throw new Exception("ip-api failed: " + json);
        return JsonNum(json, "lat") + "," + JsonNum(json, "lon");
    }

    // 地名 -> 经纬度(OpenStreetMap Nominatim, 免费无 key)
    static string Geocode(string place)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        using (var wc = new WebClient())
        {
            wc.Headers["User-Agent"] = "ThemeSwitcher/1.0 (github.com/Bais233/ThemeSwitcher)";
            wc.Encoding = System.Text.Encoding.UTF8;
            string json = wc.DownloadString("https://nominatim.openstreetmap.org/search?format=json&limit=1&q="
                + Uri.EscapeDataString(place));
            if (json.Length < 5) throw new Exception("place not found: " + place);
            return JsonNum(json, "lat") + "," + JsonNum(json, "lon");
        }
    }

    // 极简 JSON 字段提取(兼容数字与带引号的字符串/日期)
    static string JsonNum(string json, string field)
    {
        int i = json.IndexOf("\"" + field + "\"");
        if (i < 0) throw new Exception("field not found: " + field);
        i = json.IndexOf(':', i) + 1;
        while (i < json.Length && (json[i] == '"' || json[i] == ' ')) i++;
        int j = i;
        while (j < json.Length && (char.IsLetterOrDigit(json[j]) || json[j] == '-' || json[j] == '.'
            || json[j] == ':' || json[j] == '+')) j++;
        while (j > i && json[j - 1] == '"') j--;
        return json.Substring(i, j - i);
    }
}
