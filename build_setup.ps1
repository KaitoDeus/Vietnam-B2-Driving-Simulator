# Script to compile the Inno Setup installer for Vietnam B2 Driving Simulator

$InnoPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$ScriptPath = Join-Path $PSScriptRoot "setup.iss"
$BuildPath = Join-Path $PSScriptRoot "Builds\v0.0.1\Windows"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Vietnam B2 Driving Simulator Setup Builder" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Check if Unity build exists
if (-not (Test-Path (Join-Path $BuildPath "Vietnam B2 Driving Simulator.exe"))) {
    Write-Host "[Error] Unity build not found at: $BuildPath" -ForegroundColor Red
    Write-Host "Please build the project inside Unity first by clicking:" -ForegroundColor Yellow
    Write-Host "  -> 'Build' menu at the top -> 'Build Windows Game (v0.0.1)'" -ForegroundColor Yellow
    Exit 1
}

# 2. Check if Inno Setup is installed
if (-not (Test-Path $InnoPath)) {
    Write-Host "[Error] Inno Setup 6 compiler not found at: $InnoPath" -ForegroundColor Red
    Write-Host "Please download and install Inno Setup 6 from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Exit 1
}

# 3. Run Inno Setup Compiler
Write-Host "Compiling setup installer..." -ForegroundColor Green
& $InnoPath $ScriptPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSetup installer generated successfully in: Builds\v0.0.1\Installer!" -ForegroundColor Green
    $InstallerFolder = Join-Path $PSScriptRoot "Builds\v0.0.1\Installer"
    # Open the installer folder in File Explorer
    explorer.exe $InstallerFolder
} else {
    Write-Host "`n[Error] Failed to compile setup installer." -ForegroundColor Red
    Exit 1
}
