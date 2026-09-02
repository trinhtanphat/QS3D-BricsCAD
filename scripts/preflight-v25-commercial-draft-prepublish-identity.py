#!/usr/bin/env python3
"""Fail closed if V25 draft publication loses exact held downloaded semantic admission."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VALIDATOR = (ROOT / "scripts/assert-v25-commercial-draft-identity.ps1").read_text(encoding="utf-8")
WORKFLOW = (ROOT / ".github/workflows/release-v25.yml").read_text(encoding="utf-8")
DOWNLOAD_TOKEN = "gh release download $env:RELEASE_TAG"
PUBLISH_TOKEN = "$published = Invoke-RestMethod -Method Patch"
HELD_PACKAGE_TOKEN = "-PackageZip $heldRemoteZip"
HELD_SOURCE_TOKENS = (
    "$remoteZip = Join-Path $downloadRoot 'QS3D-BricsCAD-V25.zip'",
    "$heldRemoteZip = Join-Path $heldRoot 'QS3D-BricsCAD-V25.zip'",
    "& .\\scripts\\verify-v25-held-file.ps1 -Operation Copy -Path $remoteZip -Destination $heldRemoteZip | Out-Null",
    "$remoteZipHash = (& .\\scripts\\verify-v25-held-file.ps1 -Operation Hash -Path $heldRemoteZip).Trim().ToLowerInvariant()",
    "if ($remoteZipHash -ne $Matches[1])",
)


def publish_window(workflow: str) -> str:
    download_index = workflow.find(DOWNLOAD_TOKEN)
    publish_index = workflow.find(PUBLISH_TOKEN, download_index + 1 if download_index >= 0 else 0)
    if download_index < 0 or publish_index < 0 or publish_index <= download_index:
        return ""
    return workflow[download_index:publish_index]


def replace_in_publish_window(workflow: str, old: str, new: str) -> str:
    download_index = workflow.find(DOWNLOAD_TOKEN)
    publish_index = workflow.find(PUBLISH_TOKEN, download_index + 1 if download_index >= 0 else 0)
    if download_index < 0 or publish_index < 0 or publish_index <= download_index:
        return workflow
    window = workflow[download_index:publish_index]
    mutated = window.replace(old, new, 1)
    return workflow[:download_index] + mutated + workflow[publish_index:]


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

    window = publish_window(workflow)
    if not window:
        errors.append("V25 commercial release workflow must expose one draft-download to final-publish admission window")
        return errors

    workflow_tokens = (
        "assert-v25-commercial-draft-identity.ps1",
        HELD_PACKAGE_TOKEN,
        "-ChecksumPath (Join-Path $downloadRoot 'QS3D-BricsCAD-V25.zip.sha256')",
        "-UpdateManifestPath (Join-Path $downloadRoot 'QS3D-BricsCAD-V25.update.json')",
        "-ProvenancePath (Join-Path $downloadRoot 'QS3D-BricsCAD-V25.provenance.json')",
        "-ExpectedSourceCommit $env:GITHUB_SHA",
        "-ExpectedReleaseTag $env:RELEASE_TAG",
        "-ExpectedSignerThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT",
        "Downloaded V25 draft semantic admission returned no identity",
        *HELD_SOURCE_TOKENS,
    )
    for token in workflow_tokens:
        if token not in window:
            errors.append(f"V25 final publish window missing downloaded-draft admission token: {token}")

    admission_index = window.find("assert-v25-commercial-draft-identity.ps1")
    if admission_index < 0:
        errors.append("downloaded V25 draft semantic admission must occur after draft download and before final publish PATCH")
    else:
        for token in HELD_SOURCE_TOKENS:
            token_index = window.find(token)
            if token_index < 0 or token_index >= admission_index:
                errors.append(f"V25 held draft ZIP provenance must be established before semantic admission: {token}")

    return errors


errors = validate(VALIDATOR, WORKFLOW)
if errors:
    raise SystemExit("V25 commercial draft prepublish identity preflight failed:\n - " + "\n - ".join(errors))

mutations = {
    "workflow omits downloaded draft validator": (VALIDATOR, replace_in_publish_window(WORKFLOW, "assert-v25-commercial-draft-identity.ps1", "legacy-draft-check.ps1")),
    "workflow omits exact held package": (VALIDATOR, replace_in_publish_window(WORKFLOW, HELD_PACKAGE_TOKEN, "-PackageZip $remoteZip")),
    "workflow held package loses downloaded source": (VALIDATOR, replace_in_publish_window(WORKFLOW, HELD_SOURCE_TOKENS[2], "& .\\scripts\\verify-v25-held-file.ps1 -Operation Copy -Path $heldRemoteZip -Destination $heldRemoteZip | Out-Null")),
    "workflow held package loses checksum binding": (VALIDATOR, replace_in_publish_window(WORKFLOW, HELD_SOURCE_TOKENS[4], "if ($remoteZipHash -eq '')")),
    "workflow omits exact source": (VALIDATOR, replace_in_publish_window(WORKFLOW, "-ExpectedSourceCommit $env:GITHUB_SHA", "-ExpectedSourceCommit $env:RELEASE_TAG")),
    "workflow omits exact tag": (VALIDATOR, replace_in_publish_window(WORKFLOW, "-ExpectedReleaseTag $env:RELEASE_TAG", "-ExpectedReleaseTag $env:GITHUB_SHA")),
    "validator loses provenance source": (VALIDATOR.replace("[string]$provenance.sourceCommit", "[string]$ExpectedSourceCommit", 1), WORKFLOW),
    "validator loses package digest": (VALIDATOR.replace("[string]$provenance.packageSha256", "[string]$zipHash", 1), WORKFLOW),
    "validator loses update digest": (VALIDATOR.replace("[string]$provenance.updateManifestSha256", "[string]$updateHash", 1), WORKFLOW),
    "validator loses ZIP metadata source": (VALIDATOR.replace("([string]$metadata.gitCommit).Trim()", "([string]$ExpectedSourceCommit).Trim()", 1), WORKFLOW),
}
for label, (validator, workflow) in mutations.items():
    if not validate(validator, workflow):
        raise SystemExit(f"V25 commercial draft prepublish identity mutation escaped detection: {label}")

print("PASS V25 held downloaded commercial draft semantic identity before publish")
