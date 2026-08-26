#!/usr/bin/env python3
"""Hermetic regression for lossless tracked PowerShell enumeration."""

from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "list-tracked-powershell.py"


def fail(message: str) -> None:
    raise AssertionError(message)


def run(command: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        command,
        cwd=str(cwd),
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
        timeout=20,
    )


def load_helper():
    spec = importlib.util.spec_from_file_location("qs3d_list_tracked_powershell", HELPER)
    if spec is None or spec.loader is None:
        fail("could not load tracked PowerShell helper")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main() -> int:
    module = load_helper()

    lossless = module.parse_nul_paths(
        "scripts/line\nbreak.ps1\0scripts/tab\tname.PS1\0scripts/unicode-đ.Ps1\0".encode("utf-8")
    )
    if lossless != [
        "scripts/line\nbreak.ps1",
        "scripts/tab\tname.PS1",
        "scripts/unicode-đ.Ps1",
    ]:
        fail(f"NUL-safe parser changed unusual Git path records: {lossless!r}")

    malformed_cases = (
        (b"scripts/a.ps1", "NUL-terminated"),
        (b"scripts/a.ps1\0\0", "empty path record"),
        (b"scripts/\xff.ps1\0", "non-UTF-8"),
        (b"../escape.ps1\0", "escapes repository-relative"),
        (b"other/a.ps1\0", "escapes scripts/"),
    )
    for payload, token in malformed_cases:
        try:
            module.parse_nul_paths(payload)
        except module.EnumerationError as exc:
            if token not in str(exc):
                fail(f"wrong fail-closed diagnostic for {payload!r}: {exc}")
        else:
            fail(f"malformed tracked-path payload was accepted: {payload!r}")

    with tempfile.TemporaryDirectory(prefix="qs3d-ps-enum-") as temp_dir:
        repo = Path(temp_dir)
        init = run(["git", "init", "-q"], repo)
        if init.returncode != 0:
            fail(f"git init failed: {init.stderr}")

        scripts_dir = repo / "scripts"
        scripts_dir.mkdir()
        expected = [
            "scripts/Mixed.PS1",
            "scripts/normal.ps1",
            "scripts/unicode-đ.Ps1",
        ]
        for relative in expected:
            target = repo / relative
            target.write_text("Write-Host 'ok'\n", encoding="utf-8")
        (scripts_dir / "ignore.txt").write_text("not PowerShell\n", encoding="utf-8")

        add = run(["git", "add", "--", "scripts"], repo)
        if add.returncode != 0:
            fail(f"git add failed: {add.stderr}")

        completed = run([sys.executable, str(HELPER), "--root", str(repo)], repo)
        if completed.returncode != 0:
            fail(f"helper failed on hermetic repository: {completed.stderr}")
        try:
            actual = json.loads(completed.stdout)
        except json.JSONDecodeError as exc:
            fail(f"helper did not emit JSON: {exc}")

        if actual != sorted(expected):
            fail(f"case-insensitive tracked enumeration mismatch: {actual!r}")
        if any(path.endswith("ignore.txt") for path in actual):
            fail("non-PowerShell file leaked into tracked script enumeration")

    print("PASS: tracked PowerShell enumeration is NUL-safe, bounded, and case-insensitive")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
