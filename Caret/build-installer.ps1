Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Building Caret Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "[1/4] Cleaning previous builds..." -ForegroundColor Yellow
$publishDir = Join-Path $ProjectDir "bin\Release\net10.0-windows\win-x64\publish"
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
$outputDir = Join-Path $ProjectDir "Output"
if (Test-Path $outputDir) {
    Remove-Item -Recurse -Force $outputDir
}
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

Write-Host "[2/4] Publishing self-contained executable..." -ForegroundColor Yellow
Push-Location $ProjectDir
dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet publish failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

$exePath = Join-Path $publishDir "Caret.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: Published exe not found at $exePath" -ForegroundColor Red
    exit 1
}

$exeSize = [math]::Round((Get-Item $exePath).Length / 1MB, 1)
Write-Host "  Published: $exePath ($exeSize MB)" -ForegroundColor Green

Write-Host "[3/4] Building installer with Inno Setup..." -ForegroundColor Yellow
$isccPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$iscc = $null
foreach ($p in $isccPaths) {
    if (Test-Path $p) {
        $iscc = $p
        break
    }
}

if (-not $iscc) {
    Write-Host "ERROR: Inno Setup not found! Install from https://jrsoftware.org/isdl.php" -ForegroundColor Red
    exit 1
}

$issFile = Join-Path $ProjectDir "Installer\setup.iss"
& $iscc $issFile
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Inno Setup build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
$installerPath = Join-Path $outputDir "Caret_Setup_1.1.1.exe"
if (Test-Path $installerPath) {
    $installerSize = [math]::Round((Get-Item $installerPath).Length / 1MB, 1)
    Write-Host "  Installer: $installerPath" -ForegroundColor White
    Write-Host "  Size: $installerSize MB" -ForegroundColor White
} else {
    Write-Host "  Check the Output folder for the installer." -ForegroundColor White
}
Write-Host ""
