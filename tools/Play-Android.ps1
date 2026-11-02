param(
    [string]$Serial = 'emulator-5570',
    [string]$Adb = 'C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe',
    [int]$Cycles = 12,
    [switch]$Continuous
)
$ErrorActionPreference = 'Stop'
$outputDirectory = Join-Path $PSScriptRoot '..\Builds\Validation'
New-Item -ItemType Directory -Force $outputDirectory | Out-Null
$appProcessId = (& $Adb -s $Serial shell pidof com.zombiesurvivor.prototype).Trim()
if (-not $appProcessId) { throw 'Launch the prototype and start a game before running this script' }
# Coordinates target the validated 1080x2400 emulator; this does not configure a real phone.
if ($Continuous) {
    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    & $Adb -s $Serial shell input motionevent DOWN 300 1800
    try {
        Start-Sleep -Milliseconds 120
        & $Adb -s $Serial shell input motionevent MOVE 60 1800
        Start-Sleep -Milliseconds 2800
        $step = 0
        while ($timer.Elapsed.TotalSeconds -lt 190) {
            $stepStarted = $timer.Elapsed.TotalMilliseconds
            $angle = -[Math]::PI / 2 + ($step % 16) * [Math]::PI / 8
            $touchX = [int](300 + 240 * [Math]::Cos($angle))
            $touchY = [int](1800 + 240 * [Math]::Sin($angle))
            & $Adb -s $Serial shell input motionevent MOVE $touchX $touchY
            if ($LASTEXITCODE -ne 0) { throw 'ADB motion event failed' }
            $remainingDelay = [int](1100 - ($timer.Elapsed.TotalMilliseconds - $stepStarted))
            if ($remainingDelay -gt 0) { Start-Sleep -Milliseconds $remainingDelay }
            $step++
            if ($step % 32 -eq 0) {
                & $Adb -s $Serial shell screencap -p /sdcard/zombie-validation.png
                & $Adb -s $Serial pull /sdcard/zombie-validation.png (Join-Path $outputDirectory "continuous-$step.png")
                Write-Output "Continuous input: $([int]$timer.Elapsed.TotalSeconds) seconds"
            }
        }
    } finally { & $Adb -s $Serial shell input motionevent UP 300 1800 }
} else {
  $directions = @(@(540, 1800), @(300, 1560), @(60, 1800), @(300, 2040))
  for ($cycle = 0; $cycle -lt $Cycles; $cycle++) {
    foreach ($direction in $directions) {
        & $Adb -s $Serial shell input swipe 300 1800 $direction[0] $direction[1] 4000
        if ($LASTEXITCODE -ne 0) { throw 'ADB gesture failed' }
    }
    & $Adb -s $Serial shell screencap -p /sdcard/zombie-validation.png
    & $Adb -s $Serial pull /sdcard/zombie-validation.png (Join-Path $outputDirectory "play-$cycle.png")
    Write-Output "Completed input cycle $cycle"
  }
}
& $Adb -s $Serial logcat -d --pid=$appProcessId -s Unity | Set-Content (Join-Path $outputDirectory 'android-unity.log')
& $Adb -s $Serial shell dumpsys meminfo com.zombiesurvivor.prototype | Set-Content (Join-Path $outputDirectory 'android-memory.txt')
