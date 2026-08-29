#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SNAPSHOT = ROOT / "scripts/snapshot-v25-compile-references.ps1"

REQUIRED = (
    "function Open-LockedReferenceState",
    "function Assert-LockedReferenceState",
    "[IO.FileShare]::Read",
    "$locked = New-Object 'System.Collections.Generic.List[object]'",
    "$locked.Add([pscustomobject]@{",
    "state = Open-LockedReferenceState",
    "foreach ($entry in $locked)",
    "$source.stream.CopyTo($destinationStream)",
    "$sourceHashAfterCopy = Get-StreamSha256 -Stream $source.stream",
    "Rebind every source member while every lock is still held",
    "[IO.File]::WriteAllText($state, $json, $utf8)",
    "Remove-Item -LiteralPath $state -Force -ErrorAction SilentlyContinue",
    "$locked[$index].state.stream.Dispose()",
)

FORBIDDEN = (
    "[IO.File]::Copy($before.path, $destinationPath, $false)",
    "$before = Get-StableFileState -Path $sourcePath",
)


def validate(text: str) -> list[str]:
    failures: list[str] = []
    for token in REQUIRED:
        if token not in text:
            failures.append(f"missing cross-file generation contract marker: {token}")
    for token in FORBIDDEN:
        if token in text:
            failures.append(f"sequential unlocked source-capture marker remains: {token}")

    admission = text.find("# Admission is deliberately a separate phase")
    lock_open = text.find("state = Open-LockedReferenceState", admission)
    first_copy_loop = text.find("foreach ($entry in $locked)", lock_open)
    copy = text.find("$source.stream.CopyTo($destinationStream)", first_copy_loop)
    final_rebind = text.find("# Rebind every source member while every lock is still held", copy)
    publish = text.find("[IO.File]::WriteAllText($state, $json, $utf8)", final_rebind)
    dispose = text.find("$locked[$index].state.stream.Dispose()", publish)
    if not (0 <= admission < lock_open < first_copy_loop < copy < final_rebind < publish < dispose):
        failures.append(
            "all source locks must be admitted before copying, remain held through whole-set rebind/state publication, then dispose"
        )

    open_fn = text.find("function Open-LockedReferenceState")
    file_share = text.find("[IO.FileShare]::Read", open_fn)
    stream_hash = text.find("$hash = Get-StreamSha256 -Stream $stream", open_fn)
    if not (0 <= open_fn < file_share < stream_hash):
        failures.append("source generation digest must be computed from the held read lock")

    catch = text.find("catch {", publish)
    remove_state = text.find("Remove-Item -LiteralPath $state -Force -ErrorAction SilentlyContinue", catch)
    if not (0 <= publish < catch < remove_state < dispose):
        failures.append("failed set capture must remove the state manifest before source locks are released")

    return failures


def main() -> int:
    text = SNAPSHOT.read_text(encoding="utf-8")
    failures = validate(text)

    for token in (
        "[IO.FileShare]::Read",
        "state = Open-LockedReferenceState",
        "$source.stream.CopyTo($destinationStream)",
        "$sourceHashAfterCopy = Get-StreamSha256 -Stream $source.stream",
        "[IO.File]::WriteAllText($state, $json, $utf8)",
        "Remove-Item -LiteralPath $state -Force -ErrorAction SilentlyContinue",
        "$locked[$index].state.stream.Dispose()",
    ):
        mutated = text.replace(token, "MUTATED-REFERENCE-SET-GENERATION-MARKER")
        if not validate(mutated):
            failures.append(f"guard mutation escaped detection: {token}")

    if failures:
        print("V25 compile-reference set-generation preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V25 compile-reference snapshot is bound to one simultaneous source generation.")
    print(" - all required source DLLs are read-locked before the first snapshot copy")
    print(" - hashes are computed and rechecked through the held source streams")
    print(" - the manifest is published only while all source locks remain held")
    print(" - failed captures remove the manifest before releasing source locks")
    return 0


if __name__ == "__main__":
    sys.exit(main())
