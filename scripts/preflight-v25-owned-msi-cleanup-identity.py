#!/usr/bin/env python3
"""Fail closed if V25 failed-publication cleanup can delete a replacement pathname."""

from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "acquire-v25-compile-references.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"FAIL: {message}")


def main() -> int:
    source = TARGET.read_text(encoding="utf-8")

    start_marker = "$publishedByThisAttempt = $false"
    end_marker = 'Write-Warning "BricsCAD V25 installer source failed:'
    require(start_marker in source, "V25 publisher ownership state is missing")
    start = source.index(start_marker)
    end = source.find(end_marker, start)
    require(end > start, "could not isolate failed owned-publication cleanup window")
    window = source[start:end]

    # Reopening the canonical pathname after disposing the creator handle is
    # inherently racy. The created generation must instead remain owned by one
    # live handle whose close semantics delete it unless publication is
    # explicitly committed. A successful commit clears delete disposition only
    # after bytes on that same handle have been rehashed and matched to staging.
    delete_on_close = "[IO.FileOptions]::DeleteOnClose"
    native_clear = "Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $false"
    same_handle_rehash = "$publishedStream.Position = 0"
    same_handle_hash = "$publishedHashBytes = $publishedSha.ComputeHash($publishedStream)"
    pathname_delete = "[IO.File]::Delete($msi)"
    reopened_cleanup = "Get-OrdinaryFileOrNull -Path $msi -Label 'Failed owned canonical MSI publication'"

    for token, message in (
        ("SetFileInformationByHandle", "native handle disposition primitive is missing"),
        ("FileDispositionInfo", "file disposition information class is missing"),
        (delete_on_close, "owned canonical MSI is not created delete-on-close"),
        (same_handle_rehash, "owned publication handle is not rewound for same-handle verification"),
        (same_handle_hash, "owned publication bytes are not rehashed through the creator handle"),
        (native_clear, "successful publication does not explicitly clear delete disposition"),
    ):
        require(token in source, message)

    require(delete_on_close in window, "delete-on-close ownership is not established in the publication window")
    require(same_handle_rehash in window, "same-handle verification is not in the owned publication window")
    require(same_handle_hash in window, "same-handle hash verification is not in the owned publication window")
    require(native_clear in window, "publication commit is not in the owned publication window")
    require(pathname_delete not in window, "failed owned-publication cleanup must not delete the canonical pathname")
    require(reopened_cleanup not in window, "failed owned-publication cleanup must not reopen the canonical pathname")

    create_pos = window.index(delete_on_close)
    rehash_pos = window.index(same_handle_rehash, create_pos)
    hash_pos = window.index(same_handle_hash, rehash_pos)
    clear_pos = window.index(native_clear, hash_pos)
    dispose_pos = window.find("$publishedStream.Dispose()", clear_pos)
    require(dispose_pos >= 0, "owned publication stream disposal after commit is missing")
    require(create_pos < rehash_pos < hash_pos < clear_pos < dispose_pos,
            "delete-on-close must remain armed through same-handle verification and clear only at commit")

    # The ownership flag may stop triggering cleanup only after native commit;
    # clearing it earlier would turn a failed commit into a leaked canonical file.
    ownership_clear = "$publishedByThisAttempt = $false"
    ownership_clear_pos = window.find(ownership_clear, clear_pos)
    require(ownership_clear_pos > clear_pos,
            "publication ownership must not clear before delete disposition is successfully committed")

    # Catch/finally may dispose the live handle, but must not release it before
    # deciding whether the generation is committed. DeleteOnClose then targets
    # the exact handle-owned generation and cannot delete a later replacement.
    catch_pos = window.find("catch {")
    require(catch_pos >= 0, "publication catch block is missing")
    require(pathname_delete not in window[catch_pos:], "catch cleanup must remain handle-only")

    # Mutation locks for the three security-critical phases.
    for token, label in (
        (delete_on_close, "delete-on-close ownership"),
        (same_handle_hash, "same-handle verification"),
        (native_clear, "explicit publication commit"),
    ):
        mutated = window.replace(token, "", 1)
        require(token not in mutated, f"{label} mutation probe did not remove the protected invariant")

    print("PASS: V25 canonical MSI publication remains handle-owned/delete-on-close until same-handle verification commits it")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
