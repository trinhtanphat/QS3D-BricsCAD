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
        "Resolve-OrdinaryNonReparseFile -Path $PackagePath -Label 'V26 package ZIP'",
        "Assert-NoReparseDirectoryChain -Directory $item.Directory -Label $Label",
        "[IO.FileAttributes]::ReparsePoint",
        "Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum destination parent'",
        "Assert-SafeExistingOutputLeaf -Path $outputFullPath",
        "[IO.File]::Open($package.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)",
        "[Security.Cryptography.SHA256]::Create()",
        "$sha256.ComputeHash($stream)",
        "if ($hash -notmatch '^[0-9a-f]{64}$')",
        "$record = \"$hash  $($script:ExpectedPackageName)\"",
        "[Text.Encoding]::ASCII.GetBytes($record + [Environment]::NewLine)",
        '".tmp-$nonce"',
        '".bak-$nonce"',
        "[IO.File]::WriteAllBytes($tempPath, $recordBytes)",
        "[IO.File]::Replace($tempPath, $outputFullPath, $backupPath, $true)",
        "[IO.File]::Move($tempPath, $outputFullPath)",
        "Published V26 checksum bytes do not match the computed canonical record.",
        "V26 checksum staging residue remains",
        "V26 checksum backup residue remains",
    )
    for token in required:
        require(token in text, "V26 checksum helper missing safety token: " + token)

    source_guard = text.find("Resolve-OrdinaryNonReparseFile -Path $PackagePath")
    output_parent_guard = text.find("Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath")
    output_leaf_guard = text.find("Assert-SafeExistingOutputLeaf -Path $outputFullPath")
    open_pos = text.find("[IO.File]::Open($package.FullName")
    hash_pos = text.find("$sha256.ComputeHash($stream)")
    temp_write = text.find("[IO.File]::WriteAllBytes($tempPath, $recordBytes)")
    temp_guard = text.find("Resolve-OrdinaryNonReparseFile -Path $tempPath")
    replace_pos = text.find("[IO.File]::Replace($tempPath, $outputFullPath")
    move_pos = text.find("[IO.File]::Move($tempPath, $outputFullPath)")
    published_pos = text.find("$published = $true")
    verify_pos = text.find("Published V26 checksum bytes do not match the computed canonical record.")
    residue_pos = text.find("V26 checksum staging residue remains")

    positions = (source_guard, output_parent_guard, output_leaf_guard, open_pos, hash_pos, temp_write, temp_guard)
    require(min(positions) >= 0, "V26 checksum safety ordering token missing")
    require(
        source_guard < output_parent_guard < output_leaf_guard < open_pos < hash_pos < temp_write < temp_guard,
        "V26 checksum must validate source/destination before hash and validate staging before publication",
    )
    require(replace_pos > temp_guard and move_pos > temp_guard, "V26 checksum publication must occur only after staging validation")
    require(published_pos > min(replace_pos, move_pos), "V26 checksum commit marker must follow atomic publication")
    require(verify_pos > published_pos, "V26 checksum must verify canonical published bytes after commit")
    require(residue_pos > verify_pos, "V26 checksum residue checks must run after publication verification/cleanup")

    require("Get-FileHash" not in text, "V26 checksum helper must hash the guarded open stream instead of reopening by path")
    require("Set-Content" not in text, "V26 checksum helper must not write directly to the final checksum path")


def workflow_contract(text: str) -> None:
    start = text.find("      - name: Create V26 package checksum")
    end = text.find("      - name: Upload V26 qualification artifacts", start + 1)
    require(start >= 0 and end > start, "V26 release workflow checksum step is missing")
    block = text[start:end]
    for token in (
        ".\\scripts\\write-v26-package-checksum.ps1",
        "-PackagePath 'dist\\QS3D-BricsCAD-V26.zip'",
        "-OutputPath 'dist\\QS3D-BricsCAD-V26.zip.sha256'",
    ):
        require(token in block, "V26 checksum workflow missing shared-helper binding: " + token)
    for forbidden in ("Get-FileHash", "Set-Content", "Resolve-Path"):
        require(forbidden not in block, "V26 checksum workflow reintroduced inline unsafe checksum logic: " + forbidden)


def expect_helper_mutation_failure(original: str, token: str, replacement: str, label: str) -> None:
    require(token in original, "mutation source token missing for " + label)
    mutated = original.replace(token, replacement, 1)
    failed = False
    try:
        helper_contract(mutated)
    except AssertionError:
        failed = True
    require(failed, "V26 checksum guard accepted mutation: " + label)


def expect_workflow_mutation_failure(original: str, token: str, replacement: str, label: str) -> None:
    require(token in original, "workflow mutation source token missing for " + label)
    mutated = original.replace(token, replacement, 1)
    failed = False
    try:
        workflow_contract(mutated)
    except AssertionError:
        failed = True
    require(failed, "V26 checksum workflow guard accepted mutation: " + label)


def main() -> int:
    require(HELPER.is_file(), "missing scripts/write-v26-package-checksum.ps1")
    require(WORKFLOW.is_file(), "missing .github/workflows/release-v26.yml")
    helper = HELPER.read_text(encoding="utf-8")
    workflow = WORKFLOW.read_text(encoding="utf-8")

    helper_contract(helper)
    workflow_contract(workflow)

    mutations = (
        ("[IO.FileAttributes]::ReparsePoint", "[IO.FileAttributes]::Hidden", "reparse rejection"),
        ("[IO.FileShare]::Read", "[IO.FileShare]::ReadWrite", "read-only hash handle"),
        ("$sha256.ComputeHash($stream)", "$sha256.ComputeHash([IO.File]::ReadAllBytes($package.FullName))", "stream-bound hashing"),
        ('".tmp-$nonce"', '".tmp"', "nonce staging"),
        ("[IO.File]::Replace($tempPath, $outputFullPath, $backupPath, $true)", "Copy-Item $tempPath $outputFullPath -Force", "atomic replacement"),
        ("Published V26 checksum bytes do not match the computed canonical record.", "published bytes ignored", "post-commit byte verification"),
        ("V26 checksum staging residue remains", "staging residue ignored", "staging residue check"),
    )
    for token, replacement, label in mutations:
        expect_helper_mutation_failure(helper, token, replacement, label)

    expect_workflow_mutation_failure(
        workflow,
        ".\\scripts\\write-v26-package-checksum.ps1",
        "Get-FileHash -LiteralPath 'dist\\QS3D-BricsCAD-V26.zip'",
        "shared-helper routing",
    )
    expect_workflow_mutation_failure(
        workflow,
        "-OutputPath 'dist\\QS3D-BricsCAD-V26.zip.sha256'",
        "-OutputPath 'dist\\other.sha256'",
        "canonical checksum destination",
    )

    print("PASS: V26 release checksum creation is stream-bound, ordinary/non-reparse guarded, canonically encoded, sibling-staged, failure-atomic, residue-checked, and workflow-routed through the shared helper.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
