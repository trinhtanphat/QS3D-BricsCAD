#!/usr/bin/env python3
"""Fail closed unless packaged samples participate in cloud V25 final source admission."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "scripts" / "package-v25.ps1"
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"


def main() -> int:
    package = PACKAGE.read_text(encoding="utf-8")
    workflow = WORKFLOW.read_text(encoding="utf-8")
    failures: list[str] = []

    package_token = "$sampleSource = Join-Path $root 'samples/generated'"
    if package_token not in package:
        failures.append("could not prove samples/generated is a canonical V25 package input")

    fence_start = workflow.find("$finalReleaseRelevantPaths = @(")
    fence_diff = workflow.find(
        'git diff --quiet --no-ext-diff "$env:SOURCE_SHA..$finalMain" -- @finalReleaseRelevantPaths',
        fence_start if fence_start >= 0 else 0,
    )
    if fence_start < 0 or fence_diff < 0:
        failures.append("could not bound the cloud V25 final protected-main drift fence")
    else:
        fence = workflow[fence_start:fence_diff]
        if "'samples/generated/'" not in fence:
            failures.append(
                "cloud V25 final source admission omits samples/generated even though those files are packaged"
            )

    confirmation = workflow.find("$confirmedMainResponse = Invoke-RestMethod", fence_diff if fence_diff >= 0 else 0)
    publish_body = workflow.find("$publishBody = @{ draft = $false }", confirmation if confirmation >= 0 else 0)
    release_patch = workflow.find("Invoke-RestMethod -Method Patch -Uri $releaseUri", publish_body if publish_body >= 0 else 0)
    if min(fence_diff, confirmation, publish_body, release_patch) < 0 or not (
        fence_diff < confirmation < publish_body < release_patch
    ):
        failures.append("cloud V25 final source fence must remain before confirming main and the irreversible publish PATCH")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: packaged samples are covered by cloud V25 final source admission")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
