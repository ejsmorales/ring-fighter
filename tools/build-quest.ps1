<#
.SYNOPSIS
  Pull, build and install Ring Fighter on the Quest without opening the Unity window.

.DESCRIPTION
  1. git pull (skip with -NoPull)
  2. Runs Unity in the background (batch mode): rebuilds the training scene and builds Builds\RingFighter.apk
  3. Installs it on the Quest and starts it (USB cable if plugged in, otherwise the remembered wireless address)
  Close the Unity editor first: Unity can't build a project that is open in another window.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\build-quest.ps1
  powershell -ExecutionPolicy Bypass -File tools\build-quest.ps1 -NoPull -NoInstall
#>
param(
    [switch]$NoPull,      # don't run git pull first
    [switch]$NoInstall,   # build only, don't install on the headset
    [switch]$SkipScene,   # don't rebuild the training scene (faster, but keeps old scene defaults)
    [switch]$Wireless     # switch the headset to wireless instead of using the cable
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# Unity refuses to open a project that is already open, so check first.
$lock = Join-Path $root 'Temp\UnityLockfile'
if ((Get-Process -Name Unity -ErrorAction SilentlyContinue) -and (Test-Path $lock)) {
    Write-Host 'Unity seems to have this project open. Close the Unity editor window and run this again.' -ForegroundColor Red
    exit 1
}

if (-not $NoPull) {
    Write-Host '== git pull' -ForegroundColor Cyan
    git pull
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'git pull failed. Fix that first (paste the message to Claude if unsure).' -ForegroundColor Red
        exit 1
    }
}

# Find the Unity version this project uses.
$versionLine = Get-Content (Join-Path $root 'ProjectSettings\ProjectVersion.txt') | Where-Object { $_ -match '^m_EditorVersion:' }
$version = ($versionLine -split ':\s*')[1].Trim()
$unity = $null
foreach ($base in @("$env:ProgramFiles\Unity\Hub\Editor", "${env:ProgramFiles(x86)}\Unity\Hub\Editor")) {
    $candidate = Join-Path $base "$version\Editor\Unity.exe"
    if (Test-Path $candidate) { $unity = $candidate; break }
}
if (-not $unity) {
    Write-Host "Unity $version not found under Program Files\Unity\Hub\Editor. Install it from Unity Hub." -ForegroundColor Red
    exit 1
}

$buildDir = Join-Path $root 'Builds'
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
$log = Join-Path $buildDir 'unity-build.log'
$apk = Join-Path $buildDir 'RingFighter.apk'
if (Test-Path $apk) { Remove-Item $apk }

$unityArgs = "-batchmode -projectPath `"$root`" -buildTarget Android " +
             "-executeMethod RingFighter.EditorTools.CommandLineBuild.BuildQuest -logFile `"$log`""
if ($SkipScene) { $unityArgs += ' -skipScene' }

Write-Host "== Building with Unity $version (no window). This takes a few minutes; log: Builds\unity-build.log" -ForegroundColor Cyan
$start = Get-Date
$proc = Start-Process -FilePath $unity -ArgumentList $unityArgs -PassThru -WindowStyle Hidden
while (-not $proc.HasExited) {
    Start-Sleep -Seconds 10
    $elapsed = [int]((Get-Date) - $start).TotalSeconds
    $last = $null
    if (Test-Path $log) {
        $last = Get-Content $log -Tail 200 | Where-Object { $_ -match 'RingFighterBuild:|Compiling|Building|Importing' } | Select-Object -Last 1
    }
    if ($last) { $last = $last.Trim(); if ($last.Length -gt 90) { $last = $last.Substring(0, 90) } }
    Write-Host ("  {0,4}s  {1}" -f $elapsed, $last)
}

if ($proc.ExitCode -ne 0 -or -not (Test-Path $apk)) {
    Write-Host "Build FAILED (Unity exit code $($proc.ExitCode)). Errors from the log:" -ForegroundColor Red
    if (Test-Path $log) {
        Get-Content $log | Where-Object { $_ -match 'error|RingFighterBuild|Exception' } | Select-Object -Last 25
    }
    Write-Host 'Paste these lines to Claude. Full log: Builds\unity-build.log'
    exit 1
}
Write-Host ("Build OK in {0} s: Builds\RingFighter.apk" -f [int]((Get-Date) - $start).TotalSeconds) -ForegroundColor Green

if ($NoInstall) { exit 0 }

Write-Host '== Installing on the Quest' -ForegroundColor Cyan
$installArgs = @{ Apk = $apk; Launch = $true }
if (-not $Wireless) { $installArgs.Usb = $true }
& (Join-Path $PSScriptRoot 'quest-wireless.ps1') @installArgs
exit $LASTEXITCODE
