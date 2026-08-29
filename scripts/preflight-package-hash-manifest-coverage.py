#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "scripts" / "package-v25.ps1"
FINALIZE = ROOT / "scripts" / "finalize-v25-signed-package.ps1"
INSTALL = ROOT / "scripts" / "install-v25-autoload.ps1"
UPDATE = ROOT / "scripts" / "update-v25.ps1"
errors = []


def require(condition, message):
    if not condition:
        errors.append(message)


def read(path):
    if not path.is_file():
        errors.append(f"missing required package-integrity source: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


def executable_lines(source):
    return "\n".join(
        line for line in source.splitlines()
        if line.strip() and not line.lstrip().startswith("#")
    )


def exact_coverage(manifest_names, actual_names):
    manifest = [name.casefold() for name in manifest_names]
    actual = [name.casefold() for name in actual_names if name.casefold() != "sha256sums.txt"]
    return len(manifest) == len(set(manifest)) and set(manifest) == set(actual)


# Contract-level negative/positive cases independent of PowerShell runtime availability.
require(exact_coverage(["a.dll", "Samples/x.dxf"], ["a.dll", "Samples/x.dxf", "SHA256SUMS.txt"]),
        "coverage model baseline must pass")
require(exact_coverage(
            ["a.dll", "Samples/SHA256SUMS.txt"],
            ["a.dll", "Samples/SHA256SUMS.txt", "SHA256SUMS.txt"]),
        "coverage model must treat only the root SHA256SUMS.txt as the manifest")
require(not exact_coverage(["a.dll"], ["a.dll", "COMMANDS.txt", "SHA256SUMS.txt"]),
        "coverage model must reject an unmanifested package file")
require(not exact_coverage(["a.dll", "COMMANDS.txt"], ["a.dll", "SHA256SUMS.txt"]),
        "coverage model must reject a manifest entry without a package file")
require(not exact_coverage(["a.dll", "A.DLL"], ["a.dll", "SHA256SUMS.txt"]),
        "coverage model must reject case-colliding manifest entries")

package_source = read(PACKAGE)
finalize_source = read(FINALIZE)
install_source = read(INSTALL)
update_source = read(UPDATE)
package_active = executable_lines(package_source)

# Both package construction and signed-package finalization must traverse the payload
# fail-closed: reparse/non-regular entries are rejected before hashing. This preserves
# complete manifest coverage without reopening the staging tree through an unchecked
# recursive pathname enumeration.
package_traversal_tokens = (
    "function Get-SafePackageFiles",
    "Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop",
    "[IO.FileAttributes]::ReparsePoint",
    "Package staging contains a reparse-backed entry",
    "$files.Add($item)",
    "return @($files | Sort-Object FullName)",
    "$hashLines = Get-SafePackageFiles -PackageRoot $dist | ForEach-Object",
    "SHA256SUMS.txt",
    "Get-FileHash",
    "-Algorithm SHA256",
)
for token in package_traversal_tokens:
    require(token in package_source, f"package-v25.ps1 safe hash traversal missing token: {token}")
require("Get-ChildItem $dist -Recurse -File" not in package_active,
        "package-v25.ps1 must not bypass safe package traversal with recursive Get-ChildItem")

finalizer_tokens = (
    "function Get-SafePackageFiles",
    "Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop",
    "[IO.FileAttributes]::ReparsePoint",
    "$files.Add($item)",
    "return @($files | Sort-Object FullName)",
    "$hashLines = foreach ($file in Get-SafePackageFiles -PackageRoot $package)",
    "SHA256SUMS.txt",
    "Get-FileHash",
    "-Algorithm SHA256",
)
for token in finalizer_tokens:
    require(token in finalize_source, f"finalize-v25-signed-package.ps1 safe hash traversal missing token: {token}")
require("Get-ChildItem -LiteralPath $package -Recurse -File" not in finalize_source,
        "finalize-v25-signed-package.ps1 must not bypass safe package traversal with recursive Get-ChildItem")

require("install-v25-autoload.ps1" in package_source,
        "package-v25.ps1 must package the installer that enforces internal manifest coverage")
require("Where-Object { $_.Name -ne 'SHA256SUMS.txt' }" not in package_source,
        "package-v25.ps1 must not exclude nested payloads merely because their basename is SHA256SUMS.txt")

installer_tokens = (
    "[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)",
    "Duplicate SHA256SUMS payload entry",
    "Get-ChildItem -LiteralPath $Directory -File -Recurse",
    "Duplicate/case-colliding package payload path",
    "Unhashed package payload",
    "SHA256SUMS entry does not map to a regular package file",
    "$actualEntries.Count -ne $manifestEntries.Count",
)
for token in installer_tokens:
    require(token in install_source, f"installer manifest-coverage guard missing token: {token}")

if install_source:
    coverage_index = install_source.find("Unhashed package payload")
    mutation_index = install_source.find("$commands = Assert-PackageIntegrity")
    require(coverage_index >= 0 and mutation_index >= 0 and coverage_index < mutation_index,
            "installer must define complete manifest coverage before the install path invokes package integrity")

updater_tokens = (
    "$actualZipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()",
    "$actualZipHash -ne $expectedZipHash",
    "Assert-SafeArchive -ZipPath $zipPath",
    "Expand-Archive -LiteralPath $zipPath",
    "$installer = Join-Path $extractRoot 'install-v25-autoload.ps1'",
    "& $installer @arguments",
)
for token in updater_tokens:
    require(token in update_source, f"secure updater trust-chain guard missing token: {token}")

if update_source:
    hash_index = update_source.find("$actualZipHash -ne $expectedZipHash")
    extract_index = update_source.find("Expand-Archive -LiteralPath $zipPath")
    installer_index = update_source.find("& $installer @arguments")
    require(hash_index >= 0 and extract_index >= 0 and installer_index >= 0 and hash_index < extract_index < installer_index,
            "updater must verify the whole ZIP before extraction and delegate installation only afterwards")

# Deterministic regression probes: the guard must reject both traversal bypass and a
# producer that silently drops the staging reparse rejection used by safe hashing.
def producer_safe(source):
    active = executable_lines(source)
    return (
        "function Get-SafePackageFiles" in source
        and "$hashLines = Get-SafePackageFiles -PackageRoot $dist | ForEach-Object" in source
        and "Package staging contains a reparse-backed entry" in source
        and "Get-ChildItem $dist -Recurse -File" not in active
    )

require(producer_safe(package_source), "package producer safe-traversal model baseline must pass")
require(not producer_safe(package_source.replace(
    "$hashLines = Get-SafePackageFiles -PackageRoot $dist | ForEach-Object",
    "$hashLines = Get-ChildItem $dist -Recurse -File | Sort-Object FullName | ForEach-Object",
    1,
)), "package producer safe-traversal model must reject recursive pathname enumeration")
require(not producer_safe(package_source.replace(
    "Package staging contains a reparse-backed entry",
    "Package staging permits a reparse-backed entry",
    1,
)), "package producer safe-traversal model must reject removal of staging reparse rejection")

if errors:
    print("Package hash-manifest coverage preflight FAILED")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("Package hash-manifest coverage preflight passed.")
