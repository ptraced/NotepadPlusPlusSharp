@echo off
setlocal

echo.
echo ========================================
echo   Building Notepad++ # Installer
echo ========================================
echo.

set "ProjectDir=%~dp0"
set "PublishDir=%ProjectDir%bin\Release\net10.0-windows\win-x64\publish"
set "OutputDir=%ProjectDir%Output"

echo [1/4] Cleaning previous builds...
if exist "%PublishDir%" rmdir /s /q "%PublishDir%"
if exist "%OutputDir%" rmdir /s /q "%OutputDir%"
mkdir "%OutputDir%"

echo [2/4] Publishing self-contained executable...
dotnet publish "%ProjectDir%NotepadPlusPlusSharp.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false
if %errorlevel% neq 0 (
    echo ERROR: dotnet publish failed!
    pause
    exit /b 1
)

if not exist "%PublishDir%\NotepadPlusPlusSharp.exe" (
    echo ERROR: Published exe not found!
    pause
    exit /b 1
)

echo   Published successfully.

echo [3/4] Building installer with Inno Setup...
set "ISCC="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"

if "%ISCC%"=="" (
    echo ERROR: Inno Setup not found! Install from https://jrsoftware.org/isdl.php
    pause
    exit /b 1
)

"%ISCC%" "%ProjectDir%Installer\setup.iss"
if %errorlevel% neq 0 (
    echo ERROR: Inno Setup build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Build Complete!
echo ========================================

set "InstallerPath=%OutputDir%\NotepadPlusPlusSharp_Setup_1.1.0.exe"
if exist "%InstallerPath%" (
    echo   Installer: %InstallerPath%
)
echo.
pause
