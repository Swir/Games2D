#!/usr/bin/env python3
"""Normalize a Unity ULF secret without printing any license material."""

from __future__ import annotations

import argparse
import base64
import binascii
import os
from pathlib import Path
import xml.etree.ElementTree as ET


DELIMITER = "__SCRAP_DASH_ULF__"


def _strip_wrapper(value: str) -> str:
    value = value.replace("\r\n", "\n").replace("\r", "\n")
    value = value.lstrip("\ufeff \t\n")
    if len(value) >= 2 and value[0] == value[-1] and value[0] in {"'", '"'}:
        value = value[1:-1].strip()
    return value


def normalize(raw: str) -> str:
    if not raw:
        raise ValueError("UNITY_LICENSE secret is empty")

    value = _strip_wrapper(raw)
    candidates = [value]

    if "\\n" in value or "\\r" in value:
        candidates.append(_strip_wrapper(value.replace("\\r\\n", "\n").replace("\\n", "\n").replace("\\r", "\n")))

    compact = "".join(value.split())
    try:
        decoded = base64.b64decode(compact, validate=True).decode("utf-8-sig")
        candidates.append(_strip_wrapper(decoded))
    except (binascii.Error, UnicodeDecodeError, ValueError):
        pass

    for candidate in candidates:
        xml_start = candidate.find("<")
        if xml_start < 0:
            continue
        candidate = candidate[xml_start:].strip()
        try:
            ET.fromstring(candidate)
        except ET.ParseError:
            continue
        return candidate

    raise ValueError(
        "UNITY_LICENSE is present but is not valid ULF XML, escaped ULF XML, or Base64-encoded ULF XML"
    )


def write_github_env(value: str, env_path: Path) -> None:
    with env_path.open("a", encoding="utf-8", newline="\n") as handle:
        handle.write(f"UNITY_LICENSE_NORMALIZED<<{DELIMITER}\n")
        handle.write(value)
        if not value.endswith("\n"):
            handle.write("\n")
        handle.write(f"{DELIMITER}\n")


def self_test() -> None:
    sample = '<?xml version="1.0" encoding="UTF-8"?><root><License id="test"/></root>'
    assert normalize(sample) == sample
    assert normalize("\ufeff  " + sample.replace("><", ">\r\n<"))
    assert normalize(sample.replace("><", ">\\n<"))
    assert normalize(base64.b64encode(sample.encode("utf-8")).decode("ascii")) == sample


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()

    if args.self_test:
        self_test()
        print("Unity license normalizer self-test: PASS")
        return 0

    try:
        value = normalize(os.environ.get("RAW_UNITY_LICENSE", ""))
    except ValueError as error:
        raise SystemExit(str(error)) from error

    env_path = os.environ.get("GITHUB_ENV")
    if not env_path:
        raise SystemExit("GITHUB_ENV is unavailable")
    write_github_env(value, Path(env_path))
    print("Unity license normalized and validated as XML")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
