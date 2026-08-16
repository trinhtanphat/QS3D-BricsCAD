#!/usr/bin/env python3
from pathlib import Path
import sys
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/release-v25-cloud.yml"
DOC = ROOT / "docs/CLOUD-V25-PREVIEW-RELEASE.md"
PINNED_SHA256 = "F44DF674C0E165D96BF579E243B20A8301E3F395F929779F47BF39A7D9DACDE1"
PINNED_PUBLIC_URL = "https://storage.googleapis.com/production-boa-storage/ftp/release/en_US/BricsCAD/Windows/25.2.10/BricsCAD-V25.2.10-1-en_US%28x64%29.msi"
errors = []


def normalized_port(parts):
    if parts.port is not None:
        return parts.port
    if parts.scheme.lower() == "https":
        return 443
    if parts.scheme.lower() == "http":
        return 80
    return None


def is_same_pinned_object(candidate):
    try:
        expected = urlsplit(PINNED_PUBLIC_URL)
        actual = urlsplit(candidate)
        return (
            actual.scheme.lower() == expected.scheme.lower()
            and (actual.hostname or "").lower() == (expected.hostname or "").lower()
            and normalized_port(actual) == normalized_port(expected)
            and actual.path == expected.path
            and actual.username is None
            and actual.password is None
            and not actual.fragment
        )
    except ValueError:
        return False


for value in (PINNED_PUBLIC_URL, PINNED_PUBLIC_URL + "?X-Goog-Signature=abc&X-Goog-Expires=600"):
    if not is_same_pinned_object(value):
        errors.append("cloud fallback URI regression unexpectedly rejected approved object: " + value)
for value in (
    PINNED_PUBLIC_URL + ".bak",
    PINNED_PUBLIC_URL + "/other",
    PINNED_PUBLIC_URL.replace("storage.googleapis.com", "storage.googleapis.com.evil.example"),
    PINNED_PUBLIC_URL.replace("https://", "http://"),
    PINNED_PUBLIC_URL.replace("https://storage.googleapis.com/", "https://storage.googleapis.com:444/"),
    PINNED_PUBLIC_URL.replace("https://", "https://user:pass@"),
    PINNED_PUBLIC_URL + "#fragment",
):
    if is_same_pinned_object(value):
        errors.append("cloud fallback URI regression unexpectedly accepted different object: " + value)

if not WORKFLOW.is_file():
    errors.append("missing .github/workflows/release-v25-cloud.yml")
else:
    text = WORKFLOW.read_text(encoding="utf-8")
    required = (
        "workflow_dispatch:",
        "github.event_name == 'workflow_dispatch' && inputs.confirm_release == 'RELEASE'",
        "actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1",
        "actions/setup-python@5fda3b95a4ea91299a34e894583c3862153e4b97",
        "actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68",
        "actions/cache/restore@55cc8345863c7cc4c66a329aec7e433d2d1c52a9",
        "actions/cache/save@55cc8345863c7cc4c66a329aec7e433d2d1c52a9",
        "actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a",
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
        "$fallbackUri.UserInfo",
        "$fallbackUri.Fragment",
        "[string]::Equals($fallbackUri.Scheme, $publicUri.Scheme, [StringComparison]::OrdinalIgnoreCase)",
        "[string]::Equals($fallbackUri.Host, $publicUri.Host, [StringComparison]::OrdinalIgnoreCase)",
        "$fallbackUri.Port -ne $publicUri.Port",
        "[string]::Equals($fallbackUri.AbsolutePath, $publicUri.AbsolutePath, [StringComparison]::Ordinal)",
        "only its signed query string may differ",
    )
    for token in required:
        if token not in text:
            errors.append("cloud V25 workflow missing cache/pinning/signature/version/URI/manual-release token: " + token)

    if ".StartsWith($env:BRICSCAD_V25_PUBLIC_MSI_URL" in text:
        errors.append("cloud V25 fallback URI must not use string-prefix matching for pinned-object identity")

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
        errors.append("cloud V25 workflow must define approved mirror and pinned HTTPS public candidates")
    elif not candidates_index < mirror_index < public_index:
        errors.append("approved mirror must be attempted before the pinned HTTPS public candidate after cache miss")

    uri_parse_index = text.find("[Uri]::TryCreate($env:BRICSCAD_V25_MSI_URL")
    uri_path_index = text.find("$fallbackUri.AbsolutePath", uri_parse_index if uri_parse_index >= 0 else 0)
    candidates_index_after_uri = text.find("$candidates = @(", uri_path_index if uri_path_index >= 0 else 0)
    if min(uri_parse_index, uri_path_index, candidates_index_after_uri) < 0 or not uri_parse_index < uri_path_index < candidates_index_after_uri:
        errors.append("cloud V25 secret fallback must be bound to the pinned URI object before it can enter the download candidate list")

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
        "only its query string may differ",
    ):
        if token not in doc:
            errors.append("cloud V25 release documentation missing integrity/cache/URI statement: " + token)

print("QS3D cloud V25 preview release preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: cloud V25 preview remains manual-only; secret fallback is bound to the exact pinned official MSI object except query, the V25.2.10 digest is pinned, cache hits are re-verified, Bricsys Authenticode + MSI identity checks precede bounded extraction, and download/extraction waits are finite.")
