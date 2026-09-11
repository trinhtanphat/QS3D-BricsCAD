#!/usr/bin/env python3
"""Fail closed unless V25 commercial release extraction is bounded and Windows-path safe."""

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
        ("$expandedBytes -gt $MaxExpandedBytes", "declared expanded-size enforcement"),
        ("$materializedBytes -gt ($MaxExpandedBytes - [int64]$read)", "actual pre-write expanded-size enforcement"),
        ("$input.Read($buffer", "bounded read loop"),
        ("$output.Write($buffer", "bounded write loop"),
        ("[IO.Path]::IsPathRooted($name)", "rooted-path rejection"),
        ("$name.IndexOf([char]0)", "NUL rejection"),
        ("$name.IndexOf([char]92) -ge 0", "single-backslash rejection"),
        ("$name.Contains(':')", "drive/ADS separator rejection"),
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
        ("[IO.FileAttributes]::ReparsePoint", "reparse-point rejection"),
    )
    for token, label in required:
        if token not in extractor:
            errors.append(f"missing {label}: {token}")

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
        tokens = (
            CALL,
            f"-ZipPath {zip_var}",
            f"-DestinationRoot {root_var}",
            "-MaxPackageBytes 268435456",
            "-MaxExpandedBytes 536870912",
            "-MaxEntries 4096",
        )
        for token in tokens:
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
        "expand-v25-commercial-candidate.ps1",
        "../escape.txt",
        "Payload.txt",
        "payload.TXT",
        "COM¹.txt",
        "nested\\escape.txt",
        "payload.txt:stream",
        "payload./file.txt",
        "-MaxEntries 1",
        "-MaxExpandedBytes 32",
        "-MaxPackageBytes 1",
        "must not already exist",
    )
    for token in required:
        if token not in runtime:
            errors.append(f"runtime archive test missing fixture/contract: {token}")
    return errors


def self_test() -> list[str]:
    safe = f"""
[IO.Compression.ZipArchive] $archive = $null
$zipStream = [IO.File]::Open($ZipPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
if ($zipStream.Length -le 0 -or $zipStream.Length -gt $MaxPackageBytes) {{ throw 'compressed' }}
if ($entryCount -gt $MaxEntries) {{ throw 'entries' }}
$expandedBytes += [int64]$entry.Length
if ($expandedBytes -gt $MaxExpandedBytes) {{ throw 'expanded' }}
while (($read = $input.Read($buffer,0,$buffer.Length)) -gt 0) {{
 if ($materializedBytes -gt ($MaxExpandedBytes - [int64]$read)) {{ throw 'actual' }}
 $output.Write($buffer,0,$read)
}}
if ([IO.Path]::IsPathRooted($name) -or $name.IndexOf([char]0) -ge 0 -or $name.IndexOf([char]92) -ge 0 -or $name.Contains(':')) {{ throw 'unsafe' }}
if ($segment -eq '..' -or $segment.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or $segment -match '^(?i:{DEVICE})(?:[.]|$)' -or $segment.EndsWith('.', [StringComparison]::Ordinal) -or $segment.EndsWith(' ', [StringComparison]::Ordinal)) {{ throw 'unsafe' }}
$seenTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
if (-not $target.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {{ throw 'escape' }}
$out = [IO.File]::Open($target,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {{ throw 'reparse' }}
"""
    calls = (
        ".\\scripts\\expand-v25-commercial-candidate.ps1 -ZipPath $heldZip -DestinationRoot $verificationRoot -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096",
        ".\\scripts\\expand-v25-commercial-candidate.ps1 -ZipPath $heldZip -DestinationRoot $extract -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096",
        ".\\scripts\\expand-v25-commercial-candidate.ps1 -ZipPath $heldRemoteZip -DestinationRoot $extract -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096",
    )
    workflow = f"""Verify finalized package after private-key cleanup
{calls[0]}
verify-v25-signatures.ps1
      - name: next
Verify candidate after job boundary
{calls[1]}
verify-v25-signatures.ps1
      - name: next
$extract = Join-Path $downloadRoot 'verified-package'
{calls[2]}
verify-v25-signatures.ps1
$downloadedIdentity = x
"""
    errors: list[str] = []
    if contract_errors(workflow, safe):
        errors.append("guard rejected intended safe extraction contract")
    mutants = {
        "raw expansion": (workflow.replace(calls[0], "Expand-Archive -LiteralPath $heldZip -DestinationPath $verificationRoot", 1), safe),
        "missing compressed limit": (workflow, safe.replace("$zipStream.Length -le 0 -or $zipStream.Length -gt $MaxPackageBytes", "$false", 1)),
        "missing actual limit": (workflow, safe.replace("$materializedBytes -gt ($MaxExpandedBytes - [int64]$read)", "$false", 1)),
        "single-backslash false negative": (workflow, safe.replace("$name.IndexOf([char]92) -ge 0", "$name.Contains('\\\\')", 1)),
        "ASCII-only devices": (workflow, safe.replace(DEVICE, "con|prn|aux|nul|com[1-9]|lpt[1-9]", 1)),
        "device extension not bounded": (workflow, safe.replace("(?:[.]|$)", "$", 1)),
        "case-sensitive aliases": (workflow, safe.replace("[StringComparer]::OrdinalIgnoreCase", "[StringComparer]::Ordinal", 1)),
        "clobber output": (workflow, safe.replace("[IO.FileMode]::CreateNew", "[IO.FileMode]::Create", 1)),
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
    workflow = read(WORKFLOW)
    extractor = read(EXTRACTOR)
    runtime = read(RUNTIME_TEST)
    registry = read(REGISTRY)
    if workflow is None:
        errors.append(f"unable to read {WORKFLOW}")
    else:
        errors.extend(contract_errors(workflow, extractor))
    errors.extend(runtime_errors(registry, runtime))
    if errors:
        print("ERROR: V25 commercial safe archive extraction preflight failed closed:", file=sys.stderr)
        for error in errors:
            print(f" - {error}", file=sys.stderr)
        return 1
    print("V25 commercial safe archive extraction preflight passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
