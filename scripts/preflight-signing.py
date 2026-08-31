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
        errors.append("sign-v25.ps1 must not accept PFX/private-key passwords; ephemeral PFX import belongs to the dedicated import helper")
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
        "name: QS3D Manual V25 Commercial Release",
        "confirm_release:",
        "run_runtime:",
        "prerelease:",
        "environment: commercial-release",
        "QS3D_SIGNING_CERT_THUMBPRINT: ${{ vars.QS3D_SIGNING_CERT_THUMBPRINT }}",
        "QS3D_TIMESTAMP_SERVER: ${{ vars.QS3D_TIMESTAMP_SERVER }}",
        "RELEASE_RUN_RUNTIME: ${{ inputs.run_runtime }}",
        "if ($env:GITHUB_REF -ne 'refs/heads/main')",
        "Commercial V25 releases must be dispatched from refs/heads/main",
        "Stable commercial releases require run_runtime=true.",
        "prerelease input must match whether release_tag contains a SemVer prerelease suffix.",
        "QS3D_SIGNING_CERT_PFX_BASE64: ${{ secrets.QS3D_SIGNING_CERT_PFX_BASE64 }}",
        "QS3D_SIGNING_CERT_PASSWORD: ${{ secrets.QS3D_SIGNING_CERT_PASSWORD }}",
        "import-v25-signing-certificate.ps1",
        "scripts\\sign-v25.ps1",
        "scripts\\verify-v25-signatures.ps1",
        "scripts\\finalize-v25-signed-package.ps1",
        "-CertificateThumbprint $env:SIGNING_THUMBPRINT",
        "-TimestampServer $env:QS3D_TIMESTAMP_SERVER",
        "-ExpectedThumbprint $env:SIGNING_THUMBPRINT",
        "-ExpectedSignerThumbprint $env:SIGNING_THUMBPRINT",
        "Remove ephemeral signing certificate and private key",
        "Remove-Item -Path $certificatePath -DeleteKey -Force",
        "Verify finalized package after private-key cleanup",
        "Licensed V25 runtime validation for exact signed payload",
        "if: ${{ inputs.run_runtime }}",
        "dist\\QS3D-BricsCAD-V25\\QS3D.BricsCAD.V25.dll",
        "Create signed auto-update manifest",
        "Create commercial checksum and provenance",
        "Upload signed commercial candidate",
        "Verify candidate after job boundary",
        "Commercial candidate provenance does not exactly bind tag, product, source, signer and ZIP digest.",
        "Create draft, verify uploaded bytes, then publish",
        '$tagRefUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/git/refs"',
        "$tagCreatedByThisRun = $true",
        "$releaseId = [long]$release.id",
        "Assert-RemoteReleaseTagTargetsWorkflowSha",
        "gh release download $env:RELEASE_TAG",
        "Draft release asset SHA-256 mismatch for $name; release remains a draft.",
        "verify-v25-signatures.ps1 -Path $payload -ExpectedThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT",
        "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri",
        "GitHub release remained a draft after publication request.",
        "rollback-v25-draft-release.ps1",
    )
    for needle in required:
        if needle not in text:
            errors.append("release-v25.yml missing signed-only commercial release contract: " + needle)

    if "sign_package:" in text or "inputs.sign_package" in text:
        errors.append("commercial release workflow must remain signed-only; obsolete optional sign_package branching reappeared")

    package_index = text.find("- name: Build V25 release package")
    bind_index = text.find("- name: Validate exact release tag, product version and source binding")
    import_index = text.find("- name: Import ephemeral code-signing certificate")
    sign_index = text.find("- name: Authenticode-sign commercial V25 payload")
    verify_index = text.find("- name: Verify Authenticode publisher and trusted timestamp")
    finalize_index = text.find("- name: Finalize signed V25 package")
    cleanup_index = text.find("- name: Remove ephemeral signing certificate and private key")
    reverify_index = text.find("- name: Verify finalized package after private-key cleanup")
    runtime_index = text.find("- name: Licensed V25 runtime validation for exact signed payload")
    manifest_index = text.find("- name: Create signed auto-update manifest")
    provenance_index = text.find("- name: Create commercial checksum and provenance")
    upload_index = text.find("- name: Upload signed commercial candidate")
    ordered = (package_index, bind_index, import_index, sign_index, verify_index, finalize_index, cleanup_index, reverify_index, runtime_index, manifest_index, provenance_index, upload_index)
    if any(index < 0 for index in ordered) or list(ordered) != sorted(ordered):
        errors.append("commercial release must package -> bind exact source/product -> import -> sign -> verify -> finalize -> remove key -> reverify -> optional runtime -> manifest/provenance -> artifact handoff")

    publication_index = text.find("- name: Create draft, verify uploaded bytes, then publish")
    tag_create_index = text.find("$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri", publication_index)
    tag_owned_index = text.find("$tagCreatedByThisRun = $true", tag_create_index)
    draft_index = text.find("$releaseId = [long]$release.id", tag_owned_index)
    tag_verify_index = text.find("Assert-RemoteReleaseTagTargetsWorkflowSha", draft_index)
    download_index = text.find("gh release download $env:RELEASE_TAG", tag_verify_index)
    hash_index = text.find("Draft release asset SHA-256 mismatch for $name; release remains a draft.", download_index)
    signature_index = text.find("verify-v25-signatures.ps1 -Path $payload -ExpectedThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT", hash_index)
    publish_draft_index = text.find("$published = Invoke-RestMethod -Method Patch -Uri $releaseUri", signature_index)
    rollback_index = text.find("rollback-v25-draft-release.ps1", publish_draft_index)
    publish_order = (publication_index, tag_create_index, tag_owned_index, draft_index, tag_verify_index, download_index, hash_index, signature_index, publish_draft_index, rollback_index)
    if any(index < 0 for index in publish_order) or list(publish_order) != sorted(publish_order):
        errors.append("commercial publication must own exact tag -> create exact draft -> assert exact tag target -> verify downloaded bytes/hashes/signatures -> publish exact release -> retain bounded rollback")

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
print("PASS: the commercial V25 workflow is signed-only, exact-source/product bound, ephemeral-key isolated, Authenticode/timestamp verified before and after finalization, runtime-gated for stable releases, provenance-bound across the job boundary, and exact-draft-byte verified after exact-tag assertion before bounded publication.")