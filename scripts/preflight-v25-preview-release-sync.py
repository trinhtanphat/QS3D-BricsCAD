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
            raise ValueError(f"{label} lost required guard: {token}")


def contains_executable_line(text, token):
    return any(token.lower() in line.lower() for line in text.splitlines() if not line.lstrip().startswith("#"))


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
                "$workspaceVersionPaths = @(",
                "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
                "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj",
                "src/QS3D.Core/QS3D.Core.csproj",
                "function Set-WorkspaceProductVersion",
                "$productVersion = $tag.Substring(1)",
                "Set-ProjectVersionValue -Name 'Version' -Value $productVersion",
                "Set-ProjectVersionValue -Name 'FileVersion' -Value $fileVersion",
                "Set-ProjectVersionValue -Name 'InformationalVersion' -Value $productVersion",
                "Set-WorkspaceProductVersion -ReleaseTagValue $tag",
                "Runtime product-version identity preflight failed after workspace synchronization.",
                "$expectedProductVersion = $tag.Substring(1)",
                "$finalStatus.Count -ne 0 -and $finalStatus.Count -ne $workspaceVersionPaths.Count",
                "Workspace version synchronization must either be a no-op or produce exactly three bounded project modifications.",
                "Workspace ProductVersion is already synchronized",
                "if ($finalStatus.Count -eq $workspaceVersionPaths.Count)",
                "Unexpected release-preparation workspace change",
                "git diff --check",
                "git fetch --no-tags origin '+refs/heads/main:refs/remotes/origin/main'",
                "main moved after dispatch with release-relevant changes",
                "Release workspace HEAD must remain the protected-main source commit",
                "$latestMain = Get-RemoteMain",
                "main advanced through additional non-release paths while validating release source",
                "No commit, push, branch-protection bypass, or protected-main mutation was performed by release preparation.",
                "Write-Output $releaseBase",
            ],
            "V25 release preparation helper",
        )
        for stale in (
            "function Get-CommittedProductVersion",
            "Merge the version update to protected main before publishing.",
            "No commit, push, branch-protection bypass, workspace-only version rewrite, or main mutation was performed by release preparation.",
        ):
            if stale in prepare:
                raise ValueError(f"V25 release preparation retained stale committed-version contract: {stale}")
        if "sync-preview-release-version.ps1" in prepare:
            raise ValueError("V25 release preparation must keep workspace synchronization bounded inside prepare helper")
        if contains_executable_line(prepare, "git diff --name-only"):
            raise ValueError("V25 release preparation must not classify release drift from line-oriented pathname output")
        if "Test-IsExpectedNuGetCachePath" in prepare:
            raise ValueError("V25 release preparation must exclude NuGet cache before status parsing")
        for forbidden in ("git add", "git commit", "git push", "HEAD:refs/heads/main"):
            if contains_executable_line(prepare, forbidden):
                raise ValueError(f"V25 release preparation must keep protected main read-only: {forbidden}")

        anchors = [
            prepare.find("$initialStatus = @(Get-ReleaseStatusEntries)"),
            prepare.find("git checkout --detach $releaseBase"),
            prepare.find("preflight-runtime-product-version-identity.py"),
            prepare.find("Set-WorkspaceProductVersion -ReleaseTagValue $tag"),
            prepare.find("Runtime product-version identity preflight failed after workspace synchronization."),
            prepare.find("$expectedProductVersion = $tag.Substring(1)"),
            prepare.find("git diff --check"),
            prepare.find("Release workspace HEAD must remain the protected-main source commit"),
            prepare.find("$finalStatus = @(Get-ReleaseStatusEntries)"),
            prepare.find("$latestMain = Get-RemoteMain"),
            prepare.find("Write-Output $releaseBase"),
        ]
        if min(anchors) < 0 or anchors != sorted(anchors):
            raise ValueError(
                "V25 release preparation must start clean, select protected main, synchronize bounded workspace identity, validate it, preserve HEAD, bound dirty paths, recheck main, then return source SHA"
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
        "PASS: V25 preview release accepts an already-synchronized requested preview identity or derives it only in the bounded V25/V26/Core workspace, preserves exact protected-main HEAD provenance, and packages only the synchronized tag identity"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
