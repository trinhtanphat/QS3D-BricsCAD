#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "write-v26-package-checksum.ps1"


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
        ("$publicationStarted = $false", "publication-start state"),
        ("$publicationCommitted = $false", "verified-commit state"),
        ("$publicationStarted = $true", "post-mutation publication-start marker"),
        ("$publicationCommitted = $true", "post-verification commit marker"),
        ("if ($publicationStarted -and -not $publicationCommitted)", "pre-commit rollback condition"),
        ("Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum rollback parent'", "rollback parent revalidation"),
        ("Resolve-OrdinaryNonReparseFile -Path $backupPath -Label 'V26 checksum rollback backup'", "rollback backup revalidation"),
        ("[void](Assert-SafeExistingOutputLeaf -Path $outputFullPath)", "rollback destination revalidation"),
        ("[IO.File]::Move($backup.FullName, $outputFullPath)", "existing-destination restoration"),
        ("elseif (Test-Path -LiteralPath $outputFullPath)", "new-destination rollback branch"),
        ("throw \"V26 checksum publication failed and rollback could not safely restore the pre-publication state.", "fail-closed rollback failure"),
        ("if ($publicationCommitted) {", "backup cleanup commit gate"),
        ("Remove-SafeChecksumLeaf -Path $backupPath", "safe committed-backup cleanup"),
    ):
        require(text, token, errors, label)

    require_order(
        text,
        "[IO.File]::Replace($tempPath, $outputFullPath, $backupPath, $true)",
        "$publicationStarted = $true",
        errors,
        "replacement then publication-start state",
    )
    require_order(
        text,
        "$publicationStarted = $true",
        "$publishedItem = Resolve-OrdinaryNonReparseFile",
        errors,
        "publication before destination verification",
    )
    require_order(
        text,
        "$publishedItem = Resolve-OrdinaryNonReparseFile",
        "$publishedText = [IO.File]::ReadAllText",
        errors,
        "ordinary-file validation before byte verification",
    )
    require_order(
        text,
        "if (-not [string]::Equals($publishedText, $record, [StringComparison]::Ordinal))",
        "$publicationCommitted = $true",
        errors,
        "byte verification before commit",
    )

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


def reference_model(
    *,
    had_existing: bool,
    verification_ok: bool,
    destination_safe: bool = True,
    backup_safe: bool = True,
) -> ModelResult:
    old = "old-bytes" if had_existing else None
    destination = "new-canonical-bytes"
    backup_preserved = had_existing

    if verification_ok:
        return ModelResult(destination, False, True, False)

    if not destination_safe or (had_existing and not backup_safe):
        # A hostile path substitution must stop rollback rather than follow/delete it.
        return ModelResult(destination, backup_preserved, False, True)

    if had_existing:
        destination = old
        backup_preserved = False
    else:
        destination = None
    return ModelResult(destination, backup_preserved, False, False)


def validate_reference_model() -> list[str]:
    errors: list[str] = []
    cases = (
        (
            "existing verification success",
            dict(had_existing=True, verification_ok=True),
            ModelResult("new-canonical-bytes", False, True, False),
        ),
        (
            "new verification success",
            dict(had_existing=False, verification_ok=True),
            ModelResult("new-canonical-bytes", False, True, False),
        ),
        (
            "existing verification failure restores exact prior bytes",
            dict(had_existing=True, verification_ok=False),
            ModelResult("old-bytes", False, False, False),
        ),
        (
            "new verification failure removes candidate",
            dict(had_existing=False, verification_ok=False),
            ModelResult(None, False, False, False),
        ),
        (
            "unsafe replacement fails closed and preserves backup",
            dict(had_existing=True, verification_ok=False, destination_safe=False),
            ModelResult("new-canonical-bytes", True, False, True),
        ),
        (
            "unsafe backup fails closed without following it",
            dict(had_existing=True, verification_ok=False, backup_safe=False),
            ModelResult("new-canonical-bytes", True, False, True),
        ),
    )
    for label, kwargs, expected in cases:
        actual = reference_model(**kwargs)
        if actual != expected:
            errors.append(f"reference model mismatch for {label}: {actual!r} != {expected!r}")
    return errors


def mutation_probes(source: str) -> list[str]:
    errors: list[str] = []
    mutations = {
        "premature commit": source.replace(
            "$publicationStarted = $true\n\n    # Publication is not committed",
            "$publicationStarted = $true\n    $publicationCommitted = $true\n\n    # Publication is not committed",
            1,
        ),
        "missing rollback state gate": source.replace(
            "if ($publicationStarted -and -not $publicationCommitted)",
            "if ($false)",
            1,
        ),
        "missing backup revalidation": source.replace(
            "$backup = Resolve-OrdinaryNonReparseFile -Path $backupPath -Label 'V26 checksum rollback backup'",
            "$backup = Get-Item -LiteralPath $backupPath",
            1,
        ),
        "missing destination revalidation": source.replace(
            "[void](Assert-SafeExistingOutputLeaf -Path $outputFullPath)\n                    Remove-Item -LiteralPath $outputFullPath",
            "Remove-Item -LiteralPath $outputFullPath",
            1,
        ),
        "legacy silent backup cleanup": source.replace(
            "Remove-SafeChecksumLeaf -Path $backupPath -Label 'V26 checksum committed backup residue'",
            "Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue",
            1,
        ),
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

    print(
        "PASS: V26 checksum publication is two-phase, verification-gated, "
        "rollback-safe for existing/new destinations, and reparse-safe during rollback."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
