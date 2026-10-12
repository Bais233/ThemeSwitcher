@echo off
rem Compile with the .NET Framework compiler built into Windows (no tools to install)
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /optimize+ /out:ThemeSwitcher.exe ThemeSwitcher.cs ThemeSwitcher.Sun.cs ThemeSwitcher.Location.cs
if %errorlevel%==0 (echo OK: ThemeSwitcher.exe) else (echo BUILD FAILED)
