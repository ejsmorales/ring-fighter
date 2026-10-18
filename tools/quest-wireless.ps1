<#
.SYNOPSIS
  Switches a USB-connected Quest to wireless ADB and connects to it. Optionally installs an APK.

.DESCRIPTION
  Run this with the Quest plugged in by USB (and on the same Wi-Fi as this PC). Afterwards you can unplug
  the cable and use Unity's Build And Run, or `-Apk` below, over Wi-Fi. Safe to run again at any time.
  Needed again after the headset restarts or its IP address changes.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\quest-wireless.ps1
  powershell -ExecutionPolicy Bypass -File tools\quest-wireless.ps1 -Apk C:\Builds\RingFighter.apk
#>
param(
    [string]$Apk,
    [int]$Port = 5555
)

$ErrorActionPreference = 'Stop'

function Find-Adb {
    $cmd = Get-Command adb -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $candidates = @()
    $tail = 'Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
    foreach ($root in @("$env:ProgramFiles\Unity\Hub\Editor", "${env:ProgramFiles(x86)}\Unity\Hub\Editor")) {
        if ($root -and (Test-Path $root)) {
            $candidates += Get-ChildItem -Path (Join-Path $root "*\$tail") -ErrorAction SilentlyContinue
        }
    }
    foreach ($sdk in @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT)) {
        if ($sdk) {
            $p = Join-Path $sdk 'platform-tools\adb.exe'
            if (Test-Path $p) { $candidates += Get-Item $p }
        }
    }
    if ($candidates.Count -eq 0) { return $null }
    return ($candidates | Sort-Object FullName | Select-Object -Last 1).FullName
}

function Get-Devices($adb) {
    $lines = & $adb devices | Select-Object -Skip 1 | Where-Object { $_ -match '\S' }
    $result = @()
    foreach ($l in $lines) {
        $parts = $l -split '\s+'
        $result += [pscustomobject]@{ Serial = $parts[0]; State = $parts[1] }
    }
    return $result
}

$adb = Find-Adb
if (-not $adb) {
    Write-Host 'Could not find adb.exe. Install the Android Build Support module for Unity (Unity Hub > Installs > gear > Add modules).' -ForegroundColor Red
    exit 1
}
Write-Host "Using adb: $adb"
& $adb start-server | Out-Null

$devices = Get-Devices $adb
$unauthorized = $devices | Where-Object { $_.State -eq 'unauthorized' }
if ($unauthorized) {
    Write-Host 'The headset is asking for permission. Put it on, tick "Always allow" and accept, then run this again.' -ForegroundColor Yellow
    exit 1
}

$usb = $devices | Where-Object { $_.State -eq 'device' -and $_.Serial -notmatch ':' } | Select-Object -First 1
$wifi = $devices | Where-Object { $_.State -eq 'device' -and $_.Serial -match ':' } | Select-Object -First 1
$target = $null

if ($usb) {
    Write-Host "Quest found over USB ($($usb.Serial))."
    $ipOut = (& $adb -s $usb.Serial shell ip -f inet addr show wlan0) -join "`n"
    if ($ipOut -match 'inet (\d+\.\d+\.\d+\.\d+)/') {
        $ip = $Matches[1]
    } else {
        Write-Host 'Could not read the headset Wi-Fi address. Is the headset connected to Wi-Fi?' -ForegroundColor Red
        exit 1
    }
    Write-Host "Headset Wi-Fi address: $ip"
    & $adb -s $usb.Serial tcpip $Port | Out-Null
    Start-Sleep -Seconds 2
    $target = "${ip}:$Port"
    $out = & $adb connect $target
    Write-Host $out
    if ($out -notmatch 'connected') {
        Write-Host 'Wireless connection failed. Check the PC and headset are on the same Wi-Fi network.' -ForegroundColor Red
        exit 1
    }
    Write-Host 'Wireless ADB is ready. You can unplug the cable now.' -ForegroundColor Green
}
elseif ($wifi) {
    $target = $wifi.Serial
    Write-Host "Already connected wirelessly ($target)." -ForegroundColor Green
}
else {
    Write-Host 'No Quest found. Plug it in with the USB cable, put the headset on and allow USB debugging, then run this again.' -ForegroundColor Red
    Write-Host '(If it was connected wirelessly before, the IP may have changed or the headset restarted. The cable step fixes that.)'
    exit 1
}

if ($Apk) {
    if (-not (Test-Path $Apk)) {
        Write-Host "APK not found: $Apk" -ForegroundColor Red
        exit 1
    }
    Write-Host "Installing $Apk ..."
    & $adb -s $target install -r $Apk
}
