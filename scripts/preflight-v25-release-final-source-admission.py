#!/usr/bin/env python3
"""Fail closed unless V25 publication revalidates protected-main source freshness."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"


def main() -> int:
    source = WORKFLOW.read_text(encoding="utf-8")
    failures: list[str] = []

    publish_step = source.find("      - name: Publish GitHub prerelease")
    if publish_step < 0:
        failures.append("could not locate V25 publication step")
        publish = ""
    else:
        publish = source[publish_step:]

    required = (
        "$finalMain =",
        "repos/$env:GITHUB_REPOSITORY/commits/main",
        "$finalMainRef = 'refs/remotes/origin/qs3d-release-final-main'",
        "+refs/heads/main:$finalMainRef",
        "$fetchedFinalMain =",
        "$confirmedFinalMain =",
        "$fetchedFinalMain -ne $finalMain",
        "$confirmedFinalMain -ne $finalMain",
        "git merge-base --is-ancestor $env:SOURCE_SHA $finalMain",
        "git diff --quiet --no-ext-diff \"$env:SOURCE_SHA..$finalMain\" --",
        "$finalReleaseDriftStatus = $LASTEXITCODE",
        "newer release-relevant main integration supersedes this publication",
        "main advanced only through non-release paths during final publication admission",
    )
    for token in required:
        if token not in publish:
            failures.append(f"final V25 publication admission is incomplete; missing: {token}")

    final_api = publish.find("repos/$env:GITHUB_REPOSITORY/commits/main")
    final_asset_identity = publish.find("$assetIdentityDrift = @(")
    publish_body = publish.find("$publishBody = @{ draft = $false }")
    release_patch = publish.find("Invoke-RestMethod -Method Patch -Uri $releaseUri")
    if min(final_api, final_asset_identity, publish_body, release_patch) < 0:
        failures.append("could not bound final source admission and draft-to-published transition")
    elif not (final_asset_identity < final_api < publish_body < release_patch):
        failures.append("final protected-main admission must run after final draft/asset identity verification and immediately before the draft-to-published PATCH")

    draft_creation = publish.find("Invoke-RestMethod -Method Post -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases\"")
    if draft_creation >= 0 and final_api >= 0 and final_api < draft_creation:
        failures.append("final publication admission must not run only before draft creation; upload and round-trip verification time must remain inside the TOCTOU fence")

    if "continue-on-error" in source:
        failures.append("release source admission must not become fail-open through continue-on-error")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: V25 release revalidates protected main immediately before publication")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
