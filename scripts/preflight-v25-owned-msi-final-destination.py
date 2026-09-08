#!/usr/bin/env python3
"""Fail closed unless V25 canonical MSI publication proves creator-handle destination identity."""

from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "acquire-v25-compile-references.ps1"
MAX_SOURCE_BYTES = 256 * 1024

DECL = "public static extern uint GetFinalPathNameByHandleW("
HELPER = "function Get-OwnedMsiFinalPath {"
HANDLE_CALL = "[QS3DV25NativeFileDisposition]::GetFinalPathNameByHandleW("
PROOF_ASSIGN = "$publishedFinalPath = Get-OwnedMsiFinalPath -Stream $publishedStream"
EXPECTED_ASSIGN = "$expectedPublishedPath = Get-CanonicalAbsolutePath -Path $msi"
COMPARE = "Test-CanonicalPathEqual -Left $publishedFinalPath -Right $expectedPublishedPath"
MISMATCH = "Owned canonical MSI creator handle resolved to an unexpected final destination"
PRECOMMIT_PROOF = "$publishedFinalPathBeforeCommit = Get-OwnedMsiFinalPath -Stream $publishedStream"
PRECOMMIT_COMPARE = "Test-CanonicalPathEqual -Left $publishedFinalPathBeforeCommit -Right $expectedPublishedPath"
PRECOMMIT_MISMATCH = "Owned canonical MSI creator handle final destination changed before publication commit"
OPEN = "$publishedStream = Open-OwnedMsiPublication -Path $msi"
COPY = "$stagingAdmission.Stream.CopyTo($publishedStream)"
COMMIT = "Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $false"


def fail(message: str) -> None:
    raise SystemExit(f"::error::{message}")


def load_source() -> str:
    try:
        stat = TARGET.stat()
    except OSError as exc:
        fail(f"cannot stat V25 acquisition script: {exc}")
    if not TARGET.is_file() or TARGET.is_symlink():
        fail("V25 acquisition script must be an ordinary non-symlink file")
    if stat.st_size > MAX_SOURCE_BYTES:
        fail(f"V25 acquisition script unexpectedly exceeds {MAX_SOURCE_BYTES} bytes")
    try:
        return TARGET.read_bytes().decode("utf-8", errors="strict")
    except (OSError, UnicodeDecodeError) as exc:
        fail(f"cannot read V25 acquisition script as strict UTF-8: {exc}")


def validate(source: str) -> list[str]:
    failures: list[str] = []
    required = (
        (DECL, "GetFinalPathNameByHandleW native declaration is missing"),
        (HELPER, "owned-handle final-path helper is missing"),
        (HANDLE_CALL, "owned-handle GetFinalPathNameByHandleW call is missing"),
        ("$Stream.SafeFileHandle", "final-path query must use the owned publication stream handle"),
        ("\\\\?\\UNC\\", "extended UNC final-path normalization is missing"),
        ("\\\\?\\", "extended DOS final-path normalization is missing"),
        (PROOF_ASSIGN, "creator-handle final path is not captured"),
        (EXPECTED_ASSIGN, "expected canonical publication path is not captured"),
        (COMPARE, "creator-handle final path is not compared with the canonical MSI path"),
        (MISMATCH, "final-destination mismatch does not fail closed"),
        (PRECOMMIT_PROOF, "creator-handle final path is not re-queried before commit"),
        (PRECOMMIT_COMPARE, "pre-commit creator-handle final path is not compared with the canonical MSI path"),
        (PRECOMMIT_MISMATCH, "pre-commit destination drift does not fail closed"),
        (OPEN, "fresh canonical MSI creator is missing"),
        (COPY, "held staging payload copy is missing"),
        (COMMIT, "owned publication commit disposition is missing"),
    )
    for token, message in required:
        if token not in source:
            failures.append(message)

    open_pos = source.find(OPEN)
    proof_pos = source.find(PROOF_ASSIGN, open_pos + len(OPEN) if open_pos >= 0 else 0)
    expected_pos = source.find(EXPECTED_ASSIGN, open_pos + len(OPEN) if open_pos >= 0 else 0)
    compare_pos = source.find(COMPARE, max(proof_pos, expected_pos, 0))
    mismatch_pos = source.find(MISMATCH, compare_pos if compare_pos >= 0 else 0)
    copy_pos = source.find(COPY, open_pos + len(OPEN) if open_pos >= 0 else 0)
    precommit_proof_pos = source.find(PRECOMMIT_PROOF, copy_pos + len(COPY) if copy_pos >= 0 else 0)
    precommit_compare_pos = source.find(PRECOMMIT_COMPARE, precommit_proof_pos if precommit_proof_pos >= 0 else 0)
    precommit_mismatch_pos = source.find(PRECOMMIT_MISMATCH, precommit_compare_pos if precommit_compare_pos >= 0 else 0)
    commit_pos = source.find(COMMIT, precommit_mismatch_pos if precommit_mismatch_pos >= 0 else 0)
    if min(open_pos, proof_pos, expected_pos, compare_pos, mismatch_pos, copy_pos, precommit_proof_pos, precommit_compare_pos, precommit_mismatch_pos, commit_pos) < 0:
        failures.append("creator-handle final-destination proof sequence is incomplete")
    else:
        if not (open_pos < proof_pos < compare_pos < mismatch_pos < copy_pos and open_pos < expected_pos < compare_pos):
            failures.append("creator-handle final-destination identity must be proven fail-closed after CREATE_NEW and before any staging payload bytes are copied")
        if not (copy_pos < precommit_proof_pos < precommit_compare_pos < precommit_mismatch_pos < commit_pos):
            failures.append("creator-handle final-destination identity must be re-proven after payload verification and before delete-disposition commit")

    if 0 <= open_pos < copy_pos and HANDLE_CALL not in source[source.find(HELPER):open_pos]:
        failures.append("final-destination proof helper must query the creator handle, not only repeat pathname checks")

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
        (HELPER, "final-path helper"),
        (HANDLE_CALL, "native final-path invocation"),
        (PROOF_ASSIGN, "creator-handle final-path capture"),
        (EXPECTED_ASSIGN, "expected canonical-path capture"),
        (COMPARE, "final-path comparison"),
        (MISMATCH, "fail-closed mismatch branch"),
        (PRECOMMIT_PROOF, "pre-commit final-path capture"),
        (PRECOMMIT_COMPARE, "pre-commit final-path comparison"),
        (PRECOMMIT_MISMATCH, "pre-commit fail-closed mismatch branch"),
    )
    for token, label in mutations:
        if token not in source:
            print(f"FAIL: mutation fixture missing: {label}")
            return 1
        mutated = source.replace(token, "MUTATED-FINAL-DESTINATION-PROOF", 1)
        if not validate(mutated):
            print(f"FAIL: guard mutation escaped detection: {label}")
            return 1

    without_proof = source.replace(PROOF_ASSIGN, "", 1)
    copy_index = without_proof.find(COPY)
    ordering_mutated = without_proof[: copy_index + len(COPY)] + "\n" + PROOF_ASSIGN + without_proof[copy_index + len(COPY) :]
    if not validate(ordering_mutated):
        print("FAIL: guard mutation escaped detection: first proof moved after payload copy")
        return 1

    without_reproof = source.replace(PRECOMMIT_PROOF, "", 1)
    commit_index = without_reproof.find(COMMIT)
    reproof_mutated = without_reproof[: commit_index + len(COMMIT)] + "\n" + PRECOMMIT_PROOF + without_reproof[commit_index + len(COMMIT) :]
    if not validate(reproof_mutated):
        print("FAIL: guard mutation escaped detection: pre-commit re-proof moved after commit")
        return 1

    print("PASS: V25 canonical MSI creator handle proves final destination before copy and commit")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
