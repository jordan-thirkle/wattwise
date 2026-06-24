param()

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$binDir = Join-Path $root "src-tauri\binaries"
$srcExe = Join-Path $binDir "wattwise-sensor.exe"
$destExe = Join-Path $binDir "wattwise-sensor-x86_64-pc-windows-msvc.exe"

# Publish .NET backend
Write-Output "[1/3] Publishing .NET backend..."
dotnet publish "$root\backend" -c Release -r win-x64 --self-contained false -o $binDir
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed"; exit $LASTEXITCODE }

# Rename sidecar with target triple suffix
Write-Output "[2/3] Renaming sidecar..."
if (Test-Path $destExe) { Remove-Item -Force $destExe }
Rename-Item -Force $srcExe -NewName "wattwise-sensor-x86_64-pc-windows-msvc.exe"

# Build frontend
Write-Output "[3/3] Building Astro frontend..."
Set-Location "$root\frontend"
npm run build
if ($LASTEXITCODE -ne 0) { Write-Error "npm build failed"; exit $LASTEXITCODE }
Set-Location $root
