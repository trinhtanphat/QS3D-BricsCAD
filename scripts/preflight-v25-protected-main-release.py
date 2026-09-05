#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PREPARE = ROOT / "scripts/prepare-v25-cloud-release.ps1"
RELEASE_WORKFLOW = ROOT / ".github/workflows/release-v25-cloud.yml"
SHARED_CI = ROOT / ".github/workflows/ci.yml"
ACQUIRE = ROOT / "scripts/acquire-v25-compile-references.ps1"

CHECKOUT_V7 = "actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1"
SETUP_DOTNET_V6 = "actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68 # v6"
CACHE_V6 = "actions/cache/restore@55cc8345863c7cc4c66a329aec7e433d2d1c52a9 # v6.1.0"


def require(text: str, tokens: tuple[str, ...], label: str, failures: list[str]) -> None:
    for token in tokens:
        if token not in text:
            failures.append(f"{label} missing contract marker: {token}")


def contains_executable_line(text: str, token: str) -> bool:
    token_lower = token.lower()
    return any(
        token_lower in line.lower()
        for line in text.splitlines()
        if not line.lstrip().startswith("#")
    )


def main() -> int:
    failures = []
    prepare = PREPARE.read_text(encoding="utf-8")
    release_workflow = RELEASE_WORKFLOW.read_text(encoding="utf-8")
    shared_ci = SHARED_CI.read_text(encoding="utf-8")
    acquire = ACQUIRE.read_text(encoding="utf-8")

    forbidden_prepare = (
        "git push",
        "git commit",
        "git add",
        "git config user.name",
        "git config user.email",
        "HEAD:refs/heads/main",
        "sync-preview-release-version.ps1",
        "git diff --name-only",
    )
    for token in forbidden_prepare:
        if contains_executable_line(prepare, token):
            failures.append(f"release preparation must not contain protected-main/unstable-source primitive: {token}")

    for stale in (
        "function Get-CommittedProductVersion",
        "$committedProductVersion = Get-CommittedProductVersion",
        "Merge the version update to protected main before publishing.",
    ):
        if stale in prepare:
            failures.append(f"release preparation retained stale committed-version admission: {stale}")

    require(
        prepare,
        (
            "validate-preview-release-sequence.ps1",
            "preflight-runtime-product-version-identity.py",
            "$workspaceVersionPaths = @(",
            "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj",
            "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj",
            "src/QS3D.Core/QS3D.Core.csproj",
            "function Set-WorkspaceProductVersion",
            "$productVersion = $tag.Substring(1)",
            "Set-WorkspaceProductVersion -ReleaseTagValue $tag",
            "Runtime product-version identity preflight failed after workspace synchronization.",
            "$expectedProductVersion = $tag.Substring(1)",
            "$finalStatus.Count -ne 0 -and $finalStatus.Count -ne $workspaceVersionPaths.Count",
            "Workspace version synchronization must either be a no-op or produce exactly three bounded project modifications.",
            "Workspace ProductVersion is already synchronized",
            "if ($finalStatus.Count -eq $workspaceVersionPaths.Count)",
            "Unexpected release-preparation workspace change",
            "$releaseRelevantPathspecs = @(",
            "external/QS3D-Platform",
            "git diff --quiet --no-ext-diff $range -- @releaseRelevantPathspecs",
            "Release workspace HEAD must remain the protected-main source commit",
            "No commit, push, branch-protection bypass, or protected-main mutation was performed by release preparation.",
            "Write-Output $releaseBase",
        ),
        "release preparation",
        failures,
    )

    require(
        release_workflow,
        (
            "prepare-v25-cloud-release.ps1",
            '"RELEASE_COMMIT_SHA=$releaseCommit"',
            "target_commitish = $env:RELEASE_COMMIT_SHA",
            "PACKAGE-METADATA gitCommit must match exact release source commit",
        ),
        "V25 release workflow",
        failures,
    )
    if "permissions:\n  contents: write" not in release_workflow:
        failures.append("release workflow must retain contents:write only for release/tag publication")

    require(
        shared_ci,
        (
            "  core:",
            "needs: preflight",
            "Run deterministic smoke tests",
            CHECKOUT_V7,
            SETUP_DOTNET_V6,
            CACHE_V6,
            "acquire-v25-compile-references.ps1",
            "BRICSCAD_V25_PINNED_MSI_SHA256",
            "Validate BricsCAD V25 compile references",
            "Build BricsCAD V25 plugin against locked reference generations",
            ".\\scripts\\build-v25-with-stable-references.ps1",
            "src\\QS3D.BricsCAD.V25\\QS3D.BricsCAD.V25.csproj",
        ),
        "canonical core branch/PR check",
        failures,
    )
    if "v25-compile:" in shared_ci:
        failures.append("V25 compile must stay inside canonical core status check instead of creating an unrequired third check")
    if "permissions:\n  contents: read" not in shared_ci:
        failures.append("shared branch/PR CI must remain read-only")

    require(
        acquire,
        (
            "function Open-PinnedMsiReadLock",
            "[IO.FileShare]::Read",
            "[Security.Cryptography.SHA256]::Create()",
            "$sha.ComputeHash($stream)",
            "function Test-PinnedMsiGeneration",
            "Invoke-WebRequest -Uri $candidate.Url -OutFile $staging",
            "[IO.File]::Move($staging, $msi)",
            "Test-PinnedMsiGeneration -Path $msi -Label 'Published BricsCAD V25 MSI'",
            "Get-AuthenticodeSignature -FilePath $msiState.Path",
            "WindowsInstaller.Installer",
            "ProductVersion",
            "ProductName",
            "BrxMgd.dll",
            "TD_Mgd.dll",
            "TD_MgdBrep.dll",
            "^25\\.2\\.10(?:\\.|$)",
            "Write-Output $bricsDir",
        ),
        "V25 managed-reference acquisition",
        failures,
    )
    if "Get-FileHash" in acquire:
        failures.append("V25 managed-reference acquisition must hash installer generations through held streams, not pathname Get-FileHash")
    if "Invoke-WebRequest -Uri $candidate.Url -OutFile $msi" in acquire:
        failures.append("V25 managed-reference acquisition must not download remote bytes directly to the canonical cache pathname")

    if failures:
        print("V25 protected-main release/compile preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V25 preview release and pre-merge compile contracts are protected-main safe.")
    print(" - manual preview identity may already be synchronized or is derived only in the bounded V25/V26/Core workspace; protected main is never mutated")
    print(" - source HEAD/provenance remains an exact protected-main commit and release drift uses Git pathspec semantics")
    print(" - canonical core check compiles V25 through locked, held-verified reference generations with immutable Action refs")
    return 0


if __name__ == "__main__":
    sys.exit(main())
