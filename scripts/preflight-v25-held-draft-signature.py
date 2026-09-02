#!/usr/bin/env python3
"""Fail closed if V25 draft signature admission leaves the held ZIP generation."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VALIDATOR = (ROOT / "scripts/assert-v25-commercial-draft-identity.ps1").read_text(encoding="utf-8")
WORKFLOW = (ROOT / ".github/workflows/release-v25.yml").read_text(encoding="utf-8")
DOWNLOAD_TOKEN = "gh release download $env:RELEASE_TAG"
PUBLISH_TOKEN = "$published = Invoke-RestMethod -Method Patch"

REQUIRED_PAYLOAD = (
    "QS3D.BricsCAD.V25.dll",
    "QS3D.Core.dll",
    "install-v25-autoload.ps1",
    "uninstall-v25-autoload.ps1",
    "update-v25.ps1",
    "unblock-v25-netload.ps1",
)


def publish_window(workflow: str) -> str:
    start = workflow.find(DOWNLOAD_TOKEN)
    end = workflow.find(PUBLISH_TOKEN, start + 1 if start >= 0 else 0)
    if start < 0 or end < 0 or end <= start:
        return ""
    return workflow[start:end]


def validate(validator: str, workflow: str) -> list[str]:
    errors: list[str] = []
    required_validator_tokens = (
        "function Test-HeldZipPayloadSignatures",
        "$ZipHeld.Stream.Seek(0, [IO.SeekOrigin]::Begin)",
        "[IO.Compression.ZipArchive]::new($ZipHeld.Stream, [IO.Compression.ZipArchiveMode]::Read, $true)",
        "$MaxSignedPayloadEntryBytes = 268435456",
        "$MaxSignedPayloadTotalBytes = 536870912",
        "if ($matches.Count -ne 1)",
        "[IO.FileMode]::CreateNew",
        "[IO.FileShare]::None",
        "[IO.FileAttributes]::ReparsePoint",
        "verify-v25-signatures.ps1",
        "Test-HeldZipPayloadSignatures -ZipHeld $zipHeld -ExpectedThumbprint $expectedSigner",
    )
    for token in required_validator_tokens:
        if token not in validator:
            errors.append(f"V25 held draft signature validator missing token: {token}")
    for name in REQUIRED_PAYLOAD:
        if f"'{name}'" not in validator:
            errors.append(f"V25 held draft signature validator missing required payload entry: {name}")

    invocation = validator.find("Test-HeldZipPayloadSignatures -ZipHeld $zipHeld")
    dispose = validator.find("$held.Stream.Dispose()")
    if invocation < 0 or dispose < 0 or invocation >= dispose:
        errors.append("V25 held draft signature verification must complete before held generations are disposed")

    window = publish_window(workflow)
    if not window:
        errors.append("V25 commercial release workflow must expose one draft-download to final-publish window")
        return errors
    if "assert-v25-commercial-draft-identity.ps1" not in window:
        errors.append("V25 final publish window must invoke held commercial draft identity admission")

    forbidden_after_admission = (
        "qs3d-commercial-draft-held-",
        "$heldRemoteZip = Join-Path $heldRoot 'QS3D-BricsCAD-V25.zip'",
        "Expand-Archive -LiteralPath $heldRemoteZip",
        "verify-v25-signatures.ps1 -Path $payload",
    )
    admission = window.find("assert-v25-commercial-draft-identity.ps1")
    tail = window[admission:] if admission >= 0 else window
    for token in forbidden_after_admission:
        if token in tail:
            errors.append(f"V25 publish window reopens/reverifies draft ZIP after held admission: {token}")

    return errors


errors = validate(VALIDATOR, WORKFLOW)
if errors:
    raise SystemExit("V25 held draft signature preflight failed:\n - " + "\n - ".join(errors))

mutations = {
    "held signature call removed": (VALIDATOR.replace("Test-HeldZipPayloadSignatures -ZipHeld $zipHeld -ExpectedThumbprint $expectedSigner", "Write-Host 'skip'", 1), WORKFLOW),
    "duplicate-entry rejection removed": (VALIDATOR.replace("if ($matches.Count -ne 1)", "if ($matches.Count -lt 1)", 1), WORKFLOW),
    "bounded entry cap removed": (VALIDATOR.replace("$MaxSignedPayloadEntryBytes = 268435456", "$MaxSignedPayloadEntryBytes = [int64]::MaxValue", 1), WORKFLOW),
    "workflow reopens draft after admission": (VALIDATOR, WORKFLOW.replace("if ($null -eq $downloadedIdentity) { throw 'Downloaded V25 draft semantic admission returned no identity.' }", "if ($null -eq $downloadedIdentity) { throw 'Downloaded V25 draft semantic admission returned no identity.' }\n              $heldRemoteZip = Join-Path $downloadRoot 'QS3D-BricsCAD-V25.zip'\n              Expand-Archive -LiteralPath $heldRemoteZip -DestinationPath (Join-Path $downloadRoot 'legacy')\n              & .\\scripts\\verify-v25-signatures.ps1 -Path $payload -ExpectedThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT", 1)),
}
for label, (validator, workflow) in mutations.items():
    if not validate(validator, workflow):
        raise SystemExit(f"V25 held draft signature mutation escaped detection: {label}")

print("PASS V25 draft signatures are verified on the held admitted ZIP generation")
