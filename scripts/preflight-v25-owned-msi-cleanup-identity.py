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

    # A pathname identity check followed by File.Delete(path) is still racy: a
    # replacement can occur between the check and deletion. Cleanup must keep a
    # handle for the exact generation created by this attempt and request
    # deletion through that handle while ownership is still proven.
    cleanup_handle_state = "$publishedCleanupHandle = $null"
    capture = "$publishedCleanupHandle = Open-OwnedCanonicalMsiCleanupHandle -Path $msi"
    deletion = "Remove-OwnedCanonicalMsiByHandle -Handle $publishedCleanupHandle"
    pathname_delete = "[IO.File]::Delete($msi)"

    for token, message in (
        ("SetFileInformationByHandle", "native handle-bound deletion primitive is missing"),
        ("FileDispositionInfo", "file-disposition delete class is missing"),
        (cleanup_handle_state, "owned cleanup-handle state is missing"),
        (capture, "owned canonical MSI cleanup handle is not captured for the created generation"),
        (deletion, "failed owned-publication cleanup is not performed through the captured handle"),
    ):
        require(token in source, message)

    require(capture in window, "cleanup handle is not acquired in the owned publication window")
    require(deletion in window, "handle-bound deletion is not executed in failed owned-publication cleanup")
    require(pathname_delete not in window, "failed owned-publication cleanup must not reopen/delete the canonical pathname")

    capture_pos = window.index(capture)
    delete_pos = window.index(deletion)
    dispose_token = "$publishedCleanupHandle.Dispose()"
    dispose_pos = window.find(dispose_token, capture_pos)
    require(dispose_pos >= 0, "owned cleanup handle disposal is missing")
    require(capture_pos < delete_pos < dispose_pos, "owned generation must be deleted before its cleanup handle is released")

    # The canonical publication write handle must not be mistaken for proof of
    # later cleanup ownership: the dedicated cleanup handle is acquired while
    # the created generation is still protected and remains live through delete.
    publish_dispose = window.find("$publishedStream.Dispose()")
    require(publish_dispose >= 0, "published stream disposal is missing")
    require(capture_pos < publish_dispose, "cleanup ownership handle must be captured before the publication handle is released")

    # Mutation locks: removing either ownership capture or handle-bound delete
    # must make the focused contract unsatisfied.
    for token, label in (
        (capture, "cleanup handle capture"),
        (deletion, "handle-bound deletion"),
    ):
        mutated = window.replace(token, "", 1)
        require(token not in mutated, f"{label} mutation probe did not remove the protected invariant")

    print("PASS: V25 failed-publication cleanup deletes only the exact owned MSI generation through a live handle")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
