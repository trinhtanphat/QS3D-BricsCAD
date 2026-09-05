#!/usr/bin/env python3
"""Fail closed unless the QS3D Code CLI smoke project is submodule-independent."""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SMOKE_PROJECT = ROOT / "tests" / "QS3D.Code.Cli.SmokeTests" / "QS3D.Code.Cli.SmokeTests.csproj"

REQUIRED_SOURCE_LINKS = (
    "..\\..\\src\\QS3D.Code.Cli\\ConsoleTraceRenderer.cs",
    "..\\..\\src\\QS3D.Code.Cli\\Qs3dCliApplication.cs",
    "..\\..\\src\\QS3D.Code.Cli\\RepositorySkillLoader.cs",
    "..\\..\\src\\QS3D.Core\\Agent\\Harness\\*.cs",
)


def fail(message: str) -> int:
    print("ERROR: QS3D Code CLI bootstrap preflight failed: " + message)
    return 1


def main() -> int:
    if not SMOKE_PROJECT.is_file():
        return fail("smoke project is missing")

    project_text = SMOKE_PROJECT.read_text(encoding="utf-8")
    if "<ProjectReference" in project_text:
        return fail("smoke project must not use ProjectReference; it must remain buildable before submodule hydration")

    missing = [source for source in REQUIRED_SOURCE_LINKS if source not in project_text]
    if missing:
        return fail("missing bounded source link(s): " + ", ".join(missing))

    forbidden = (
        "external\\QS3D-Platform",
        "external/QS3D-Platform",
        "..\\..\\src\\QS3D.Code.Cli\\Program.cs",
    )
    found = [token for token in forbidden if token in project_text]
    if found:
        return fail("forbidden bootstrap dependency/source found: " + ", ".join(found))

    completed = subprocess.run(
        ["dotnet", "run", "--project", str(SMOKE_PROJECT), "--configuration", "Release"],
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
        return fail("source-only smoke executable returned exit=" + str(completed.returncode))

    print("PASS: QS3D Code CLI smoke/bootstrap is source-only and executable before submodule hydration.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
