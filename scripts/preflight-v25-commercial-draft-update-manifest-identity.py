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

require(source, "[Uri]::TryCreate", "packageUri is not structurally validated as an absolute URI")
require(source, "[Uri]::UriSchemeHttps", "packageUri admission does not require HTTPS")
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

parts = source.split("$updateHeld = Open-HeldGeneration", 1)
if len(parts) != 2:
    raise SystemExit("ERROR: update manifest held-generation admission is missing")
post_open = parts[1]
if "Get-Content -LiteralPath $UpdateManifestPath" in post_open or "[IO.File]::Open($UpdateManifestPath" in post_open:
    raise SystemExit("ERROR: update manifest is reopened by pathname after held-generation admission")

# Signed payload extraction requires an explicit no-delete handoff. The writer denies
# concurrent writers/deletes. Before it closes, a transition read handle is opened
# without FILE_SHARE_DELETE. After the writer closes, a strict read-only held handle
# is opened with FileShare.Read. A SHA-256 comparison across the handoff detects any
# in-place mutation by a writer that races the short transition-only window. The
# strict held handle then denies write/delete replacement through Authenticode verify.
for token, message in (
    ("$heldPayloads = New-Object System.Collections.Generic.List[object]", "signed payload generations are not retained"),
    ("$output = [IO.File]::Open($destinationFull, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::Read)", "extraction writer does not deny other writers/delete while allowing transition read"),
    ("$output.Flush($true)", "extracted payload generation is not durably flushed before handoff"),
    ("$outputDigest = Get-HeldSha256 -Held $outputHeld", "writer generation digest is not captured before handoff"),
    ("$transition = [IO.File]::Open($destinationFull, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)", "no-delete transition handle is missing"),
    ("$heldStream = [IO.File]::Open($destinationFull, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)", "strict read-only payload hold is missing"),
    ("$heldDigest = Get-HeldSha256 -Held $heldPayload", "strict held generation digest is not verified"),
    ("[string]::Equals($heldDigest, $outputDigest, [StringComparison]::OrdinalIgnoreCase)", "handoff does not bind the strict held bytes to the extracted bytes"),
    ("$heldPayloads.Add($heldPayload)", "strict held payload ownership is not transferred to the held set"),
    ("$extracted.Add($destinationFull)", "held payload path is not passed to Authenticode verification"),
    ("foreach ($heldPayload in $heldPayloads)", "held payload generations are not deterministically disposed"),
    ("$heldPayload.Stream.Dispose()", "held payload stream disposal is missing"),
):
    require(source, token, message)

require_order(source, "$outputDigest = Get-HeldSha256 -Held $outputHeld", "$output.Dispose()", "writer digest must be captured before the writer closes")
require_order(source, "$transition = [IO.File]::Open($destinationFull", "$output.Dispose()", "transition handle must exist before the writer closes")
require_order(source, "$output.Dispose()", "$heldStream = [IO.File]::Open($destinationFull", "strict read-only hold must be acquired after the writer releases write access")
require_order(source, "$heldStream = [IO.File]::Open($destinationFull", "$transition.Dispose()", "transition handle must remain alive until the strict read-only hold exists")
require_order(source, "$heldPayloads.Add($heldPayload)", "& $verifyScriptBlock -Path $extracted.ToArray() -ExpectedThumbprint $ExpectedThumbprint", "strict payload handles must be acquired before Authenticode verification")
require_order(source, "& $verifyScriptBlock -Path $extracted.ToArray() -ExpectedThumbprint $ExpectedThumbprint", "foreach ($heldPayload in $heldPayloads)", "strict payload handles must remain alive until Authenticode verification completes")

# Hosted Windows behavioral proof of the handoff protocol. A mutation in the
# transition-only window must be detected by SHA continuity. On the clean path the
# final strict read handle must deny write/delete while allowing cooperative readers.
if os.name == "nt":
    smoke = r'''
$ErrorActionPreference = 'Stop'
function Get-StreamSha256([IO.Stream]$Stream) {
    $Stream.Seek(0, [IO.SeekOrigin]::Begin) | Out-Null
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return (-join ($sha.ComputeHash($Stream) | ForEach-Object { $_.ToString('x2') })) }
    finally { $sha.Dispose() }
}
$root = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v25-payload-handoff-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($root) | Out-Null
try {
    $tampered = Join-Path $root 'tampered.bin'
    $writer = [IO.File]::Open($tampered, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::Read)
    $transition = $null
    $strict = $null
    try {
        $writer.Write([byte[]](1,2,3,4), 0, 4)
        $writer.Flush($true)
        $expected = Get-StreamSha256 $writer
        $transition = [IO.File]::Open($tampered, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
        $writer.Dispose(); $writer = $null

        $attacker = [IO.File]::Open($tampered, [IO.FileMode]::Open, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
        try { $attacker.Seek(0, [IO.SeekOrigin]::Begin) | Out-Null; $attacker.WriteByte(9); $attacker.Flush($true) }
        finally { $attacker.Dispose() }

        $strict = [IO.File]::Open($tampered, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        $actual = Get-StreamSha256 $strict
        if ([string]::Equals($expected, $actual, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'transition-window mutation was not detected by SHA continuity'
        }
    }
    finally {
        if ($strict) { $strict.Dispose() }
        if ($transition) { $transition.Dispose() }
        if ($writer) { $writer.Dispose() }
    }

    $clean = Join-Path $root 'clean.bin'
    $writer = [IO.File]::Open($clean, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::Read)
    $transition = $null
    $strict = $null
    $reader = $null
    try {
        $writer.Write([byte[]](5,6,7,8), 0, 4)
        $writer.Flush($true)
        $expected = Get-StreamSha256 $writer
        $transition = [IO.File]::Open($clean, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
        $writer.Dispose(); $writer = $null
        $strict = [IO.File]::Open($clean, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        $actual = Get-StreamSha256 $strict
        if (-not [string]::Equals($expected, $actual, [StringComparison]::OrdinalIgnoreCase)) { throw 'clean handoff changed payload bytes' }
        $transition.Dispose(); $transition = $null

        $deleteBlocked = $false
        try { [IO.File]::Delete($clean) } catch { $deleteBlocked = $true }
        if (-not $deleteBlocked) { throw 'strict held payload allowed delete/recreate' }

        $writeBlocked = $false
        try {
            $otherWriter = [IO.File]::Open($clean, [IO.FileMode]::Open, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
            $otherWriter.Dispose()
        }
        catch { $writeBlocked = $true }
        if (-not $writeBlocked) { throw 'strict held payload allowed a writer' }

        $reader = [IO.File]::Open($clean, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        if ($reader.Length -ne 4) { throw 'cooperative verification reader did not observe strict held bytes' }
    }
    finally {
        if ($reader) { $reader.Dispose() }
        if ($strict) { $strict.Dispose() }
        if ($transition) { $transition.Dispose() }
        if ($writer) { $writer.Dispose() }
    }
}
finally {
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
        raise SystemExit(f"ERROR: V25 held signed-payload handoff probe failed: {detail}")

print("V25 commercial draft update-manifest semantic identity and signed-payload handoff preflight passed")
