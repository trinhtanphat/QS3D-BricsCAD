#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "assert-v26-host-reference-safety.ps1"
BUILD_WORKFLOW = ROOT / ".github" / "workflows" / "bricscad-v26.yml"
RELEASE_WORKFLOW = ROOT / ".github" / "workflows" / "release-v26.yml"

CALL = r"& .\scripts\assert-v26-host-reference-safety.ps1 -BricsCadDir $env:BRICSCAD_V26_DIR"


def fail(message: str) -> None:
    raise SystemExit(f"FAIL: {message}")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"{label}: missing {token!r}")


def require_before(text: str, first: str, second: str, label: str) -> None:
    left = text.find(first)
    right = text.find(second)
    if left < 0 or right < 0 or left >= right:
        fail(f"{label}: expected {first!r} before {second!r}")


def validate_helper(text: str) -> None:
    for token in (
        "function Get-CanonicalAbsolutePath",
        "[IO.Path]::GetFullPath",
        "function Assert-NoExistingReparseComponent",
        "[IO.FileAttributes]::ReparsePoint",
        "function Get-RequiredOrdinaryFile",
        "$item.PSIsContainer",
        "Assert-NoExistingReparseComponent -Path $canonicalDir -Label 'BricsCadDir'",
        "Assert-NoExistingReparseComponent -Path $path -Label $name",
        "Get-RequiredOrdinaryFile -Path $path -Label $name",
        "@('bricscad.exe', 'BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')",
        "$version.FileMajorPart -ne 26",
    ):
        require(text, token, "V26 host-safety helper")

    require_before(
        text,
        "Assert-NoExistingReparseComponent -Path $path -Label $name",
        "Get-RequiredOrdinaryFile -Path $path -Label $name",
        "reparse check before ordinary-file trust",
    )
    require_before(
        text,
        "Get-RequiredOrdinaryFile -Path $path -Label $name",
        "$version = $required['bricscad.exe'].VersionInfo",
        "ordinary host files before version trust",
    )


def validate_workflow(text: str, label: str) -> None:
    require(text, CALL, label)
    require(text, "BRICSCAD_V26_DIR", label)
    require(text, "dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj", label)
    require_before(text, CALL, "dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj", f"{label} safety before plugin build")
    if "test-bricscad-v26-runtime.ps1" in text:
        require_before(text, CALL, "test-bricscad-v26-runtime.ps1", f"{label} safety before runtime")


def expect_rejected(validator, original: str, mutated: str, label: str) -> None:
    try:
        validator(mutated)
    except SystemExit:
        return
    fail(f"mutation probe accepted: {label}")


def main() -> None:
    helper = HELPER.read_text(encoding="utf-8")
    build = BUILD_WORKFLOW.read_text(encoding="utf-8")
    release = RELEASE_WORKFLOW.read_text(encoding="utf-8")

    validate_helper(helper)
    validate_workflow(build, "manual V26 build workflow")
    validate_workflow(release, "manual V26 release workflow")

    expect_rejected(
        validate_helper,
        helper,
        helper.replace("[IO.FileAttributes]::ReparsePoint", "[IO.FileAttributes]::Archive", 1),
        "removed reparse attribute check",
    )
    expect_rejected(
        validate_helper,
        helper,
        helper.replace("$item.PSIsContainer", "$false", 1),
        "removed ordinary-file container rejection",
    )
    expect_rejected(
        lambda text: validate_workflow(text, "manual V26 build workflow"),
        build,
        build.replace(CALL, "# removed V26 host path safety", 1),
        "removed build-workflow host safety call",
    )
    expect_rejected(
        lambda text: validate_workflow(text, "manual V26 release workflow"),
        release,
        release.replace(CALL, "# removed V26 host path safety", 1),
        "removed release-workflow host safety call",
    )

    print("PASS V26 host reference path-safety contract")
    print(" - both V26 workflows fail closed through the shared helper before plugin build/runtime")
    print(" - host path components and required V26 leaves reject filesystem reparse aliases")
    print(" - required host leaves are ordinary files before V26 version trust")


if __name__ == "__main__":
    main()
