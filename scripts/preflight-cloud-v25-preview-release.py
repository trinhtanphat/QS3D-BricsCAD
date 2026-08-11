#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/release-v25-cloud.yml"
DOC = ROOT / "docs/CLOUD-V25-PREVIEW-RELEASE.md"
errors = []

if not WORKFLOW.is_file():
    errors.append("missing .github/workflows/release-v25-cloud.yml")
else:
    text = WORKFLOW.read_text(encoding="utf-8")
    required = (
        "workflow_dispatch:",
        "github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE'",
        "BRICSCAD_V25_MIRROR_MSI_URL:",
        "BRICSCAD_V25_PUBLIC_MSI_URL:",
        "BRICSCAD_V25_MSI_SHA256: ${{ vars.BRICSCAD_V25_MSI_SHA256 }}",
        "BRICSCAD_V25_MSI_SHA256 must be a 64-hex SHA-256 value when configured.",
        "Skipping pinned HTTP mirror because BRICSCAD_V25_MSI_SHA256 is not configured.",
        "$actual = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash",
        "[string]::IsNullOrWhiteSpace($env:BRICSCAD_V25_MSI_SHA256) -and",
        "[string]::Equals($actual, $env:BRICSCAD_V25_MSI_SHA256, [StringComparison]::OrdinalIgnoreCase)",
        "BricsCAD V25 MSI SHA-256 mismatch.",
        "$signature = Get-AuthenticodeSignature -FilePath $msi",
        "$signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid",
        "BricsCAD V25 MSI Authenticode signature is not valid",
        "New-Object -ComObject WindowsInstaller.Installer",
        "$database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductVersion''')",
        "$productVersion -notmatch '^25\\.2\\.10(?:\\.|$)'",
        "Downloaded MSI is not the pinned BricsCAD V25.2.10 product.",
        "$database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductName''')",
        "$productName -notmatch 'BricsCAD'",
        "([string]$metadata.productVersion).Trim()",
        "PACKAGE-METADATA productVersion must match source product version.",
        "PACKAGE-METADATA assembly version is missing.",
    )
    for token in required:
        if token not in text:
            errors.append("cloud V25 workflow missing pinning/signature/version/manual-release token: " + token)

    candidates_index = text.find("$candidates = @()")
    mirror_guard_index = text.find(
        "if (-not [string]::IsNullOrWhiteSpace($env:BRICSCAD_V25_MSI_SHA256)) {",
        candidates_index if candidates_index >= 0 else 0,
    )
    mirror_index = text.find("Name = 'pinned-user-mirror'", candidates_index if candidates_index >= 0 else 0)
    skip_index = text.find(
        "Skipping pinned HTTP mirror because BRICSCAD_V25_MSI_SHA256 is not configured.",
        candidates_index if candidates_index >= 0 else 0,
    )
    public_index = text.find("Name = 'pinned-public'", candidates_index if candidates_index >= 0 else 0)
    if min(candidates_index, mirror_guard_index, mirror_index, skip_index, public_index) < 0:
        errors.append("cloud V25 workflow must define conditional hash-pinned HTTP mirror and pinned HTTPS public candidates")
    elif not candidates_index < mirror_guard_index < mirror_index < skip_index < public_index:
        errors.append("HTTP mirror must be added only inside the configured SHA-256 guard before the pinned HTTPS public candidate")

    hash_index = text.find("$actual = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash")
    signature_index = text.find("$signature = Get-AuthenticodeSignature -FilePath $msi")
    product_index = text.find("$database.OpenView('SELECT `Value` FROM `Property` WHERE `Property`=''ProductVersion''')")
    extract_index = text.find("$process = Start-Process -FilePath msiexec.exe")
    if min(hash_index, signature_index, product_index, extract_index) < 0 or not hash_index < signature_index < product_index < extract_index:
        errors.append("cloud V25 workflow must calculate digest and verify valid Authenticode + V25.2.10 MSI identity before administrative extraction")

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
        "`BRICSCAD_V25_MSI_SHA256`: optional for the normal pinned HTTPS path",
        "required before the workflow will consider the pinned HTTP mirror",
        "HTTP mirror is skipped when no SHA-256 pin is configured",
        "mandatory Authenticode signature",
        "MSI ProductVersion must identify V25.2.10",
        "calculate and log the downloaded MSI SHA-256",
    ):
        if token not in doc:
            errors.append("cloud V25 release documentation missing integrity statement: " + token)

print("QS3D cloud V25 preview release preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: cloud V25 preview remains manual-only; the plain-HTTP mirror is available only behind a configured SHA-256 pin, while mandatory Authenticode and V25.2.10 MSI identity checks still precede extraction and source/package version binding precedes publish.")
