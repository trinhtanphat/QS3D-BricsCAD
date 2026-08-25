#!/usr/bin/env python3
"""Guard Shared CI validation-scope classification for build-impact root files."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CI_WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"

REQUIRED_BUILD_ROOTS = {
    ".gitmodules",
    "Directory.Build.props",
    "QS3D.sln",
    "QS3D.V26.sln",
}
LIGHTWEIGHT_CONTROL = "docs/ARCHITECTURE.md"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def extract_build_root_files(text: str) -> set[str]:
    match = re.search(
        r"\$path\s+-in\s+@\((?P<items>[^)]*)\)\)\s*\{\s*\n\s*\$sourceValidation\s*=\s*\$true\s*\n\s*\$buildValidation\s*=\s*\$true",
        text,
        flags=re.MULTILINE,
    )
    if match is None:
        fail("could not locate the Shared CI build-root classification block")

    return set(re.findall(r"'([^']+)'", match.group("items")))


def main() -> int:
    text = CI_WORKFLOW.read_text(encoding="utf-8")
    build_roots = extract_build_root_files(text)

    missing = sorted(REQUIRED_BUILD_ROOTS - build_roots)
    if missing:
        fail("build-impact root paths missing from full validation scope: " + ", ".join(missing))

    if LIGHTWEIGHT_CONTROL in build_roots:
        fail(f"ordinary documentation path unexpectedly forces full build validation: {LIGHTWEIGHT_CONTROL}")

    if "submodules: recursive" not in text:
        fail("Shared CI no longer checks out recursive submodules in its build-validation path")

    print("PASS: Shared CI build-validation scope covers dependency/build root inputs")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
