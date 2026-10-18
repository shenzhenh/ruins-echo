#!/usr/bin/env python3
"""Ruins Echo repository sanity checks used by CI.

Keeps the check dependency-free (Python standard library only) so it can run
before Unity is available.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"

REQUIRED_PATHS = [
    "README.md",
    "Packages/manifest.json",
    "ProjectSettings/ProjectVersion.txt",
    "Assets/RuinsEcho/Runtime/RuinsEcho.Runtime.asmdef",
    "Assets/RuinsEcho/Editor/RuinsEcho.Editor.asmdef",
    "Assets/RuinsEcho/Tests/EditMode/RuinsEcho.Tests.EditMode.asmdef",
    "docs/spec/遗迹回响_影子共生_单机MVP开发规格文档.md",
    "docs/architecture.md",
    "docs/roadmap.md",
    "docs/development.md",
    "docs/implementation-status.md",
]

FORBIDDEN_DIRS = {"Library", "Temp", "Obj", "Logs", "UserSettings", "Build", "Builds", "MemoryCaptures"}

errors: list[str] = []


def check_required_paths() -> None:
    for relative in REQUIRED_PATHS:
        if not (ROOT / relative).exists():
            errors.append(f"missing required path: {relative}")


def check_json_files() -> None:
    json_files = [ROOT / "Packages/manifest.json"]
    json_files.extend(ASSETS.rglob("*.asmdef"))

    for path in json_files:
        try:
            with path.open("r", encoding="utf-8") as handle:
                json.load(handle)
        except Exception as exc:  # noqa: BLE001 - report path + reason
            errors.append(f"invalid JSON in {path.relative_to(ROOT)}: {exc}")


def check_unity_version() -> None:
    version_file = ROOT / "ProjectSettings/ProjectVersion.txt"
    if not version_file.exists():
        return

    content = version_file.read_text(encoding="utf-8")
    if "m_EditorVersion: 2022.3" not in content:
        errors.append("ProjectVersion.txt must pin a Unity 2022.3 editor")


def check_meta_files() -> None:
    for path in ASSETS.rglob("*"):
        if path.is_dir():
            continue
        if path.name.endswith(".meta"):
            continue
        if path.suffix == ".meta":
            continue

        meta = path.with_name(path.name + ".meta")
        if not meta.exists():
            errors.append(f"missing .meta for {path.relative_to(ROOT)}")
            continue

        content = meta.read_text(encoding="utf-8", errors="replace")
        if "guid:" not in content:
            errors.append(f"meta without guid: {meta.relative_to(ROOT)}")


def check_forbidden_dirs() -> None:
    for path in ROOT.rglob("*"):
        if path.is_dir() and path.name in FORBIDDEN_DIRS:
            errors.append(f"generated directory committed: {path.relative_to(ROOT)}")


def main() -> int:
    check_required_paths()
    check_json_files()
    check_unity_version()
    check_meta_files()
    check_forbidden_dirs()

    if errors:
        print("Repository validation failed:")
        for error in errors:
            print(f"  - {error}")
        return 1

    print("Repository validation passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
