$path = Join-Path $PSScriptRoot "bin\Debug\net8.0-windows10.0.19041.0\win-x64\AMBATU-LAUNCH.exe"
if (Test-Path $path) {
    Start-Process $path
} else {
    Write-Host "Executable not found. Please build the project first."
}
