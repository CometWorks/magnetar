@echo off
REM build.bat
REM
REM Magnetar Windows build + packaging orchestrator - the Windows counterpart
REM to build.sh (Linux).
REM
REM Builds Magnetar.sln in Release. The Legacy project's post-build step
REM (Legacy\deploy.bat) copies both launchers and their managed dependencies
REM into the folder named by the `Magnetar` MSBuild property:
REM
REM   <Magnetar>\MagnetarLegacy.exe   (.NET Framework 4.8 launcher)
REM   <Magnetar>\MagnetarInterim.exe  (.NET 10 launcher)
REM   <Magnetar>\LICENSE
REM   <Magnetar>\Libraries\MagnetarLegacy\...
REM   <Magnetar>\Libraries\MagnetarInterim\...
REM
REM We additionally publish the ConfigTerminal TUI (net10, framework-dependent -
REM requires the .NET 10 runtime, same as MagnetarInterim) into <Magnetar>\Config\
REM and drop a <Magnetar>\MagnetarConfig.bat shim next to the launchers so
REM operators can configure the instance from the install:
REM
REM   <Magnetar>\MagnetarConfig.bat   (launches Config\MagnetarConfig.exe)
REM   <Magnetar>\Config\MagnetarConfig.exe + Terminal.Gui.dll + deps
REM   <Magnetar>\README.txt           (short Windows usage note naming both
REM                                    launchers and MagnetarConfig)
REM
REM We point `Magnetar` at a staging tree under build\ (so the real
REM %APPDATA%\Magnetar install is left untouched), then 7-Zip the staged
REM Magnetar\ folder into dist\MagnetarForWindows.<date>.<hash>.7z.
REM
REM Extract that archive into %APPDATA% (Roaming) to install Magnetar:
REM the archive's top-level Magnetar\ folder lands as %APPDATA%\Magnetar.
REM Run either launcher in place of SpaceEngineersDedicated.exe.
REM
REM Unlike Linux, Windows needs no native wrappers and no Vendor\ libraries -
REM those are Linux-only. Steamworks.NET ships next to the dedicated server.
REM
REM Prerequisites (see Docs\Build.md):
REM   * .NET 10 SDK
REM   * .NET Framework 4.8 Developer Pack (for the MagnetarLegacy launcher target)
REM   * Space Engineers Dedicated Server installed (DS64 auto-detected; override
REM     with the DS64 env var or -p:DS64=... - see Directory.Build.props)
REM   * 7z.exe on PATH
REM
REM Usage:
REM   build.bat            Build the solution and package the .7z.
REM   build.bat --clean    dotnet clean first, then build and package.

setlocal enabledelayedexpansion

REM ---- locate self / repo ----------------------------------------------------
set "REPO_DIR=%~dp0"
if "%REPO_DIR:~-1%"=="\" set "REPO_DIR=%REPO_DIR:~0,-1%"

set "SOLUTION=%REPO_DIR%\Magnetar.sln"
set "BUILD_DIR=%REPO_DIR%\build"
set "STAGE_DIR=%BUILD_DIR%\MagnetarForWindows"
set "MAGNETAR_STAGE=%STAGE_DIR%\Magnetar"
set "DIST_DIR=%REPO_DIR%\dist"

REM ---- parse arguments -------------------------------------------------------
set "DO_CLEAN=0"
:argloop
if "%~1"=="" goto argsdone
if /i "%~1"=="--clean" (
    set "DO_CLEAN=1"
    shift
    goto argloop
)
echo ERROR: unknown argument: %~1 1>&2
echo Usage: build.bat [--clean] 1>&2
exit /b 2
:argsdone

REM ---- require 7z on PATH ----------------------------------------------------
REM Bail out now, before the build, if 7z.exe is missing - packaging needs it.
where 7z.exe >NUL 2>&1
if errorlevel 1 (
    echo ERROR: 7z.exe was not found on PATH. 1>&2
    echo        Install 7-Zip and ensure 7z.exe is on your PATH, then re-run. 1>&2
    exit /b 1
)

REM ---- clean -----------------------------------------------------------------
if "%DO_CLEAN%"=="1" (
    echo ==^> dotnet clean
    dotnet clean "%SOLUTION%" -c Release -v quiet
)

REM Always start from a fresh staging tree so stale files can never leak in.
if exist "%STAGE_DIR%" rmdir /s /q "%STAGE_DIR%"
mkdir "%MAGNETAR_STAGE%" >NUL 2>&1
if not exist "%DIST_DIR%" mkdir "%DIST_DIR%" >NUL 2>&1

REM ---- build + deploy into the staging tree ----------------------------------
REM Overriding the `Magnetar` property redirects deploy.bat (Legacy's PostBuild
REM event) from %APPDATA%\Magnetar into our staging tree. Both inner builds
REM (net48 and net10.0) deploy into the same folder.
echo.
echo ############################################################
echo # build: Magnetar.sln (Release)
echo #   deploy target: %MAGNETAR_STAGE%
echo ############################################################
dotnet build "%SOLUTION%" -c Release -p:Magnetar="%MAGNETAR_STAGE%"
if errorlevel 1 (
    echo ERROR: build failed. 1>&2
    exit /b 1
)

REM ---- stage the ConfigTerminal TUI (MagnetarConfig) --------------------------
REM Ships the terminal configuration UI next to the launchers. Published
REM framework-dependent for net10, so it needs the .NET 10 runtime (same as
REM MagnetarInterim). Its managed deps (Terminal.Gui, NStack, ...) live alongside
REM the exe in Config\, and a root MagnetarConfig.bat shim next to
REM MagnetarInterim.exe launches it.
echo.
echo ############################################################
echo # publish: ConfigTerminal / MagnetarConfig (net10.0)
echo ############################################################
dotnet publish "%REPO_DIR%\ConfigTerminal\ConfigTerminal.csproj" -c Release -f net10.0 -p:DebugType=None -p:DebugSymbols=false -o "%MAGNETAR_STAGE%\Config"
if errorlevel 1 (
    echo ERROR: ConfigTerminal publish failed. 1>&2
    exit /b 1
)

REM Root shim so MagnetarConfig launches from beside MagnetarInterim.exe.
> "%MAGNETAR_STAGE%\MagnetarConfig.bat" echo @echo off
>>"%MAGNETAR_STAGE%\MagnetarConfig.bat" echo "%%~dp0Config\MagnetarConfig.exe" %%*

REM ---- generate Magnetar\README.txt ------------------------------------------
REM Ships a short usage note inside the archive (extracts to
REM %APPDATA%\Magnetar\README.txt). Windows counterpart of the Linux bundle's
REM README; names both launchers and MagnetarConfig so operators find the
REM config tool. %%APPDATA%% is written as a literal (not expanded at build time).
set "README=%MAGNETAR_STAGE%\README.txt"
> "%README%" echo MagnetarForWindows
>>"%README%" echo ==================
>>"%README%" echo.
>>"%README%" echo Magnetar is a plugin and mod loader for the Space Engineers Dedicated
>>"%README%" echo Server on Windows. This archive ships two launchers - MagnetarLegacy.exe
>>"%README%" echo (.NET Framework 4.8) and MagnetarInterim.exe (.NET 10) - together with
>>"%README%" echo MagnetarConfig, a terminal UI to configure and operate the server.
>>"%README%" echo.
>>"%README%" echo Prerequisites
>>"%README%" echo -------------
>>"%README%" echo - Space Engineers Dedicated Server installed (via Steam or steamcmd).
>>"%README%" echo - MagnetarInterim.exe and MagnetarConfig need the .NET 10 runtime
>>"%README%" echo   (Microsoft.NETCore.App 10.x); MagnetarLegacy.exe uses .NET Framework 4.8.
>>"%README%" echo.
>>"%README%" echo Quick start
>>"%README%" echo -----------
>>"%README%" echo 1. Extract this archive into %%APPDATA%% (Roaming) so its top-level
>>"%README%" echo    Magnetar\ folder lands as %%APPDATA%%\Magnetar.
>>"%README%" echo 2. Launch the dedicated server through Magnetar in place of
>>"%README%" echo    SpaceEngineersDedicated.exe:
>>"%README%" echo        %%APPDATA%%\Magnetar\MagnetarInterim.exe
>>"%README%" echo    (or MagnetarLegacy.exe for the .NET Framework 4.8 build).
>>"%README%" echo 3. Configure and operate the instance from the terminal UI:
>>"%README%" echo        %%APPDATA%%\Magnetar\MagnetarConfig.bat
>>"%README%" echo    (edit DS/world settings, plugins, mods, profiles; start/stop; read
>>"%README%" echo     logs. Add -diag for a headless status report, or -help for options.)
>>"%README%" echo.
>>"%README%" echo Files
>>"%README%" echo -----
>>"%README%" echo   MagnetarLegacy.exe    Server launcher (.NET Framework 4.8).
>>"%README%" echo   MagnetarInterim.exe   Server launcher (.NET 10).
>>"%README%" echo   MagnetarConfig.bat    Launches Config\MagnetarConfig.exe (terminal UI).
>>"%README%" echo   Config\               MagnetarConfig.exe + Terminal.Gui + managed deps.
>>"%README%" echo   Libraries\            Per-launcher managed dependencies.
>>"%README%" echo   LICENSE               License text.

REM ---- verify the staged tree ------------------------------------------------
set "MISSING=0"
for %%F in (MagnetarLegacy.exe MagnetarInterim.exe MagnetarInterim.dll LICENSE) do (
    if not exist "%MAGNETAR_STAGE%\%%F" (
        echo MISSING: %MAGNETAR_STAGE%\%%F 1>&2
        set "MISSING=1"
    )
)
if not exist "%MAGNETAR_STAGE%\Libraries\MagnetarLegacy\" (
    echo MISSING: %MAGNETAR_STAGE%\Libraries\MagnetarLegacy 1>&2
    set "MISSING=1"
)
if not exist "%MAGNETAR_STAGE%\Libraries\MagnetarInterim\" (
    echo MISSING: %MAGNETAR_STAGE%\Libraries\MagnetarInterim 1>&2
    set "MISSING=1"
)
for %%F in ("Config\MagnetarConfig.exe" "Config\Terminal.Gui.dll" "MagnetarConfig.bat") do (
    if not exist "%MAGNETAR_STAGE%\%%~F" (
        echo MISSING: %MAGNETAR_STAGE%\%%~F 1>&2
        set "MISSING=1"
    )
)
if "%MISSING%"=="1" (
    echo ERROR: staged tree is incomplete - did deploy.bat run? 1>&2
    exit /b 1
)

REM ---- archive name ----------------------------------------------------------
set "ARCHIVE_NAME=MagnetarForWindows.7z"
set "ARCHIVE_PATH=%DIST_DIR%\%ARCHIVE_NAME%"

if exist "%ARCHIVE_PATH%" del /f /q "%ARCHIVE_PATH%"

REM ---- pack ------------------------------------------------------------------
REM Pack the staged Magnetar\ folder so the archive root is Magnetar\..., which
REM extracts straight into %APPDATA% as %APPDATA%\Magnetar.
echo.
echo ############################################################
echo # package: %ARCHIVE_NAME%
echo ############################################################
pushd "%STAGE_DIR%"
7z.exe a -t7z -mx=9 -bso0 -bsp1 "%ARCHIVE_PATH%" "Magnetar"
if errorlevel 1 (
    popd
    echo ERROR: 7-Zip packaging failed. 1>&2
    exit /b 1
)
popd

echo.
echo Done: %ARCHIVE_PATH%
for %%I in ("%ARCHIVE_PATH%") do echo Size: %%~zI bytes
exit /b 0
