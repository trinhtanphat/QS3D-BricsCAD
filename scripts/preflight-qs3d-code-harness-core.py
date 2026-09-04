#!/usr/bin/env python3
"""Restore the dedicated QS3D Code harness-core smoke project during CI diagnosis."""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "tests" / "QS3D.AgentHarness.Core.SmokeTests" / "QS3D.AgentHarness.Core.SmokeTests.csproj"


def main() -> int:
    if not PROJECT.is_file():
        print(f"ERROR: missing harness smoke project: {PROJECT.relative_to(ROOT)}")
        return 1

    completed = subprocess.run(
        ["dotnet", "restore", str(PROJECT)],
        cwd=ROOT,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )

    if completed.stdout:
        print(completed.stdout, end="")
    if completed.stderr:
        print(completed.stderr, end="", file=sys.stderr)

    if completed.returncode != 0:
        print("ERROR: QS3D Code harness core restore diagnostic failed.")
        return completed.returncode

    print("PASS: QS3D Code harness core restore diagnostic.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
