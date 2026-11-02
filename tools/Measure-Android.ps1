param(
    [ValidateSet('Soak', 'Stress')][string]$Mode = 'Soak',
    [string]$Serial = '127.0.0.1:5571',
    [string]$Adb = 'C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
)
$ErrorActionPreference = 'Stop'
$label = $Mode.ToLowerInvariant()
$outputDirectory = Join-Path $PSScriptRoot '..\Builds\Validation'
New-Item -ItemType Directory -Force $outputDirectory | Out-Null
if ($Mode -eq 'Stress') {
    $existingProcessId = & $Adb -s $Serial shell pidof com.zombiesurvivor.prototype
    if ($existingProcessId) {
        $existingLog = (& $Adb -s $Serial logcat -d --pid=$existingProcessId -s Unity) -join "`n"
        if ($existingLog -match 'ZS_SOAK' -and $existingLog -notmatch 'soak=True state=Won elapsed=180.00') {
            throw 'Previous real-time soak is still running; finish collecting it before starting stress'
        }
    }
}
& $Adb -s $Serial shell am force-stop com.zombiesurvivor.prototype
& $Adb -s $Serial shell am start -W -n com.zombiesurvivor.prototype/com.unity3d.player.UnityPlayerGameActivity --ez "zs_$label" true
if ($LASTEXITCODE -ne 0) { throw 'Android activity launch failed' }
$appProcessId = (& $Adb -s $Serial shell pidof com.zombiesurvivor.prototype).Trim()
if (-not $appProcessId) { throw 'Android app is not running' }
$duration = if ($Mode -eq 'Soak') { 300 } else { 180 }
$timer = [System.Diagnostics.Stopwatch]::StartNew()
while ($timer.Elapsed.TotalSeconds -lt $duration) {
    Start-Sleep -Seconds 15
    $runtimeLog = & $Adb -s $Serial logcat -d --pid=$appProcessId -s Unity
    $runtimeLog | Set-Content (Join-Path $outputDirectory "$label-unity.log")
    Write-Output "$Mode measurement: $([int]$timer.Elapsed.TotalSeconds) seconds"
    $runtimeLog | Select-String 'ZS_METRICS' | Select-Object -Last 1 | ForEach-Object { Write-Output $_.Line }
    if ($Mode -eq 'Soak' -and ($runtimeLog -join "`n") -match 'soak=True state=Won elapsed=180.00') { break }
    $fullStressWindows = @($runtimeLog | Select-String 'ZS_METRICS stress=True.*enemies=100 peakEnemies=100 samples=(?:[2-9]|[1-9][0-9]+) ')
    if ($Mode -eq 'Stress' -and $fullStressWindows.Count -ge 2) { break }
}
& $Adb -s $Serial logcat -d --pid=$appProcessId | Set-Content (Join-Path $outputDirectory "$label-full.log")
& $Adb -s $Serial shell screencap -p /sdcard/zombie-measurement.png
& $Adb -s $Serial pull /sdcard/zombie-measurement.png (Join-Path $outputDirectory "$label.png")
& $Adb -s $Serial shell dumpsys meminfo com.zombiesurvivor.prototype | Set-Content (Join-Path $outputDirectory "$label-memory.txt")
$runtimeLog = Get-Content (Join-Path $outputDirectory "$label-unity.log") -Raw
if ($Mode -eq 'Soak' -and $runtimeLog -notmatch 'soak=True state=Won elapsed=180.00') { throw 'Real-time soak did not reach victory' }
if ($Mode -eq 'Stress' -and $runtimeLog -notmatch 'enemies=100 peakEnemies=100') { throw 'Stress measurement did not reach 100 enemies' }
