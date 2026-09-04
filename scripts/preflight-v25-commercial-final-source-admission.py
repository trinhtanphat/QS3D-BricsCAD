#!/usr/bin/env python3
"""Fail closed unless manual V25 commercial publication revalidates protected main."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"


def main() -> int:
    source = WORKFLOW.read_text(encoding="utf-8")
    failures: list[str] = []

    publish_step = source.find("      - name: Create draft, verify uploaded bytes, then publish")
    if publish_step < 0:
        failures.append("could not locate manual V25 commercial publication step")
        publish = ""
    else:
        publish = source[publish_step:]

    required = (
        "$finalMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"",
        "$finalMain =",
        "$finalMainRef = 'refs/remotes/origin/qs3d-commercial-final-main'",
        "+refs/heads/main:$finalMainRef",
        "$fetchedFinalMain =",
        "$fetchedFinalMain -ne $finalMain",
        "git merge-base --is-ancestor $env:GITHUB_SHA $finalMain",
        "$finalReleaseRelevantPaths = @(",
        "git diff --quiet --no-ext-diff \"$env:GITHUB_SHA..$finalMain\" -- @finalReleaseRelevantPaths",
        "$finalReleaseDriftStatus = $LASTEXITCODE",
        "newer release-relevant protected main supersedes this commercial publication",
        "$confirmedMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"",
        "$confirmedFinalMain =",
        "$confirmedFinalMain -ne $finalMain",
        "main advanced only through non-release paths during final commercial publication admission",
    )
    for token in required:
        if token not in publish:
            failures.append(f"final commercial publication admission is incomplete; missing: {token}")

    semantic_verification = publish.find("$downloadedIdentity = & .\\scripts\\assert-v25-commercial-draft-identity.ps1")
    semantic_success = publish.find("Downloaded V25 draft semantic admission returned no identity.")
    tag_assert = publish.find("Assert-RemoteReleaseTagTargetsWorkflowSha", semantic_success + 1)
    final_api = publish.find("$finalMainResponse = Invoke-RestMethod -Method Get -Uri")
    final_fetch = publish.find("& git fetch --no-tags --force origin \"+refs/heads/main:$finalMainRef\"")
    fetched_identity = publish.find("$fetchedFinalMain =")
    api_fetch_equality = publish.find("$fetchedFinalMain -ne $finalMain")
    ancestry = publish.find("git merge-base --is-ancestor $env:GITHUB_SHA $finalMain")
    release_diff = publish.find("git diff --quiet --no-ext-diff \"$env:GITHUB_SHA..$finalMain\" -- @finalReleaseRelevantPaths")
    drift_status = publish.find("$finalReleaseDriftStatus = $LASTEXITCODE")
    confirmed_api = publish.find("$confirmedMainResponse = Invoke-RestMethod -Method Get -Uri")
    confirmed_identity = publish.find("$confirmedFinalMain =")
    confirmed_equality = publish.find("$confirmedFinalMain -ne $finalMain")
    publish_attempt = publish.find("$publishPatchAttempted = $true")
    release_patch = publish.find("Invoke-RestMethod -Method Patch -Uri $releaseUri")

    ordered = (
        semantic_verification,
        semantic_success,
        tag_assert,
        final_api,
        final_fetch,
        fetched_identity,
        api_fetch_equality,
        ancestry,
        release_diff,
        drift_status,
        confirmed_api,
        confirmed_identity,
        confirmed_equality,
        publish_attempt,
        release_patch,
    )
    if min(ordered) < 0:
        failures.append("could not bound the complete final commercial source-admission ordering")
    elif list(ordered) != sorted(ordered):
        failures.append(
            "final commercial admission must order semantic verification -> tag identity -> main API/fetch equality -> "
            "ancestry -> release-relevant drift -> confirming API snapshot -> publish attempt -> PATCH"
        )

    if "exit 0" in publish[final_api:publish_attempt] if final_api >= 0 and publish_attempt >= 0 else False:
        failures.append("commercial stale-source admission must enter existing rollback/reconciliation instead of exiting success")

    if "continue-on-error" in source:
        failures.append("commercial release source admission must not become fail-open through continue-on-error")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: manual V25 commercial release revalidates protected main immediately before publication")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
