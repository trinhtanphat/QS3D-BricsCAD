#!/usr/bin/env python3
"""Fail closed if V25 draft publication loses exact downloaded semantic admission."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VALIDATOR = (ROOT / "scripts/assert-v25-commercial-draft-identity.ps1").read_text(encoding="utf-8")
WORKFLOW = (ROOT / ".github/workflows/release-v25.yml").read_text(encoding="utf-8")


def validate(validator: str, workflow: str) -> list[str]:
    errors: list[str] = []
    for token in (
        "[IO.FileShare]::Read",
        "[IO.FileAttributes]::ReparsePoint",
        "$MaxMetadataBytes = 65536",
        "$MaxProvenanceBytes = 65536",
        "[Text.UTF8Encoding]::new($false, $true)",
        "$zipHeld = Open-HeldGeneration",
        "$checksumHeld = Open-HeldGeneration",
        "$updateHeld = Open-HeldGeneration",
        "$provenanceHeld = Open-HeldGeneration",
        "$zipHash = (Get-HeldSha256 -Held $zipHeld)",
        "$updateHash = (Get-HeldSha256 -Held $updateHeld)",
        "Read-ZipMetadataIdentity -ZipHeld $zipHeld",
        "([string]$metadata.gitCommit).Trim()",
        "[string]$provenance.packageSha256",
        "[string]$provenance.updateManifestSha256",
        "[string]$provenance.sourceCommit",
        "[string]$provenance.releaseTag",
        "[string]$provenance.signerThumbprint",
    ):
        if token not in validator:
            errors.append(f"downloaded V25 draft validator missing token: {token}")
    if "Get-Content -LiteralPath" in validator:
        errors.append("downloaded V25 draft validator must not admit semantic inputs through ordinary pathname Get-Content")
    if "[IO.FileShare]::ReadWrite" in validator or "[IO.FileShare]::Write" in validator:
        errors.append("downloaded V25 draft semantic generations must not share write access")

    workflow_tokens = (
        "assert-v25-commercial-draft-identity.ps1",
        "-PackageZip (Join-Path $downloadRoot 'QS3D-BricsCAD-V25.zip')",
        "-ChecksumPath (Join-Path $downloadRoot 'QS3D-BricsCAD-V25.zip.sha256')",
        "-UpdateManifestPath (Join-Path $downloadRoot 'QS3D-BricsCAD-V25.update.json')",
        "-ProvenancePath (Join-Path $downloadRoot 'QS3D-BricsCAD-V25.provenance.json')",
        "-ExpectedSourceCommit $env:GITHUB_SHA",
        "-ExpectedReleaseTag $env:RELEASE_TAG",
        "-ExpectedSignerThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT",
        "Downloaded V25 draft semantic admission returned no identity",
    )
    for token in workflow_tokens:
        if token not in workflow:
            errors.append(f"V25 commercial release workflow missing downloaded-draft admission token: {token}")

    download_index = workflow.find("gh release download $env:RELEASE_TAG")
    admission_index = workflow.find("assert-v25-commercial-draft-identity.ps1")
    publish_index = workflow.find("$published = Invoke-RestMethod -Method Patch")
    if download_index < 0 or admission_index < 0 or publish_index < 0 or not (download_index < admission_index < publish_index):
        errors.append("downloaded V25 draft semantic admission must occur after draft download and before final publish PATCH")

    return errors


errors = validate(VALIDATOR, WORKFLOW)
if errors:
    raise SystemExit("V25 commercial draft prepublish identity preflight failed:\n - " + "\n - ".join(errors))

mutations = {
    "workflow omits downloaded draft validator": (VALIDATOR, WORKFLOW.replace("assert-v25-commercial-draft-identity.ps1", "legacy-draft-check.ps1", 1)),
    "workflow omits exact source": (VALIDATOR, WORKFLOW.replace("-ExpectedSourceCommit $env:GITHUB_SHA", "-ExpectedSourceCommit $env:RELEASE_TAG", 1)),
    "validator loses provenance source": (VALIDATOR.replace("[string]$provenance.sourceCommit", "[string]$ExpectedSourceCommit", 1), WORKFLOW),
    "validator loses package digest": (VALIDATOR.replace("[string]$provenance.packageSha256", "[string]$zipHash", 1), WORKFLOW),
    "validator loses update digest": (VALIDATOR.replace("[string]$provenance.updateManifestSha256", "[string]$updateHash", 1), WORKFLOW),
    "validator loses ZIP metadata source": (VALIDATOR.replace("([string]$metadata.gitCommit).Trim()", "([string]$ExpectedSourceCommit).Trim()", 1), WORKFLOW),
}
for label, (validator, workflow) in mutations.items():
    if not validate(validator, workflow):
        raise SystemExit(f"V25 commercial draft prepublish identity mutation escaped detection: {label}")

print("PASS V25 downloaded commercial draft semantic identity before publish")
