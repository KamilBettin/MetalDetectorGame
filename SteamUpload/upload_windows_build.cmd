@echo off
setlocal

set "PROJECT_ROOT=C:\Users\Kamil\MetalDetectorGame"
set "STEAMCMD=%PROJECT_ROOT%\SteamUpload\builder\steamcmd.exe"
set "APP_SCRIPT=%PROJECT_ROOT%\SteamUpload\scripts\app_build_4916780.vdf"
set "GAME_EXE=%PROJECT_ROOT%\Builds\MetalDetectorGame.exe"

if not exist "%STEAMCMD%" (
    echo ERROR: SteamCMD is missing.
    pause
    exit /b 1
)

if not exist "%GAME_EXE%" (
    echo ERROR: The Windows build is missing: %GAME_EXE%
    pause
    exit /b 1
)

echo App ID: 4916780
echo Depot ID: 4916781
echo Build: %GAME_EXE%
echo.
set /p "STEAM_LOGIN=Enter your Steam login name: "

if "%STEAM_LOGIN%"=="" (
    echo ERROR: Steam login cannot be empty.
    pause
    exit /b 1
)

echo.
echo SteamCMD may now ask for your password and Steam Guard code.
echo Nothing entered here is saved in the project.
echo.
"%STEAMCMD%" +login "%STEAM_LOGIN%" +run_app_build "%APP_SCRIPT%" +quit

echo.
echo Upload process finished. Check the result above before closing this window.
pause
