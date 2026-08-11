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


require(parity([("a.dll", "AA"), ("Samples/x.dxf", "BB")], [("a.dll", "AA"), ("Samples/x.dxf", "BB")]),
        "ZIP/staging parity baseline must pass")
require(not parity([("a.dll", "AA")], [("a.dll", "AA"), ("extra.txt", "CC")]),
        "ZIP/staging parity must reject extra ZIP files")
require(not parity([("a.dll", "AA"), ("README.txt", "BB")], [("a.dll", "AA")]),
        "ZIP/staging parity must reject missing ZIP files")
require(not parity([("a.dll", "AA")], [("a.dll", "AB")]),
        "ZIP/staging parity must reject changed ZIP file content")
require(not parity([("a.dll", "AA")], [("a.dll", "AA"), ("A.DLL", "AA")]),
        "ZIP/staging parity must reject case-colliding ZIP paths")

if not MANIFEST.is_file():
    errors.append("missing scripts/new-v25-update-manifest.ps1")
    source = ""
else:
    source = MANIFEST.read_text(encoding="utf-8")

required_tokens = (
    "function Get-ZipEntrySha256",
    "Get-ChildItem -LiteralPath $PackageRoot -File -Recurse",
    "[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)",
    "[Collections.Generic.Dictionary[string,System.IO.Compression.ZipArchiveEntry]]::new([StringComparer]::OrdinalIgnoreCase)",
    "Duplicate/case-colliding staged package path",
    "Duplicate/case-colliding package ZIP path",
    "Package ZIP contains file not present in signed staging",
    "Package ZIP is missing signed staging file",
    "Get-ZipEntrySha256 -Entry $zipByName[$name]",
    "Package ZIP payload does not match signed staging file",
    "$zipByName.Count -ne $stagedByName.Count",
    "Assert-AuthenticodeSigner -Path $destination",
)
for token in required_tokens:
    require(token in source, f"update-manifest ZIP parity guard missing token: {token}")

if source:
    parity_call = source.find("Assert-ZipPayloadMatchesSignedStaging -ZipPath $zip")
    archive_hash = source.find("$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()")
    manifest_write = source.find("$manifest = [ordered]@{")
    require(parity_call >= 0 and archive_hash >= 0 and manifest_write >= 0 and parity_call < archive_hash < manifest_write,
            "full staging parity must succeed before the ZIP hash is published into the update manifest")

if errors:
    print("Update ZIP staging parity preflight FAILED")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("Update ZIP staging parity preflight passed.")
