#!/usr/bin/env python3
"""Fail closed unless V25 commercial release candidate extraction is bounded and path-safe."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"
EXTRACTOR = ROOT / "scripts" / "expand-v25-commercial-candidate.ps1"


def contract_errors(workflow: str, extractor: str | None) -> list[str]:
    errors: list[str] = []
    if "Expand-Archive" in workflow:
        errors.append("raw Expand-Archive is forbidden at V25 commercial release trust boundaries")
    if extractor is None:
        return errors + ["missing reusable bounded V25 commercial candidate archive extractor script"]

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
        ("FileMode]::CreateNew", "no-clobber extraction"),
    )
    for token, label in required:
        if token not in extractor:
            errors.append(f"missing {label}: {token}")

    call = ".\\scripts\\expand-v25-commercial-candidate.ps1"
    cleanup_marker = "Verify finalized package after private-key cleanup"
    boundary_marker = "Verify candidate after job boundary"
    signature_marker = "verify-v25-signatures.ps1"
    for marker, label in ((cleanup_marker, "post-key-cleanup"), (boundary_marker, "job-boundary")):
        start = workflow.find(marker)
        if start < 0:
            errors.append(f"missing {label} verification region")
            continue
        next_step = workflow.find("\n      - name:", start + len(marker))
        region = workflow[start:] if next_step < 0 else workflow[start:next_step]
        call_index = region.find(call)
        sig_index = region.find(signature_marker)
        for token, token_label in (
            ("-ZipPath", "ZIP path"),
            ("-DestinationRoot", "destination root"),
            ("-MaxPackageBytes", "compressed-size bound"),
            ("-MaxExpandedBytes", "expanded-size bound"),
            ("-MaxEntries", "entry-count bound"),
        ):
            if token not in region:
                errors.append(f"{label} safe extractor call is missing explicit {token_label}")
        if call_index < 0:
            errors.append(f"{label} verifier does not invoke reusable bounded safe extraction")
        if sig_index >= 0 and call_index >= 0 and call_index > sig_index:
            errors.append(f"{label} archive safety admission must precede extracted-payload signature verification")

    if workflow.count(call) != 2:
        errors.append("reusable safe commercial extractor must be invoked exactly once at each of the two V25 verification boundaries")
    return errors


def self_test() -> list[str]:
    safe_extractor = r"""
param([int64]$MaxPackageBytes,[int64]$MaxExpandedBytes,[int]$MaxEntries)
$stream = [IO.File]::Open($ZipPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
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
  $out = [IO.File]::Open($target,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
}
"""
    safe_workflow = r"""
      - name: Verify finalized package after private-key cleanup
        run: |
          .\scripts\expand-v25-commercial-candidate.ps1 -ZipPath $heldZip -DestinationRoot $verificationRoot -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096
          & .\scripts\verify-v25-signatures.ps1
      - name: Verify candidate after job boundary
        run: |
          .\scripts\expand-v25-commercial-candidate.ps1 -ZipPath $heldZip -DestinationRoot $extract -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096
          & .\scripts\verify-v25-signatures.ps1
"""
    errors: list[str] = []
    if contract_errors(safe_workflow, safe_extractor):
        errors.append("guard rejected intended reusable bounded/path-safe extraction contract")
    mutants = {
        "raw expansion": (safe_workflow.replace(".\\scripts\\expand-v25-commercial-candidate.ps1 -ZipPath $heldZip -DestinationRoot $verificationRoot -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096", "Expand-Archive -LiteralPath $heldZip -DestinationPath $verificationRoot", 1), safe_extractor),
        "missing extractor": (safe_workflow, None),
        "no expanded accounting": (safe_workflow, safe_extractor.replace("$expandedBytes += [int64]$entry.Length", "$expandedBytes += 0", 1)),
        "no traversal rejection": (safe_workflow, safe_extractor.replace("$segment -eq '..' -or ", "", 1)),
        "case-sensitive duplicates": (safe_workflow, safe_extractor.replace("[StringComparer]::OrdinalIgnoreCase", "[StringComparer]::Ordinal", 1)),
        "clobber output": (safe_workflow, safe_extractor.replace("[IO.FileMode]::CreateNew", "[IO.FileMode]::Create", 1)),
        "one protected boundary": (safe_workflow.replace("          .\\scripts\\expand-v25-commercial-candidate.ps1 -ZipPath $heldZip -DestinationRoot $extract -MaxPackageBytes 268435456 -MaxExpandedBytes 536870912 -MaxEntries 4096\n", "", 1), safe_extractor),
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
