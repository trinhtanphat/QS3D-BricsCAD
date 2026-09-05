#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"
SIGN = ROOT / "scripts" / "sign-v25.ps1"
VERIFY = ROOT / "scripts" / "verify-v25-signatures.ps1"
IMPORT = ROOT / "scripts" / "import-v25-signing-certificate.ps1"
DOC = ROOT / "docs" / "COMMERCIAL-RELEASE-SIGNING.md"

errors = []

def read(path: Path) -> str:
    if not path.is_file():
        errors.append(f"missing required commercial signing surface: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")

workflow = read(WORKFLOW)
signing = read(SIGN)
verification = read(VERIFY)
certificate_import = read(IMPORT)
runbook = read(DOC)

required_workflow = (
    "name: QS3D Manual V25 Commercial Release",
    "environment: commercial-release",
    "permissions:\n  contents: read",
    "build_sign:",
    "permissions:\n      contents: read",
    "release:",
    "needs: build_sign",
    "contents: write",
    "Import ephemeral code-signing certificate",
    "QS3D_SIGNING_CERT_PFX_BASE64",
    "QS3D_SIGNING_CERT_PASSWORD",
    "Remove ephemeral signing certificate and private key",
    "if: ${{ always() }}",
    "-DeleteKey",
    "Validate exact release tag, product version and source binding",
    "$metadata.productVersion",
    "$env:RELEASE_TAG.Substring(1)",
    "Authenticode-sign commercial V25 payload",
    "Verify Authenticode publisher and trusted timestamp",
    "Finalize signed V25 package",
    "Verify finalized package after private-key cleanup",
    "Licensed V25 runtime validation for exact signed payload",
    "Create commercial checksum and provenance",
    "QS3D-BricsCAD-V25.provenance.json",
    "Verify candidate after job boundary",
    "Create draft, verify uploaded bytes, then publish",
    '$tagRefUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/git/refs"',
    "$tagCreatedByThisRun = $true",
    "$releaseId = [long]$release.id",
    "scripts/invoke-v25-held-release-upload.ps1",
    "$admittedAssets = @(& .\\scripts\\invoke-v25-held-release-upload.ps1",
    "$localAssets[$asset.Name] = $asset",
    "gh release download $env:RELEASE_TAG",
    "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri",
    "Assert-PublishedReleaseMatchesVerifiedTransaction",
    "-ReleaseSnapshot $published",
    "rollback-v25-draft-release.ps1",
)
for token in required_workflow:
    if token not in workflow:
        errors.append(f"release-v25.yml missing required commercial gate token: {token}")

for forbidden in (
    "sign_package:",
    "RELEASE_SIGN_PACKAGE",
    "unsigned prerelease",
    "if: ${{ inputs.sign_package",
    "!inputs.sign_package",
    "& gh @createArgs",
    "gh release edit $env:RELEASE_TAG",
    "gh release upload $env:RELEASE_TAG $resolvedAsset --repo $env:GITHUB_REPOSITORY",
):
    if forbidden in workflow:
        errors.append(f"commercial release workflow exposes obsolete/unsafe publication shape: {forbidden}")

build_index = workflow.find("  build_sign:")
publish_index = workflow.find("  release:")
write_index = workflow.find("contents: write")
if min(build_index, publish_index, write_index) < 0 or not (build_index < publish_index <= write_index):
    errors.append("contents: write must appear only in the downstream release job after build_sign")
if workflow.count("contents: write") != 1:
    errors.append("commercial release workflow must grant contents: write exactly once")
if workflow.find("Import ephemeral code-signing certificate") > workflow.find("Authenticode-sign commercial V25 payload"):
    errors.append("certificate import must happen before signing")
if workflow.find("Authenticode-sign commercial V25 payload") > workflow.find("Verify Authenticode publisher and trusted timestamp"):
    errors.append("signing must happen before signature verification")
if workflow.find("Verify Authenticode publisher and trusted timestamp") > workflow.find("Finalize signed V25 package"):
    errors.append("signature verification must happen before signed package finalization")
if workflow.find("Remove ephemeral signing certificate and private key") > workflow.find("Verify finalized package after private-key cleanup"):
    errors.append("private signing key must be removed before final package verification")
if workflow.find("Verify candidate after job boundary") > workflow.find("Create draft, verify uploaded bytes, then publish"):
    errors.append("release job must verify transferred candidate before creating a draft release")

publication = workflow.find("Create draft, verify uploaded bytes, then publish")
tag_create = workflow.find("$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri", publication)
tag_owned = workflow.find("$tagCreatedByThisRun = $true", tag_create)
draft_id = workflow.find("$releaseId = [long]$release.id", tag_owned)
held_upload = workflow.find("$admittedAssets = @(& .\\scripts\\invoke-v25-held-release-upload.ps1", draft_id)
download = workflow.find("gh release download $env:RELEASE_TAG", held_upload)
signature = workflow.find("verify-v25-signatures.ps1 -Path $payload -ExpectedThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT", download)
publish = workflow.find("$published = Invoke-RestMethod -Method Patch -Uri $releaseUri", signature)
publish_assert = workflow.find("Assert-PublishedReleaseMatchesVerifiedTransaction", publish)
publish_snapshot = workflow.find("-ReleaseSnapshot $published", publish_assert)
rollback = workflow.find("rollback-v25-draft-release.ps1", publish_snapshot)
publication_order = (publication, tag_create, tag_owned, draft_id, held_upload, download, signature, publish, publish_assert, publish_snapshot, rollback)
if any(index < 0 for index in publication_order) or list(publication_order) != sorted(publication_order):
    errors.append("commercial publication must positively own exact tag -> capture exact draft -> upload held local generations -> verify downloaded signed bytes -> publish exact release -> verify successful response against exact transaction -> retain bounded rollback")

required_signing = (
    "Get-SignTool",
    "sign /sha1",
    "/fd SHA256",
    "/tr $TimestampServer",
    "/td SHA256",
    "Set-AuthenticodeSignature",
    "-HashAlgorithm SHA256",
    "TimeStamperCertificate",
    "verify /pa /all /v",
)
for token in required_signing:
    if token not in signing:
        errors.append(f"sign-v25.ps1 missing signing hardening token: {token}")

required_verification = (
    "[Parameter(Mandatory = $true)]",
    "ExpectedThumbprint",
    "TimeStamperCertificate",
    "Get-SignTool",
    "verify /pa /all /v",
    "Unexpected signer",
)
for token in required_verification:
    if token not in verification:
        errors.append(f"verify-v25-signatures.ps1 missing fail-closed token: {token}")

required_import = (
    "Import-PfxCertificate",
    "-Exportable:$false",
    "Cert:\\CurrentUser\\My",
    "already exists in Cert:\\CurrentUser\\My",
    "HasPrivateKey",
    "1.3.6.1.5.5.7.3.3",
    "Remove-ImportedCertificates",
    "-DeleteKey",
    "SIGNING_THUMBPRINT=",
    "IMPORTED_THUMBPRINTS=",
    "[Array]::Clear",
)
for token in required_import:
    if token not in certificate_import:
        errors.append(f"import-v25-signing-certificate.ps1 missing secret-lifecycle token: {token}")

for token in (
    "QS3D_SIGNING_CERT_PFX_BASE64",
    "QS3D_SIGNING_CERT_PASSWORD",
    "QS3D_SIGNING_CERT_THUMBPRINT",
    "QS3D_TIMESTAMP_SERVER",
    "commercial-release",
    "RFC3161",
    "rotation",
    "revocation",
    "draft",
    "signtool verify /pa /all /v",
):
    if token not in runbook:
        errors.append(f"commercial signing runbook missing required operator guidance: {token}")

if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} commercial release signing hardening error(s).")
    sys.exit(1)

print("PASS: commercial V25 release remains signed-only, exact-version/source bound, RFC3161 PE timestamped, ephemeral-key cleaned, least-privilege published, exact-tag owned, held-generation draft-byte verified, exact publish response transaction-verified, and restart-safe under bounded rollback.")
