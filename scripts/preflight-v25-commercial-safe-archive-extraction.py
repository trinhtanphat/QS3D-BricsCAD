#!/usr/bin/env python3
"""Fail closed unless V25 commercial release extraction is bounded, Windows-safe and generation-bound."""

from __future__ import annotations
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"
EXTRACTOR = ROOT / "scripts" / "expand-v25-commercial-candidate.ps1"
RUNTIME_TEST = ROOT / "scripts" / "test-v25-commercial-safe-archive-extraction.ps1"
REGISTRY = ROOT / "scripts" / "test-v25-package-verifier.ps1"
CALL = ".\\scripts\\expand-v25-commercial-candidate.ps1"
DEVICE = "con|prn|aux|nul|com(?:[1-9]|¹|²|³)|lpt(?:[1-9]|¹|²|³)"


def contract_errors(workflow: str, extractor: str | None) -> list[str]:
    errors: list[str] = []
    if "Expand-Archive" in workflow:
        errors.append("raw Expand-Archive is forbidden at V25 commercial release trust boundaries")
    if extractor is None:
        return errors + ["missing reusable bounded V25 commercial candidate extractor"]
    if ".CopyTo(" in extractor:
        errors.append("archive materialization must not use unbounded CopyTo")
    required = (
        ("[IO.Compression.ZipArchive]", "ZipArchive inspection"),
        ("$zipStream.Length -le 0 -or $zipStream.Length -gt $MaxPackageBytes", "compressed-size budget"),
        ("$entryCount -gt $MaxEntries", "entry-count budget"),
        ("$expandedBytes += [int64]$entry.Length", "declared expanded-byte accounting"),
        ("$materializedBytes -gt ($MaxExpandedBytes - [int64]$read)", "actual pre-write expanded-size enforcement"),
        ("$input.Read($buffer", "bounded read loop"),
        ("$output.Write($buffer", "bounded write loop"),
        ("[IO.Path]::IsPathRooted($name)", "rooted-path rejection"),
        ("$name.IndexOf([char]0)", "NUL rejection"),
        ("$name.IndexOf([char]92) -ge 0", "single-backslash rejection"),
        ("$name.Contains(':')", "drive/ADS rejection"),
        ("$segment -eq '..'", "parent-traversal rejection"),
        ("GetInvalidFileNameChars", "Windows invalid-name rejection"),
        (DEVICE, "Windows device-name rejection including superscript digits"),
        ("(?:[.]|$)", "device-name extension boundary"),
        ("EndsWith('.',", "trailing-dot rejection"),
        ("EndsWith(' ',", "trailing-space rejection"),
        ("HashSet[string]", "duplicate target tracking"),
        ("[StringComparer]::OrdinalIgnoreCase", "case-alias rejection"),
        ("StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)", "destination containment"),
        ("[IO.FileMode]::CreateNew", "no-clobber extraction"),
        ("FILE_FLAG_OPEN_REPARSE_POINT", "native no-follow directory open"),
        ("FILE_FLAG_BACKUP_SEMANTICS", "native directory handle open"),
        ("FILE_LIST_DIRECTORY = 0x00000001", "directory-list access required to deny rename/delete substitution"),
        ("FILE_SHARE_WRITE = 0x00000002", "native write-sharing constant"),
        ("FILE_SHARE_READ | FILE_SHARE_WRITE", "directory hold permits child materialization while delete sharing remains denied"),
        ("FILE_LIST_DIRECTORY | FILE_READ_ATTRIBUTES", "directory hold requests list access so denied delete sharing blocks rename substitution"),
        ("GetFileInformationByHandle", "handle-bound directory attributes"),
        ("GetFinalPathNameByHandleW", "handle-bound final path"),
        ("OpenDirectoryNoFollow", "no-follow directory hold"),
        ("$parentHold = Open-HeldSafeDirectory", "destination-parent generation hold"),
        ("$rootHold = Open-HeldSafeDirectory", "destination-root generation hold"),
        ("Ensure-HeldSafeDirectory -Path $parent", "file-parent generation admission"),
        ("$directoryHolds.ContainsKey($parent)", "file-parent hold assertion"),
        ("$holdOrder[$i].Handle.Dispose()", "post-materialization hold disposal"),
        ("leaving destination residue because its generation can no longer be proven safe for pathname cleanup", "fail-closed ambiguous-generation cleanup"),
        ("$expectedZipSha256 = [string]$env:QS3D_V25_COMMERCIAL_ZIP_SHA256", "admitted ZIP digest input"),
        ("$parsedDigestBytes = $zipSha.ComputeHash($zipStream)", "exact opened-stream hashing"),
        ("[string]::Equals($parsedDigest, $expectedZipSha256, [StringComparison]::OrdinalIgnoreCase)", "exact stream digest comparison"),
        ("$zipStream.Position = 0", "rewind exact stream before parse"),
    )
    for token, label in required:
        if token not in extractor:
            errors.append(f"missing {label}: {token}")
    if "FILE_SHARE_DELETE" in extractor:
        errors.append("directory-generation holds must deny delete/rename sharing")
    if "Get-FileHash" in extractor:
        errors.append("extractor must not reopen the ZIP pathname for digest verification")
    if "Remove-Item -LiteralPath $destinationFull" in extractor:
        errors.append("failed extraction must not release generation holds and then recursively delete an unproven destination pathname")

    open_zip = extractor.find("$zipStream = [IO.File]::Open($zipFull")
    compute = extractor.find("$parsedDigestBytes = $zipSha.ComputeHash($zipStream)", open_zip)
    compare = extractor.find("[string]::Equals($parsedDigest, $expectedZipSha256", compute)
    rewind = extractor.find("$zipStream.Position = 0", compare)
    parse = extractor.find("$archive = [IO.Compression.ZipArchive]::new($zipStream", rewind)
    if not (0 <= open_zip < compute < compare < rewind < parse):
        errors.append("exact ZIP stream must be opened, hashed, compared, rewound and only then parsed")
    parent_hold = extractor.find("$parentHold = Open-HeldSafeDirectory")
    create_root = extractor.find("[IO.Directory]::CreateDirectory($destinationFull)")
    root_hold = extractor.find("$rootHold = Open-HeldSafeDirectory")
    parent_assert = extractor.find("$directoryHolds.ContainsKey($parent)")
    create_file = extractor.find("[IO.File]::Open([string]$record.Target, [IO.FileMode]::CreateNew")
    dispose_hold = extractor.find("$holdOrder[$i].Handle.Dispose()")
    if not (0 <= parent_hold < create_root < root_hold < parent_assert < create_file < dispose_hold):
        errors.append("directory generations must be held before root/file creation and remain held through all writes")

    boundaries = (
        ("Verify finalized package after private-key cleanup", "post-key-cleanup", "$heldZip", "$verificationRoot"),
        ("Verify candidate after job boundary", "job-boundary", "$heldZip", "$extract"),
        ("$extract = Join-Path $downloadRoot 'verified-package'", "downloaded-draft", "$heldRemoteZip", "$extract"),
    )
    for marker, label, zip_var, root_var in boundaries:
        start = workflow.find(marker)
        if start < 0:
            errors.append(f"missing {label} verification region")
            continue
        end = workflow.find("$downloadedIdentity =", start) if label == "downloaded-draft" else workflow.find("\n      - name:", start + len(marker))
        region = workflow[start:] if end < 0 else workflow[start:end]
        for token in (CALL, f"-ZipPath {zip_var}", f"-DestinationRoot {root_var}", "-MaxPackageBytes 268435456", "-MaxExpandedBytes 536870912", "-MaxEntries 4096"):
            if token not in region:
                errors.append(f"{label} safe extractor contract missing: {token}")
        call_index = region.find(CALL)
        sig_index = region.find("verify-v25-signatures.ps1")
        if call_index >= 0 and sig_index >= 0 and call_index > sig_index:
            errors.append(f"{label} extraction admission must precede payload signature verification")
    if workflow.count(CALL) != 3:
        errors.append("safe extractor must be invoked exactly once at each of three commercial verification boundaries")
    return errors


def runtime_errors(registry: str | None, runtime: str | None) -> list[str]:
    errors: list[str] = []
    if registry is None or "test-v25-commercial-safe-archive-extraction.ps1" not in registry:
        errors.append("commercial safe archive runtime test is not registered in package-integrity harness")
    if runtime is None:
        return errors + ["missing commercial safe archive runtime test"]
    required = (
        "verify-v25-held-file.ps1",
        "QS3D_V25_COMMERCIAL_ZIP_SHA256",
        "exact parsed generation digest mismatch",
        "generation changed between admission and exact-stream extraction",
        "Hashing an unrelated release asset overwrote",
        "../escape.txt", "Payload.txt", "payload.TXT", "COM¹.txt", "nested\\escape.txt", "payload.txt:stream", "payload./file.txt",
        "-MaxEntries 1", "-MaxExpandedBytes 32", "-MaxPackageBytes 1", "must not already exist", "fail-closed residue",
    )
    for token in required:
        if token not in runtime:
            errors.append(f"runtime archive test missing fixture/contract: {token}")
    return errors


def self_test() -> list[str]:
    safe = f"""
FILE_FLAG_OPEN_REPARSE_POINT FILE_FLAG_BACKUP_SEMANTICS
FILE_LIST_DIRECTORY = 0x00000001
FILE_SHARE_WRITE = 0x00000002
FILE_SHARE_READ | FILE_SHARE_WRITE
FILE_LIST_DIRECTORY | FILE_READ_ATTRIBUTES
GetFileInformationByHandle GetFinalPathNameByHandleW OpenDirectoryNoFollow
$parentHold = Open-HeldSafeDirectory
[IO.Directory]::CreateDirectory($destinationFull)
$rootHold = Open-HeldSafeDirectory
$zipStream = [IO.File]::Open($zipFull,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
if ($zipStream.Length -le 0 -or $zipStream.Length -gt $MaxPackageBytes) {{throw}}
$expectedZipSha256 = [string]$env:QS3D_V25_COMMERCIAL_ZIP_SHA256
$zipSha = [Security.Cryptography.SHA256]::Create()
$parsedDigestBytes = $zipSha.ComputeHash($zipStream)
$parsedDigest = x
if (-not [string]::Equals($parsedDigest, $expectedZipSha256, [StringComparison]::OrdinalIgnoreCase)) {{throw}}
$zipStream.Position = 0
$archive = [IO.Compression.ZipArchive]::new($zipStream,x,x)
if ($entryCount -gt $MaxEntries) {{throw}}
$expandedBytes += [int64]$entry.Length
if ($expandedBytes -gt $MaxExpandedBytes) {{throw}}
while (($read = $input.Read($buffer,0,$buffer.Length)) -gt 0) {{ if ($materializedBytes -gt ($MaxExpandedBytes - [int64]$read)) {{throw}}; $output.Write($buffer,0,$read) }}
if ([IO.Path]::IsPathRooted($name) -or $name.IndexOf([char]0) -ge 0 -or $name.IndexOf([char]92) -ge 0 -or $name.Contains(':')) {{throw}}
if ($segment -eq '..' -or $segment.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or $segment -match '^(?i:{DEVICE})(?:[.]|$)' -or $segment.EndsWith('.',x) -or $segment.EndsWith(' ',x)) {{throw}}
$seenTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
if (-not $target.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {{throw}}
Ensure-HeldSafeDirectory -Path $parent
if (-not $directoryHolds.ContainsKey($parent)) {{throw}}
$out = [IO.File]::Open([string]$record.Target, [IO.FileMode]::CreateNew,x,x)
$holdOrder[$i].Handle.Dispose()
leaving destination residue because its generation can no longer be proven safe for pathname cleanup
"""
    calls = (
        ".\\scripts\\expand-v25-commercial-candidate.ps1 -ZipPath $heldZip -DestinationRoot $verificationRoot -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096",
        ".\\scripts\\expand-v25-commercial-candidate.ps1 -ZipPath $heldZip -DestinationRoot $extract -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096",
        ".\\scripts\\expand-v25-commercial-candidate.ps1 -ZipPath $heldRemoteZip -DestinationRoot $extract -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096",
    )
    workflow = f"Verify finalized package after private-key cleanup\n{calls[0]}\nverify-v25-signatures.ps1\n      - name: next\nVerify candidate after job boundary\n{calls[1]}\nverify-v25-signatures.ps1\n      - name: next\n$extract = Join-Path $downloadRoot 'verified-package'\n{calls[2]}\nverify-v25-signatures.ps1\n$downloadedIdentity = x\n"
    errors: list[str] = []
    if contract_errors(workflow, safe):
        errors.append("guard rejected intended safe extraction contract")
    mutants = {
        "raw expansion": (workflow.replace(calls[0], "Expand-Archive -LiteralPath $heldZip -DestinationPath $verificationRoot", 1), safe),
        "missing exact stream digest": (workflow, safe.replace("$parsedDigestBytes = $zipSha.ComputeHash($zipStream)", "$parsedDigestBytes = x", 1)),
        "pathname digest": (workflow, safe.replace("$parsedDigestBytes = $zipSha.ComputeHash($zipStream)", "$parsedDigestBytes = Get-FileHash $ZipPath", 1)),
        "missing digest comparison": (workflow, safe.replace("[string]::Equals($parsedDigest, $expectedZipSha256, [StringComparison]::OrdinalIgnoreCase)", "$true", 1)),
        "single-backslash false negative": (workflow, safe.replace("$name.IndexOf([char]92) -ge 0", "$name.Contains('\\\\')", 1)),
        "ASCII-only devices": (workflow, safe.replace(DEVICE, "con|prn|aux|nul|com[1-9]|lpt[1-9]", 1)),
        "case-sensitive aliases": (workflow, safe.replace("[StringComparer]::OrdinalIgnoreCase", "[StringComparer]::Ordinal", 1)),
        "clobber output": (workflow, safe.replace("[IO.FileMode]::CreateNew", "[IO.FileMode]::Create", 1)),
        "missing write sharing": (workflow, safe.replace("FILE_SHARE_READ | FILE_SHARE_WRITE", "FILE_SHARE_READ", 1)),
        "missing list-directory access": (workflow, safe.replace("FILE_LIST_DIRECTORY | FILE_READ_ATTRIBUTES", "FILE_READ_ATTRIBUTES", 1)),
        "delete-sharing directory hold": (workflow, safe + " FILE_SHARE_DELETE"),
        "missing parent hold": (workflow, safe.replace("$directoryHolds.ContainsKey($parent)", "$true", 1)),
        "pathname cleanup after hold release": (workflow, safe + "\nRemove-Item -LiteralPath $destinationFull -Recurse -Force"),
        "wrong downloaded ZIP": (workflow.replace("-ZipPath $heldRemoteZip", "-ZipPath $remoteZip", 1), safe),
    }
    for label, (wf, ex) in mutants.items():
        if not contract_errors(wf, ex):
            errors.append(f"guard failed to reject mutant: {label}")
    return errors


def read(path: Path) -> str | None:
    try:
        return path.read_text(encoding="utf-8")
    except OSError:
        return None


def main() -> int:
    errors = self_test()
    workflow = read(WORKFLOW); extractor = read(EXTRACTOR); runtime = read(RUNTIME_TEST); registry = read(REGISTRY)
    if workflow is None: errors.append(f"unable to read {WORKFLOW}")
    else: errors.extend(contract_errors(workflow, extractor))
    errors.extend(runtime_errors(registry, runtime))
    if errors:
        print("ERROR: V25 commercial safe archive extraction preflight failed closed:", file=sys.stderr)
        for error in errors: print(f" - {error}", file=sys.stderr)
        return 1
    print("V25 commercial safe archive extraction preflight passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
