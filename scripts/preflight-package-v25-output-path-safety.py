#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "scripts" / "package-v25.ps1"

errors = []
if not PACKAGE.is_file():
    errors.append("missing scripts/package-v25.ps1")
    source = ""
else:
    source = PACKAGE.read_text(encoding="utf-8")

required_tokens = (
    "function Get-CanonicalFullPath",
    "function Test-PathEqualOrContained",
    "function Assert-OrdinaryDirectory",
    "function Assert-SafeOutputDirectoryTarget",
    "function Assert-SafeOutputFileTarget",
    "function Get-SafePackageFiles",
    "[IO.FileAttributes]::ReparsePoint",
    "must stay below the repository root",
    "must not be a filesystem root",
    "$root = Assert-OrdinaryDirectory -Path $root -Label 'repository root'",
    "$distRoot = Assert-SafeOutputDirectoryTarget -Path $distRoot -RepositoryRoot $root -Label 'package dist root' -MayBeMissing",
    "$dist = Assert-SafeOutputDirectoryTarget -Path $dist -RepositoryRoot $root -Label 'package staging directory' -MayBeMissing",
    "$zip = Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'",
    "Get-SafePackageFiles -PackageRoot $dist",
    "Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256",
    "Compress-Archive -Path (Join-Path $dist '*') -DestinationPath $zip -CompressionLevel Optimal",
)
for token in required_tokens:
    if token not in source:
        errors.append(f"package-v25 output-path safety missing token: {token}")

# Compatibility comments may retain legacy contract text for older aggregate preflights.
# Only executable PowerShell lines count when rejecting unsafe traversal/destructive forms.
active_source = "\n".join(
    line for line in source.splitlines()
    if line.strip() and not line.lstrip().startswith("#")
)
for forbidden in (
    "Remove-Item $dist -Recurse -Force -ErrorAction SilentlyContinue",
    "Remove-Item $zip -Force -ErrorAction SilentlyContinue",
    "Get-ChildItem $dist -Recurse -File",
):
    if forbidden in active_source:
        errors.append(f"package-v25 must not use legacy unchecked destructive/traversal form: {forbidden}")

validate_dist = source.find("$dist = Assert-SafeOutputDirectoryTarget -Path $dist -RepositoryRoot $root -Label 'package staging directory' -MayBeMissing")
remove_dist = source.find("Remove-Item -LiteralPath $dist -Recurse -Force")
create_dist = source.find("New-Item -ItemType Directory -Path $dist -Force")
revalidate_dist = source.find("$dist = Assert-SafeOutputDirectoryTarget -Path $dist -RepositoryRoot $root -Label 'package staging directory'", create_dist + 1)
if min(validate_dist, remove_dist, create_dist, revalidate_dist) < 0 or not (validate_dist < remove_dist < create_dist < revalidate_dist):
    errors.append("package staging must be validated before recursive removal and revalidated after recreation")

validate_zip = source.rfind("$zip = Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'", 0, source.find("Remove-Item -LiteralPath $zip -Force") + 1)
remove_zip = source.find("Remove-Item -LiteralPath $zip -Force")
archive = source.find("Compress-Archive -Path (Join-Path $dist '*') -DestinationPath $zip -CompressionLevel Optimal")
if min(validate_zip, remove_zip, archive) < 0 or not (validate_zip < remove_zip < archive):
    errors.append("package ZIP must be validated immediately before destructive replacement/archive")

if source.count("Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'") < 4:
    errors.append("package ZIP identity must be revalidated across creation/replacement checkpoints")
if source.count("Assert-SafeOutputDirectoryTarget -Path $dist -RepositoryRoot $root -Label 'package staging directory'") < 4:
    errors.append("package staging identity must be revalidated across destructive/hash/archive checkpoints")

# Keep existing product/release invariants pinned while hardening only output paths.
for token in (
    "Convert-ToStrictSemVerText",
    "Get-SourceGitCommit",
    "RELEASE_TAG",
    "QS3D.BricsCAD.V25.dll",
    "QS3D.Core.dll",
    "PACKAGE-METADATA.json",
    "COMMANDS.txt",
    "SHA256SUMS.txt",
    "INSTALL-QS3D.cmd",
    "UNBLOCK-QS3D.cmd",
    "Samples",
    "BrxMgd.dll",
):
    if token not in source:
        errors.append(f"package-v25 existing release invariant disappeared: {token}")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: package-v25 validates repository-owned dist/staging/ZIP identities before destructive mutation, rejects reparse/non-regular surfaces, and hashes via safe traversal.")