<!-- SWIR-README-STANDARD:v2 -->

<div align="center">

<img width="100%" src="assets/readme/hero.svg" alt="SCRAP DASH — original Unreal Engine 2.5D platformer" />

</div>

# SCRAP DASH

**SCRAP DASH** is an original 2.5D platformer built with Unreal Engine: a small scrap-built robot wakes inside an abandoned amusement park and has to recover lost energy scrap while surviving broken rides, hostile maintenance machines and unstable magnetic systems.

Current target: **Level 1 — Closing Time Circuit** vertical slice for Windows.

## Status

| Item | State |
|---|---|
| Engine | Unreal Engine 5 project source |
| Presentation | 3D world with side-on 2.5D gameplay |
| Gameplay implementation | C++ source vertical slice in progress |
| Windows package | Not yet verified |
| Public demo | Not published |

> The repository currently contains the Unreal C++ gameplay implementation and build scripts. A real Unreal editor compile, runtime playthrough and packaged Win64 EXE are still required before this is called a playable demo.

## Level 1 target

The first vertical slice is intentionally finite:

1. Spawn into **Closing Time Circuit**.
2. Move and jump through the first park section.
3. Use dash to cross hazards.
4. Avoid a patrol maintenance enemy.
5. Recover all five scrap cores.
6. Ride the moving cart platform.
7. Use the **Magnet Lift** to reach the upper route.
8. Activate the checkpoint.
9. Reach the exit gate.
10. Finish only after all scrap has been recovered.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | A/D or arrows | Left stick / D-pad |
| Jump | Space / W / Up | Bottom face button |
| Dash | Left Shift | Right face button |
| Pause | Esc | Start/Menu |
| Restart checkpoint | R | Top face button |

Input is implemented through **Enhanced Input** in C++.

## 2.5D architecture

- Unreal world and collision are fully 3D.
- Player movement is constrained to the **X/Z gameplay plane**.
- A perspective side camera keeps the platforming silhouette readable.
- Level geometry uses real 3D collision.
- Final visuals can use materials, lighting, Niagara and parallax without turning the core movement into free-roam 3D.

## Current gameplay source

The first source vertical slice already contains code paths for:

- plane-constrained player movement;
- variable jump foundation with coyote time and jump buffering;
- directional dash;
- perspective side camera;
- keyboard and gamepad Enhanced Input;
- lethal hazard and fall recovery;
- patrol enemy;
- five scrap collectibles;
- moving platform/cart;
- Magnet Lift;
- checkpoint;
- scrap/death HUD;
- pause/restart;
- objective-gated finish screen.

These are **source-level implementations** until a real Unreal compile/runtime pass proves them.

## Run in Unreal Editor

Set `UE_ROOT` to the installed Unreal Engine directory or let the helper discover an Epic Games installation, then run:

```powershell
.\scripts\Run-Editor.ps1
```

The project intentionally avoids pinning an invented engine minor version in `.uproject`; use the supported Unreal Engine 5 installation available on the development machine.

## Build Windows demo

```powershell
.\scripts\Build-Win64.ps1
```

The script calls Unreal Automation Tool **BuildCookRun** for Win64 and only reports success when a packaged `ScrapDash.exe` actually exists.

No packaged build is committed to source control.

## Project structure

```text
games/001_scrap_dash/
├── ScrapDash.uproject
├── Config/
├── Content/
├── Source/
│   ├── ScrapDash.Target.cs
│   ├── ScrapDashEditor.Target.cs
│   └── ScrapDash/
├── assets/readme/
├── scripts/
├── README.md
└── ROADMAP.md
```

## Roadmap

See [ROADMAP.md](ROADMAP.md). Completion is based on verified Unreal gameplay/build evidence, not on file count.

## Legal / asset policy

SCRAP DASH is original SWIR game content. Do not add ripped commercial characters, art, music, code or other protected game assets. Placeholder geometry currently comes from Unreal Engine's built-in basic shapes and will be replaced by original game art during production.

## 🔎 Search Keywords

`Unreal Engine 2.5D platformer` • `Unreal C++ platformer` • `Windows 2.5D game` • `Enhanced Input platformer` • `side scrolling Unreal game` • `gamepad platformer` • `indie robot platformer` • `Unreal Win64 game` • `2.5D action platformer` • `original indie game`

<div align="center">

### `RUN • DASH • SALVAGE • ESCAPE`

[**← Games2D**](../../../README.md) · [**SWIR profile →**](https://github.com/Swir)

</div>
