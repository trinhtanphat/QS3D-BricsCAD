#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "assert-v26-host-reference-safety.ps1"
BUILD_WORKFLOW = ROOT / ".github" / "workflows" / "bricscad-v26.yml"
RELEASE_WORKFLOW = ROOT / ".github" / "workflows" / "release-v26.yml"

CALL = r"& .\scripts\assert-v26-host-reference-safety.ps1 -BricsCadDir $env:BRICSCAD_V26_DIR"
HELD_BUILD_CALL = r"& .\scripts\build-v26-with-stable-references.ps1"
HELD_BUILD_STATE = "-StatePath $env:V26_HOST_REFERENCE_STATE"
DIRECT_BUILD = "dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"
REPARSE_TOKEN = "[IO.FileAttributes]::ReparsePoint"
ROOTED_TOKEN = "if (-not [IO.Path]::IsPathRooted($trimmed))"
STATE_CAPTURE_TOKEN = "function Get-StableHostFileState"
STATE_ASSERT_TOKEN = "function Assert-StableHostFileState"
SECOND_CAPTURE_HASH_TOKEN = "$secondHash = Get-FileStreamSha256 -File $second -Label $Label"
CURRENT_VERIFY_HASH_TOKEN = "$currentHash = Get-FileStreamSha256 -File $current -Label $Label"
STATE_WRITE_TOKEN = "-StatePath $env:V26_HOST_REFERENCE_STATE"
STATE_VERIFY_TOKEN = "-VerifyStatePath $env:V26_HOST_REFERENCE_STATE"


def fail(message: str) -> None:
    raise SystemExit(f"FAIL: {message}")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"{label}: missing {token!r}")


def require_absent(text: str, token: str, label: str) -> None:
    if token in text:
        fail(f"{label}: forbidden stale token remains {token!r}")


def require_before(text: str, first: str, second: str, label: str) -> None:
    left = text.find(first)
    right = text.find(second)
    if left < 0 or right < 0 or left >= right:
        fail(f"{label}: expected {first!r} before {second!r}")


def require_between(text: str, token: str, after: str, before: str, label: str) -> None:
    left = text.find(after)
    right = text.find(before, left + 1)
    if left < 0 or right < 0:
        fail(f"{label}: missing boundary anchors")
    position = text.find(token, left + len(after), right)
    if position < 0:
        fail(f"{label}: missing {token!r} between {after!r} and {before!r}")


def validate_helper(text: str) -> None:
    component_guard = "Assert-NoExistingReparseComponent -Path $Path -Label $Label"
    second_resolve = "Get-RequiredOrdinaryFile -Path $first.FullName -Label $Label"
    for token in (
        "function Get-CanonicalAbsolutePath",
        ROOTED_TOKEN,
        "[IO.Path]::GetFullPath($trimmed)",
        "function Assert-NoExistingReparseComponent",
        REPARSE_TOKEN,
        "function Get-RequiredOrdinaryFile",
        "$item.PSIsContainer",
        "Assert-NoExistingReparseComponent -Path $canonicalDir -Label 'BricsCadDir'",
        component_guard,
        "Get-RequiredOrdinaryFile -Path $Path -Label $Label",
        second_resolve,
        SECOND_CAPTURE_HASH_TOKEN,
        CURRENT_VERIFY_HASH_TOKEN,
        "@('bricscad.exe', 'BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')",
        "$version.FileMajorPart -ne 26",
        STATE_CAPTURE_TOKEN,
        STATE_ASSERT_TOKEN,
        "[Security.Cryptography.SHA256]::Create()",
        "LastWriteTimeUtc.Ticks",
        "[IO.File]::Open(",
        "[IO.FileShare]::Read",
        "ConvertTo-Json",
        "ConvertFrom-Json",
    ):
        require(text, token, "V26 host-safety helper")

    require_before(text, ROOTED_TOKEN, "[IO.Path]::GetFullPath($trimmed)", "absolute root before canonicalization")
    if text.count(REPARSE_TOKEN) < 3:
        fail("V26 host-safety helper: expected reparse rejection at path-component, file-leaf, and host-directory boundaries")
    require_before(text, component_guard, "Get-RequiredOrdinaryFile -Path $Path -Label $Label", "reparse check before ordinary-file trust")
    require_before(text, second_resolve, SECOND_CAPTURE_HASH_TOKEN, "second ordinary-file resolve before second generation hash")
    require_before(text, "Get-RequiredOrdinaryFile -Path $Path -Label $Label", "$version = $versionFile.VersionInfo", "ordinary host files before version trust")
    require_before(text, STATE_CAPTURE_TOKEN, STATE_ASSERT_TOKEN, "stable-state capture before revalidation")


def validate_workflow(text: str, label: str, expected_runtime_verify_count: int) -> None:
    require(text, CALL, label)
    require(text, "BRICSCAD_V26_DIR", label)
    require(text, "V26_HOST_REFERENCE_STATE", label)
    require(text, STATE_WRITE_TOKEN, label)
    require(text, HELD_BUILD_CALL, label)
    require(text, HELD_BUILD_STATE, label)
    require_absent(text, DIRECT_BUILD, label)
    require_before(text, STATE_WRITE_TOKEN, HELD_BUILD_CALL, f"{label} capture before held-reference plugin build")

    actual_verify_count = text.count(STATE_VERIFY_TOKEN)
    if actual_verify_count < expected_runtime_verify_count:
        fail(f"{label}: expected at least {expected_runtime_verify_count} runtime host-generation revalidations, found {actual_verify_count}")

    runtime = "test-bricscad-v26-runtime.ps1"
    if runtime in text:
        require_between(text, STATE_VERIFY_TOKEN, HELD_BUILD_CALL, runtime, f"{label} runtime revalidation after held-reference plugin build")
    if label == "manual V26 release workflow":
        signed_runtime_anchor = "Real V26 runtime validation for signed release payload"
        require_between(text, STATE_VERIFY_TOKEN, signed_runtime_anchor, runtime, f"{label} signed-runtime revalidation")


def expect_rejected(validator, mutated: str, label: str) -> None:
    try:
        validator(mutated)
    except SystemExit:
        return
    fail(f"mutation probe accepted: {label}")


def remove_last(text: str, token: str) -> str:
    left, separator, right = text.rpartition(token)
    return left + right if separator else text


def main() -> None:
    helper = HELPER.read_text(encoding="utf-8")
    build = BUILD_WORKFLOW.read_text(encoding="utf-8")
    release = RELEASE_WORKFLOW.read_text(encoding="utf-8")

    validate_helper(helper)
    validate_workflow(build, "manual V26 build workflow", 1)
    validate_workflow(release, "manual V26 release workflow", 2)

    expect_rejected(validate_helper, helper.replace(ROOTED_TOKEN, "if ($false)", 1), "removed absolute-root rejection")
    expect_rejected(validate_helper, helper.replace(REPARSE_TOKEN, "[IO.FileAttributes]::Archive"), "removed reparse attribute checks")
    expect_rejected(validate_helper, helper.replace("$item.PSIsContainer", "$false", 1), "removed ordinary-file container rejection")
    expect_rejected(validate_helper, helper.replace(STATE_CAPTURE_TOKEN, "function Get-UnstableHostFileState", 1), "removed stable host-file capture")
    expect_rejected(validate_helper, helper.replace(STATE_ASSERT_TOKEN, "function Ignore-StableHostFileState", 1), "removed stable host-file revalidation")
    expect_rejected(validate_helper, helper.replace(SECOND_CAPTURE_HASH_TOKEN, "$secondHash = $firstHash", 1), "removed second-generation hash capture")
    expect_rejected(validate_helper, helper.replace(CURRENT_VERIFY_HASH_TOKEN, "$currentHash = [string]$Expected.Sha256", 1), "removed current-generation verification hash")
    expect_rejected(lambda text: validate_workflow(text, "manual V26 build workflow", 1), build.replace(HELD_BUILD_CALL, "& .\\scripts\\missing-held-build.ps1", 1), "removed build-workflow held-reference boundary")
    expect_rejected(lambda text: validate_workflow(text, "manual V26 release workflow", 2), release.replace(HELD_BUILD_CALL, "& .\\scripts\\missing-held-build.ps1", 1), "removed release-workflow held-reference boundary")
    expect_rejected(lambda text: validate_workflow(text, "manual V26 build workflow", 1), remove_last(build, STATE_VERIFY_TOKEN), "removed build-workflow runtime revalidation")
    expect_rejected(lambda text: validate_workflow(text, "manual V26 release workflow", 2), remove_last(release, STATE_VERIFY_TOKEN), "removed release-workflow signed-runtime revalidation")

    print("PASS V26 host reference path/generation-safety contract")
    print(" - both V26 workflows capture admitted host-reference generations and route plugin compilation through the held-reference wrapper")
    print(" - configured host roots must be absolute before canonicalization")
    print(" - host path components and required V26 leaves reject filesystem reparse aliases")
    print(" - required host leaves are independently re-resolved and re-hashed across admission/runtime consumption boundaries")


if __name__ == "__main__":
    main()
