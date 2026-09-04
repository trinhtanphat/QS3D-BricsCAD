#!/usr/bin/env python3
"""Probe the repo-local .NET 8 restore boundary for the harness smoke project."""

from __future__ import annotations

import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TESTS = ROOT / "tests"


def main() -> int:
    with tempfile.TemporaryDirectory(prefix="qs3d-harness-restore-probe-", dir=TESTS) as temp_dir:
        probe_dir = Path(temp_dir)
        project = probe_dir / "Probe.csproj"
        project.write_text(
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
            "  <PropertyGroup>\n"
            "    <OutputType>Exe</OutputType>\n"
            "    <TargetFramework>net8.0</TargetFramework>\n"
            "    <Nullable>enable</Nullable>\n"
            "  </PropertyGroup>\n"
            "</Project>\n",
            encoding="utf-8",
        )

        completed = subprocess.run(
            ["dotnet", "restore", str(project)],
            cwd=probe_dir,
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
            print("ERROR: repo-local .NET 8 restore probe failed.")
            return completed.returncode

    print("PASS: repo-local .NET 8 restore probe.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
