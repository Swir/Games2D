# SCRAP DASH — Level 1 Demo Roadmap

Current delivery target: **Closing Time Circuit — playable Unreal 2.5D Windows demo**.

Progress counts only items backed by the required Unreal compile/runtime/build evidence.

| Completed | Remaining | Total | Progress |
|---:|---:|---:|---:|
| 0 | 16 | 16 | 0.0% |

## Acceptance checklist

- [ ] Unreal C++ project opens and compiles with the supported local Unreal Engine installation
- [ ] Player movement is constrained to a stable 2.5D gameplay plane in runtime
- [ ] Variable jump, coyote time and jump buffering are verified in runtime
- [ ] Directional dash is verified in runtime
- [ ] Side perspective camera follows the player without breaking the gameplay plane
- [ ] Keyboard and gamepad controls are both verified through Enhanced Input
- [ ] Hazard/fall death and checkpoint restart work in runtime
- [ ] Patrol enemy is present and dangerous in runtime
- [ ] Five scrap collectibles update the real objective state/HUD
- [ ] Moving cart/platform section is traversable
- [ ] Magnet Lift mechanic launches the player into the upper route
- [ ] Checkpoint updates the actual respawn position
- [ ] HUD, pause and manual restart work during play
- [ ] Exit gate requires the objective and produces a clear win state
- [ ] Full Level 1 runtime smoke test succeeds from spawn to finish
- [ ] Packaged Windows x64 demo contains a runnable `ScrapDash.exe`

## Source implementation checkpoint

The current branch may contain source implementations for many checklist items above, but **source-only code is not acceptance evidence**. Items become complete only after the Unreal editor/build/runtime validation appropriate to that item.

## NEXT after demo

Deferred until the 16/16 demo target is complete:

- final character model/animation pass;
- additional park zones;
- boss encounter;
- save/profile progression;
- richer Niagara/VFX and audio polish;
- release/installer work beyond the first demo package.
