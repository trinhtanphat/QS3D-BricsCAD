#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "write-v26-package-checksum.ps1"
ROLLBACK_DESTINATION_GUARD = (
    "if (Test-Path -LiteralPath $outputFullPath) {\n"
    "                        [void](Assert-SafeExistingOutputLeaf -Path $outputFullPath)\n"
    "                        Remove-Item -LiteralPath $outputFullPath"
)
UNCHANGED_PROOF = (
    "$currentOutputBytes = Read-BoundedChecksumBytes -Path $outputFullPath "
    "-Label 'V26 checksum rollback unchanged-destination proof'"
)


def require(text: str, token: str, errors: list[str], label: str) -> None:
    if token not in text:
        errors.append(f"missing {label}: {token}")


def require_order(text: str, before: str, after: str, errors: list[str], label: str) -> None:
    before_pos = text.find(before)
    after_pos = text.find(after)
    if before_pos < 0 or after_pos < 0 or before_pos >= after_pos:
        errors.append(f"invalid ordering for {label}: {before!r} must precede {after!r}")


def validate_source(text: str) -> list[str]:
    errors: list[str] = []
    for token, label in (
        ("$script:MaxChecksumBytes = 1024", "bounded checksum snapshot size"),
        ("$originalOutputBytes = Read-BoundedChecksumBytes", "pre-publication destination snapshot"),
        ("$publicationStarted = $false", "publication-start state"),
        ("$publicationCommitted = $false", "verified-commit state"),
        ("$publicationStarted = $true", "pre-mutation publication-start marker"),
        ("$publicationCommitted = $true", "post-verification commit marker"),
        ("if ($publicationStarted -and -not $publicationCommitted)", "pre-commit rollback condition"),
        ("Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum rollback parent'", "rollback parent revalidation"),
        ("if (Test-Path -LiteralPath $backupPath)", "backup-presence rollback branch"),
        ("Resolve-OrdinaryNonReparseFile -Path $backupPath -Label 'V26 checksum rollback backup'", "rollback backup revalidation"),
        (ROLLBACK_DESTINATION_GUARD, "rollback destination revalidation immediately before removal"),
        ("[IO.File]::Move($backup.FullName, $outputFullPath)", "existing-destination restoration"),
        (UNCHANGED_PROOF, "backup-absent unchanged-destination proof"),
        ("[Convert]::ToBase64String($originalOutputBytes)", "bounded original-byte comparison"),
        ("[Convert]::ToBase64String($currentOutputBytes)", "bounded current-byte comparison"),
        ("the original destination cannot be proven unchanged", "fail-closed missing-backup mismatch"),
        ("elseif (Test-Path -LiteralPath $outputFullPath)", "new-destination rollback branch"),
        ("throw \"V26 checksum publication failed and rollback could not safely restore the pre-publication state.", "fail-closed rollback failure"),
        ("if ($publicationCommitted) {", "backup cleanup commit gate"),
        ("Remove-SafeChecksumLeaf -Path $backupPath", "safe committed-backup cleanup"),
    ):
        require(text, token, errors, label)

    replace = "[IO.File]::Replace($tempPath, $outputFullPath, $backupPath, $true)"
    move = "[IO.File]::Move($tempPath, $outputFullPath)"
    require_order(text, "$publicationStarted = $true", replace, errors, "publication state before replacement mutation")
    require_order(text, "$publicationStarted = $true", move, errors, "publication state before move mutation")
    require_order(text, "$publicationStarted = $true", "$publishedItem = Resolve-OrdinaryNonReparseFile", errors, "publication before destination verification")
    require_order(text, "$publishedItem = Resolve-OrdinaryNonReparseFile", "$publishedText = [IO.File]::ReadAllText", errors, "ordinary-file validation before byte verification")
    require_order(text, "if (-not [string]::Equals($publishedText, $record, [StringComparison]::Ordinal))", "$publicationCommitted = $true", errors, "byte verification before commit")

    if "$published = $true" in text:
        errors.append("legacy premature $published commit marker must not return")
    if "if (-not $published -and (Test-Path -LiteralPath $backupPath))" in text:
        errors.append("legacy rollback condition skips post-publication verification failures")
    if "Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue" in text:
        errors.append("backup cleanup must not silently delete an unvalidated path")
    return errors


@dataclass(frozen=True)
class ModelResult:
    destination: str | None
    backup_preserved: bool
    committed: bool
    rollback_failed_closed: bool


def reference_model(*, had_existing: bool, mutation_changed: bool, backup_created: bool, verification_ok: bool, destination_safe: bool = True, backup_safe: bool = True) -> ModelResult:
    old = "old-bytes" if had_existing else None
    destination = "new-canonical-bytes" if mutation_changed else old
    backup_preserved = had_existing and backup_created

    if mutation_changed and verification_ok:
        return ModelResult(destination, False, True, False)
    if not destination_safe or (backup_created and not backup_safe):
        return ModelResult(destination, backup_preserved, False, True)
    if had_existing:
        if backup_created:
            return ModelResult(old, False, False, False)
        if not mutation_changed:
            return ModelResult(old, False, False, False)
        return ModelResult(destination, False, False, True)
    return ModelResult(None, False, False, False)


def validate_reference_model() -> list[str]:
    errors: list[str] = []
    cases = (
        ("existing success", dict(had_existing=True, mutation_changed=True, backup_created=True, verification_ok=True), ModelResult("new-canonical-bytes", False, True, False)),
        ("new success", dict(had_existing=False, mutation_changed=True, backup_created=False, verification_ok=True), ModelResult("new-canonical-bytes", False, True, False)),
        ("verification failure restores backup", dict(had_existing=True, mutation_changed=True, backup_created=True, verification_ok=False), ModelResult("old-bytes", False, False, False)),
        ("replace throws before mutation and backup", dict(had_existing=True, mutation_changed=False, backup_created=False, verification_ok=False), ModelResult("old-bytes", False, False, False)),
        ("replace mutates then throws without backup fails closed", dict(had_existing=True, mutation_changed=True, backup_created=False, verification_ok=False), ModelResult("new-canonical-bytes", False, False, True)),
        ("new move failure removes candidate", dict(had_existing=False, mutation_changed=True, backup_created=False, verification_ok=False), ModelResult(None, False, False, False)),
    )
    for label, kwargs, expected in cases:
        actual = reference_model(**kwargs)
        if actual != expected:
            errors.append(f"reference model mismatch for {label}: {actual!r} != {expected!r}")
    return errors


def mutation_probes(source: str) -> list[str]:
    errors: list[str] = []
    mutations = {
        "publication marker moved after Replace": source.replace(
            "$publicationStarted = $true\n    if ($hadExistingOutput) {\n        [IO.File]::Replace",
            "if ($hadExistingOutput) {\n        [IO.File]::Replace",
            1,
        ),
        "missing original snapshot": source.replace("$originalOutputBytes = Read-BoundedChecksumBytes", "$originalOutputBytes = [byte[]]@()", 1),
        "missing unchanged proof": source.replace(UNCHANGED_PROOF, "$currentOutputBytes = [byte[]]@()", 1),
        "missing rollback state gate": source.replace("if ($publicationStarted -and -not $publicationCommitted)", "if ($false)", 1),
        "missing backup revalidation": source.replace("$backup = Resolve-OrdinaryNonReparseFile -Path $backupPath -Label 'V26 checksum rollback backup'", "$backup = Get-Item -LiteralPath $backupPath", 1),
        "missing destination revalidation": source.replace(ROLLBACK_DESTINATION_GUARD, "if (Test-Path -LiteralPath $outputFullPath) {\n                        Remove-Item -LiteralPath $outputFullPath", 1),
        "legacy silent backup cleanup": source.replace("Remove-SafeChecksumLeaf -Path $backupPath -Label 'V26 checksum committed backup residue'", "Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue", 1),
    }
    for label, mutated in mutations.items():
        if mutated == source:
            errors.append(f"mutation probe could not modify source: {label}")
            continue
        if not validate_source(mutated):
            errors.append(f"mutation escaped checksum publication atomicity guard: {label}")
    return errors


def main() -> int:
    if not TARGET.is_file():
        print(f"ERROR: missing target: {TARGET.relative_to(ROOT)}")
        return 1
    try:
        source = TARGET.read_text(encoding="utf-8")
    except UnicodeError as exc:
        print(f"ERROR: target is not strict UTF-8: {exc}")
        return 1
    errors = validate_source(source)
    errors.extend(validate_reference_model())
    errors.extend(mutation_probes(source))
    if errors:
        for error in errors:
            print("ERROR:", error)
        print(f"FAILED with {len(errors)} error(s).")
        return 1
    print("PASS: V26 checksum publication covers mutation-before-return exceptions, verification failures, bounded snapshot proof, and reparse-safe rollback.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
