from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "assert-v25-commercial-draft-identity.ps1"


def require(source: str, needle: str, message: str) -> None:
    if needle not in source:
        raise SystemExit(f"ERROR: {message}: missing {needle!r}")


def require_order(source: str, earlier: str, later: str, message: str) -> None:
    left = source.find(earlier)
    right = source.find(later)
    if left < 0 or right < 0 or left >= right:
        raise SystemExit(f"ERROR: {message}")


source = TARGET.read_text(encoding="utf-8")

# The commercial draft admission must validate the exact update-manifest generation
# that was already opened and hashed. A provenance digest alone proves byte identity,
# not that those bytes describe the release being admitted.
require(source, "$MaxUpdateManifestBytes", "update manifest is not size bounded")
require(
    source,
    "Read-HeldStrictUtf8 -Held $updateHeld -MaxBytes $MaxUpdateManifestBytes",
    "held update manifest is not read as bounded strict UTF-8",
)
require(
    source,
    "$updateManifestExpectedPropertyCounts",
    "update manifest identity cardinality is not checked before parsing",
)
for name in (
    "schemaVersion",
    "product",
    "target",
    "productVersion",
    "version",
    "packageUri",
    "sha256",
    "signerThumbprint",
):
    require(source, f"{name} = 1", f"update manifest identity does not require exactly one {name}")

require_order(
    source,
    "Assert-JsonPropertyCounts -JsonText $updateManifestText -ExpectedPropertyCounts $updateManifestExpectedPropertyCounts",
    "$updateManifestText | ConvertFrom-Json",
    "update manifest must prove duplicate-resistant identity before full-document ConvertFrom-Json",
)

for semantic in (
    "[int]$updateManifest.schemaVersion -ne 2",
    "[string]$updateManifest.product -ne 'QS3D'",
    "[string]$updateManifest.target -ne 'BricsCAD V25 x64'",
    "[string]$updateManifest.productVersion",
    "[string]$updateManifest.version",
    "[string]$updateManifest.sha256",
    "[string]$updateManifest.signerThumbprint",
    "[string]$updateManifest.packageUri",
):
    require(source, semantic, f"update manifest semantic admission is incomplete ({semantic})")

require(
    source,
    "[Uri]::TryCreate",
    "packageUri is not structurally validated as an absolute URI",
)
require(
    source,
    "[Uri]::UriSchemeHttps",
    "packageUri admission does not require HTTPS",
)

# HTTPS plus a canonical filename is insufficient release provenance: an attacker-
# controlled origin can serve the same filename. Bind the manifest URI to the exact
# GitHub release asset for the expected tag, which also rejects query/fragment drift.
require(
    source,
    '$expectedPackageUri = "https://github.com/trinhtanphat/QS3D-BricsCAD/releases/download/$ExpectedReleaseTag/QS3D-BricsCAD-V25.zip"',
    "packageUri admission is not bound to the canonical QS3D GitHub release asset",
)
require(
    source,
    "[string]::Equals($updatePackageUri.AbsoluteUri, $expectedPackageUri, [StringComparison]::Ordinal)",
    "packageUri admission does not compare the exact trusted release URI",
)
require_order(
    source,
    '$expectedPackageUri = "https://github.com/trinhtanphat/QS3D-BricsCAD/releases/download/$ExpectedReleaseTag/QS3D-BricsCAD-V25.zip"',
    "$updateManifestText = Read-HeldStrictUtf8",
    "trusted package URI must be derived from the already-validated expected release tag before manifest admission",
)

require(
    source,
    "[string]::Equals($metadataVersionRaw, $updateVersionRaw, [StringComparison]::Ordinal)",
    "update manifest assembly version is not bound to PACKAGE-METADATA version",
)
require_order(
    source,
    "$metadataExpectedPropertyCounts = @{",
    "try { return $text | ConvertFrom-Json -ErrorAction Stop }",
    "PACKAGE-METADATA identity cardinality must be checked before parsing",
)
metadata_block = source.split("$metadataExpectedPropertyCounts = @{", 1)[1].split("}", 1)[0]
if "version = 1" not in metadata_block:
    raise SystemExit("ERROR: PACKAGE-METADATA version must appear exactly once before parser admission")

# Same-generation invariant: after Open-HeldGeneration, the manifest must be consumed
# from $updateHeld. Reopening UpdateManifestPath would reintroduce a pathname TOCTOU.
parts = source.split("$updateHeld = Open-HeldGeneration", 1)
if len(parts) != 2:
    raise SystemExit("ERROR: update manifest held-generation admission is missing")
post_open = parts[1]
if "Get-Content -LiteralPath $UpdateManifestPath" in post_open or "[IO.File]::Open($UpdateManifestPath" in post_open:
    raise SystemExit("ERROR: update manifest is reopened by pathname after held-generation admission")

print("V25 commercial draft update-manifest semantic identity preflight passed")
