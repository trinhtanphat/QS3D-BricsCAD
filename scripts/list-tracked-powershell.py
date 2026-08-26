#!/usr/bin/env python3
"""Losslessly enumerate tracked PowerShell scripts below scripts/."""

from __future__ import annotations

import argparse
import json
from pathlib import Path, PurePosixPath
import subprocess
import sys

MAX_GIT_OUTPUT_BYTES = 4 * 1024 * 1024
MAX_GIT_DIAGNOSTIC_BYTES = 64 * 1024
GIT_TIMEOUT_SECONDS = 30.0


class EnumerationError(RuntimeError):
    pass


def _run_git(root: Path) -> bytes:
    try:
        completed = subprocess.run(
            ["git", "ls-files", "-z", "--", "scripts"],
            cwd=str(root),
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=GIT_TIMEOUT_SECONDS,
            check=False,
        )
    except (OSError, subprocess.TimeoutExpired) as exc:
        raise EnumerationError(f"could not enumerate tracked PowerShell scripts: {exc}") from exc

    if len(completed.stdout) > MAX_GIT_OUTPUT_BYTES:
        raise EnumerationError(
            f"tracked-path output exceeds {MAX_GIT_OUTPUT_BYTES} bytes"
        )
    if len(completed.stderr) > MAX_GIT_DIAGNOSTIC_BYTES:
        raise EnumerationError(
            f"Git diagnostic output exceeds {MAX_GIT_DIAGNOSTIC_BYTES} bytes"
        )
    if completed.returncode != 0:
        diagnostic = completed.stderr.decode("utf-8", errors="replace").strip()
        raise EnumerationError(
            f"git ls-files failed with exit {completed.returncode}: {diagnostic}"
        )
    return completed.stdout


def parse_nul_paths(raw: bytes) -> list[str]:
    if not raw:
        return []
    if not raw.endswith(b"\0"):
        raise EnumerationError("tracked-path output is not NUL-terminated")

    encoded = raw[:-1].split(b"\0")
    if any(not item for item in encoded):
        raise EnumerationError("tracked-path output contains an empty path record")

    paths: list[str] = []
    for item in encoded:
        try:
            path = item.decode("utf-8", errors="strict")
        except UnicodeDecodeError as exc:
            raise EnumerationError("tracked-path output contains a non-UTF-8 pathname") from exc
        parsed = PurePosixPath(path)
        if parsed.is_absolute() or ".." in parsed.parts:
            raise EnumerationError(f"tracked path escapes repository-relative scope: {path!r}")
        if not parsed.parts or parsed.parts[0] != "scripts":
            raise EnumerationError(f"tracked path escapes scripts/ scope: {path!r}")
        paths.append(path)
    return paths


def enumerate_powershell_scripts(root: Path) -> list[str]:
    paths = parse_nul_paths(_run_git(root))
    scripts = [path for path in paths if PurePosixPath(path).suffix.casefold() == ".ps1"]
    if not scripts:
        raise EnumerationError("no tracked PowerShell scripts were found below scripts/")
    if len(set(scripts)) != len(scripts):
        raise EnumerationError("duplicate tracked PowerShell path records were returned by Git")
    return sorted(scripts)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", default=".", help="repository root containing .git")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        scripts = enumerate_powershell_scripts(Path(args.root).resolve())
    except EnumerationError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    print(json.dumps(scripts, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
