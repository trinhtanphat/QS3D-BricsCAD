#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PREPARE = ROOT / "scripts/prepare-v25-cloud-release.ps1"
RELEASE_WORKFLOW = ROOT / ".github/workflows/release-v25-cloud.yml"
SHARED_CI = ROOT / ".github/workflows/ci.yml"
ACQUIRE = ROOT / "scripts/acquire-v25-compile-references.ps1"


def require(text: str, tokens: tuple[str, ...], label: str, failures: list[str]) -> None:
    for token in tokens:
        if token not in text:
            failures.append(f"{label} missing contract marker: {token}")


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
        "refs/heads/main",
    )
    for token in forbidden_prepare:
        if token.lower() in prepare.lower():
            failures.append(f"release preparation must not contain protected-main write primitive: {token}")

    require(
        prepare,
        (
            "sync-preview-release-version.ps1",
            "preflight-runtime-product-version-identity.py",
            "Release workspace HEAD must remain the protected-main source commit",
            "No commit, push, branch-protection bypass, or main mutation was performed by release preparation.",
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
            "actions/cache/restore@v6",
            "acquire-v25-compile-references.ps1",
            "BRICSCAD_V25_PINNED_MSI_SHA256",
            "Validate BricsCAD V25 compile references",
            "dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64",
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
            "Get-FileHash",
            "Get-AuthenticodeSignature",
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

    if failures:
        print("V25 protected-main release/compile preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V25 preview release and pre-merge compile contracts are protected-main safe.")
    print(" - preview version synchronization is workspace-only")
    print(" - source HEAD/provenance remains an exact protected-main commit")
    print(" - canonical core check also compiles V25 against pinned, verified managed references")
    return 0


if __name__ == "__main__":
    sys.exit(main())
