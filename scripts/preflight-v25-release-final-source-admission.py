#!/usr/bin/env python3
"""Guard V25 preview publication against source substitution without release starvation."""

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
        "$fetchedFinalMain -ne $finalMain",
        "git merge-base --is-ancestor $env:SOURCE_SHA $finalMain",
        "Release SOURCE_SHA remains an ancestor of protected main; publication stays pinned to the already verified SOURCE_SHA even if newer main integrations landed.",
        "$publishBody = @{ draft = $false }",
    )
    for token in required:
        if token not in publish:
            failures.append(f"final V25 publication admission is incomplete; missing: {token}")

    forbidden = (
        "git diff --quiet --no-ext-diff \"$env:SOURCE_SHA..$finalMain\" --",
        "$finalReleaseDriftStatus = $LASTEXITCODE",
        "newer release-relevant main integration supersedes this publication",
        "$confirmedFinalMain =",
        "$confirmedFinalMain -ne $finalMain",
        "Protected main moved during final release admission.",
    )
    for token in forbidden:
        if token in publish:
            failures.append(f"final V25 publication still contains release-starvation gate: {token}")

    final_asset_identity = publish.find("$assetIdentityDrift = @(")
    final_api = publish.find("repos/$env:GITHUB_REPOSITORY/commits/main")
    final_fetch = publish.find("& git fetch --no-tags --force origin \"+refs/heads/main:$finalMainRef\"")
    fetched_identity = publish.find("$fetchedFinalMain =")
    api_fetch_equality = publish.find("$fetchedFinalMain -ne $finalMain")
    ancestry = publish.find("git merge-base --is-ancestor $env:SOURCE_SHA $finalMain")
    pinned_notice = publish.find("Release SOURCE_SHA remains an ancestor of protected main; publication stays pinned to the already verified SOURCE_SHA even if newer main integrations landed.")
    publish_body = publish.find("$publishBody = @{ draft = $false }")
    release_patch = publish.find("Invoke-RestMethod -Method Patch -Uri $releaseUri")
    ordered = (
        final_asset_identity,
        final_api,
        final_fetch,
        fetched_identity,
        api_fetch_equality,
        ancestry,
        pinned_notice,
        publish_body,
        release_patch,
    )
    if min(ordered) < 0:
        failures.append("could not bound final source ancestry admission and draft-to-published transition")
    elif list(ordered) != sorted(ordered):
        failures.append(
            "final preview publication must order verified assets -> main API/fetch equality -> SOURCE_SHA ancestry -> pinned-source notice -> publish PATCH"
        )

    draft_creation = publish.find("Invoke-RestMethod -Method Post -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases\"")
    if draft_creation >= 0 and final_api >= 0 and final_api < draft_creation:
        failures.append("final source ancestry admission must remain after draft upload/round-trip verification")

    if final_asset_identity >= 0 and publish_body >= 0:
        final_window = publish[final_asset_identity:publish_body]
        if "exit 0" in final_window:
            failures.append("verified V25 preview publication must not exit success merely because protected main advanced")

    if "continue-on-error" in source:
        failures.append("release source admission must not become fail-open through continue-on-error")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: V25 preview publication keeps verified SOURCE_SHA provenance without starving when protected main advances")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
