#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PREPARE = ROOT / "scripts/prepare-v25-cloud-release.ps1"
WORKFLOW = ROOT / ".github/workflows/release-v25-cloud.yml"


def main() -> int:
    failures = []
    prepare = PREPARE.read_text(encoding="utf-8")
    workflow = WORKFLOW.read_text(encoding="utf-8")

    forbidden_prepare = (
        "git push",
        "git commit",
        "git add",
        "git config user.name",
        "git config user.email",
        "HEAD:refs/heads/main",
    )
    for token in forbidden_prepare:
        if token.lower() in prepare.lower():
            failures.append(f"release preparation must not contain protected-main write primitive: {token}")

    required_prepare = (
        "sync-preview-release-version.ps1",
        "preflight-runtime-product-version-identity.py",
        "Release workspace HEAD must remain the protected-main source commit",
        "No commit, push, branch-protection bypass, or main mutation was performed by release preparation.",
        "Write-Output $releaseBase",
    )
    for token in required_prepare:
        if token not in prepare:
            failures.append(f"release preparation missing protection-safe contract marker: {token}")

    required_workflow = (
        "prepare-v25-cloud-release.ps1",
        '"RELEASE_COMMIT_SHA=$releaseCommit"',
        "target_commitish = $env:RELEASE_COMMIT_SHA",
        "PACKAGE-METADATA gitCommit must match exact release source commit",
    )
    for token in required_workflow:
        if token not in workflow:
            failures.append(f"V25 release workflow missing provenance contract marker: {token}")

    if "permissions:\n  contents: write" not in workflow:
        failures.append("release workflow must retain contents:write only for release/tag publication")

    if failures:
        print("V25 protected-main release preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V25 preview release preparation is protected-main safe.")
    print(" - preview version synchronization is workspace-only")
    print(" - source HEAD/provenance remains an exact protected-main commit")
    print(" - release/tag publication still targets RELEASE_COMMIT_SHA")
    return 0


if __name__ == "__main__":
    sys.exit(main())
