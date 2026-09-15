param(
    [string]$Avd = 'Medium_Phone',
    [int]$Port = 5580,
    [int]$BootTimeoutSeconds = 300,
    [string]$EmulatorPath = "$env:LOCALAPPDATA/Android/Sdk/emulator/emulator.exe",
    [string]$AdbPath = 'C:/Program Files/Unity/Hub/Editor/6000.3.2f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe'
)
$ErrorActionPreference = 'Stop'
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Finish Unity batch work before starting emulator validation.' }
if ($Port -lt 5554 -or $Port -gt 5682 -or $Port % 2) { throw 'Use an even emulator port from 5554 through 5682.' }
if (!(Test-Path -LiteralPath $EmulatorPath) -or !(Test-Path -LiteralPath $AdbPath)) { throw 'Android emulator or adb is missing.' }
$serial = "emulator-$Port"
$devices = & $AdbPath devices
if ($devices -match [regex]::Escape($serial)) { throw "An emulator already uses $serial; leave it untouched and choose another port." }
$repoRoot = Split-Path $PSScriptRoot -Parent
$outputDirectory = Join-Path $repoRoot "artifacts/device-$Port"
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$arguments = @('-avd', $Avd, '-port', "$Port", '-read-only', '-no-snapshot', '-no-window', '-no-audio', '-no-boot-anim', '-gpu', 'software', '-cores', '2')
$process = Start-Process -FilePath $EmulatorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput (Join-Path $outputDirectory 'emulator.log') `
    -RedirectStandardError (Join-Path $outputDirectory 'emulator-errors.log')
$timer = [Diagnostics.Stopwatch]::StartNew()
Write-Output "Starting isolated $serial with software rendering. Logs: $outputDirectory"
try {
    while ($timer.Elapsed.TotalSeconds -lt $BootTimeoutSeconds) {
        if ($process.HasExited) { throw "Emulator exited with code $($process.ExitCode)." }
        $connected = (& $AdbPath devices) -match "^$serial\s+device$"
        if ($connected) {
            $boot = & $AdbPath -s $serial shell getprop sys.boot_completed
            if ($LASTEXITCODE -eq 0 -and "$boot".Trim() -eq '1') {
                Write-Output "Android boot complete: $serial (process $($process.Id)). Install the APK, then stop with adb -s $serial emu kill."
                return
            }
        }
        Start-Sleep -Seconds 2
    }
    & $AdbPath -s $serial logcat -d -b crash 2>$null | Set-Content (Join-Path $outputDirectory 'boot-crash.log')
    throw "Android did not finish booting within $BootTimeoutSeconds seconds."
} catch {
    # Only terminate the process tree this invocation owns. The AVD was opened read-only.
    if (!$process.HasExited) { $process.Kill($true) }
    throw
}
