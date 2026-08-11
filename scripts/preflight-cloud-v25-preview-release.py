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
        "BRICSCAD_V25_MSI_SHA256: ${{ vars.BRICSCAD_V25_MSI_SHA256 }}",
        "[string]::IsNullOrWhiteSpace($env:BRICSCAD_V25_MSI_SHA256) -or $env:BRICSCAD_V25_MSI_SHA256 -notmatch '^[0-9A-Fa-f]{64}$'",
        "Repository variable BRICSCAD_V25_MSI_SHA256 is required",
        "$actual = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash",
        "[string]::Equals($actual, $env:BRICSCAD_V25_MSI_SHA256, [StringComparison]::OrdinalIgnoreCase)",
        "BricsCAD V25 MSI SHA-256 mismatch.",
    )
    for token in required:
        if token not in text:
            errors.append("cloud V25 workflow missing pinning/manual-release token: " + token)

    if "if (-not [string]::IsNullOrWhiteSpace($env:BRICSCAD_V25_MSI_SHA256))" in text:
        errors.append("cloud V25 workflow must not make MSI SHA-256 verification optional")

    hash_index = text.find("$actual = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash")
    extract_index = text.find("$process = Start-Process -FilePath msiexec.exe")
    if hash_index < 0 or extract_index < 0 or hash_index > extract_index:
        errors.append("cloud V25 workflow must verify the pinned MSI digest before administrative extraction")

if not DOC.is_file():
    errors.append("missing docs/CLOUD-V25-PREVIEW-RELEASE.md")
else:
    doc = DOC.read_text(encoding="utf-8")
    for token in (
        "`BRICSCAD_V25_MSI_SHA256`: **required**",
        "fails closed if the digest is missing/malformed",
        "verify the downloaded MSI against the required pinned SHA-256",
    ):
        if token not in doc:
            errors.append("cloud V25 release documentation missing required-digest statement: " + token)

print("QS3D cloud V25 preview release preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: cloud V25 preview release remains manual-only and requires SHA-256 pinning before using downloaded BricsCAD compile references.")
