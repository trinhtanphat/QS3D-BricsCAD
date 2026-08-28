#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "acquire-v25-compile-references.ps1"
CLOUD_WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise SystemExit(f"FAIL: {label}: forbidden duplicate inline token {token!r}")


def require_before(text: str, first: str, second: str, label: str) -> None:
    left = text.find(first)
    right = text.find(second)
    if left < 0 or right < 0 or left >= right:
        raise SystemExit(f"FAIL: {label}: expected {first!r} before {second!r}")


def validate_helper(text: str) -> None:
    require(text, "function Assert-NoExistingReparseComponent", "reparse helper")
    require(text, "[IO.FileAttributes]::ReparsePoint", "reparse attribute check")
    require(text, "function Get-OrdinaryFileOrNull", "ordinary-file helper")
    require(text, "Assert-NoExistingReparseComponent -Path $cacheDir", "cache path guard")
    require(text, "Assert-NoExistingReparseComponent -Path $msi", "MSI path guard")
    require(text, "Assert-NoExistingReparseComponent -Path $extract", "extract path guard")
    require(text, "$item = Get-OrdinaryFileOrNull -Path $msi", "MSI ordinary-file trust")
    require(text, "Get-FileHash -LiteralPath $msi -Algorithm SHA256", "pinned hash")
    require(text, "Get-AuthenticodeSignature -FilePath $msi", "Authenticode validation")
    require(text, "ProductVersion", "MSI version validation")
    require(text, "ProductName", "MSI name validation")
    require(text, "$process.WaitForExit(900000)", "bounded extraction")
    require(text, "Stop-OwnedProcessTree -Process $process", "PID-scoped cleanup")

    destructive = "Remove-Item -LiteralPath $extract -Recurse -Force"
    require_before(text, "Assert-NoExistingReparseComponent -Path $cacheDir", destructive,
                   "cache reparse guard before recursive cleanup")
    require_before(text, "Assert-NoExistingReparseComponent -Path $msi", destructive,
                   "MSI reparse guard before recursive cleanup")
    require_before(text, "Assert-NoExistingReparseComponent -Path $extract", destructive,
                   "extract reparse guard before recursive cleanup")
    require_before(text, "$item = Get-OrdinaryFileOrNull -Path $msi", "Get-FileHash -LiteralPath $msi",
                   "ordinary MSI check before hash trust")


def acquisition_step(workflow: str) -> str:
    start_token = "      - name: Acquire BricsCAD V25 compile references"
    end_token = "      - name: Save BricsCAD V25 installer cache"
    start = workflow.find(start_token)
    end = workflow.find(end_token, start + 1)
    if start < 0 or end < 0 or start >= end:
        raise SystemExit("FAIL: cloud acquisition step boundary is missing or malformed")
    return workflow[start:end]


def validate_cloud_workflow(workflow: str) -> None:
    step = acquisition_step(workflow)
    helper = ".\\scripts\\acquire-v25-compile-references.ps1"
    require(step, helper, "cloud shared-helper call")
    require(step, "-MsiPath $msi", "cloud MSI binding")
    require(step, "-ExtractDir $extract", "cloud extract binding")
    require(step, "-ExpectedSha256 $env:BRICSCAD_V25_PINNED_MSI_SHA256", "cloud digest binding")
    require(step, "-MirrorUrl $env:BRICSCAD_V25_MIRROR_MSI_URL", "cloud mirror binding")
    require(step, "-PublicUrl $env:BRICSCAD_V25_PUBLIC_MSI_URL", "cloud public URL binding")
    require(step, "-FallbackUrl $env:BRICSCAD_V25_MSI_URL", "cloud fallback binding")
    require(step, '"BRICSCAD_V25_DIR=$bricsDir"', "cloud reference-directory publication")
    require_before(step, helper, '"BRICSCAD_V25_DIR=$bricsDir"', "helper before reference publication")

    # The cloud workflow must not grow a second acquisition implementation. In
    # particular, destructive extraction cleanup, direct MSI trust, and MSI
    # process ownership all belong to the shared helper above.
    forbid(step, "Remove-Item -LiteralPath $extract -Recurse", "cloud recursive cleanup")
    forbid(step, "Get-AuthenticodeSignature -FilePath $msi", "cloud duplicate signer trust")
    forbid(step, "Start-Process -FilePath msiexec.exe", "cloud duplicate MSI extraction")
    forbid(step, "Get-FileHash -LiteralPath $msi", "cloud duplicate MSI hash trust")


def expect_rejected(validator, original: str, mutated: str, label: str) -> None:
    try:
        validator(mutated)
    except SystemExit:
        return
    raise SystemExit(f"FAIL: mutation probe was accepted: {label}")


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    workflow = CLOUD_WORKFLOW.read_text(encoding="utf-8")
    validate_helper(text)
    validate_cloud_workflow(workflow)

    expect_rejected(
        validate_helper,
        text,
        text.replace("Assert-NoExistingReparseComponent -Path $extract -Label 'ExtractDir'", "# removed", 1),
        "removed extract reparse guard",
    )
    expect_rejected(
        validate_helper,
        text,
        text.replace("$item = Get-OrdinaryFileOrNull -Path $msi -Label 'BricsCAD V25 MSI'", "$item = Get-Item -LiteralPath $msi", 1),
        "removed ordinary MSI trust boundary",
    )
    expect_rejected(
        validate_helper,
        text,
        text.replace("Assert-NoExistingReparseComponent -Path $cacheDir -Label 'MSI cache directory'", "# delayed cache guard", 1)
            .replace(destructive := "Remove-Item -LiteralPath $extract -Recurse -Force -ErrorAction SilentlyContinue",
                     destructive + "\nAssert-NoExistingReparseComponent -Path $cacheDir -Label 'MSI cache directory'", 1),
        "moved cache guard after recursive cleanup",
    )

    helper = ".\\scripts\\acquire-v25-compile-references.ps1"
    expect_rejected(
        validate_cloud_workflow,
        workflow,
        workflow.replace(helper, ".\\scripts\\removed-acquisition-helper.ps1", 1),
        "removed cloud shared-helper call",
    )
    expect_rejected(
        validate_cloud_workflow,
        workflow,
        workflow.replace("-ExpectedSha256 $env:BRICSCAD_V25_PINNED_MSI_SHA256", "-ExpectedSha256 '0'", 1),
        "changed cloud pinned-digest binding",
    )
    expect_rejected(
        validate_cloud_workflow,
        workflow,
        workflow.replace("-ExtractDir $extract", "-ExtractDir $env:GITHUB_WORKSPACE", 1),
        "changed cloud extract binding",
    )
    step_start = workflow.find("      - name: Acquire BricsCAD V25 compile references")
    expect_rejected(
        validate_cloud_workflow,
        workflow,
        workflow[:step_start] + workflow[step_start:].replace(
            "          $ErrorActionPreference = 'Stop'",
            "          $ErrorActionPreference = 'Stop'\n          Remove-Item -LiteralPath $extract -Recurse -Force",
            1,
        ),
        "reintroduced cloud inline recursive cleanup",
    )

    print("PASS V25 compile-reference acquisition path/cache and cloud parity contract")


if __name__ == "__main__":
    main()
