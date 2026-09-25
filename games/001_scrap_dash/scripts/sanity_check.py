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
    assert "ScrapDashTests" in modules, "ScrapDash editor test module is not declared"
    assert "EnhancedInput" in plugins, "Enhanced Input plugin must be enabled"

    must_contain(
        "Source/ScrapDashEditor.Target.cs",
        '"ScrapDash"',
        '"ScrapDashTests"',
    )
    must_contain(
        "Source/ScrapDashTests/ScrapDashTests.Build.cs",
        '"ScrapDash"',
        '"UnrealEd"',
    )
    must_contain(
        "Source/ScrapDashTests/ScrapDashLevelRuntimeTests.cpp",
        "ScrapDash.Level1.RuntimeAssembly",
        "FStartPIECommand(false)",
        "GEditor->PlayWorld",
        "Player is possessed in PIE",
        "Moving platform travels during runtime",
        "Moving platform establishes a movement base",
        "Respawn detaches the player from the cart",
        "Hazard contact is lethal",
        "Patrol enemy contact is lethal",
        "Directional dash starts in runtime",
        "Dash opens its attack window",
        "Dash defeats the patrol enemy",
        "Patrol enemy moves during runtime",
        "Magnet Lift captures the player",
        "Magnet Lift applies sustained upward velocity",
        "Magnet Lift centers the player toward its route",
        "Checkpoint controls the real respawn location",
        "Five scrap collectibles spawn",
        "Moving platform section spawns",
        "Magnet Lift spawns",
        "Checkpoint spawns",
        "Finish gate spawns",
        "TryCompleteLevel",
        "Enhanced Input installs after possession",
    )
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
        "TryStartDash",
        "DashAttackUntil",
        "IsDashAttacking",
        "Gamepad_LeftX",
        "Gamepad_FaceButton_Bottom",
        "Gamepad_FaceButton_Right",
        "FullscreenAction",
        "UGameUserSettings",
        "RespawnGuard.Arm",
        "RespawnPlayer(this, false)",
        "SetBase(nullptr)",
        "CanReceiveLethalHit",
        "PossessedBy",
        "OnRep_Controller",
        "bRuntimeMappingsBuilt",
        "bInputMapInstalled = true",
        "GetCurrentLevelName(this, true)",
        "OpenLevel",
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
        "ResolvePlayerContact",
        "IsDashAttacking",
        "SetActorLocation(Location, true)",
        "RiderTrigger",
        "SetCollisionResponseToChannel(ECC_Pawn",
        "AttachRider",
        "SetBase(PlatformMesh)",
        "GetMovementBase() == PlatformMesh",
        "OnComponentEndOverlap",
        "EngagePlayer",
        "TargetHorizontalSpeed",
        "Velocity.Z = FMath::Max",
        "Closing Time Circuit",
    )
    must_contain(
        "Source/ScrapDash/ScrapDashGameMode.cpp",
        "AScrapDashLevelDirector",
        "RegisterScrap",
        "TryCompleteLevel",
        "RespawnPlayer",
        "bCountAsDeath",
        "PC->Possess(Player)",
        "Objective.IsExitReady()",
        "Objective.RegisterScrap()",
        "Objective.CollectScrap()",
    )
    must_contain(
        "Source/ScrapDash/ScrapDashRespawnTests.cpp",
        "ScrapDash.Gameplay.RespawnGuard",
        "Protected immediately after respawn",
        "Protection expires at the configured boundary",
    )
    must_contain(
        "Source/ScrapDash/ScrapDashObjectiveTests.cpp",
        "ScrapDash.Gameplay.ObjectiveProgress",
        "Five registered scraps remain required",
        "Exit becomes ready after all five scraps",
    )
    must_contain(
        "Source/ScrapDash/ScrapDashHUD.cpp",
        "OBJECTIVE  RECOVER %d MORE SCRAP",
        "OBJECTIVE  EXIT POWERED",
        "GetRemainingScrap",
        "PAUSE  Esc/Menu",
        "PRESS R / Y TO REPLAY",
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
