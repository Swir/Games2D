<!-- SWIR-README-STANDARD:v2 -->

# Games2D

**Games2D** is the SWIR monorepo for original game projects. Each game lives in its own self-contained project under `games/` and can use the engine that best fits that title.

## Games

| ID | Game | Engine | Style | Status | Project |
|---:|---|---|---|---|---|
| 001 | **SCRAP DASH** | Unreal Engine | 2.5D platformer | Unreal migration / Level 1 source vertical slice | `games/001_scrap_dash/` |

## Repository layout

```text
games/
  001_scrap_dash/
    ScrapDash.uproject
    Config/
    Content/
    Source/
    scripts/
    README.md
    ROADMAP.md
```

Each game owns its gameplay code, content, tests, roadmap and build path. Engine-generated caches/build folders are never shared between games.

## Current focus — SCRAP DASH

SCRAP DASH is an original 2.5D platformer set in a dead amusement park. The world is rendered in 3D, while the core platforming movement is constrained to a side-on gameplay plane.

The immediate target is a finite **Closing Time Circuit** vertical slice followed by a real packaged Windows x64 demo.

## Rules

- Original games and legally usable assets only.
- Unreal-generated `Binaries/`, `DerivedDataCache/`, `Intermediate/`, `Saved/` and packaged builds are not committed.
- Build/runtime claims require real engine verification.
- Larger changes use feature branches/PRs; source-only scaffolding is not called a playable demo.
- Each new game gets its own numbered directory.

## 🔎 Search Keywords

`Unreal Engine 2.5D game` • `Unreal C++ platformer` • `Windows indie games` • `2.5D platformer source` • `Enhanced Input platformer` • `gamepad platformer` • `Unreal Win64 game` • `original indie game`

<div align="center">

### `BUILD • PLAY • ITERATE • RELEASE`

[**← SWIR profile**](https://github.com/Swir) · [**SCRAP DASH →**](games/001_scrap_dash/README.md)

</div>
