#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/release-v25-cloud.yml"
DOC = ROOT / "docs/CLOUD-V25-PREVIEW-RELEASE.md"
PINNED_SHA256 = "F44DF674C0E165D96BF579E243B20A8301E3F395F929779F47BF39A7D9DACDE1"
errors = []

if not WORKFLOW.is_file():
    errors.append("missing .github/workflows/release-v25-cloud.yml")
else:
    text = WORKFLOW.read_text(encoding="utf-8")
    required = (
        "workflow_dispatch:",
        "github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE'",
        "actions/checkout@v7",
        "actions/setup-python@v7",
        "actions/setup-dotnet@v6",
        "actions/cache/restore@v6",
        "actions/cache/save@v6",
        "actions/upload-artifact@v7",
        "BRICSCAD_V25_MIRROR_MSI_URL:",
        "BRICSCAD_V25_PUBLIC_MSI_URL:",
        "BRICSCAD_V25_PINNED_MSI_SHA256: " + PINNED_SHA256,
        "BRICSCAD_V25_MSI_SHA256: ${{ vars.BRICSCAD_V25_MSI_SHA256 }}",
        "bricscad-v25.2.10-x64-en-us-${{ env.BRICSCAD_V25_PINNED_MSI_SHA256 }}",
        "Name = 'pinned-user-mirror'",
        "Name = 'pinned-public'",
        "Invoke-WebRequest -Uri $candidate.Url -OutFile $msi -MaximumRedirection 10 -TimeoutSec 1200 -UseBasicParsing",
        "$actual = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash",
        "[string]::Equals($actual, $env:BRICSCAD_V25_PINNED_MSI_SHA256, [StringComparison]::OrdinalIgnoreCase)",
        "BricsCAD V25 MSI SHA-256 mismatch.",
        "$signature = Get-AuthenticodeSignature -FilePath $msi",
        "$signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid",
        "BricsCAD V25 MSI Authenticode signature is not valid",
        "$signerSubject -notmatch '(^|,\\s*)(CN|O)=Bricsys(,|$)'",
        "BricsCAD V25 MSI signer is not Bricsys",
        "New-Object -ComObject WindowsInstaller.Installer",
        "$database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductVersion''')",
        "$productVersion -notmatch '^25\\.2\\.10(?:\\.|$)'",
        "Downloaded MSI is not the pinned BricsCAD V25.2.10 product.",
        "$database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductName''')",
        "$productName -notmatch 'BricsCAD'",
        "$process.WaitForExit(900000)",
        "administrative extraction timed out after 15 minutes",
        "([string]$metadata.productVersion).Trim()",
        "PACKAGE-METADATA productVersion must match source product version.",
        "PACKAGE-METADATA assembly version is missing.",
    )
    for token in required:
        if token not in text:
            errors.append("cloud V25 workflow missing Node24/cache/pinning/signature/version/manual-release token: " + token)

    restore_index = text.find("- name: Restore BricsCAD V25 installer cache")
    acquire_index = text.find("- name: Acquire BricsCAD V25 compile references")
    save_index = text.find("- name: Save BricsCAD V25 installer cache")
    validate_refs_index = text.find("- name: Validate BricsCAD V25 compile references")
    if min(restore_index, acquire_index, save_index, validate_refs_index) < 0 or not restore_index < acquire_index < save_index < validate_refs_index:
        errors.append("BricsCAD installer cache restore must precede acquisition and verified cache save must precede reference validation")

    candidates_index = text.find("$candidates = @(", acquire_index if acquire_index >= 0 else 0)
    mirror_index = text.find("Name = 'pinned-user-mirror'", candidates_index if candidates_index >= 0 else 0)
    public_index = text.find("Name = 'pinned-public'", candidates_index if candidates_index >= 0 else 0)
    if min(candidates_index, mirror_index, public_index) < 0:
        errors.append("cloud V25 workflow must define approved HTTP mirror and pinned HTTPS public candidates")
    elif not candidates_index < mirror_index < public_index:
        errors.append("approved mirror must be attempted before the pinned HTTPS public candidate after cache miss")

    cache_hash_index = text.find("$cachedHash = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash")
    download_index = text.find("Invoke-WebRequest -Uri $candidate.Url", acquire_index if acquire_index >= 0 else 0)
    hash_index = text.find("$actual = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash")
    signature_index = text.find("$signature = Get-AuthenticodeSignature -FilePath $msi")
    signer_index = text.find("$signerSubject -notmatch", signature_index if signature_index >= 0 else 0)
    product_index = text.find("$database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductVersion''')")
    extract_index = text.find("$process = Start-Process -FilePath msiexec.exe")
    timeout_index = text.find("$process.WaitForExit(900000)", extract_index if extract_index >= 0 else 0)
    if min(cache_hash_index, download_index, hash_index, signature_index, signer_index, product_index, extract_index, timeout_index) < 0:
        errors.append("cloud V25 workflow is missing a required cache/download/integrity/extraction boundary")
    elif not hash_index < signature_index < signer_index < product_index < extract_index < timeout_index:
        errors.append("cloud V25 workflow must pin digest and verify Bricsys Authenticode + V25.2.10 MSI identity before bounded administrative extraction")

    tag_check = text.find("Release tag must exactly match source product version.")
    product_check = text.find("PACKAGE-METADATA productVersion must match source product version.")
    checksum_step = text.find("- name: Create package checksum")
    publish_step = text.find("- name: Publish GitHub prerelease")
    if min(tag_check, product_check, checksum_step, publish_step) < 0 or not tag_check < product_check < checksum_step < publish_step:
        errors.append("cloud V25 workflow must bind tag and package productVersion to source before checksum/publish")

if not DOC.is_file():
    errors.append("missing docs/CLOUD-V25-PREVIEW-RELEASE.md")
else:
    doc = DOC.read_text(encoding="utf-8")
    for token in (
        PINNED_SHA256,
        "Actions cache",
        "cache hit is re-verified",
        "approved pinned HTTP mirror",
        "valid Bricsys Authenticode signer",
        "MSI ProductVersion must identify V25.2.10",
        "download timeout",
        "administrative extraction timeout",
    ):
        if token not in doc:
            errors.append("cloud V25 release documentation missing integrity/cache statement: " + token)

print("QS3D cloud V25 preview release preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: cloud V25 preview remains manual-only on current Node 24 action majors; the exact V25.2.10 MSI digest is pinned, cache hits are re-verified, the approved mirror is bounded by digest + Bricsys Authenticode + MSI identity checks, and download/extraction waits are finite.")
