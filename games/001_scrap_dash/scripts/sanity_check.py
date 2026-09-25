from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def require(path: str) -> Path:
    target = ROOT / path
    if not target.exists():
        raise AssertionError(f"Missing required file: {path}")
    return target


def must_contain(path: str, *needles: str) -> None:
    text = require(path).read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            raise AssertionError(f"{path} missing contract: {needle}")


def main() -> int:
    project = json.loads(require("ScrapDash.uproject").read_text(encoding="utf-8"))
    modules = {item["Name"] for item in project.get("Modules", [])}
    plugins = {item["Name"] for item in project.get("Plugins", []) if item.get("Enabled")}
    assert "ScrapDash" in modules, "ScrapDash runtime module is not declared"
    assert "EnhancedInput" in plugins, "Enhanced Input plugin must be enabled"

    must_contain(
        "Source/ScrapDash/ScrapDash.Build.cs",
        '"EnhancedInput"',
        '"Engine"',
        '"InputCore"',
    )
    must_contain(
        "Source/ScrapDash/ScrapDashCharacter.cpp",
        "SetPlaneConstraintEnabled(true)",
        "SetPlaneConstraintNormal(FVector(0.0f, 1.0f, 0.0f))",
        "UEnhancedInputLocalPlayerSubsystem",
        "JumpBufferedUntil",
        "CoyoteTime",
        "DashStarted",
        "Gamepad_LeftX",
        "Gamepad_FaceButton_Bottom",
        "Gamepad_FaceButton_Right",
        "FullscreenAction",
        "UGameUserSettings",
        "RespawnGuard.Arm",
        "RespawnPlayer(this, false)",
        "CanReceiveLethalHit",
    )
    must_contain(
        "Source/ScrapDash/ScrapDashActors.cpp",
        "AScrapCollectible",
        "AScrapHazard",
        "AScrapEnemy",
        "AScrapMovingPlatform",
        "AScrapMagnetZone",
        "AScrapCheckpoint",
        "AScrapFinishGate",
        "GetVelocity().X",
        "Closing Time Circuit",
    )
    must_contain(
        "Source/ScrapDash/ScrapDashGameMode.cpp",
        "AScrapDashLevelDirector",
        "RegisterScrap",
        "TryCompleteLevel",
        "RespawnPlayer",
        "bCountAsDeath",
    )
    must_contain(
        "Source/ScrapDash/ScrapDashRespawnTests.cpp",
        "ScrapDash.Gameplay.RespawnGuard",
        "Protected immediately after respawn",
        "Protection expires at the configured boundary",
    )
    must_contain(
        "Config/DefaultGame.ini",
        "GlobalDefaultGameMode=/Script/ScrapDash.ScrapDashGameMode",
        "GameDefaultMap=/Engine/Maps/Entry",
    )
    must_contain(
        "Config/DefaultGameUserSettings.ini",
        "FullscreenMode=1",
        "bUseVSync=True",
    )
    must_contain(
        "Config/DefaultInput.ini",
        "EnhancedInput.EnhancedPlayerInput",
        "EnhancedInput.EnhancedInputComponent",
    )
    must_contain(
        "scripts/Build-Win64.ps1",
        "$ProjectRoot = Split-Path -Parent $PSScriptRoot",
        "BuildCookRun",
        "-platform=Win64",
        "ScrapDash.exe",
    )
    must_contain(
        "scripts/Run-Editor.ps1",
        "$ProjectRoot = Split-Path -Parent $PSScriptRoot",
        "UnrealEditor.exe",
    )
    must_contain(
        "scripts/Test-Unreal.ps1",
        "$ProjectRoot = Split-Path -Parent $PSScriptRoot",
        "ScrapDashEditor",
        "Automation RunTests ScrapDash",
        "UnrealEditor-Cmd.exe",
        "ReportExportPath",
    )

    forbidden = {"Assets", "Packages", "ProjectSettings"}
    exact_child_names = {child.name for child in ROOT.iterdir() if child.is_dir()}
    for name in sorted(forbidden):
        if name in exact_child_names:
            raise AssertionError(f"Unity directory must not exist in active Unreal project: {name}")

    print("SCRAP DASH Unreal 2.5D source contracts: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
