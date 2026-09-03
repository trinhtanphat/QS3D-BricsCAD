#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"
PREPARE = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"
PACKAGE = ROOT / "scripts" / "package-v25.ps1"


def fail(message):
    print("ERROR:", message)
    return 1


def require_tokens(text, tokens, label):
    for token in tokens:
        if token not in text:
            raise ValueError(f"{label} lost required committed-source guard: {token}")


def main():
    try:
        workflow = WORKFLOW.read_text(encoding="utf-8")
        prepare = PREPARE.read_text(encoding="utf-8")
        package = PACKAGE.read_text(encoding="utf-8")
    except OSError as exc:
        return fail(str(exc))

    try:
        require_tokens(
            workflow,
            [
                "workflow_dispatch:",
                "Prepare exact release source commit",
                ".\\scripts\\prepare-v25-cloud-release.ps1",
                "-ReleaseTag $env:RELEASE_TAG -DispatchSha $env:SOURCE_SHA",
                '"RELEASE_COMMIT_SHA=$releaseCommit"',
                "Package source HEAD must equal RELEASE_COMMIT_SHA",
                "PACKAGE-METADATA gitCommit must match exact release source commit",
                "target_commitish = $env:RELEASE_COMMIT_SHA",
                "Publish source HEAD must equal RELEASE_COMMIT_SHA",
                "Draft prerelease target commit mismatch",
            ],
            "V25 cloud workflow",
        )
        if "target_commitish = $env:GITHUB_SHA" in workflow:
            raise ValueError("V25 cloud workflow regressed to publishing the stale dispatch SHA")
        validate_pos = workflow.find("- name: Validate cloud prerelease request")
        prepare_pos = workflow.find("- name: Prepare exact release source commit")
        manual_gate_pos = workflow.find("- name: Manual-only CI policy gate")
        package_pos = workflow.find("- name: Build V25 preview package")
        publish_pos = workflow.find("- name: Publish GitHub prerelease")
        if min(validate_pos, prepare_pos, manual_gate_pos, package_pos, publish_pos) < 0 or not (
            validate_pos < prepare_pos < manual_gate_pos < package_pos < publish_pos
        ):
            raise ValueError("V25 cloud workflow release preparation/order regressed")

        require_tokens(
            prepare,
            [
                "Get-ReleaseStatusEntries",
                "git status --porcelain=v1 --untracked-files=all -- . ':(exclude).nuget/packages/**'",
                "Release preparation must start from a clean checkout/index",
                "$releaseRelevantPathspecs = @(",
                "external/QS3D-Platform",
                "git diff --quiet --no-ext-diff $range -- @releaseRelevantPathspecs",
                "git reset --hard",
                "git checkout --detach $releaseBase",
                "preflight-runtime-product-version-identity.py",
                "function Get-CommittedProductVersion",
                "Committed V25 project must contain exactly one unambiguous Version value",
                "requires tag '$expectedReleaseTag'",
                "git diff --check",
                "git fetch --no-tags origin '+refs/heads/main:refs/remotes/origin/main'",
                "main moved after dispatch with release-relevant changes",
                "Release workspace HEAD must remain the protected-main source commit",
                "$latestMain = Get-RemoteMain",
                "main advanced through additional non-release paths while validating committed release source",
                "No commit, push, branch-protection bypass, workspace-only version rewrite, or main mutation was performed by release preparation.",
                "Write-Output $releaseBase",
            ],
            "V25 release preparation helper",
        )
        if "sync-preview-release-version.ps1" in prepare:
            raise ValueError("V25 release preparation must not rewrite preview identity only in the workspace")
        if "git diff --name-only" in prepare:
            raise ValueError("V25 release preparation must not classify release drift from line-oriented pathname output")
        if "Test-IsExpectedNuGetCachePath" in prepare:
            raise ValueError("V25 release preparation must exclude NuGet cache before status parsing")
        for forbidden in (
            "git add",
            "git commit",
            "git push",
            "HEAD:refs/heads/main",
        ):
            if forbidden.lower() in prepare.lower():
                raise ValueError(
                    f"V25 release preparation must keep protected main read-only; forbidden primitive: {forbidden}"
                )

        anchors = [
            prepare.find("$initialStatus = @(Get-ReleaseStatusEntries)"),
            prepare.find("git checkout --detach $releaseBase"),
            prepare.find("preflight-runtime-product-version-identity.py"),
            prepare.find("$committedProductVersion = Get-CommittedProductVersion"),
            prepare.find("git diff --check"),
            prepare.find("Release workspace HEAD must remain the protected-main source commit"),
            prepare.find("$latestMain = Get-RemoteMain"),
            prepare.find("Write-Output $releaseBase"),
        ]
        if min(anchors) < 0 or anchors != sorted(anchors):
            raise ValueError(
                "V25 release preparation must start clean, select protected main, validate committed identity, preserve HEAD, recheck main, then return source SHA"
            )

        require_tokens(
            package,
            [
                "$expectedTag = 'v' + $productVersion",
                "RELEASE_TAG must exactly match the source product version",
                "gitCommit = $gitCommit",
            ],
            "V25 package guard",
        )
    except ValueError as exc:
        return fail(str(exc))

    print(
        "PASS: V25 preview release requires a clean committed product identity on an exact protected-main HEAD, "
        "uses pathname-safe release-drift admission, and publishes exact release-source provenance without workspace-only version rewriting"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
