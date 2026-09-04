#!/usr/bin/env python3
"""Fail closed unless manual V26 publication revalidates protected main."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PUBLISH = ROOT / "scripts" / "publish-v26-release.ps1"


def main() -> int:
    source = PUBLISH.read_text(encoding="utf-8")
    failures: list[str] = []

    required = (
        "$finalMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"",
        "$finalMain =",
        "$finalMainRef = 'refs/remotes/origin/qs3d-v26-release-final-main'",
        "+refs/heads/main:$finalMainRef",
        "$fetchedFinalMain =",
        "$fetchedFinalMain -ne $finalMain",
        "git merge-base --is-ancestor $env:GITHUB_SHA $finalMain",
        "$finalReleaseRelevantPaths = @(",
        "'src/QS3D.BricsCAD.V25/'",
        "'src/QS3D.BricsCAD.V26/'",
        "git diff --quiet --no-ext-diff \"$env:GITHUB_SHA..$finalMain\" -- @finalReleaseRelevantPaths",
        "$finalReleaseDriftStatus = $LASTEXITCODE",
        "newer release-relevant protected main supersedes this V26 publication",
        "$confirmedMainResponse = Invoke-RestMethod -Method Get -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main\"",
        "$confirmedFinalMain =",
        "$confirmedFinalMain -ne $finalMain",
        "main advanced only through non-release paths during final V26 publication admission",
    )
    for token in required:
        if token not in source:
            failures.append(f"final V26 publication admission is incomplete; missing: {token}")

    publish_attempt = source.find("$publishPatchAttempted = $true")
    release_patch = source.find("Invoke-RestMethod -Method Patch -Uri $releaseUri")
    final_tag = source.rfind("Assert-RemoteReleaseTagTargetsWorkflowSha", 0, publish_attempt if publish_attempt >= 0 else len(source))
    verified_asset = source.rfind("$verifiedAssetIds[$expectedAsset] = $uploadedAssetId", 0, final_tag if final_tag >= 0 else len(source))
    final_api = source.find("$finalMainResponse = Invoke-RestMethod -Method Get -Uri")
    final_fetch = source.find("& git fetch --no-tags --force origin \"+refs/heads/main:$finalMainRef\"")
    fetched_identity = source.find("$fetchedFinalMain =")
    api_fetch_equality = source.find("$fetchedFinalMain -ne $finalMain")
    ancestry = source.find("git merge-base --is-ancestor $env:GITHUB_SHA $finalMain")
    release_diff = source.find("git diff --quiet --no-ext-diff \"$env:GITHUB_SHA..$finalMain\" -- @finalReleaseRelevantPaths")
    drift_status = source.find("$finalReleaseDriftStatus = $LASTEXITCODE")
    confirmed_api = source.find("$confirmedMainResponse = Invoke-RestMethod -Method Get -Uri")
    confirmed_identity = source.find("$confirmedFinalMain =")
    confirmed_equality = source.find("$confirmedFinalMain -ne $finalMain")

    ordered = (
        verified_asset,
        final_tag,
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
        failures.append("could not bound the complete final V26 source-admission ordering")
    elif list(ordered) != sorted(ordered):
        failures.append(
            "final V26 admission must order verified asset identity -> tag identity -> main API/fetch equality -> "
            "ancestry -> release-relevant drift -> confirming API snapshot -> publish attempt -> PATCH"
        )

    if "exit 0" in source[final_api:publish_attempt] if final_api >= 0 and publish_attempt >= 0 else False:
        failures.append("V26 stale-source admission must enter existing rollback/reconciliation instead of exiting success")

    if "continue-on-error" in source:
        failures.append("V26 release source admission must not become fail-open through continue-on-error")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: manual V26 release revalidates protected main immediately before publication")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
