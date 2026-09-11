#!/usr/bin/env python3
"""Fail closed unless V25 commercial release candidate extraction is bounded and path-safe."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"


def contract_errors(source: str) -> list[str]:
    errors: list[str] = []
    if "Expand-Archive" in source:
        errors.append("raw Expand-Archive is forbidden at V25 commercial release trust boundaries")

    helper = "function Expand-V25CommercialCandidateArchive"
    helper_index = source.find(helper)
    if helper_index < 0:
        return errors + ["missing bounded V25 commercial candidate archive extractor"]

    helper_region = source[helper_index:]
    required = (
        ("[IO.Compression.ZipArchive]", "ZipArchive inspection before extraction"),
        ("MaxPackageBytes", "compressed-size budget"),
        ("MaxExpandedBytes", "expanded-size budget"),
        ("MaxEntries", "entry-count budget"),
        ("$archive.Entries", "entry enumeration"),
        ("[IO.Path]::IsPathRooted", "rooted-path rejection"),
        ("$segment -eq '..'", "parent-traversal rejection"),
        ("GetInvalidFileNameChars", "Windows invalid-name rejection"),
        ("con|prn|aux|nul|com[1-9]|lpt[1-9]", "Windows device-name rejection"),
        ("EndsWith('.',", "trailing-dot rejection"),
        ("EndsWith(' ',", "trailing-space rejection"),
        ("HashSet[string]", "duplicate target tracking"),
        ("OrdinalIgnoreCase", "Windows case-alias rejection"),
        ("$expandedBytes", "expanded-byte accounting"),
        ("$entry.Length", "uncompressed-entry length accounting"),
    )
    for token, label in required:
        if token not in helper_region:
            errors.append(f"missing {label}: {token}")

    call = "Expand-V25CommercialCandidateArchive"
    # One definition plus two calls: post-key-cleanup verification and build->release artifact boundary.
    if source.count(call) < 3:
        errors.append("safe commercial archive extractor must protect both V25 verification boundaries")

    cleanup_marker = "Verify finalized package after private-key cleanup"
    boundary_marker = "Verify candidate after job boundary"
    signature_marker = "verify-v25-signatures.ps1"
    for marker, label in ((cleanup_marker, "post-key-cleanup"), (boundary_marker, "job-boundary")):
        start = source.find(marker)
        if start < 0:
            errors.append(f"missing {label} verification region")
            continue
        next_step = source.find("\n      - name:", start + len(marker))
        region = source[start:] if next_step < 0 else source[start:next_step]
        call_index = region.find(call)
        sig_index = region.find(signature_marker)
        if call_index < 0:
            errors.append(f"{label} verifier does not use bounded safe extraction")
        if sig_index >= 0 and call_index >= 0 and call_index > sig_index:
            errors.append(f"{label} archive safety admission must precede extracted-payload signature verification")

    return errors


def self_test() -> list[str]:
    safe_helper = r"""
function Expand-V25CommercialCandidateArchive {
  param([int64]$MaxPackageBytes,[int64]$MaxExpandedBytes,[int]$MaxEntries)
  $stream = [IO.File]::OpenRead($ZipPath)
  $archive = [IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$true)
  $seenTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
  $invalid = [IO.Path]::GetInvalidFileNameChars()
  [int64]$expandedBytes = 0
  foreach ($entry in $archive.Entries) {
    if ([IO.Path]::IsPathRooted($entry.FullName)) { throw 'rooted' }
    foreach ($segment in $entry.FullName.Split('/')) {
      if ($segment -eq '..' -or $segment.IndexOfAny($invalid) -ge 0 -or $segment -match '^(?i:con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\\.|$)' -or $segment.EndsWith('.', [StringComparison]::Ordinal) -or $segment.EndsWith(' ', [StringComparison]::Ordinal)) { throw 'unsafe' }
    }
    $expandedBytes += [int64]$entry.Length
    if ($expandedBytes -gt $MaxExpandedBytes) { throw 'expanded' }
    if (-not $seenTargets.Add($entry.FullName)) { throw 'duplicate' }
  }
}
"""
    safe = safe_helper + r"""
      - name: Verify finalized package after private-key cleanup
        run: |
          Expand-V25CommercialCandidateArchive -MaxPackageBytes 1 -MaxExpandedBytes 2 -MaxEntries 3
          & .\scripts\verify-v25-signatures.ps1
      - name: Verify candidate after job boundary
        run: |
          Expand-V25CommercialCandidateArchive -MaxPackageBytes 1 -MaxExpandedBytes 2 -MaxEntries 3
          & .\scripts\verify-v25-signatures.ps1
"""
    errors: list[str] = []
    if contract_errors(safe):
        errors.append("guard rejected intended bounded/path-safe extraction contract")
    mutants = {
        "raw expansion": safe.replace("Expand-V25CommercialCandidateArchive -MaxPackageBytes 1 -MaxExpandedBytes 2 -MaxEntries 3", "Expand-Archive -LiteralPath $zip -DestinationPath $extract", 1),
        "no expanded budget": safe.replace("$expandedBytes += [int64]$entry.Length", "$expandedBytes += 0", 1),
        "no traversal rejection": safe.replace("$segment -eq '..' -or ", "", 1),
        "no case-insensitive duplicate defense": safe.replace("[StringComparer]::OrdinalIgnoreCase", "[StringComparer]::Ordinal", 1),
        "only one protected boundary": safe.replace("          Expand-V25CommercialCandidateArchive -MaxPackageBytes 1 -MaxExpandedBytes 2 -MaxEntries 3\n          & .\\scripts\\verify-v25-signatures.ps1\n", "          & .\\scripts\\verify-v25-signatures.ps1\n", 1),
    }
    for label, mutant in mutants.items():
        if not contract_errors(mutant):
            errors.append(f"guard failed to reject mutant: {label}")
    return errors


def main() -> int:
    errors = self_test()
    try:
        source = WORKFLOW.read_text(encoding="utf-8")
    except OSError as exc:
        errors.append(f"unable to read {WORKFLOW}: {exc}")
        source = ""
    if source:
        errors.extend(contract_errors(source))
    if errors:
        print("ERROR: V25 commercial safe archive extraction preflight failed closed:", file=sys.stderr)
        for error in errors:
            print(f" - {error}", file=sys.stderr)
        return 1
    print("V25 commercial safe archive extraction preflight passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
