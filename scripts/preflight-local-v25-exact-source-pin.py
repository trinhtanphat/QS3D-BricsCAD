#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "scripts" / "run-local-v25-qualification.ps1"
RUNBOOK = ROOT / "docs" / "LOCAL-V25-QUALIFICATION.md"


def fail(message: str) -> None:
    print(f"ERROR: local V25 exact-source pin preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, pattern: str, description: str, *, flags: int = 0) -> None:
    if re.search(pattern, text, flags) is None:
        fail(description)


def main() -> int:
    runner = RUNNER.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    require(
        runner,
        r"\[Parameter\(Mandatory\s*=\s*\$true\)\]\s*\r?\n\s*\[ValidatePattern\('\^\[0-9A-Fa-f\]\{40\}\$'\)\]\s*\r?\n\s*\[string\]\$ExpectedSourceSha",
        "run-local-v25-qualification.ps1 must require a 40-hex ExpectedSourceSha parameter",
        flags=re.MULTILINE,
    )
    require(
        runner,
        r"\$expectedSourceShaNormalized\s*=\s*\$ExpectedSourceSha\.Trim\(\)\.ToLowerInvariant\(\)",
        "runner must normalize ExpectedSourceSha before comparison",
    )
    require(
        runner,
        r"if\s*\(\$script:headSha\.ToLowerInvariant\(\)\s*-ne\s*\$expectedSourceShaNormalized\)\s*\{\s*throw\s+\"Exact source SHA mismatch:",
        "runner must fail closed when HEAD differs from ExpectedSourceSha",
        flags=re.DOTALL,
    )

    sha_guard = runner.find("Exact source SHA mismatch:")
    first_expensive_gate = runner.find('Invoke-QualificationStep "Manual-only CI policy"')
    if sha_guard < 0 or first_expensive_gate < 0 or sha_guard > first_expensive_gate:
        fail("exact-SHA mismatch check must execute before source/build/runtime qualification gates")

    require(
        runner,
        r"expectedSourceSha\s*=\s*\$expectedSourceShaNormalized",
        "qualification report must record the pinned expectedSourceSha",
    )
    require(
        runbook,
        r"run-local-v25-qualification\.ps1[\s\S]{0,500}-ExpectedSourceSha\s+\"?<exact 40-hex source SHA from handoff>\"?",
        "canonical LOCAL-V25 runbook command must pass the exact handoff SHA explicitly",
    )

    print("Local V25 exact-source pin preflight passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
