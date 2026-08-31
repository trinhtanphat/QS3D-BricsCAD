#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "v26-release-verification-workspace.ps1"
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26.yml"

CREATE_CALL = r".\scripts\v26-release-verification-workspace.ps1 -Operation Create"
CHILD_CALL = r".\scripts\v26-release-verification-workspace.ps1 -Operation Child"
CLEANUP_CALL = r".\scripts\v26-release-verification-workspace.ps1 -Operation Cleanup"
HELD_HASH_CALL = r".\scripts\verify-v26-held-file.ps1 -Operation Hash -Path $downloadedAsset"


def fail(message: str) -> None:
    raise SystemExit(f"FAIL: {message}")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"{label}: missing {token!r}")


def require_before(text: str, first: str, second: str, label: str) -> None:
    a = text.find(first)
    b = text.find(second)
    if a < 0 or b < 0 or a >= b:
        fail(f"{label}: expected {first!r} before {second!r}")


def validate_helper(text: str) -> None:
    for token in (
        "[ValidateSet('Create', 'Child', 'Cleanup')]",
        "[IO.Path]::IsPathRooted($trimmed)",
        "[IO.Path]::GetFullPath($trimmed)",
        "function Assert-NoExistingReparseComponent",
        "[IO.FileAttributes]::ReparsePoint",
        "$WorkspacePrefix = 'qs3d-v26-release-verify-'",
        "[Guid]::NewGuid().ToString('N')",
        "function Get-OwnedWorkspace",
        "^asset-[0-9a-f]{32}$",
        "Verification child path escaped the owned workspace.",
        "refusing recursive cleanup",
        "Remove-Item -LiteralPath $owned -Recurse -Force",
    ):
        require(text, token, "V26 verification workspace helper")
    require_before(text, "[IO.Path]::IsPathRooted($trimmed)", "[IO.Path]::GetFullPath($trimmed)", "absolute root before canonicalization")
    require_before(text, "Assert-NoExistingReparseComponent -Path $canonical", "[IO.Directory]::CreateDirectory($candidate)", "trusted root before workspace creation")
    require_before(text, "refusing recursive cleanup", "Remove-Item -LiteralPath $owned -Recurse -Force", "cleanup provenance before recursion")


def validate_workflow(text: str) -> None:
    for token in (
        CREATE_CALL,
        CHILD_CALL,
        CLEANUP_CALL,
        "Invoke-WebRequest",
        "-OutFile $downloadedAsset",
        HELD_HASH_CALL,
        "Uploaded V26 release asset SHA-256 mismatch",
    ):
        require(text, token, "V26 release workflow")
    if "Get-FileHash -LiteralPath $downloadedAsset" in text:
        fail("V26 release workflow: downloaded asset hash reopens the verification workspace child by pathname")
    require_before(text, CREATE_CALL, "Invoke-WebRequest", "workspace acquisition before remote write")
    require_before(text, CHILD_CALL, "Invoke-WebRequest", "owned child before remote write")
    require_before(text, "-OutFile $downloadedAsset", HELD_HASH_CALL, "remote write before held-generation hash")
    require_before(text, HELD_HASH_CALL, CLEANUP_CALL, "held-generation hash before owned cleanup")
    if "$verificationRoot = if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP))" in text:
        fail("V26 release workflow: raw RUNNER_TEMP verification-root fallback remains")
    if "Join-Path $verificationRoot ('qs3d-v26-release-asset-'" in text:
        fail("V26 release workflow: raw verification-root asset child remains")


def rejected(validator, mutated: str, label: str) -> None:
    try:
        validator(mutated)
    except SystemExit:
        return
    fail(f"mutation probe accepted: {label}")


def main() -> None:
    helper = HELPER.read_text(encoding="utf-8")
    workflow = WORKFLOW.read_text(encoding="utf-8")

    validate_helper(helper)
    validate_workflow(workflow)

    rejected(validate_helper, helper.replace("[IO.Path]::IsPathRooted($trimmed)", "$true", 1), "removed absolute-root check")
    rejected(validate_helper, helper.replace("[IO.FileAttributes]::ReparsePoint", "[IO.FileAttributes]::Archive"), "removed reparse checks")
    rejected(validate_helper, helper.replace("refusing recursive cleanup", "unsafe cleanup allowed", 1), "removed cleanup provenance marker")
    rejected(validate_workflow, workflow.replace(CREATE_CALL, "# create removed", 1), "removed workspace create")
    rejected(validate_workflow, workflow.replace(CHILD_CALL, "# child removed", 1), "removed owned child derivation")
    rejected(validate_workflow, workflow.replace(CLEANUP_CALL, "# cleanup removed", 1), "removed owned cleanup")
    rejected(
        validate_workflow,
        workflow.replace(HELD_HASH_CALL, "Get-FileHash -LiteralPath $downloadedAsset -Algorithm SHA256", 1),
        "reintroduced pathname hash for downloaded release asset",
    )

    print("PASS V26 release verification workspace safety contract")
    print(" - verification temp root is absolute/non-reparse before workspace creation")
    print(" - remote asset bytes are written only to a fresh owned nonce workspace")
    print(" - downloaded asset verification consumes a held generation before owned cleanup")
    print(" - recursive cleanup is provenance-checked and limited to that owned workspace")


if __name__ == "__main__":
    main()
