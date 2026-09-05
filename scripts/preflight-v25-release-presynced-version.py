#!/usr/bin/env python3
"""Guard V25 preview release preparation when identity is already committed."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PREPARE = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"


def main() -> int:
    source = PREPARE.read_text(encoding="utf-8")
    failures: list[str] = []

    required = (
        "$finalStatus.Count -ne 0 -and $finalStatus.Count -ne $workspaceVersionPaths.Count",
        "if ($finalStatus.Count -eq $workspaceVersionPaths.Count)",
        "Workspace ProductVersion is already synchronized",
    )
    for token in required:
        if token not in source:
            failures.append(f"pre-synchronized V25 release preparation contract is incomplete; missing: {token}")

    forbidden = "if ($finalStatus.Count -ne $workspaceVersionPaths.Count)"
    if forbidden in source:
        failures.append(
            "release preparation still rejects a safe no-op when protected main already contains the requested preview identity"
        )

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print(
        "PASS: V25 release preparation accepts either an already synchronized identity or the complete bounded three-file sync"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
