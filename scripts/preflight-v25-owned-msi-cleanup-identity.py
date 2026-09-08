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

    capture = "$publishedFileIdentity = Get-FileIdentityFromStream -Stream $publishedStream"
    verification = "Assert-PathMatchesFileIdentity -Path $msi -ExpectedIdentity $publishedFileIdentity"
    deletion = "[IO.File]::Delete($msi)"

    require(capture in window, "owned canonical MSI file identity is not captured before handle disposal")
    require(verification in window, "canonical MSI pathname is not rebound to the captured owned identity before deletion")
    require(deletion in window, "owned failed-publication cleanup no longer performs an explicit deletion")

    capture_pos = window.index(capture)
    verify_pos = window.index(verification)
    delete_pos = window.index(deletion)
    dispose_pos = window.find("$publishedStream.Dispose()")

    require(dispose_pos >= 0, "published stream disposal is missing from cleanup")
    require(capture_pos < dispose_pos, "owned identity must be captured while the exclusive publication handle is still open")
    require(dispose_pos < verify_pos < delete_pos, "pathname identity must be revalidated after handle close and immediately before delete")

    # Mutation locks: weakening either side of the identity handoff must fail this guard.
    for token, label in (
        (capture, "identity capture"),
        (verification, "identity revalidation"),
    ):
        mutated = window.replace(token, "", 1)
        require(token not in mutated, f"{label} mutation probe did not remove the protected invariant")

    print("PASS: V25 failed-publication cleanup is bound to the exact owned MSI file identity")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
