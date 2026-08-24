#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
PINNED_RUNNER = ROOT / "scripts" / "run-local-v25-pinned-qualification.ps1"
LOW_LEVEL_RUNNER = ROOT / "scripts" / "run-local-v25-qualification.ps1"
ENTRYPOINT_DOC = ROOT / "docs" / "LOCAL-V25-PINNED-ENTRYPOINT.md"


def fail(message: str) -> None:
    print(f"ERROR: local V25 exact-source pin preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, pattern: str, description: str, *, flags: int = 0) -> None:
    if re.search(pattern, text, flags) is None:
        fail(description)


def main() -> int:
    if not PINNED_RUNNER.is_file():
        fail("missing scripts/run-local-v25-pinned-qualification.ps1")
    if not ENTRYPOINT_DOC.is_file():
        fail("missing docs/LOCAL-V25-PINNED-ENTRYPOINT.md")

    pinned = PINNED_RUNNER.read_text(encoding="utf-8")
    low_level = LOW_LEVEL_RUNNER.read_text(encoding="utf-8")
    entrypoint_doc = ENTRYPOINT_DOC.read_text(encoding="utf-8")

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
        "pinned runner must delegate to the existing V25 qualification implementation",
    )
    require(
        pinned,
        r"qualification\.json",
        "pinned runner must inspect qualification.json after delegated execution",
    )
    require(
        pinned,
        r"\$reportedExactSha\s*=\s*\(\[string\]\$report\.exactSha\)\.Trim\(\)\.ToLowerInvariant\(\)",
        "pinned runner must normalize the emitted report exactSha",
    )
    require(
        pinned,
        r"if\s*\(\$reportedExactSha\s*-ne\s*\$expectedSourceShaNormalized\)",
        "pinned runner must verify the emitted report exactSha against the requested pin",
    )

    mismatch_guard = pinned.find("Exact source SHA mismatch:")
    delegate = pinned.find("run-local-v25-qualification.ps1")
    if mismatch_guard < 0 or delegate < 0 or mismatch_guard > delegate:
        fail("exact-SHA mismatch check must execute before the delegated expensive qualification runner")

    for optional_name in ("Profile", "ArtifactDir", "PythonPath", "ReleaseTag"):
        require(
            pinned,
            rf"if\s*\(-not\s+\[string\]::IsNullOrWhiteSpace\(\${optional_name}\)\)\s*\{{\s*\$runnerArgs\.{optional_name}\s*=\s*\${optional_name}",
            f"pinned runner must forward optional {optional_name} only when non-empty",
            flags=re.DOTALL,
        )
    require(
        pinned,
        r"if\s*\(\$SignPackage\)\s*\{[\s\S]*?\$runnerArgs\.SignPackage\s*=\s*\$true[\s\S]*?\$runnerArgs\.SigningCertThumbprint\s*=\s*\$SigningCertThumbprint[\s\S]*?\$runnerArgs\.TimestampUrl\s*=\s*\$TimestampUrl",
        "signing-only validated strings must be forwarded only when SignPackage is requested",
    )

    require(
        entrypoint_doc,
        r"run-local-v25-pinned-qualification\.ps1[\s\S]{0,500}-ExpectedSourceSha\s+\"<exact 40-hex source SHA from handoff>\"",
        "pinned entrypoint documentation must pass the exact handoff SHA explicitly",
    )
    require(
        entrypoint_doc,
        r"run-local-v25-qualification\.ps1.*implementation detail",
        "entrypoint documentation must mark the unpinned runner as an implementation detail",
        flags=re.IGNORECASE,
    )

    if "exactSha = $headSha" not in low_level:
        fail("low-level qualification runner must continue emitting exactSha for post-run pin verification")

    print("Local V25 exact-source pin preflight passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
