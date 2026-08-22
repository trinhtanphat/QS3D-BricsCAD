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
        "BRICSCAD_V25_PUBLIC_MSI_URL:",
        "BRICSCAD_V25_MSI_SHA256: ${{ vars.BRICSCAD_V25_MSI_SHA256 }}",
        "[string]::IsNullOrWhiteSpace($env:BRICSCAD_V25_MSI_SHA256) -or $env:BRICSCAD_V25_MSI_SHA256 -notmatch '^[0-9A-Fa-f]{64}$'",
        "Repository variable BRICSCAD_V25_MSI_SHA256 is required",
        "$actual = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash",
        "[string]::Equals($actual, $env:BRICSCAD_V25_MSI_SHA256, [StringComparison]::OrdinalIgnoreCase)",
        "BricsCAD V25 MSI SHA-256 mismatch.",
        "$signature = Get-AuthenticodeSignature -FilePath $msi",
        "$signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid",
        "BricsCAD V25 MSI Authenticode signature is not valid",
        "([string]$metadata.productVersion).Trim()",
        "PACKAGE-METADATA productVersion must match source product version.",
        "PACKAGE-METADATA assembly version is missing.",
    )
    for token in required:
        if token not in text:
            errors.append("cloud V25 workflow missing pinning/signature/version/manual-release token: " + token)

    optional_hash_patterns = (
        "if (-not [string]::IsNullOrWhiteSpace($env:BRICSCAD_V25_MSI_SHA256))",
        "if (-not [string]::IsNullOrWhiteSpace($env:BRICSCAD_V25_MSI_SHA256) -and",
    )
    for token in optional_hash_patterns:
        if token in text:
            errors.append("cloud V25 workflow must not make MSI SHA-256 verification optional: " + token)

    hash_index = text.find("$actual = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash")
    compare_index = text.find("[string]::Equals($actual, $env:BRICSCAD_V25_MSI_SHA256, [StringComparison]::OrdinalIgnoreCase)")
    signature_index = text.find("$signature = Get-AuthenticodeSignature -FilePath $msi")
    extract_index = text.find("$process = Start-Process -FilePath msiexec.exe")
    if min(hash_index, compare_index, signature_index, extract_index) < 0 or not hash_index < compare_index < signature_index < extract_index:
        errors.append("cloud V25 workflow must verify mandatory SHA-256 and valid Authenticode before administrative extraction")

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
        "`BRICSCAD_V25_MSI_SHA256`: **required**",
        "fails closed if the digest is missing/malformed",
        "verify the downloaded MSI against the required pinned SHA-256",
        "verify the MSI Authenticode signature",
    ):
        if token not in doc:
            errors.append("cloud V25 release documentation missing integrity statement: " + token)

print("QS3D cloud V25 preview release preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: cloud V25 preview release remains manual-only, requires SHA-256 plus Authenticode before extraction, and binds PACKAGE-METADATA productVersion to source before publication.")
