param([string]$EngineRoot = $env:UE_ROOT)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "ScrapDash.uproject"

if ([string]::IsNullOrWhiteSpace($EngineRoot)) {
    $EpicRoot = "C:\Program Files\Epic Games"
    if (Test-Path $EpicRoot) {
        $Candidate = Get-ChildItem $EpicRoot -Directory -Filter "UE_*" |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        if ($Candidate) { $EngineRoot = $Candidate.FullName }
    }
}

if ([string]::IsNullOrWhiteSpace($EngineRoot)) {
    throw "Unreal Engine was not found. Set UE_ROOT first."
}

$Editor = Join-Path $EngineRoot "Engine\Binaries\Win64\UnrealEditor.exe"
if (-not (Test-Path $Editor)) {
    throw "UnrealEditor.exe was not found under UE_ROOT: $EngineRoot"
}

Start-Process -FilePath $Editor -ArgumentList @($ProjectFile)
