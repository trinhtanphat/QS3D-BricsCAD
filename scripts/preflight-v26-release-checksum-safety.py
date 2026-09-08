#!/usr/bin/env python3
"""Cross-guard release-v26 checksum routing and generation-safe helper semantics."""
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
        "$packageCanonicalPath = $package.FullName", "$packageLength = [int64]$package.Length",
        "$packageLastWriteUtcTicks = [int64]$package.LastWriteTimeUtc.Ticks",
        "Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum destination parent'",
        "$originalOutputBytes = Read-BoundedChecksumBytes",
        "[IO.File]::Open($packageCanonicalPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)",
        "Resolve-OrdinaryNonReparseFile -Path $packageCanonicalPath -Label 'V26 package ZIP after open'",
        "$sha256.ComputeHash($stream)", "$record = \"$hash  $($script:ExpectedPackageName)\"",
        "$tempGeneration = New-OwnedChecksumGeneration", "$originalOutputGeneration = Open-OwnedChecksumGeneration",
        "$publicationStarted = $true",
        "[IO.File]::Replace($tempPath, $outputFullPath, $backupPath, $true)", "[IO.File]::Move($tempPath, $outputFullPath)",
        "$publishedAttemptIdentity = $tempGeneration.Identity",
        "$publishedGeneration = Open-PinnedChecksumGeneration -Path $outputFullPath",
        "publishedGeneration.Identity, $publishedAttemptIdentity",
        "Published V26 checksum bytes do not match the computed canonical record.", "$publicationCommitted = $true",
        "if ($publicationStarted -and -not $publicationCommitted)",
        "$rollbackPublishedGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath",
        "rollbackPublishedGeneration.Identity, $publishedAttemptIdentity",
        "Remove-OwnedChecksumGeneration -Generation $rollbackPublishedGeneration",
        "backupProof.Identity, $originalOutputGeneration.Identity",
        "Remove-OwnedChecksumGeneration -Generation $originalOutputGeneration",
        "PackagePath = $packageCanonicalPath",
    )
    for token in required:
        require(token in text, "V26 checksum helper missing safety token: " + token)
    for forbidden in ("Get-FileHash", "Set-Content", "Remove-Item -LiteralPath $outputFullPath", "Remove-SafeChecksumLeaf -Path $backupPath"):
        require(forbidden not in text, "V26 checksum helper reintroduced unsafe/legacy operation: " + forbidden)

    source_guard = text.index("Resolve-OrdinaryNonReparseFile -Path $PackagePath")
    open_pos = text.index("[IO.File]::Open($packageCanonicalPath")
    hash_pos = text.index("$sha256.ComputeHash($stream)", open_pos)
    stage_pos = text.index("$tempGeneration = New-OwnedChecksumGeneration", hash_pos)
    started = text.index("$publicationStarted = $true", stage_pos)
    replace = text.index("[IO.File]::Replace($tempPath", started)
    move = text.index("[IO.File]::Move($tempPath", started)
    identity = text.index("$publishedAttemptIdentity = $tempGeneration.Identity", max(replace, move))
    pin = text.index("$publishedGeneration = Open-PinnedChecksumGeneration -Path $outputFullPath", identity)
    proof = text.index("publishedGeneration.Identity, $publishedAttemptIdentity", pin)
    verify = text.index("Published V26 checksum bytes do not match the computed canonical record.", proof)
    commit = text.index("$publicationCommitted = $true", verify)
    rollback = text.index("if ($publicationStarted -and -not $publicationCommitted)", commit)
    require(source_guard < open_pos < hash_pos < stage_pos < started < replace < identity, "V26 checksum source/hash/staging/replace ordering invalid")
    require(started < move < identity < pin < proof < verify < commit < rollback, "V26 checksum generation pin/verify/commit/rollback ordering invalid")


def workflow_contract(text: str) -> None:
    start = text.find("      - name: Create V26 package checksum")
    end = text.find("      - name: Upload V26 qualification artifacts", start + 1)
    require(start >= 0 and end > start, "V26 release workflow checksum step is missing")
    block = text[start:end]
    for token in (".\\scripts\\write-v26-package-checksum.ps1", "-PackagePath 'dist\\QS3D-BricsCAD-V26.zip'", "-OutputPath 'dist\\QS3D-BricsCAD-V26.zip.sha256'"):
        require(token in block, "V26 checksum workflow missing shared-helper binding: " + token)
    for forbidden in ("Get-FileHash", "Set-Content", "Resolve-Path"):
        require(forbidden not in block, "V26 checksum workflow reintroduced inline unsafe checksum logic: " + forbidden)


def mutation_failure(original: str, token: str) -> None:
    require(token in original, "mutation source token missing: " + token)
    mutated = original.replace(token, "__C05_MUTATION_REMOVED__")
    try:
        helper_contract(mutated)
    except (AssertionError, ValueError):
        return
    raise AssertionError("V26 checksum guard accepted mutation: " + token)


def main() -> int:
    require(HELPER.is_file(), "missing scripts/write-v26-package-checksum.ps1")
    require(WORKFLOW.is_file(), "missing .github/workflows/release-v26.yml")
    helper = HELPER.read_text(encoding="utf-8")
    workflow = WORKFLOW.read_text(encoding="utf-8")
    helper_contract(helper)
    workflow_contract(workflow)
    for token in (
        "$sha256.ComputeHash($stream)", "$tempGeneration = New-OwnedChecksumGeneration",
        "$publishedAttemptIdentity = $tempGeneration.Identity",
        "$publishedGeneration = Open-PinnedChecksumGeneration -Path $outputFullPath",
        "publishedGeneration.Identity, $publishedAttemptIdentity", "$publicationCommitted = $true",
        "$rollbackPublishedGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath",
        "Remove-OwnedChecksumGeneration -Generation $rollbackPublishedGeneration",
    ):
        mutation_failure(helper, token)
    print("PASS: V26 release checksum remains held-stream hashed, generation-owned/pinned through publication and rollback, and routed through the shared helper.")
    return 0

if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (AssertionError, ValueError) as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
