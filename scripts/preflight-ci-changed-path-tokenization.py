#!/usr/bin/env python3
"""Guard Shared CI changed-path tokenization against Git C-quoting ambiguity."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def classify_control(path: str) -> tuple[bool, bool]:
    normalized = path.replace("\\", "/")
    build = bool(
        re.match(r"^(src|tests|scripts)/", normalized)
        or re.match(r"^samples/generated/", normalized)
        or re.match(r"^\.github/workflows/", normalized)
        or normalized in {"Directory.Build.props", "QS3D.sln", "QS3D.V26.sln"}
    )
    source = build or normalized in {
        "CI_POLICY.md",
        "AGENTS.md",
        "README.md",
        "docs/MAIN-WRITE-AUTHORIZATION.md",
        "docs/AGENT-WORK-REGISTRATION.md",
        "docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md",
        "docs/AGENT-STATUS-MARKER-SEMANTICS.md",
        "docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md",
    }
    return source, build


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")

    required = (
        "git -c core.quotePath=false diff --name-only",
        "$path = [string]$rawPath",
        "[string]::IsNullOrEmpty($path)",
        "Git returned a C-quoted changed path; refusing ambiguous validation classification",
        "$path = $path.Replace('\\', '/')",
    )
    for snippet in required:
        if snippet not in text:
            fail(f"Shared CI changed-path safety contract is missing: {snippet}")

    if "([string]$rawPath).Trim()" in text or "$path = $path.Trim()" in text:
        fail("Shared CI must not trim Git path tokens before validation-scope classification")

    quote_guard = text.index("Git returned a C-quoted changed path; refusing ambiguous validation classification")
    build_classifier = text.index("$path -match '^(src|tests|scripts)/'")
    if quote_guard > build_classifier:
        fail("C-quoted path rejection must occur before source/build path classification")

    controls = {
        "src/đường-dẫn/fixture.cs": (True, True),
        "scripts/preflight-unicode-ß.py": (True, True),
        "tests/name with trailing space ": (True, True),
        ".github/workflows/ci.yml": (True, True),
        "Directory.Build.props": (True, True),
        "CI_POLICY.md": (True, False),
        "docs/ARCHITECTURE.md": (False, False),
    }
    for path, expected in controls.items():
        actual = classify_control(path)
        if actual != expected:
            fail(f"classification control failed for {path!r}: expected {expected}, got {actual}")

    quoted_controls = (
        '"src/control\\nname.cs"',
        '"scripts/quote\\"name.py"',
    )
    for path in quoted_controls:
        if not (len(path) >= 2 and path[0] == '"' and path[-1] == '"'):
            fail(f"quoted-path negative control is invalid: {path!r}")

    print("PASS: Shared CI preserves Unicode path identity and rejects ambiguous C-quoted tokens")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
