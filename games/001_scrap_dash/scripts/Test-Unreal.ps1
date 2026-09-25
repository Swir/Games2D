param(
    [string]$EngineRoot = $env:UE_ROOT,
    [string]$ReportDir
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "ScrapDash.uproject"

if ([string]::IsNullOrWhiteSpace($ReportDir)) {
    $ReportDir = Join-Path $ProjectRoot "Saved\AutomationReports"
}

if ([string]::IsNullOrWhiteSpace($EngineRoot)) {
    $EpicRoot = "C:\Program Files\Epic Games"
    if (Test-Path $EpicRoot) {
        $Candidate = Get-ChildItem $EpicRoot -Directory -Filter "UE_*" |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        if ($Candidate) {
            $EngineRoot = $Candidate.FullName
        }
    }
}

if ([string]::IsNullOrWhiteSpace($EngineRoot)) {
    throw "Unreal Engine was not found. Set UE_ROOT to your installed Unreal Engine directory."
}

$Build = Join-Path $EngineRoot "Engine\Build\BatchFiles\Build.bat"
$EditorCmd = Join-Path $EngineRoot "Engine\Binaries\Win64\UnrealEditor-Cmd.exe"
if (-not (Test-Path $Build)) {
    throw "Build.bat was not found under UE_ROOT: $EngineRoot"
}
if (-not (Test-Path $EditorCmd)) {
    throw "UnrealEditor-Cmd.exe was not found under UE_ROOT: $EngineRoot"
}

& $Build "ScrapDashEditor" "Win64" "Development" "-Project=$ProjectFile" "-WaitMutex" "-FromMsBuild"
if ($LASTEXITCODE -ne 0) {
    throw "ScrapDashEditor compile failed with exit code $LASTEXITCODE"
}

Remove-Item $ReportDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $ReportDir | Out-Null

$Arguments = @(
    $ProjectFile,
    "-unattended",
    "-nop4",
    "-nosplash",
    "-NullRHI",
    "-stdout",
    "-FullStdOutLogOutput",
    "-ExecCmds=Automation RunTests ScrapDash; Quit",
    "-TestExit=Automation Test Queue Empty",
    "-ReportExportPath=$ReportDir"
)

& $EditorCmd @Arguments
if ($LASTEXITCODE -ne 0) {
    throw "Unreal automation tests failed with exit code $LASTEXITCODE"
}

$ReportFiles = Get-ChildItem $ReportDir -Recurse -File -ErrorAction SilentlyContinue
if (-not $ReportFiles) {
    throw "Unreal automation finished without producing a report under $ReportDir"
}

Write-Host "SCRAP DASH Unreal automation: PASS ($ReportDir)" -ForegroundColor Green
