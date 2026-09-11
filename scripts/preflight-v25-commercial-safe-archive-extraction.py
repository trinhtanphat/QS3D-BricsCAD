#!/usr/bin/env python3
"""Fail closed unless V25 commercial release candidate extraction is bounded and path-safe."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"
EXTRACTOR = ROOT / "scripts" / "expand-v25-commercial-candidate.ps1"
CALL = ".\\scripts\\expand-v25-commercial-candidate.ps1"
WINDOWS_DEVICE_PATTERN = "con|prn|aux|nul|com(?:[1-9]|¹|²|³)|lpt(?:[1-9]|¹|²|³)"


def contract_errors(workflow: str, extractor: str | None) -> list[str]:
    errors: list[str] = []
    if "Expand-Archive" in workflow:
        errors.append("raw Expand-Archive is forbidden at V25 commercial release trust boundaries")
    if extractor is None:
        return errors + ["missing reusable bounded V25 commercial candidate archive extractor script"]
    if ".CopyTo(" in extractor:
        errors.append("commercial archive materialization must not use unbounded stream CopyTo")

    required = (
        ("[IO.Compression.ZipArchive]", "ZipArchive inspection before extraction"),
        ("$zipStream.Length -le 0 -or $zipStream.Length -gt $MaxPackageBytes", "enforced compressed-size budget"),
        ("$entryCount++", "entry counter"),
        ("$entryCount -gt $MaxEntries", "enforced entry-count budget"),
        ("$expandedBytes += [int64]$entry.Length", "declared uncompressed-byte accounting"),
        ("$expandedBytes -gt $MaxExpandedBytes", "enforced declared expanded-size budget"),
        ("$materializedBytes", "actual materialized-byte accounting"),
        ("$input.Read($buffer", "bounded stream read loop"),
        ("$materializedBytes -gt ($MaxExpandedBytes - [int64]$read)", "actual expanded-size pre-write budget"),
        ("$output.Write($buffer", "explicit bounded stream write"),
        ("$materializedBytes += [int64]$read", "actual expanded-byte increment"),
        ("$archive.Entries", "entry enumeration"),
        ("[IO.Path]::IsPathRooted($name)", "rooted-path rejection"),
        ("$name.IndexOf([char]0)", "NUL rejection"),
        ("$name.Contains('\\')", "backslash rejection"),
        ("$name.Contains(':')", "drive/ADS separator rejection"),
        ("$segment -eq '..'", "parent-traversal rejection"),
        ("GetInvalidFileNameChars", "Windows invalid-name rejection"),
        (WINDOWS_DEVICE_PATTERN, "Windows device-name rejection including superscript COM/LPT digits"),
        ("EndsWith('.',", "trailing-dot rejection"),
        ("EndsWith(' ',", "trailing-space rejection"),
        ("HashSet[string]", "duplicate target tracking"),
        ("[StringComparer]::OrdinalIgnoreCase", "Windows case-alias rejection"),
        ("StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)", "destination-root containment"),
        ("[IO.FileMode]::CreateNew", "no-clobber extraction"),
    )
    for token, label in required:
        if token not in extractor:
            errors.append(f"missing {label}: {token}")

    boundaries = (
        ("Verify finalized package after private-key cleanup", "post-key-cleanup", "$heldZip", "$verificationRoot"),
        ("Verify candidate after job boundary", "job-boundary", "$heldZip", "$extract"),
        ("$extract = Join-Path $downloadRoot 'verified-package'", "downloaded-draft", "$heldRemoteZip", "$extract"),
    )
    signature_marker = "verify-v25-signatures.ps1"
    for marker, label, zip_var, root_var in boundaries:
        start = workflow.find(marker)
        if start < 0:
            errors.append(f"missing {label} verification region")
            continue
        if label == "downloaded-draft":
            end = workflow.find("$downloadedIdentity =", start)
        else:
            end = workflow.find("\n      - name:", start + len(marker))
        region = workflow[start:] if end < 0 else workflow[start:end]
        call_index = region.find(CALL)
        sig_index = region.find(signature_marker)
        exact_tokens = (
            (f"-ZipPath {zip_var}", "exact ZIP path"),
            (f"-DestinationRoot {root_var}", "exact destination root"),
            ("-MaxPackageBytes 268435456", "256 MiB compressed-size bound"),
            ("-MaxExpandedBytes 536870912", "512 MiB expanded-size bound"),
            ("-MaxEntries 4096", "4096-entry bound"),
        )
        for token, token_label in exact_tokens:
            if token not in region:
                errors.append(f"{label} safe extractor call is missing {token_label}")
        if call_index < 0:
            errors.append(f"{label} verifier does not invoke reusable bounded safe extraction")
        if sig_index >= 0 and call_index >= 0 and call_index > sig_index:
            errors.append(f"{label} archive safety admission must precede extracted-payload signature verification")

    if workflow.count(CALL) != 3:
        errors.append("reusable safe commercial extractor must be invoked exactly once at each of the three V25 verification boundaries")
    return errors


def self_test() -> list[str]:
    safe_extractor = r"""
param([int64]$MaxPackageBytes,[int64]$MaxExpandedBytes,[int]$MaxEntries)
$zipStream = [IO.File]::Open($ZipPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
if ($zipStream.Length -le 0 -or $zipStream.Length -gt $MaxPackageBytes) { throw 'compressed' }
$archive = [IO.Compression.ZipArchive]::new($zipStream,[IO.Compression.ZipArchiveMode]::Read,$true)
$seenTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$invalid = [IO.Path]::GetInvalidFileNameChars()
$destinationFull = [IO.Path]::GetFullPath($DestinationRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
$rootPrefix = $destinationFull + [IO.Path]::DirectorySeparatorChar
[int64]$expandedBytes = 0
[int64]$materializedBytes = 0
$entryCount = 0
foreach ($entry in $archive.Entries) {
  $entryCount++
  if ($entryCount -gt $MaxEntries) { throw 'entries' }
  $name = [string]$entry.FullName
  if ([IO.Path]::IsPathRooted($name) -or $name.IndexOf([char]0) -ge 0 -or $name.Contains('\\') -or $name.Contains(':')) { throw 'rooted' }
  foreach ($segment in $name.Split('/')) {
    if ($segment -eq '..' -or $segment.IndexOfAny($invalid) -ge 0 -or $segment -match '^(?i:con|prn|aux|nul|com(?:[1-9]|¹|²|³)|lpt(?:[1-9]|¹|²|³))(?:\\.|$)' -or $segment.EndsWith('.', [StringComparison]::Ordinal) -or $segment.EndsWith(' ', [StringComparison]::Ordinal)) { throw 'unsafe' }
  }
  $expandedBytes += [int64]$entry.Length
  if ($expandedBytes -gt $MaxExpandedBytes) { throw 'expanded' }
  $target = [IO.Path]::GetFullPath((Join-Path $destinationFull $name))
  if (-not $target.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'escape' }
  if (-not $seenTargets.Add($target)) { throw 'duplicate' }
  $out = [IO.File]::Open($target,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
  $buffer = New-Object byte[] 81920
  while (($read = $input.Read($buffer, 0, $buffer.Length)) -gt 0) {
    if ($materializedBytes -gt ($MaxExpandedBytes - [int64]$read)) { throw 'actual-expanded' }
    $output.Write($buffer, 0, $read)
    $materializedBytes += [int64]$read
  }
}
"""
    call1 = r".\scripts\expand-v25-commercial-candidate.ps1 -ZipPath $heldZip -DestinationRoot $verificationRoot -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096"
    call2 = r".\scripts\expand-v25-commercial-candidate.ps1 -ZipPath $heldZip -DestinationRoot $extract -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096"
    call3 = r".\scripts\expand-v25-commercial-candidate.ps1 -ZipPath $heldRemoteZip -DestinationRoot $extract -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096"
    safe_workflow = f"""
      - name: Verify finalized package after private-key cleanup
        run: |
          {call1}
          & .\\scripts\\verify-v25-signatures.ps1
      - name: Verify candidate after job boundary
        run: |
          {call2}
          & .\\scripts\\verify-v25-signatures.ps1
      - name: Create draft, verify uploaded bytes, then publish
        run: |
          $extract = Join-Path $downloadRoot 'verified-package'
          {call3}
          & .\\scripts\\verify-v25-signatures.ps1
          $downloadedIdentity = & .\\scripts\\assert-v25-commercial-draft-identity.ps1
"""
    errors: list[str] = []
    if contract_errors(safe_workflow, safe_extractor):
        errors.append("guard rejected intended reusable bounded/path-safe extraction contract")
    mutants = {
        "raw expansion": (safe_workflow.replace(call1, "Expand-Archive -LiteralPath $heldZip -DestinationPath $verificationRoot", 1), safe_extractor),
        "missing extractor": (safe_workflow, None),
        "declared but unenforced compressed budget": (safe_workflow, safe_extractor.replace("if ($zipStream.Length -le 0 -or $zipStream.Length -gt $MaxPackageBytes) { throw 'compressed' }", "# no compressed limit", 1)),
        "declared but unenforced entry budget": (safe_workflow, safe_extractor.replace("if ($entryCount -gt $MaxEntries) { throw 'entries' }", "# no entry limit", 1)),
        "no declared expanded accounting": (safe_workflow, safe_extractor.replace("$expandedBytes += [int64]$entry.Length", "$expandedBytes += 0", 1)),
        "no declared expanded enforcement": (safe_workflow, safe_extractor.replace("if ($expandedBytes -gt $MaxExpandedBytes) { throw 'expanded' }", "# no expanded limit", 1)),
        "unbounded actual materialization": (safe_workflow, safe_extractor.replace("while (($read = $input.Read($buffer, 0, $buffer.Length)) -gt 0) {\n    if ($materializedBytes -gt ($MaxExpandedBytes - [int64]$read)) { throw 'actual-expanded' }\n    $output.Write($buffer, 0, $read)\n    $materializedBytes += [int64]$read\n  }", "$input.CopyTo($output)", 1)),
        "no actual pre-write budget": (safe_workflow, safe_extractor.replace("if ($materializedBytes -gt ($MaxExpandedBytes - [int64]$read)) { throw 'actual-expanded' }", "# no actual limit", 1)),
        "no traversal rejection": (safe_workflow, safe_extractor.replace("$segment -eq '..' -or ", "", 1)),
        "ascii-only device names": (safe_workflow, safe_extractor.replace(WINDOWS_DEVICE_PATTERN, "con|prn|aux|nul|com[1-9]|lpt[1-9]", 1)),
        "case-sensitive duplicates": (safe_workflow, safe_extractor.replace("[StringComparer]::OrdinalIgnoreCase", "[StringComparer]::Ordinal", 1)),
        "no root containment": (safe_workflow, safe_extractor.replace("if (-not $target.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'escape' }", "# no containment", 1)),
        "clobber output": (safe_workflow, safe_extractor.replace("[IO.FileMode]::CreateNew", "[IO.FileMode]::Create", 1)),
        "omit job boundary": (safe_workflow.replace(f"          {call2}\n", "", 1), safe_extractor),
        "omit downloaded draft boundary": (safe_workflow.replace(f"          {call3}\n", "", 1), safe_extractor),
        "wrong downloaded draft zip": (safe_workflow.replace("-ZipPath $heldRemoteZip", "-ZipPath $remoteZip", 1), safe_extractor),
    }
    for label, (workflow, extractor) in mutants.items():
        if not contract_errors(workflow, extractor):
            errors.append(f"guard failed to reject mutant: {label}")
    return errors


def main() -> int:
    errors = self_test()
    try:
        workflow = WORKFLOW.read_text(encoding="utf-8")
    except OSError as exc:
        errors.append(f"unable to read {WORKFLOW}: {exc}")
        workflow = ""
    extractor: str | None
    try:
        extractor = EXTRACTOR.read_text(encoding="utf-8")
    except FileNotFoundError:
        extractor = None
    except OSError as exc:
        errors.append(f"unable to read {EXTRACTOR}: {exc}")
        extractor = None
    if workflow:
        errors.extend(contract_errors(workflow, extractor))
    if errors:
        print("ERROR: V25 commercial safe archive extraction preflight failed closed:", file=sys.stderr)
        for error in errors:
            print(f" - {error}", file=sys.stderr)
        return 1
    print("V25 commercial safe archive extraction preflight passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
