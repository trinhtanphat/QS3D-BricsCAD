import os
import subprocess
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
require(
    source,
    "[string]::Equals($updatePackageUriRaw, $expectedPackageUri, [StringComparison]::Ordinal)",
    "packageUri admission must reject alternate textual origins, query/fragment drift and URI aliases",
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

# Signed payload generations must remain held from extraction through Authenticode
# verification. Merely retaining their path lets a same-name generation replace the
# admitted bytes between extraction/length validation and verifier reopen.
for token, message in (
    ("$heldPayloads = New-Object System.Collections.Generic.List[object]", "signed payload generations are not retained"),
    ("[IO.FileAccess]::ReadWrite, [IO.FileShare]::Read", "extracted payload handle does not deny writers/delete while permitting verification readers"),
    ("$output.Flush($true)", "extracted payload generation is not durably flushed before verification"),
    ("$heldPayloads.Add([pscustomobject]@{ Stream = $output; Path = $destinationFull; Length = [int64]$entry.Length })", "extracted payload handle ownership is not transferred to the held set"),
    ("$extracted.Add($destinationFull)", "held payload path is not passed to Authenticode verification"),
    ("foreach ($heldPayload in $heldPayloads)", "held payload generations are not deterministically disposed"),
    ("$heldPayload.Stream.Dispose()", "held payload stream disposal is missing"),
):
    require(source, token, message)

require_order(
    source,
    "$heldPayloads = New-Object System.Collections.Generic.List[object]",
    "& $verifyScriptBlock -Path $extracted.ToArray() -ExpectedThumbprint $ExpectedThumbprint",
    "payload handles must be acquired before Authenticode verification",
)
require_order(
    source,
    "& $verifyScriptBlock -Path $extracted.ToArray() -ExpectedThumbprint $ExpectedThumbprint",
    "foreach ($heldPayload in $heldPayloads)",
    "payload handles must remain alive until Authenticode verification completes",
)

# Hosted Windows behavioral proof for the sharing contract used by the production
# extraction handles: a cooperative read reopen is allowed, while delete and write
# replacement are denied for the exact held generation.
if os.name == "nt":
    smoke = r'''
$ErrorActionPreference = 'Stop'
$root = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v25-payload-hold-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($root) | Out-Null
$path = Join-Path $root 'payload.bin'
[IO.File]::WriteAllBytes($path, [byte[]](1,2,3,4))
$held = $null
$reader = $null
try {
    $held = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::Read)

    $deleteBlocked = $false
    try { [IO.File]::Delete($path) } catch { $deleteBlocked = $true }
    if (-not $deleteBlocked) { throw 'held payload generation allowed delete/recreate' }

    $writeBlocked = $false
    try {
        $writer = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
        $writer.Dispose()
    }
    catch { $writeBlocked = $true }
    if (-not $writeBlocked) { throw 'held payload generation allowed a second writer' }

    $reader = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    if ($reader.Length -ne 4) { throw 'cooperative verification reader did not observe held payload bytes' }
}
finally {
    if ($reader) { $reader.Dispose() }
    if ($held) { $held.Dispose() }
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
'''
    completed = subprocess.run(
        ["pwsh", "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", smoke],
        cwd=ROOT,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="strict",
        check=False,
    )
    if completed.returncode != 0:
        detail = (completed.stderr or completed.stdout).strip()
        raise SystemExit(f"ERROR: V25 held signed-payload behavioral probe failed: {detail}")

print("V25 commercial draft update-manifest semantic identity and held signed-payload preflight passed")
