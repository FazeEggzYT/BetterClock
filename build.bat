@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ==============================================
echo   Better Clock - build
echo ==============================================

rem --- read version ---
set "VERSION=1.0.0"
if exist "version.txt" set /p VERSION=<"version.txt"
echo Version: %VERSION%

rem --- GitHub repository (for the update check). Edit repo.txt once: yourname/BetterClock ---
set "REPO="
if exist "repo.txt" set /p REPO=<"repo.txt"

rem --- find the C# compiler that ships with Windows ---
set "FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
if not exist "%FW%\csc.exe" set "FW=%WINDIR%\Microsoft.NET\Framework\v4.0.30319"
if not exist "%FW%\csc.exe" (
  echo.
  echo ERROR: Could not find csc.exe in the .NET Framework folder.
  echo Looked in: %FW%
  goto :fail
)
echo Compiler: %FW%\csc.exe

set "WPF=%FW%\WPF"
if not exist "%WPF%\PresentationFramework.dll" (
  echo.
  echo ERROR: WPF libraries not found in %WPF%
  goto :fail
)

rem --- generate version info ---
set "ASMVER=%VERSION%.0"
for /f "tokens=1-4 delims=." %%a in ("%VERSION%") do if not "%%d"=="" set "ASMVER=%VERSION%"
if not exist "obj" mkdir "obj"
if not exist "dist" mkdir "dist"
(
  echo using System.Reflection;
  echo [assembly: AssemblyVersion^("%ASMVER%"^)]
  echo [assembly: AssemblyFileVersion^("%ASMVER%"^)]
  echo [assembly: AssemblyTitle^("Better Clock"^)]
  echo [assembly: AssemblyProduct^("Better Clock"^)]
  echo namespace BetterClock { public static partial class AppInfo { public const string Version = "%VERSION%"; public const string Repo = "%REPO%"; } }
) > "obj\Version.g.cs"

rem --- optional icon ---
set "ICON="
if exist "icon.ico" set "ICON=/win32icon:icon.ico"

echo.
echo Compiling...
"%FW%\csc.exe" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
  /out:"dist\BetterClock.exe" %ICON% ^
  /r:"%WPF%\PresentationFramework.dll" ^
  /r:"%WPF%\PresentationCore.dll" ^
  /r:"%WPF%\WindowsBase.dll" ^
  /r:"%FW%\System.Xaml.dll" ^
  /r:"%FW%\System.Windows.Forms.dll" ^
  /r:"%FW%\System.Drawing.dll" ^
  /recurse:"*.cs"

if errorlevel 1 (
  echo.
  echo BUILD FAILED. Copy the error lines above and send them to Claude.
  goto :fail
)

echo.
echo ==============================================
echo   BUILD OK:  dist\BetterClock.exe  (v%VERSION%)
echo ==============================================
echo.
if defined CI goto :finished
set /p RUN=Run it now? (y/n): 
if /i "%RUN%"=="y" start "" "dist\BetterClock.exe"
:finished
endlocal
exit /b 0

:fail
echo.
if not defined CI pause
endlocal
exit /b 1
