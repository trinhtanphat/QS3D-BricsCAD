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
        raise SystemExit(f"FAIL: {label}: forbidden token {token!r}")


def require_before(text: str, first: str, second: str, label: str) -> None:
    left = text.find(first)
    right = text.find(second)
    if left < 0 or right < 0 or left >= right:
        raise SystemExit(f"FAIL: {label}: expected {first!r} before {second!r}")


def validate_helper(text: str) -> None:
    require(text, "function Assert-NoExistingReparseComponent", "reparse helper")
    require(text, "[IO.FileAttributes]::ReparsePoint", "reparse attribute check")
    require(text, "function Get-OrdinaryFileOrNull", "ordinary-file helper")
    require(text, "function Open-PinnedMsiReadLock", "held MSI helper")
    require(text, "function Test-PinnedMsiGeneration", "held admission helper")
    require(text, "[IO.FileShare]::Read", "non-replaceable held read")
    require(text, "$sha.ComputeHash($stream)", "held SHA-256")
    require(text, "Assert-NoExistingReparseComponent -Path $cacheDir", "cache path guard")
    require(text, "Assert-NoExistingReparseComponent -Path $msi", "MSI path guard")
    require(text, "Assert-NoExistingReparseComponent -Path $extract", "extract path guard")
    require(text, "if (Test-Path -LiteralPath $extract)", "fresh ExtractDir absence guard")
    require(text, "ExtractDir unexpectedly already exists; refusing pathname reuse", "fresh ExtractDir refusal diagnostic")
    require(text, "New-Item -ItemType Directory -Path $extract | Out-Null", "non-Force fresh ExtractDir creation")
    require(text, "Invoke-WebRequest -Uri $candidate.Url -OutFile $staging", "isolated staged download")
    require(text, "Test-PinnedMsiGeneration -Path $staging", "staged held admission")
    require(text, "[IO.File]::Move($staging, $msi)", "canonical publication")
    require(text, "Test-PinnedMsiGeneration -Path $msi -Label 'Published BricsCAD V25 MSI'", "post-publication held admission")
    require(text, "$msiState = Open-PinnedMsiReadLock -Path $msi", "final held trust boundary")
    require(text, "Get-AuthenticodeSignature -FilePath $msiState.Path", "held-path Authenticode validation")
    require(text, "$database = $installer.OpenDatabase($msiState.Path, 0)", "held-path MSI metadata validation")
    require(text, "ProductVersion", "MSI version validation")
    require(text, "ProductName", "MSI name validation")
    require(text, "$process.WaitForExit(900000)", "bounded extraction")
    require(text, "Stop-OwnedProcessTree -Process $process", "PID-scoped cleanup")

    forbid(text, "Get-FileHash", "pathname MSI hash")
    forbid(text, "[IO.FileShare]::ReadWrite", "write-share MSI generation lock")
    forbid(text, "[IO.FileShare]::Delete", "delete-share MSI generation lock")
    forbid(text, "Invoke-WebRequest -Uri $candidate.Url -OutFile $msi", "direct canonical download")
    forbid(text, "Get-AuthenticodeSignature -FilePath $msi\n", "unheld MSI Authenticode validation")
    forbid(text, "Remove-Item -LiteralPath $extract -Recurse", "recursive ExtractDir pathname cleanup")
    forbid(text, "New-Item -ItemType Directory -Path $extract -Force", "Force-based ExtractDir reuse")

    absent = "if (Test-Path -LiteralPath $extract)"
    create = "New-Item -ItemType Directory -Path $extract | Out-Null"
    require_before(text, "Assert-NoExistingReparseComponent -Path $cacheDir", absent,
                   "cache reparse guard before fresh-root admission")
    require_before(text, "Assert-NoExistingReparseComponent -Path $msi", absent,
                   "MSI reparse guard before fresh-root admission")
    require_before(text, "Assert-NoExistingReparseComponent -Path $extract", absent,
                   "extract reparse guard before fresh-root admission")
    require_before(text, absent, create, "existing ExtractDir refusal before non-Force creation")
    require_before(text, "Invoke-WebRequest -Uri $candidate.Url -OutFile $staging",
                   "Test-PinnedMsiGeneration -Path $staging",
                   "staged download before held admission")
    require_before(text, "Test-PinnedMsiGeneration -Path $staging",
                   "[IO.File]::Move($staging, $msi)",
                   "held staging admission before publication")
    require_before(text, "[IO.File]::Move($staging, $msi)",
                   "Test-PinnedMsiGeneration -Path $msi -Label 'Published BricsCAD V25 MSI'",
                   "publication before canonical re-admission")
    require_before(text, "$msiState = Open-PinnedMsiReadLock -Path $msi",
                   "Get-AuthenticodeSignature -FilePath $msiState.Path",
                   "final held admission before Authenticode")


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

    # The cloud workflow must not grow a second acquisition implementation.
    forbid(step, "Remove-Item -LiteralPath $extract -Recurse", "cloud recursive cleanup")
    forbid(step, "Get-AuthenticodeSignature -FilePath $msi", "cloud duplicate signer trust")
    forbid(step, "Start-Process -FilePath msiexec.exe", "cloud duplicate MSI extraction")
    forbid(step, "Get-FileHash -LiteralPath $msi", "cloud duplicate MSI hash trust")
    forbid(step, "Invoke-WebRequest", "cloud duplicate MSI download")


def expect_rejected(validator, mutated: str, label: str) -> None:
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
        text.replace("Assert-NoExistingReparseComponent -Path $extract -Label 'ExtractDir'", "# removed", 1),
        "removed extract reparse guard",
    )
    expect_rejected(
        validate_helper,
        text.replace("[IO.FileShare]::Read", "[IO.FileShare]::ReadWrite", 1),
        "weakened held sharing mode",
    )
    expect_rejected(
        validate_helper,
        text.replace("$sha.ComputeHash($stream)", "Get-FileHash -LiteralPath $Path", 1),
        "reintroduced pathname hash",
    )
    expect_rejected(
        validate_helper,
        text.replace("Invoke-WebRequest -Uri $candidate.Url -OutFile $staging",
                     "Invoke-WebRequest -Uri $candidate.Url -OutFile $msi", 1),
        "downloaded directly to canonical MSI",
    )
    expect_rejected(
        validate_helper,
        text.replace("Test-PinnedMsiGeneration -Path $staging", "# removed staged held admission", 1),
        "removed staged held admission",
    )
    expect_rejected(
        validate_helper,
        text.replace("Test-PinnedMsiGeneration -Path $msi -Label 'Published BricsCAD V25 MSI'",
                     "# removed post-publication held admission", 1),
        "removed canonical post-publication admission",
    )
    expect_rejected(
        validate_helper,
        text.replace("Get-AuthenticodeSignature -FilePath $msiState.Path",
                     "Get-AuthenticodeSignature -FilePath $msi", 1),
        "detached Authenticode from final held state",
    )
    expect_rejected(
        validate_helper,
        text.replace("if (Test-Path -LiteralPath $extract)", "if ($false)", 1),
        "removed fresh ExtractDir absence guard",
    )
    expect_rejected(
        validate_helper,
        text.replace("New-Item -ItemType Directory -Path $extract | Out-Null",
                     "New-Item -ItemType Directory -Path $extract -Force | Out-Null", 1),
        "made ExtractDir creation reusable with Force",
    )
    fresh_create = "New-Item -ItemType Directory -Path $extract | Out-Null"
    cache_guard = "Assert-NoExistingReparseComponent -Path $cacheDir -Label 'MSI cache directory'"
    expect_rejected(
        validate_helper,
        text.replace(cache_guard, "# delayed cache guard", 1)
            .replace(fresh_create, fresh_create + "\n" + cache_guard, 1),
        "moved cache reparse guard after fresh-root creation",
    )
    expect_rejected(
        validate_helper,
        text.replace(fresh_create,
                     "Remove-Item -LiteralPath $extract -Recurse -Force\n" + fresh_create, 1),
        "reintroduced recursive pathname cleanup",
    )

    helper = ".\\scripts\\acquire-v25-compile-references.ps1"
    expect_rejected(
        validate_cloud_workflow,
        workflow.replace(helper, ".\\scripts\\removed-acquisition-helper.ps1", 1),
        "removed cloud shared-helper call",
    )
    expect_rejected(
        validate_cloud_workflow,
        workflow.replace("-ExpectedSha256 $env:BRICSCAD_V25_PINNED_MSI_SHA256", "-ExpectedSha256 '0'", 1),
        "changed cloud pinned-digest binding",
    )
    expect_rejected(
        validate_cloud_workflow,
        workflow.replace("-ExtractDir $extract", "-ExtractDir $env:GITHUB_WORKSPACE", 1),
        "changed cloud extract binding",
    )
    step_start = workflow.find("      - name: Acquire BricsCAD V25 compile references")
    expect_rejected(
        validate_cloud_workflow,
        workflow[:step_start] + workflow[step_start:].replace(
            "          $ErrorActionPreference = 'Stop'",
            "          $ErrorActionPreference = 'Stop'\n          Remove-Item -LiteralPath $extract -Recurse -Force",
            1,
        ),
        "reintroduced cloud inline recursive cleanup",
    )

    print("PASS V25 compile-reference fresh-root/path/cache and cloud parity contract")


if __name__ == "__main__":
    main()
