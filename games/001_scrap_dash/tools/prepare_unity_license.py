#!/usr/bin/env python3
"""Recover a Unity ULF secret without printing license material."""

from __future__ import annotations

import argparse
import base64
import binascii
import html
import json
import os
from pathlib import Path
import re
import xml.etree.ElementTree as ET


DELIMITER = "__SCRAP_DASH_ULF__"


def _clean(value: str) -> str:
    value = value.replace("\x00", "").replace("\r\n", "\n").replace("\r", "\n")
    value = value.lstrip("\ufeff \t\n")
    for prefix in ("UNITY_LICENSE=", "UNITY_LICENSE:"):
        if value.upper().startswith(prefix):
            value = value[len(prefix):].lstrip()
    if value.startswith("```") and value.rstrip().endswith("```"):
        lines = value.strip().splitlines()
        value = "\n".join(lines[1:-1])
    if len(value) >= 2 and value[0] == value[-1] and value[0] in {"'", '"'}:
        if value[0] == '"':
            try:
                decoded = json.loads(value)
                if isinstance(decoded, str):
                    value = decoded
            except json.JSONDecodeError:
                value = value[1:-1]
        else:
            value = value[1:-1]
    if "<" not in value and ("\\n" in value or "\\r" in value):
        value = value.replace("\\r\\n", "\n").replace("\\n", "\n").replace("\\r", "\n")
    return html.unescape(value).replace("\x00", "").lstrip("\ufeff \t\n").strip()


def _decoded_texts(value: str):
    sources = [value]
    if "base64," in value:
        sources.append(value.split("base64,", 1)[1])
    if "-----BEGIN" in value and "-----END" in value:
        sources.append("".join(line for line in value.splitlines() if not line.startswith("-----")))

    for source in sources:
        compact = re.sub(r"\s+", "", source)
        if not compact:
            continue
        padded = compact + "=" * (-len(compact) % 4)
        for altchars in (None, b"-_"):
            try:
                data = base64.b64decode(padded, altchars=altchars, validate=False)
            except (binascii.Error, ValueError):
                continue
            for encoding in ("utf-8-sig", "utf-16", "utf-16-le", "utf-16-be"):
                try:
                    yield data.decode(encoding)
                except UnicodeError:
                    continue

    compact_hex = re.sub(r"\s+", "", value)
    if len(compact_hex) >= 2 and len(compact_hex) % 2 == 0 and re.fullmatch(r"[0-9a-fA-F]+", compact_hex):
        data = bytes.fromhex(compact_hex)
        for encoding in ("utf-8-sig", "utf-16", "utf-16-le", "utf-16-be"):
            try:
                yield data.decode(encoding)
            except UnicodeError:
                continue


def normalize(raw: str) -> str:
    if not raw:
        raise ValueError("UNITY_LICENSE secret is empty")

    candidates = [_clean(raw)]
    candidates.extend(_clean(value) for value in _decoded_texts(raw))

    for candidate in candidates:
        start = candidate.find("<")
        end = candidate.rfind(">")
        if start < 0 or end <= start:
            continue
        xml = candidate[start:end + 1].strip()
        try:
            root = ET.fromstring(xml)
        except ET.ParseError:
            continue
        tag = root.tag.rsplit("}", 1)[-1].lower()
        if tag == "root" or tag == "license" or root.find(".//License") is not None:
            return xml

    raise ValueError(
        "UNITY_LICENSE reached CI, but no valid ULF XML could be recovered from plain, escaped, "
        "quoted, UTF-16, Base64, URL-safe Base64, PEM-wrapped, data-URL or hexadecimal input"
    )


def write_github_env(value: str, env_path: Path) -> None:
    with env_path.open("a", encoding="utf-8", newline="\n") as handle:
        handle.write(f"UNITY_LICENSE<<{DELIMITER}\n")
        handle.write(value)
        if not value.endswith("\n"):
            handle.write("\n")
        handle.write(f"{DELIMITER}\n")


def self_test() -> None:
    sample = '<?xml version="1.0" encoding="UTF-8"?><root><License id="test"/></root>'
    variants = (
        sample,
        "\ufeff  " + sample.replace("><", ">\r\n<"),
        json.dumps(sample.replace("><", ">\n<")),
        f"```xml\n{sample}\n```",
        "UNITY_LICENSE=" + sample,
        base64.b64encode(sample.encode("utf-8")).decode("ascii").rstrip("="),
        base64.urlsafe_b64encode(sample.encode("utf-16")).decode("ascii").rstrip("="),
        sample.encode("utf-16-le").hex(),
    )
    for value in variants:
        assert ET.fromstring(normalize(value)).tag == "root"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()

    if args.self_test:
        self_test()
        print("Unity license recovery self-test: PASS")
        return 0

    try:
        value = normalize(os.environ.get("RAW_UNITY_LICENSE", ""))
    except ValueError as error:
        raise SystemExit(str(error)) from error

    env_path = os.environ.get("GITHUB_ENV")
    if not env_path:
        raise SystemExit("GITHUB_ENV is unavailable")
    write_github_env(value, Path(env_path))
    print("UNITY_LICENSE recovered and exported as validated ULF XML")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
