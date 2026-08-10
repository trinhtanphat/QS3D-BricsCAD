#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

sign = ROOT / "scripts/sign-v25.ps1"
verify = ROOT / "scripts/verify-v25-signatures.ps1"
finalize = ROOT / "scripts/finalize-v25-signed-package.ps1"
release = ROOT / ".github/workflows/release-v25.yml"
for path in (sign, verify, finalize, release):
    if not path.is_file():
        errors.append("missing signing/release file: " + str(path.relative_to(ROOT)))

if sign.is_file():
    text = sign.read_text(encoding="utf-8")
    for needle in (
        "Cert:\\CurrentUser\\My", "HasPrivateKey", "1.3.6.1.5.5.7.3.3",
        "Set-AuthenticodeSignature", "-HashAlgorithm SHA256", "-TimestampServer $TimestampServer",
        "Get-AuthenticodeSignature", "SignatureStatus]::Valid", "SupportsShouldProcess"
    ):
        if needle not in text:
            errors.append("sign-v25.ps1 missing guard/token: " + needle)
    if not re.search(r"ValidatePattern\('\^https://", text):
        errors.append("sign-v25.ps1 must require an HTTPS timestamp server")
    if re.search(r"(?i)\b(pfx|pfxpassword|password|securestring)\b", text):
        errors.append("sign-v25.ps1 must not accept PFX/private-key passwords; use the Windows certificate store")
    if re.search(r"(?i)SECURELOAD\s*[=:]|setvar[^\n]*SECURELOAD", text):
        errors.append("sign-v25.ps1 must not lower BricsCAD SECURELOAD")

if verify.is_file():
    text = verify.read_text(encoding="utf-8")
    for needle in (
        "Get-AuthenticodeSignature", "SignatureStatus]::Valid", "ExpectedThumbprint",
        "TimeStamperCertificate", "Missing trusted timestamp"
    ):
        if needle not in text:
            errors.append("verify-v25-signatures.ps1 missing guard/token: " + needle)

if finalize.is_file():
    text = finalize.read_text(encoding="utf-8")
    for needle in (
        "ExpectedSignerThumbprint", "Assert-AuthenticodeSigner", "PACKAGE-METADATA.json",
        "signedPayloadSignerThumbprint", "signedPluginAssemblyVersion", "SHA256SUMS.txt",
        "Compress-Archive"
    ):
        if needle not in text:
            errors.append("finalize-v25-signed-package.ps1 missing guard/token: " + needle)

if release.is_file():
    text = release.read_text(encoding="utf-8")
    required = (
        "sign_package:",
        "QS3D_SIGNING_CERT_THUMBPRINT: ${{ vars.QS3D_SIGNING_CERT_THUMBPRINT }}",
        "QS3D_TIMESTAMP_SERVER: ${{ vars.QS3D_TIMESTAMP_SERVER }}",
        "RELEASE_RUN_RUNTIME: ${{ inputs.run_runtime }}",
        "RELEASE_SIGN_PACKAGE: ${{ inputs.sign_package }}",
        "if ($env:GITHUB_REF -ne 'refs/heads/main')",
        "V25 releases must be dispatched from refs/heads/main",
        "Stable release requires run_runtime=true.",
        "Stable release requires sign_package=true.",
        "prerelease input must match the release_tag suffix",
        "Release tag $env:RELEASE_TAG does not match plugin package version $packageVersion. Refusing relabelled release.",
        "scripts\\sign-v25.ps1",
        "scripts\\verify-v25-signatures.ps1",
        "scripts\\finalize-v25-signed-package.ps1",
        "-CertificateThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT",
        "-TimestampServer $env:QS3D_TIMESTAMP_SERVER",
        "-ExpectedThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT",
        "-ExpectedSignerThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT",
        "draft = $true",
        "Expected release asset was not uploaded:",
        "$publishBody = @{ draft = $false }",
        "-Method Patch",
        "GitHub release remained a draft after publish request.",
    )
    for needle in required:
        if needle not in text:
            errors.append("release-v25.yml missing stable release signing/runtime contract: " + needle)

    package_index = text.find("- name: Build V25 release package")
    version_index = text.find("- name: Validate release tag and package version binding")
    sign_index = text.find("- name: Authenticode-sign V25 executable payload")
    verify_index = text.find("- name: Verify Authenticode publisher and timestamp")
    finalize_index = text.find("- name: Finalize signed V25 package")
    checksum_index = text.find("- name: Create package checksum")
    publish_index = text.find("- name: Publish GitHub Release")
    ordered = (package_index, version_index, sign_index, verify_index, finalize_index, checksum_index, publish_index)
    if any(index < 0 for index in ordered) or list(ordered) != sorted(ordered):
        errors.append("release-v25.yml must package -> version-bind -> sign -> verify -> finalize -> checksum -> publish in that order")

    if "if: ${{ inputs.sign_package }}" not in text:
        errors.append("release-v25.yml signing steps must be explicitly controlled by sign_package")
    if "if: ${{ inputs.run_runtime }}" not in text:
        errors.append("release-v25.yml runtime step must remain explicitly controlled by run_runtime")
    if "prerelease=true for an explicitly unqualified preview" not in text or "prerelease=true for an explicitly unsigned preview" not in text:
        errors.append("release-v25.yml must distinguish explicit prerelease exceptions from stable release requirements")
    if "(?:-[0-9A-Za-z.-]+)?(?:\\+[0-9A-Za-z.-]+)?" not in text:
        errors.append("release-v25.yml release tag validation must support separate prerelease/build-metadata components")

    draft_index = text.find("draft = $true")
    upload_index = text.find("$uploadBase = $release.upload_url")
    verify_assets_index = text.find("Expected release asset was not uploaded:")
    publish_draft_index = text.find("$publishBody = @{ draft = $false }")
    if any(index < 0 for index in (draft_index, upload_index, verify_assets_index, publish_draft_index)) or not (
        draft_index < upload_index < verify_assets_index < publish_draft_index
    ):
        errors.append("release-v25.yml must create a draft, upload and verify all assets, then publish the release")

for path in ROOT.rglob("*.pfx"):
    errors.append("private signing certificate must not be committed: " + str(path.relative_to(ROOT)))
for path in ROOT.rglob("*.p12"):
    errors.append("private signing certificate must not be committed: " + str(path.relative_to(ROOT)))

print("QS3D V25 signing/release preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Authenticode uses the Windows certificate store/SHA-256/HTTPS timestamping, stable releases require real V25 runtime plus signed finalized payloads, prerelease flags match tag semantics, and release tags are version-bound before publication.")
