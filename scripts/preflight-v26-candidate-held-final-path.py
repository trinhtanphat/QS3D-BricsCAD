#!/usr/bin/env python3
"""Fail closed unless V26 held candidate inputs bind pathname admission to the opened handle."""

from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "assert-v26-candidate-identity.ps1"
MAX_SOURCE_BYTES = 256 * 1024

DECL = "public static extern uint GetFinalPathNameByHandleW("
HELPER = "function Get-HeldFinalPath {"
HANDLE_CALL = "[QS3DV26HeldFileIdentity]::GetFinalPathNameByHandleW("
SAFE_HANDLE = "$Stream.SafeFileHandle"
UNC_BRANCH = "if ($resolved.StartsWith('\\\\?\\UNC\\', [StringComparison]::OrdinalIgnoreCase)) {"
DOS_BRANCH = "elseif ($resolved.StartsWith('\\\\?\\', [StringComparison]::OrdinalIgnoreCase)) {"
CANONICAL = "[IO.Path]::GetFullPath($item.FullName)"
OPEN = "$stream = [IO.File]::Open($canonicalPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)"
PROOF = "$openedFinalPath = Get-HeldFinalPath -Stream $stream"
COMPARE = "[string]::Equals($openedFinalPath, $canonicalPath, [StringComparison]::OrdinalIgnoreCase)"
MISMATCH = "opened handle resolved to a different final path"
RETURN = "return [pscustomobject]@{ Path=$canonicalPath; Length=[int64]$stream.Length; LastWriteUtcTicks=[int64]$current.LastWriteTimeUtc.Ticks; Stream=$stream }"


def fail(message: str) -> None:
    raise SystemExit(f"::error::{message}")


def load_source() -> str:
    try:
        stat = TARGET.stat()
    except OSError as exc:
        fail(f"cannot stat V26 candidate identity script: {exc}")
    if not TARGET.is_file() or TARGET.is_symlink():
        fail("V26 candidate identity script must be an ordinary non-symlink file")
    if stat.st_size > MAX_SOURCE_BYTES:
        fail(f"V26 candidate identity script unexpectedly exceeds {MAX_SOURCE_BYTES} bytes")
    try:
        return TARGET.read_bytes().decode("utf-8", errors="strict")
    except (OSError, UnicodeDecodeError) as exc:
        fail(f"cannot read V26 candidate identity script as strict UTF-8: {exc}")


def validate(source: str) -> list[str]:
    failures: list[str] = []
    required = (
        (DECL, "GetFinalPathNameByHandleW declaration is missing"),
        (HELPER, "held-handle final-path helper is missing"),
        (HANDLE_CALL, "held-handle final-path invocation is missing"),
        (SAFE_HANDLE, "final-path query is not bound to the held stream SafeFileHandle"),
        (UNC_BRANCH, "extended UNC final-path normalization is missing"),
        (DOS_BRANCH, "extended DOS final-path normalization is missing"),
        (CANONICAL, "canonical admitted pathname capture is missing"),
        (OPEN, "held read open is not bound to the captured canonical pathname"),
        (PROOF, "opened handle final-path proof is missing"),
        (COMPARE, "opened handle final path is not compared with the admitted canonical pathname"),
        (MISMATCH, "opened-handle final-path mismatch does not fail closed"),
        (RETURN, "held state does not retain the admitted canonical pathname and exact stream"),
    )
    for token, message in required:
        if token not in source:
            failures.append(message)

    helper_pos = source.find(HELPER)
    call_pos = source.find(HANDLE_CALL, helper_pos if helper_pos >= 0 else 0)
    open_pos = source.find(OPEN)
    proof_pos = source.find(PROOF, open_pos + len(OPEN) if open_pos >= 0 else 0)
    compare_pos = source.find(COMPARE, proof_pos + len(PROOF) if proof_pos >= 0 else 0)
    mismatch_pos = source.find(MISMATCH, compare_pos if compare_pos >= 0 else 0)
    return_pos = source.find(RETURN, mismatch_pos if mismatch_pos >= 0 else 0)
    if min(helper_pos, call_pos, open_pos, proof_pos, compare_pos, mismatch_pos, return_pos) < 0:
        failures.append("held-handle final-path proof sequence is incomplete")
    else:
        if not (helper_pos < call_pos < open_pos < proof_pos < compare_pos < mismatch_pos < return_pos):
            failures.append("opened handle final-path identity must be proven fail-closed after open and before held state is returned")

    if "Resolve-OrdinaryFile -Path $Path -Label $Label" not in source:
        failures.append("ordinary-file/reparse pathname admission was removed")
    if "Assert-Held -Held $item -Label 'V26 candidate identity input'" not in source:
        failures.append("post-admission held-generation checks were removed")
    return failures


def main() -> int:
    source = load_source()
    failures = validate(source)
    if failures:
        for message in failures:
            print(f"FAIL: {message}")
        return 1

    mutations = (
        (DECL, "native final-path declaration"),
        (HELPER, "held final-path helper"),
        (HANDLE_CALL, "native final-path invocation"),
        (SAFE_HANDLE, "exact held SafeFileHandle"),
        (UNC_BRANCH, "extended UNC normalization"),
        (DOS_BRANCH, "extended DOS normalization"),
        (CANONICAL, "canonical admitted path capture"),
        (PROOF, "opened-handle final-path proof"),
        (COMPARE, "opened/admitted path comparison"),
        (MISMATCH, "fail-closed mismatch branch"),
    )
    for token, label in mutations:
        if token not in source:
            print(f"FAIL: mutation fixture missing: {label}")
            return 1
        mutated = source.replace(token, "MUTATED-V26-HELD-FINAL-PATH", 1)
        if not validate(mutated):
            print(f"FAIL: guard mutation escaped detection: {label}")
            return 1

    without_proof = source.replace(PROOF, "", 1)
    return_index = without_proof.find(RETURN)
    moved = without_proof[: return_index + len(RETURN)] + "\n" + PROOF + without_proof[return_index + len(RETURN) :]
    if not validate(moved):
        print("FAIL: guard mutation escaped detection: handle proof moved after held-state return")
        return 1

    print("PASS: V26 candidate admission binds every held input to the opened handle final-path identity")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
