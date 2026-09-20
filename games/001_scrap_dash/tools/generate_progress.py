#!/usr/bin/env python3
"""Generate SCRAP DASH progress SVGs from the Level 1 roadmap checklist."""

from __future__ import annotations

import argparse
import html
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
ROADMAP = ROOT / "ROADMAP.md"
OUTPUT = ROOT / "assets" / "readme"


def read_progress() -> tuple[int, int, float]:
    text = ROADMAP.read_text(encoding="utf-8")
    section = text.split("## Level 1 — Closing Time Circuit", 1)[1].split("## Next after Level 1", 1)[0]
    states = re.findall(r"^- \[([ xX])\] ", section, flags=re.MULTILINE)
    if not states:
        raise ValueError("Level 1 roadmap contains no checklist items")
    completed = sum(state.lower() == "x" for state in states)
    total = len(states)
    return completed, total, completed / total * 100.0


def card(completed: int, total: int, percentage: float) -> str:
    fill = 1100.0 * completed / total
    fill_markup = "" if completed == 0 else f'  <rect x="50" y="120" width="{fill:.3f}" height="20" rx="10" fill="url(#progress)"/>\n'
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 180" role="img" aria-labelledby="title description">
  <title id="title">SCRAP DASH Level 1 progress: {percentage:.1f}%</title>
  <desc id="description">{completed} of {total} Level 1 deliverables verified. Status: in progress.</desc>
  <defs>
    <linearGradient id="progress" x1="0" x2="1"><stop stop-color="#0088FF"/><stop offset="1" stop-color="#62E5FF"/></linearGradient>
    <pattern id="grid" width="24" height="24" patternUnits="userSpaceOnUse"><path d="M24 0H0V24" fill="none" stroke="#143047" stroke-width="1" opacity=".3"/></pattern>
  </defs>
  <rect width="1200" height="180" rx="24" fill="#02050A"/>
  <rect x="1" y="1" width="1198" height="178" rx="23" fill="url(#grid)" stroke="#16364A" stroke-width="2"/>
  <text x="50" y="48" fill="#F4FAFF" font-family="Segoe UI,Arial,sans-serif" font-size="25" font-weight="700">SCRAP DASH</text>
  <text x="50" y="82" fill="#8DA8B8" font-family="Segoe UI,Arial,sans-serif" font-size="19">Level 1 — Closing Time Circuit</text>
  <text x="1150" y="54" text-anchor="end" fill="#62E5FF" font-family="Segoe UI,Arial,sans-serif" font-size="35" font-weight="700">{percentage:.1f}%</text>
  <text x="1150" y="83" text-anchor="end" fill="#8DA8B8" font-family="Segoe UI,Arial,sans-serif" font-size="17">{completed}/{total} VERIFIED • IN PROGRESS</text>
  <rect x="50" y="120" width="1100" height="20" rx="10" fill="#07111C" stroke="#16364A"/>
{fill_markup}  <text x="50" y="163" fill="#8DA8B8" font-family="Segoe UI,Arial,sans-serif" font-size="15">Source: games/001_scrap_dash/ROADMAP.md</text>
</svg>
'''


def mini(completed: int, total: int, percentage: float) -> str:
    fill = 700.0 * completed / total
    fill_markup = "" if completed == 0 else f'  <rect x="170" y="39" width="{fill:.3f}" height="14" rx="7" fill="url(#progress)"/>\n'
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 900 72" role="img" aria-labelledby="title description">
  <title id="title">SCRAP DASH Level 1 progress: {percentage:.1f}%</title>
  <desc id="description">{completed} of {total} Level 1 deliverables verified.</desc>
  <defs><linearGradient id="progress" x1="0" x2="1"><stop stop-color="#0088FF"/><stop offset="1" stop-color="#62E5FF"/></linearGradient></defs>
  <rect width="900" height="72" rx="16" fill="#02050A" stroke="#16364A"/>
  <text x="22" y="29" fill="#F4FAFF" font-family="Segoe UI,Arial,sans-serif" font-size="16" font-weight="700">LEVEL 1</text>
  <text x="22" y="52" fill="#8DA8B8" font-family="Segoe UI,Arial,sans-serif" font-size="13">{completed}/{total} verified</text>
  <rect x="170" y="39" width="700" height="14" rx="7" fill="#07111C" stroke="#16364A"/>
{fill_markup}  <text x="870" y="27" text-anchor="end" fill="#62E5FF" font-family="Segoe UI,Arial,sans-serif" font-size="19" font-weight="700">{percentage:.1f}% • IN PROGRESS</text>
</svg>
'''


def template() -> str:
    label = html.escape("TEMPLATE / NOT PROJECT DATA")
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 180" role="img" aria-labelledby="title description">
  <title id="title">SWIR progress card template</title>
  <desc id="description">Reusable template only. It does not contain project progress data.</desc>
  <defs><linearGradient id="progress" x1="0" x2="1"><stop stop-color="#0088FF"/><stop offset="1" stop-color="#62E5FF"/></linearGradient></defs>
  <rect width="1200" height="180" rx="24" fill="#02050A"/>
  <rect x="1" y="1" width="1198" height="178" rx="23" fill="#07111C" stroke="#16364A" stroke-width="2"/>
  <text x="50" y="52" fill="#F4FAFF" font-family="Segoe UI,Arial,sans-serif" font-size="25" font-weight="700">{label}</text>
  <text x="50" y="88" fill="#8DA8B8" font-family="Segoe UI,Arial,sans-serif" font-size="18">Replace from an authoritative roadmap via the generator.</text>
  <rect x="50" y="120" width="1100" height="20" rx="10" fill="#02050A" stroke="#16364A"/>
  <text x="1150" y="88" text-anchor="end" fill="#62E5FF" font-family="Segoe UI,Arial,sans-serif" font-size="28" font-weight="700">N/A</text>
</svg>
'''


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="fail when generated SVGs are stale")
    args = parser.parse_args()
    completed, total, percentage = read_progress()
    expected = {
        OUTPUT / "progress-card.svg": card(completed, total, percentage),
        OUTPUT / "progress-mini.svg": mini(completed, total, percentage),
        OUTPUT / "progress-template.svg": template(),
    }
    if args.check:
        stale = [str(path.relative_to(ROOT)) for path, value in expected.items() if not path.exists() or path.read_text(encoding="utf-8") != value]
        if stale:
            raise SystemExit("stale progress assets: " + ", ".join(stale))
        print(f"progress assets current: {completed}/{total} = {percentage:.1f}%")
        return 0
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for path, value in expected.items():
        path.write_text(value, encoding="utf-8")
    print(f"generated progress assets: {completed}/{total} = {percentage:.1f}%")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
