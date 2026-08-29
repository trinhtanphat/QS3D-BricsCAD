#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "scripts" / "new-v25-update-manifest.ps1"
errors = []


def require(condition, message):
    if not condition:
        errors.append(message)


def parity(staging, zipped):
    staged = [(name.casefold(), digest.upper()) for name, digest in staging]
    archive = [(name.casefold(), digest.upper()) for name, digest in zipped]
    if len({name for name, _ in staged}) != len(staged):
        return False
    if len({name for name, _ in archive}) != len(archive):
        return False
    return dict(staged) == dict(archive)


require(parity([("a.dll", "AA"), ("Samples/x.dxf", "BB")], [("a.dll", "AA"), ("Samples/x.dxf", "BB")]), "ZIP/staging parity baseline must pass")
require(not parity([("a.dll", "AA")], [("a.dll", "AA"), ("extra.txt", "CC")]), "ZIP/staging parity must reject extra ZIP files")
require(not parity([("a.dll", "AA"), ("README.txt", "BB")], [("a.dll", "AA")]), "ZIP/staging parity must reject missing ZIP files")
require(not parity([("a.dll", "AA")], [("a.dll", "AB")]), "ZIP/staging parity must reject changed ZIP file content")
require(not parity([("a.dll", "AA")], [("a.dll", "AA"), ("A.DLL", "AA")]), "ZIP/staging parity must reject case-colliding ZIP paths")

if not MANIFEST.is_file():
    errors.append("missing scripts/new-v25-update-manifest.ps1")
    source = ""
else:
    source = MANIFEST.read_text(encoding="utf-8")

required_tokens = (
    "function Get-ZipEntrySha256",
    "function Get-SafeStagedFiles",
    "[Collections.Generic.Stack[string]]::new()",
    "Get-ChildItem -LiteralPath $directory.FullName -Force -ErrorAction Stop",
    "Signed staging package contains a reparse-backed entry",
    "$stagedFiles = @(Get-SafeStagedFiles -Root $PackageRoot)",
    "$safeStagedFile = Resolve-OrdinaryNonReparseFile -Path $stagedFile.FullName",
    "$state = Get-StableFileState -Path $fullPath",
    "[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)",
    "[Collections.Generic.Dictionary[string,System.IO.Compression.ZipArchiveEntry]]::new([StringComparer]::OrdinalIgnoreCase)",
    "Duplicate/case-colliding staged package path",
    "Duplicate/case-colliding package ZIP path",
    "Package ZIP contains file not present in signed staging",
    "Package ZIP is missing signed staging file",
    "$null = Assert-StableFileState -Expected $stagedState",
    "$stagedHash = [string]$stagedState.Sha256",
    "Get-ZipEntrySha256 -Entry $zipByName[$name]",
    "Package ZIP payload does not match signed staging file",
    "$zipByName.Count -ne $stagedByName.Count",
    "$verified = Resolve-OrdinaryNonReparseFile -Path $destination",
    "Assert-AuthenticodeSigner -Path $verified.FullName",
    "$zipState = Get-StableFileState",
    "$zip = Assert-StableFileState -Expected $zipState",
    "$zipHash = [string]$zipState.Sha256",
)
for token in required_tokens:
    require(token in source, f"update-manifest ZIP parity guard missing token: {token}")

for forbidden in (
    "Get-ChildItem -LiteralPath $PackageRoot.FullName -File -Recurse -Force",
    "Get-FileHash -LiteralPath $stagedByName[$name]",
    "Get-FileHash -LiteralPath $zip.FullName -Algorithm SHA256",
):
    require(forbidden not in source, f"update-manifest ZIP parity retained unsafe/legacy token: {forbidden}")

if source:
    zip_capture = source.find("$zipState = Get-StableFileState")
    parity_call = source.find("Assert-ZipPayloadMatchesSignedStaging -ZipFile $zip -PackageRoot $package")
    zip_recheck = source.find("$zip = Assert-StableFileState -Expected $zipState", parity_call)
    archive_hash = source.find("$zipHash = [string]$zipState.Sha256", zip_recheck)
    manifest_write = source.find("$manifest = [ordered]@{")
    stage_write = source.find("[IO.File]::WriteAllText($stagePath")
    require(
        zip_capture >= 0 and parity_call >= 0 and zip_recheck >= 0 and archive_hash >= 0 and manifest_write >= 0 and stage_write >= 0
        and zip_capture < parity_call < zip_recheck < archive_hash < manifest_write < stage_write,
        "full reparse-safe staging parity must succeed against a state-bound ZIP before the admitted ZIP hash is published into the atomically staged update manifest",
    )

if errors:
    print("Update ZIP staging parity preflight FAILED")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("Update ZIP staging parity preflight passed with ordinary-file/reparse-safe traversal and stable staging/ZIP generation binding.")
