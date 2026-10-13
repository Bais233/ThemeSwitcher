# ThemeSwitcher

**6 KB 大小**的 Windows 深色/浅色模式自动切换工具：按**日出日落**时间自动在深色与浅色模式间切换。

- 🪶 极小体积：exe 仅约 **10 KB**（依赖系统内置的 .NET Framework 4，Win10/Win11 无需安装任何东西）
- 🌐 在线数据：[ip-api.com](https://ip-api.com) 按 IP 定位、[OpenStreetMap Nominatim](https://nominatim.openstreetmap.org) 地名解析、[sunrise-sunset.org](https://sunrise-sunset.org) 日出日落（均免费、无需注册/API Key）
- 📍 支持三种定位方式：数字坐标 / 地名（如 `ThemeSwitcher.exe 上海`）/ IP 自动定位
- 🔇 后台静默切换，托盘图标显示当前模式，右键菜单可手动切换/退出
- ⚡ 切换立即生效（写注册表 + 广播 `WM_SETTINGCHANGE`），无需重启资源管理器

## 使用

```text
ThemeSwitcher.exe              # 按 IP 自动定位经纬度
ThemeSwitcher.exe 39.9 116.4   # 手动指定纬度 经度（数字）
ThemeSwitcher.exe 上海          # 直接输入地名（中英文均可，通过 OpenStreetMap Nominatim 解析）
ThemeSwitcher.exe New York     # 含空格的地名也可以
```

地名解析和 IP 定位只在启动时进行一次。使用代理/VPN 时建议用手动坐标或地名。

### 开机自启

1. `Win + R` 输入 `shell:startup`
2. 把 `ThemeSwitcher.exe` 的快捷方式放进去

### 托盘图标

右下角常驻图标（日/夜双色），右键菜单功能：

- 📍 当前定位显示（含经纬度）
- ⏰ 下次切换时间与目标模式
- 立即切浅色 / 立即切深色
- 重新获取日出日落（网络恢复后手动刷新）
- 更改定位…（运行时直接换城市/坐标，立即生效）
- ☑ 开机自启开关（写 HKCU Run 键，无需手动放 startup 文件夹）
- 打开项目主页 / 退出

## 编译

无需安装任何编译器，Windows 自带的 .NET 编译器即可：

```bat
build.bat
```

等价命令：

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /optimize+ /out:ThemeSwitcher.exe ThemeSwitcher.cs ThemeSwitcher.Sun.cs ThemeSwitcher.Location.cs
```

## 工作原理

1. 启动时确定位置：数字参数 = 经纬度；非数字参数 = 地名（Nominatim 地理编码）；无参数 = IP 自动定位
2. 从 sunrise-sunset.org 取当天日出/日落（UTC），转换为系统本地时间
3. 睡眠到最近的切换时刻（分片睡眠，最长 1 小时一片，改系统时间后也能较快纠正）
4. 日出 → 切浅色（注册表 `AppsUseLightTheme`/`SystemUsesLightTheme` = 1），日落 → 切深色（= 0）
5. 广播 `WM_SETTINGCHANGE`(`ImmersiveColorSet`) 让应用与系统主题立即生效
6. 回到第 2 步循环；网络失败时 30 分钟后重试

## 已知说明

- IP 定位取决于出口 IP 所在地，使用代理/VPN 时会定位到代理服务器位置，此时请用手动坐标或地名
- 切换对支持 Windows 深浅色的应用（UWP、Explorer、大多数新应用）立即生效；部分传统 Win32 应用需重启应用才会跟随

## License

MIT
