#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
PINNED_RUNNER = ROOT / "scripts" / "run-local-v25-pinned-qualification.ps1"
LOW_LEVEL_RUNNER = ROOT / "scripts" / "run-local-v25-qualification.ps1"
RUNBOOK = ROOT / "docs" / "LOCAL-V25-QUALIFICATION.md"


def fail(message: str) -> None:
    print(f"ERROR: local V25 exact-source pin preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, pattern: str, description: str, *, flags: int = 0) -> None:
    if re.search(pattern, text, flags) is None:
        fail(description)


def main() -> int:
    if not PINNED_RUNNER.is_file():
        fail("missing scripts/run-local-v25-pinned-qualification.ps1")

    pinned = PINNED_RUNNER.read_text(encoding="utf-8")
    low_level = LOW_LEVEL_RUNNER.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    require(
        pinned,
        r"\[Parameter\(Mandatory\s*=\s*\$true\)\]\s*\r?\n\s*\[ValidatePattern\('\^\[0-9A-Fa-f\]\{40\}\$'\)\]\s*\r?\n\s*\[string\]\$ExpectedSourceSha",
        "pinned runner must require a 40-hex ExpectedSourceSha parameter",
        flags=re.MULTILINE,
    )
    require(
        pinned,
        r"\$expectedSourceShaNormalized\s*=\s*\$ExpectedSourceSha\.Trim\(\)\.ToLowerInvariant\(\)",
        "pinned runner must normalize ExpectedSourceSha before comparison",
    )
    require(
        pinned,
        r"if\s*\(\$headSha\.ToLowerInvariant\(\)\s*-ne\s*\$expectedSourceShaNormalized\)\s*\{\s*throw\s+\"Exact source SHA mismatch:",
        "pinned runner must fail closed when HEAD differs from ExpectedSourceSha",
        flags=re.DOTALL,
    )
    require(
        pinned,
        r"git\s+status\s+--porcelain",
        "pinned runner must reject a dirty worktree before delegation",
    )
    require(
        pinned,
        r"run-local-v25-qualification\.ps1",
        "pinned runner must delegate to the existing canonical V25 qualification implementation",
    )
    require(
        pinned,
        r"qualification\.json",
        "pinned runner must inspect qualification.json after delegated execution",
    )
    require(
        pinned,
        r"\.exactSha\s*-ne\s*\$expectedSourceShaNormalized",
        "pinned runner must verify the emitted report exactSha against the requested pin",
    )

    mismatch_guard = pinned.find("Exact source SHA mismatch:")
    delegate = pinned.find("run-local-v25-qualification.ps1")
    if mismatch_guard < 0 or delegate < 0 or mismatch_guard > delegate:
        fail("exact-SHA mismatch check must execute before the delegated expensive qualification runner")

    require(
        runbook,
        r"run-local-v25-pinned-qualification\.ps1[\s\S]{0,500}-ExpectedSourceSha\s+\"<exact 40-hex source SHA from handoff>\"",
        "canonical LOCAL-V25 runbook command must use the pinned runner and explicit handoff SHA",
    )
    if re.search(
        r"```powershell[\s\S]{0,500}run-local-v25-qualification\.ps1",
        runbook,
        re.MULTILINE,
    ):
        fail("canonical PowerShell examples must not bypass the pinned exact-SHA entrypoint")

    if "exactSha = $headSha" not in low_level:
        fail("low-level qualification runner must continue emitting exactSha for post-run pin verification")

    print("Local V25 exact-source pin preflight passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
