param(
    [string]$EngineRoot = $env:UE_ROOT,
    [string]$Configuration = "Development",
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$ProjectFile = Join-Path $ProjectRoot "ScrapDash.uproject"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $ProjectRoot "Packaged\Windows"
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

$UAT = Join-Path $EngineRoot "Engine\Build\BatchFiles\RunUAT.bat"
if (-not (Test-Path $UAT)) {
    throw "RunUAT.bat was not found under UE_ROOT: $EngineRoot"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$Arguments = @(
    "BuildCookRun",
    "-project=$ProjectFile",
    "-noP4",
    "-platform=Win64",
    "-clientconfig=$Configuration",
    "-build",
    "-cook",
    "-stage",
    "-pak",
    "-archive",
    "-archivedirectory=$OutputDir",
    "-utf8output"
)

& $UAT @Arguments
if ($LASTEXITCODE -ne 0) {
    throw "Unreal BuildCookRun failed with exit code $LASTEXITCODE"
}

$Exe = Get-ChildItem $OutputDir -Recurse -Filter "ScrapDash.exe" -File | Select-Object -First 1
if (-not $Exe) {
    throw "BuildCookRun finished but ScrapDash.exe was not found under $OutputDir"
}

Write-Host "SCRAP DASH Win64 build: $($Exe.FullName)" -ForegroundColor Green
