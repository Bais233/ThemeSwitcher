@echo off
rem 用 Windows 自带的 .NET Framework 编译器编译, 无需安装任何工具
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /optimize+ /out:ThemeSwitcher.exe ThemeSwitcher.cs
if %errorlevel%==0 (echo OK: ThemeSwitcher.exe) else (echo BUILD FAILED)
