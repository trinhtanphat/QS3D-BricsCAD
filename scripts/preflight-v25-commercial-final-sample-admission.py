#!/usr/bin/env python3
"""Fail closed unless packaged V25 samples participate in final source admission."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "scripts" / "package-v25.ps1"
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"


def main() -> int:
    package = PACKAGE.read_text(encoding="utf-8")
    workflow = WORKFLOW.read_text(encoding="utf-8")
    failures: list[str] = []

    # This guard is intentionally coupled to the package producer: only require
    # the release fence while generated samples are actual V25 package inputs.
    package_token = "$sampleSource = Join-Path $root 'samples/generated'"
    if package_token not in package:
        failures.append("could not prove samples/generated is a canonical V25 package input")

    fence_start = workflow.find("$finalReleaseRelevantPaths = @(")
    fence_diff = workflow.find(
        'git diff --quiet --no-ext-diff "$env:GITHUB_SHA..$finalMain" -- @finalReleaseRelevantPaths',
        fence_start if fence_start >= 0 else 0,
    )
    if fence_start < 0 or fence_diff < 0:
        failures.append("could not bound the final commercial V25 release drift fence")
    else:
        fence = workflow[fence_start:fence_diff]
        if "'samples/generated/'" not in fence:
            failures.append(
                "final commercial V25 source admission omits samples/generated even though those files are packaged"
            )

    publish_attempt = workflow.find("$publishPatchAttempted = $true", fence_diff if fence_diff >= 0 else 0)
    release_patch = workflow.find("Invoke-RestMethod -Method Patch -Uri $releaseUri", publish_attempt if publish_attempt >= 0 else 0)
    if min(fence_diff, publish_attempt, release_patch) < 0 or not (fence_diff < publish_attempt < release_patch):
        failures.append("final V25 source fence must remain before the irreversible publish PATCH")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: packaged V25 samples are covered by final commercial source admission")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
