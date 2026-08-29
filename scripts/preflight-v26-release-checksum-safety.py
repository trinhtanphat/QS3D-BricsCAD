#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "write-v26-package-checksum.ps1"
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26.yml"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def helper_contract(text: str) -> None:
    required = (
        "$script:ExpectedPackageName = 'QS3D-BricsCAD-V26.zip'",
        "$script:ExpectedChecksumName = 'QS3D-BricsCAD-V26.zip.sha256'",
        "$script:MaxChecksumBytes = 1024",
        "Resolve-OrdinaryNonReparseFile -Path $PackagePath -Label 'V26 package ZIP'",
        "Assert-NoReparseDirectoryChain -Directory $item.Directory -Label $Label",
        "if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)",
        "$packageCanonicalPath = $package.FullName",
        "$packageLength = [int64]$package.Length",
        "$packageLastWriteUtcTicks = [int64]$package.LastWriteTimeUtc.Ticks",
        "[string]::Equals([IO.Path]::GetFileName($outputFullPath), $script:ExpectedChecksumName, [StringComparison]::Ordinal)",
        "Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum destination parent'",
        "Assert-SafeExistingOutputLeaf -Path $outputFullPath",
        "$originalOutputBytes = Read-BoundedChecksumBytes",
        "[IO.File]::Open($packageCanonicalPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)",
        "Resolve-OrdinaryNonReparseFile -Path $packageCanonicalPath -Label 'V26 package ZIP after open'",
        "$packageLength -ne [int64]$stream.Length",
        "$packageLength -ne [int64]$reboundPackage.Length",
        "$packageLastWriteUtcTicks -ne [int64]$reboundPackage.LastWriteTimeUtc.Ticks",
        "V26 package ZIP changed between checksum admission and held-stream binding.",
        "[Security.Cryptography.SHA256]::Create()",
        "$sha256.ComputeHash($stream)",
        "if ($hash -notmatch '^[0-9a-f]{64}$')",
        "$record = \"$hash  $($script:ExpectedPackageName)\"",
        "[Text.Encoding]::ASCII.GetBytes($record + [Environment]::NewLine)",
        '".tmp-$nonce"',
        '".bak-$nonce"',
        "[IO.File]::WriteAllBytes($tempPath, $recordBytes)",
        "$publicationStarted = $false",
        "$publicationCommitted = $false",
        "$publicationStarted = $true",
        "[IO.File]::Replace($tempPath, $outputFullPath, $backupPath, $true)",
        "[IO.File]::Move($tempPath, $outputFullPath)",
        "if ($publicationStarted -and -not $publicationCommitted)",
        "if (Test-Path -LiteralPath $backupPath)",
        "V26 checksum rollback parent",
        "V26 checksum rollback backup",
        "Restored V26 checksum destination",
        "V26 checksum rollback unchanged-destination proof",
        "the original destination cannot be proven unchanged",
        "Published V26 checksum bytes do not match the computed canonical record.",
        "$publicationCommitted = $true",
        "Remove-SafeChecksumLeaf -Path $backupPath",
        "V26 checksum staging residue remains",
        "V26 checksum backup residue remains",
        "PackagePath = $packageCanonicalPath",
    )
    for token in required:
        require(token in text, "V26 checksum helper missing safety token: " + token)

    source_guard = text.find("Resolve-OrdinaryNonReparseFile -Path $PackagePath")
    admitted_path = text.find("$packageCanonicalPath = $package.FullName")
    admitted_length = text.find("$packageLength = [int64]$package.Length")
    admitted_ticks = text.find("$packageLastWriteUtcTicks = [int64]$package.LastWriteTimeUtc.Ticks")
    output_identity_guard = text.find("[string]::Equals([IO.Path]::GetFileName($outputFullPath), $script:ExpectedChecksumName")
    output_parent_guard = text.find("Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath")
    output_leaf_guard = text.find("Assert-SafeExistingOutputLeaf -Path $outputFullPath")
    snapshot_pos = text.find("$originalOutputBytes = Read-BoundedChecksumBytes")
    open_pos = text.find("[IO.File]::Open($packageCanonicalPath")
    rebound_pos = text.find("Resolve-OrdinaryNonReparseFile -Path $packageCanonicalPath -Label 'V26 package ZIP after open'")
    binding_failure_pos = text.find("V26 package ZIP changed between checksum admission and held-stream binding.")
    hash_pos = text.find("$sha256.ComputeHash($stream)")
    temp_write = text.find("[IO.File]::WriteAllBytes($tempPath, $recordBytes)")
    temp_guard = text.find("Resolve-OrdinaryNonReparseFile -Path $tempPath")
    started_pos = text.find("$publicationStarted = $true")
    replace_pos = text.find("[IO.File]::Replace($tempPath, $outputFullPath")
    move_pos = text.find("[IO.File]::Move($tempPath, $outputFullPath)")
    verify_pos = text.find("Published V26 checksum bytes do not match the computed canonical record.")
    committed_pos = text.find("$publicationCommitted = $true")
    rollback_pos = text.find("if ($publicationStarted -and -not $publicationCommitted)")
    unchanged_proof_pos = text.find("V26 checksum rollback unchanged-destination proof")
    backup_cleanup_pos = text.find("Remove-SafeChecksumLeaf -Path $backupPath")
    residue_pos = text.find("V26 checksum staging residue remains")

    positions = (
        source_guard, admitted_path, admitted_length, admitted_ticks, output_identity_guard,
        output_parent_guard, output_leaf_guard, snapshot_pos, open_pos, rebound_pos,
        binding_failure_pos, hash_pos, temp_write, temp_guard,
    )
    require(min(positions) >= 0, "V26 checksum safety ordering token missing")
    require(
        source_guard < admitted_path < admitted_length < admitted_ticks < output_identity_guard <
        output_parent_guard < output_leaf_guard < snapshot_pos < open_pos < rebound_pos <
        binding_failure_pos < hash_pos < temp_write < temp_guard,
        "V26 checksum must snapshot admitted package identity, open and rebound-bind the held generation before hashing, then stage publication",
    )
    require(started_pos > temp_guard, "V26 checksum mutation state must follow staging validation")
    require(started_pos < replace_pos and started_pos < move_pos,
            "V26 checksum publication-start marker must precede either filesystem publication mutation")
    require(verify_pos > max(replace_pos, move_pos), "V26 checksum must verify canonical published bytes after publication mutation")
    require(committed_pos > verify_pos, "V26 checksum commit marker must follow successful published-byte verification")
    require(rollback_pos > committed_pos, "V26 checksum catch rollback must be gated by started-but-uncommitted state")
    require(unchanged_proof_pos > rollback_pos, "V26 checksum missing-backup rollback must prove original destination remained unchanged")
    require(backup_cleanup_pos > rollback_pos, "V26 checksum backup cleanup must follow rollback handling and remain commit-gated")
    require(residue_pos > backup_cleanup_pos, "V26 checksum residue checks must run after publication verification/rollback/cleanup")

    require("$published = $true" not in text, "legacy premature checksum commit marker must not return")
    require("Get-FileHash" not in text, "V26 checksum helper must hash the guarded open stream instead of reopening by path")
    require("Set-Content" not in text, "V26 checksum helper must not write directly to the final checksum path")
    require("Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue" not in text,
            "V26 checksum backup must not be silently deleted without ordinary/non-reparse validation")


def workflow_contract(text: str) -> None:
    start = text.find("      - name: Create V26 package checksum")
    end = text.find("      - name: Upload V26 qualification artifacts", start + 1)
    require(start >= 0 and end > start, "V26 release workflow checksum step is missing")
    block = text[start:end]
    for token in (".\\scripts\\write-v26-package-checksum.ps1", "-PackagePath 'dist\\QS3D-BricsCAD-V26.zip'", "-OutputPath 'dist\\QS3D-BricsCAD-V26.zip.sha256'"):
        require(token in block, "V26 checksum workflow missing shared-helper binding: " + token)
    for forbidden in ("Get-FileHash", "Set-Content", "Resolve-Path"):
        require(forbidden not in block, "V26 checksum workflow reintroduced inline unsafe checksum logic: " + forbidden)


def expect_helper_mutation_failure(original: str, token: str, replacement: str, label: str) -> None:
    require(token in original, "mutation source token missing for " + label)
    mutated = original.replace(token, replacement, 1)
    try:
        helper_contract(mutated)
    except AssertionError:
        return
    raise AssertionError("V26 checksum guard accepted mutation: " + label)


def expect_workflow_mutation_failure(original: str, token: str, replacement: str, label: str) -> None:
    require(token in original, "workflow mutation source token missing for " + label)
    mutated = original.replace(token, replacement, 1)
    try:
        workflow_contract(mutated)
    except AssertionError:
        return
    raise AssertionError("V26 checksum workflow guard accepted mutation: " + label)


def main() -> int:
    require(HELPER.is_file(), "missing scripts/write-v26-package-checksum.ps1")
    require(WORKFLOW.is_file(), "missing .github/workflows/release-v26.yml")
    helper = HELPER.read_text(encoding="utf-8")
    workflow = WORKFLOW.read_text(encoding="utf-8")
    helper_contract(helper)
    workflow_contract(workflow)

    mutations = (
        ("$script:ExpectedChecksumName = 'QS3D-BricsCAD-V26.zip.sha256'", "$script:ExpectedChecksumName = 'other.sha256'", "canonical output identity"),
        ("if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)", "if (($cursor.Attributes -band [IO.FileAttributes]::Hidden) -ne 0)", "directory-chain reparse rejection"),
        ("$packageCanonicalPath = $package.FullName", "$packageCanonicalPath = [IO.Path]::GetFullPath($PackagePath)", "admitted canonical package identity"),
        ("$packageLength = [int64]$package.Length", "$packageLength = -1", "admitted package length snapshot"),
        ("$packageLastWriteUtcTicks = [int64]$package.LastWriteTimeUtc.Ticks", "$packageLastWriteUtcTicks = 0", "admitted package write-time snapshot"),
        ("$originalOutputBytes = Read-BoundedChecksumBytes", "$originalOutputBytes = [byte[]]@()", "bounded prior-state snapshot"),
        ("[IO.FileShare]::Read", "[IO.FileShare]::ReadWrite", "read-only hash handle"),
        ("Resolve-OrdinaryNonReparseFile -Path $packageCanonicalPath -Label 'V26 package ZIP after open'", "$reboundPackage = $package", "post-open pathname rebound"),
        ("$packageLength -ne [int64]$stream.Length", "$false", "held stream length binding"),
        ("$packageLastWriteUtcTicks -ne [int64]$reboundPackage.LastWriteTimeUtc.Ticks", "$false", "post-open write-time binding"),
        ("V26 package ZIP changed between checksum admission and held-stream binding.", "generation drift ignored", "generation binding failure"),
        ("$sha256.ComputeHash($stream)", "$sha256.ComputeHash([IO.File]::ReadAllBytes($packageCanonicalPath))", "stream-bound hashing"),
        ('".tmp-$nonce"', '".tmp"', "nonce staging"),
        ("$publicationStarted = $true", "$publicationStarted = $false # mutation window unguarded", "pre-mutation rollback state"),
        ("[IO.File]::Replace($tempPath, $outputFullPath, $backupPath, $true)", "Copy-Item $tempPath $outputFullPath -Force", "atomic replacement"),
        ("V26 checksum rollback unchanged-destination proof", "unchecked unchanged destination", "backup-absent rollback proof"),
        ("Published V26 checksum bytes do not match the computed canonical record.", "published bytes ignored", "pre-commit byte verification"),
        ("if ($publicationStarted -and -not $publicationCommitted)", "if ($false)", "post-publication rollback gate"),
        ("V26 checksum rollback backup", "unchecked rollback backup", "rollback backup validation"),
        ("Remove-SafeChecksumLeaf -Path $backupPath", "Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue", "safe backup cleanup"),
        ("V26 checksum staging residue remains", "staging residue ignored", "staging residue check"),
    )
    for token, replacement, label in mutations:
        expect_helper_mutation_failure(helper, token, replacement, label)

    hash_token = "$sha256.ComputeHash($stream)"
    rebound_token = "Resolve-OrdinaryNonReparseFile -Path $packageCanonicalPath -Label 'V26 package ZIP after open'"
    require(hash_token in helper and rebound_token in helper, "generation-binding movement mutation source missing")
    moved = helper.replace(rebound_token, "# rebound moved after hash", 1).replace(hash_token, hash_token + "\n    " + rebound_token, 1)
    try:
        helper_contract(moved)
    except AssertionError:
        pass
    else:
        raise AssertionError("V26 checksum guard accepted post-open generation binding moved after hashing")

    expect_workflow_mutation_failure(workflow, ".\\scripts\\write-v26-package-checksum.ps1", "Get-FileHash -LiteralPath 'dist\\QS3D-BricsCAD-V26.zip'", "shared-helper routing")
    expect_workflow_mutation_failure(workflow, "-OutputPath 'dist\\QS3D-BricsCAD-V26.zip.sha256'", "-OutputPath 'dist\\other.sha256'", "canonical checksum destination")
    print("PASS: V26 release checksum creation binds the admitted ZIP pathname/length/write-time to the held stream before hashing, then preserves staged rollback-safe publication and shared workflow routing.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
