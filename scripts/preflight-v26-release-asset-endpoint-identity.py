#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
publisher_path = root / "scripts" / "publish-v26-release.ps1"
publisher = publisher_path.read_text(encoding="utf-8")

EXPECTED_UPLOAD = '$expectedUploadUrl = "https://uploads.github.com/repos/$env:GITHUB_REPOSITORY/releases/$releaseId/assets{?name,label}"'
UPLOAD_CHECK = 'if (-not [string]::Equals([string]$release.upload_url, $expectedUploadUrl, [StringComparison]::Ordinal)) { throw "V26 draft upload endpoint does not belong to the admitted repository/release identity." }'
UPLOAD_BASE = "$uploadBase = $expectedUploadUrl -replace '\\{\\?name,label\\}$', ''"
HELD_UPLOAD = '& .\\scripts\\invoke-v26-held-release-upload.ps1 `'
EXPECTED_ASSET = '$expectedAssetApiUrl = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/assets/$uploadedAssetId"'
ASSET_CHECK = 'if (-not [string]::Equals([string]$uploadedAsset.url, $expectedAssetApiUrl, [StringComparison]::Ordinal)) { throw "Uploaded V26 release asset API endpoint identity mismatch for $expectedAsset. Release remains a draft." }'
DOWNLOAD = 'Invoke-WebRequest -Method Get -Uri $expectedAssetApiUrl -Headers $assetDownloadHeaders -OutFile $downloadedAsset -UseBasicParsing'


def active_line(text: str, literal: str) -> bool:
    return any(line.strip() == literal for line in text.splitlines())


def validate(text: str) -> list[str]:
    errors: list[str] = []
    for literal, label in (
        (EXPECTED_UPLOAD, "canonical upload endpoint construction"),
        (UPLOAD_CHECK, "exact draft upload endpoint binding"),
        (UPLOAD_BASE, "upload base derivation from validated endpoint"),
        (EXPECTED_ASSET, "canonical asset API endpoint construction"),
        (ASSET_CHECK, "exact uploaded asset API endpoint binding"),
        (DOWNLOAD, "verification download through validated asset endpoint"),
    ):
        if not active_line(text, literal):
            errors.append(f"V26 publisher missing active {label}")

    upload_expected_i = text.find(EXPECTED_UPLOAD)
    upload_check_i = text.find(UPLOAD_CHECK)
    upload_base_i = text.find(UPLOAD_BASE)
    held_upload_i = text.find(HELD_UPLOAD)
    if min(upload_expected_i, upload_check_i, upload_base_i, held_upload_i) >= 0:
        if not (upload_expected_i < upload_check_i < upload_base_i < held_upload_i):
            errors.append("V26 upload endpoint must be constructed, verified, and normalized before any held asset upload")

    asset_id_i = text.find("$uploadedAssetId = [long]$uploadedAsset.id")
    asset_expected_i = text.find(EXPECTED_ASSET)
    asset_check_i = text.find(ASSET_CHECK)
    download_i = text.find(DOWNLOAD)
    if min(asset_id_i, asset_expected_i, asset_check_i, download_i) >= 0:
        if not (asset_id_i < asset_expected_i < asset_check_i < download_i):
            errors.append("V26 asset API endpoint must be bound to the exact uploaded asset id before authenticated verification download")

    if "Invoke-WebRequest -Method Get -Uri ([string]$uploadedAsset.url)" in text:
        errors.append("V26 verification still follows the untrusted server-returned asset URL directly")
    if "$uploadBase = $release.upload_url -replace" in text:
        errors.append("V26 upload base still derives directly from an unbound server-returned upload URL")
    return errors


def require_mutation_failure(label: str, mutated: str) -> None:
    if mutated == publisher:
        raise SystemExit(f"{label} mutation probe could not mutate publisher fixture")
    if not validate(mutated):
        raise SystemExit(f"{label} mutation probe did not fail closed")


errors = validate(publisher)
if errors:
    raise SystemExit("V26 release asset endpoint identity preflight failed: " + "; ".join(errors))

for label, literal in (
    ("upload endpoint comparison", UPLOAD_CHECK + "\n"),
    ("asset endpoint comparison", ASSET_CHECK + "\n"),
    ("canonical upload endpoint", EXPECTED_UPLOAD + "\n"),
    ("canonical asset endpoint", EXPECTED_ASSET + "\n"),
):
    require_mutation_failure(label, publisher.replace(literal, "", 1))

require_mutation_failure(
    "upload base bypass",
    publisher.replace(UPLOAD_BASE, "$uploadBase = $release.upload_url -replace '\\{\\?name,label\\}$', ''", 1),
)
require_mutation_failure(
    "asset download bypass",
    publisher.replace(DOWNLOAD, "Invoke-WebRequest -Method Get -Uri ([string]$uploadedAsset.url) -Headers $assetDownloadHeaders -OutFile $downloadedAsset -UseBasicParsing", 1),
)

print("PASS V26 release asset upload/download endpoints are bound to the admitted repository/release/asset transaction identities")
