#!/usr/bin/env python3
"""Guard Shared CI validation-scope classification for build-impact root files."""

from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CI_WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
CLASSIFIER = ROOT / "scripts" / "ci-validation-scope.py"

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


def load_classifier():
    spec = importlib.util.spec_from_file_location("qs3d_ci_validation_scope_guard", CLASSIFIER)
    if spec is None or spec.loader is None:
        fail("could not load the Shared CI validation-scope classifier")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main() -> int:
    if not CLASSIFIER.is_file():
        fail("Shared CI validation-scope classifier is missing")

    workflow = CI_WORKFLOW.read_text(encoding="utf-8")
    classifier_text = CLASSIFIER.read_text(encoding="utf-8")
    classifier = load_classifier()

    build_roots = set(classifier.BUILD_EXACT)
    missing = sorted(REQUIRED_BUILD_ROOTS - build_roots)
    if missing:
        fail("build-impact root paths missing from full validation scope: " + ", ".join(missing))

    if LIGHTWEIGHT_CONTROL in build_roots or LIGHTWEIGHT_CONTROL.startswith(tuple(classifier.BUILD_PREFIXES)):
        fail(f"ordinary documentation path unexpectedly forces full build validation: {LIGHTWEIGHT_CONTROL}")

    required_workflow = (
        "python scripts/ci-validation-scope.py --all --github-output $env:GITHUB_OUTPUT",
        'python scripts/ci-validation-scope.py --base "origin/$baseBranch" --head HEAD --github-output $env:GITHUB_OUTPUT',
        'git fetch --no-tags origin "+refs/heads/$baseBranch`:refs/remotes/origin/$baseBranch"',
        "submodules: recursive",
    )
    for snippet in required_workflow:
        if snippet not in workflow:
            fail(f"Shared CI validation-scope workflow contract is missing: {snippet}")

    required_classifier = (
        '"--no-ext-diff"',
        '"--no-textconv"',
        '"--no-renames"',
        '"--name-only"',
        '"-z"',
        'decode("utf-8", errors="strict")',
    )
    for snippet in required_classifier:
        if snippet not in classifier_text:
            fail(f"validation-scope classifier lost fail-closed NUL-path contract: {snippet}")

    forbidden_workflow = (
        "core.quotePath=false diff --no-renames --name-only",
        "Git returned a C-quoted changed path",
        "foreach ($rawPath in $changed)",
    )
    for snippet in forbidden_workflow:
        if snippet in workflow:
            fail(f"Shared CI reverted to line/C-quote path parsing: {snippet}")

    print("PASS: Shared CI build-validation scope uses the lossless classifier and covers dependency/build root inputs")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
