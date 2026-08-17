#!/usr/bin/env python3
"""Fail closed on duplicate GitHub Actions policy mapping keys.

The repository's manual/automatic CI policy checker intentionally parses a bounded
YAML subset rather than loading a general YAML engine. Duplicate YAML mapping keys
are parser-dependent (commonly last-wins), so policy validation must reject them
before any later semantic checker can accidentally observe only one definition.
"""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOWS = ROOT / ".github" / "workflows"
_KEY = r"(?:\"([A-Za-z0-9_-]+)\"|'([A-Za-z0-9_-]+)'|([A-Za-z0-9_-]+))"


def parse_mapping_key(line: str, indentation: int) -> str | None:
    if indentation < 0:
        return None
    match = re.match(r"^" + re.escape(" " * indentation) + _KEY + r"\s*:", line)
    if not match:
        return None
    return next(value for value in match.groups() if value is not None)


def top_level_key_indices(lines: list[str], name: str) -> list[int]:
    return [index for index, line in enumerate(lines) if parse_mapping_key(line, 0) == name]


def indented_block(lines: list[str], start_index: int) -> list[str]:
    block: list[str] = []
    for line in lines[start_index + 1 :]:
        if line.strip() and not line.startswith((" ", "\t", "#")):
            break
        block.append(line)
    return block


def duplicate_keys(lines: list[str], indentation: int) -> list[str]:
    seen: set[str] = set()
    duplicates: list[str] = []
    for line in lines:
        name = parse_mapping_key(line, indentation)
        if name is None:
            continue
        if name in seen and name not in duplicates:
            duplicates.append(name)
        seen.add(name)
    return duplicates


def scan_workflow_text(text: str, label: str) -> list[str]:
    lines = text.splitlines()
    errors: list[str] = []

    on_indices = top_level_key_indices(lines, "on")
    jobs_indices = top_level_key_indices(lines, "jobs")
    if len(on_indices) > 1:
        errors.append(f"{label}: duplicate top-level on mapping key")
    if len(jobs_indices) > 1:
        errors.append(f"{label}: duplicate top-level jobs mapping key")

    if len(on_indices) == 1:
        duplicate_triggers = duplicate_keys(indented_block(lines, on_indices[0]), 2)
        for name in duplicate_triggers:
            errors.append(f"{label}: duplicate trigger mapping key: {name}")

    if len(jobs_indices) == 1:
        duplicate_jobs = duplicate_keys(indented_block(lines, jobs_indices[0]), 2)
        for name in duplicate_jobs:
            errors.append(f"{label}: duplicate job mapping key: {name}")

    return errors


def scan_repository() -> list[str]:
    if not WORKFLOWS.is_dir():
        return ["missing .github/workflows directory"]
    workflows = sorted(list(WORKFLOWS.glob("*.yml")) + list(WORKFLOWS.glob("*.yaml")))
    if not workflows:
        return ["no GitHub Actions workflows found"]

    errors: list[str] = []
    for path in workflows:
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeError) as exc:
            errors.append(f"{path.name}: cannot read workflow as UTF-8: {exc}")
            continue
        errors.extend(scan_workflow_text(text, path.name))
    return errors


def main() -> int:
    errors = scan_repository()
    print("QS3D CI policy mapping-key uniqueness preflight")
    if errors:
        for error in errors:
            print("ERROR:", error)
        print(f"FAILED with {len(errors)} error(s).")
        return 1

    print(
        "PASS: workflow policy surfaces contain no duplicate top-level on/jobs, "
        "trigger, or job mapping keys (quoted/unquoted equivalents included)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
