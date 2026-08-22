#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"
PREPARE = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"
SYNC = ROOT / "scripts" / "sync-preview-release-version.ps1"
PACKAGE = ROOT / "scripts" / "package-v25.ps1"


def fail(message):
    print("ERROR:", message)
    return 1


def require_tokens(text, tokens, label):
    for token in tokens:
        if token not in text:
            raise ValueError(f"{label} lost required release-sync guard: {token}")


def main():
    try:
        workflow = WORKFLOW.read_text(encoding="utf-8")
        prepare = PREPARE.read_text(encoding="utf-8")
        sync = SYNC.read_text(encoding="utf-8")
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
                "git reset --hard",
                "git checkout --detach $releaseBase",
                "sync-preview-release-version.ps1",
                "preflight-runtime-product-version-identity.py",
                "Preview synchronization produced an unexpected Git status",
                "if ($entry.State -ne ' M')",
                "if ($entry.Path -notin $allowed)",
                "git diff --check",
                "git fetch --no-tags origin '+refs/heads/main:refs/remotes/origin/main'",
                "main moved after dispatch with release-relevant changes",
                "Release workspace HEAD must remain the protected-main source commit",
                "$latestMain = Get-RemoteMain",
                "main advanced through additional non-release paths while preparing the workspace",
                "No commit, push, branch-protection bypass, or main mutation was performed by release preparation.",
                "Write-Output $releaseBase",
            ],
            "V25 release preparation helper",
        )
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
                    f"V25 release preparation must keep preview synchronization workspace-only; forbidden primitive: {forbidden}"
                )

        anchors = [
            prepare.find("$initialStatus = @(Get-ReleaseStatusEntries)"),
            prepare.find("git checkout --detach $releaseBase"),
            prepare.find("sync-preview-release-version.ps1"),
            prepare.find("$status = @(Get-ReleaseStatusEntries)"),
            prepare.find("git diff --check"),
            prepare.find("Release workspace HEAD must remain the protected-main source commit"),
            prepare.find("$latestMain = Get-RemoteMain"),
            prepare.find("Write-Output $releaseBase"),
        ]
        if min(anchors) < 0 or anchors != sorted(anchors):
            raise ValueError(
                "V25 release preparation must start clean, select protected main, sync workspace-only identity, validate dirty allowlist, preserve HEAD, recheck main, then return source SHA"
            )

        expected_projects = [
            "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
            "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj",
            "src/QS3D.Core/QS3D.Core.csproj",
        ]
        for path in expected_projects:
            if path not in prepare or path not in sync:
                raise ValueError(f"preview release synchronization lost project path: {path}")

        require_tokens(
            sync,
            [
                "(?<preview>[1-9][0-9]*)$",
                "Replace-SingleProjectValue",
                "-Element 'Version'",
                "-Element 'FileVersion'",
                "-Element 'InformationalVersion'",
                "New-Object Text.UTF8Encoding($false)",
            ],
            "preview version sync helper",
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
        "PASS: V25 preview release sync requires a positive preview ordinal, excludes NuGet cache before status parsing, "
        "keeps version changes workspace-only on an exact protected-main HEAD, and publishes exact release-source provenance"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
