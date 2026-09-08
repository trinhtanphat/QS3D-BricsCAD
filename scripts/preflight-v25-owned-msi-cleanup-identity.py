#!/usr/bin/env python3
"""Fail closed if V25 failed-publication cleanup can delete a replacement pathname."""

from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "acquire-v25-compile-references.ps1"


def validate(source: str) -> list[str]:
    failures: list[str] = []

    start_marker = "$publishedByThisAttempt = $false"
    end_marker = 'Write-Warning "BricsCAD V25 installer source failed:'
    if start_marker not in source:
        return ["V25 publisher ownership state is missing"]
    start = source.index(start_marker)
    end = source.find(end_marker, start)
    if end <= start:
        return ["could not isolate failed owned-publication cleanup window"]
    window = source[start:end]

    delete_on_close = "[IO.FileOptions]::DeleteOnClose"
    native_clear = "Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $false"
    same_handle_rehash = "$publishedStream.Position = 0"
    same_handle_hash = "$publishedHashBytes = $publishedSha.ComputeHash($publishedStream)"
    pathname_delete = "[IO.File]::Delete($msi)"
    reopened_cleanup = "Get-OrdinaryFileOrNull -Path $msi -Label 'Failed owned canonical MSI publication'"

    required_tokens = (
        ("SetFileInformationByHandle", "native handle disposition primitive is missing"),
        ("FileDispositionInfo", "file disposition information class is missing"),
        (delete_on_close, "owned canonical MSI is not created delete-on-close"),
        (same_handle_rehash, "owned publication handle is not rewound for same-handle verification"),
        (same_handle_hash, "owned publication bytes are not rehashed through the creator handle"),
        (native_clear, "successful publication does not explicitly clear delete disposition"),
    )
    for token, message in required_tokens:
        if token not in source:
            failures.append(message)

    if delete_on_close not in window:
        failures.append("delete-on-close ownership is not established in the publication window")
    if same_handle_rehash not in window:
        failures.append("same-handle verification is not in the owned publication window")
    if same_handle_hash not in window:
        failures.append("same-handle hash verification is not in the owned publication window")
    if native_clear not in window:
        failures.append("publication commit is not in the owned publication window")
    if pathname_delete in window:
        failures.append("failed owned-publication cleanup must not delete the canonical pathname")
    if reopened_cleanup in window:
        failures.append("failed owned-publication cleanup must not reopen the canonical pathname")

    if any(token not in window for token in (delete_on_close, same_handle_rehash, same_handle_hash, native_clear)):
        return failures

    create_pos = window.index(delete_on_close)
    rehash_pos = window.index(same_handle_rehash, create_pos)
    hash_pos = window.index(same_handle_hash, rehash_pos)
    clear_pos = window.index(native_clear, hash_pos)
    dispose_pos = window.find("$publishedStream.Dispose()", clear_pos)
    if dispose_pos < 0:
        failures.append("owned publication stream disposal after commit is missing")
    elif not create_pos < rehash_pos < hash_pos < clear_pos < dispose_pos:
        failures.append("delete-on-close must remain armed through same-handle verification and clear only at commit")

    ownership_clear = "$publishedByThisAttempt = $false"
    ownership_clear_pos = window.find(ownership_clear, clear_pos)
    if ownership_clear_pos <= clear_pos:
        failures.append("publication ownership must not clear before delete disposition is successfully committed")

    catch_pos = window.find("catch {")
    if catch_pos < 0:
        failures.append("publication catch block is missing")
    elif pathname_delete in window[catch_pos:]:
        failures.append("catch cleanup must remain handle-only")

    return failures


def main() -> int:
    source = TARGET.read_text(encoding="utf-8")
    failures = validate(source)
    if failures:
        for failure in failures:
            print(f"FAIL: {failure}")
        return 1

    mutation_tokens = (
        ("[IO.FileOptions]::DeleteOnClose", "delete-on-close ownership"),
        ("$publishedHashBytes = $publishedSha.ComputeHash($publishedStream)", "same-handle verification"),
        ("Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $false", "explicit publication commit"),
        ("$publishedByThisAttempt = $false\n            $publishedStream.Dispose()", "ownership clear ordering"),
    )
    for token, label in mutation_tokens:
        if token not in source:
            print(f"FAIL: mutation fixture missing: {label}")
            return 1
        mutated = source.replace(token, "", 1)
        if not validate(mutated):
            print(f"FAIL: guard mutation escaped detection: {label}")
            return 1

    for unsafe_token, label in (
        ("[IO.File]::Delete($msi)", "pathname delete"),
        ("Get-OrdinaryFileOrNull -Path $msi -Label 'Failed owned canonical MSI publication'", "pathname reopen"),
    ):
        insertion = source.find('Write-Warning "BricsCAD V25 installer source failed:')
        if insertion < 0:
            print("FAIL: mutation insertion point is missing")
            return 1
        mutated = source[:insertion] + f"            {unsafe_token}\n" + source[insertion:]
        if not validate(mutated):
            print(f"FAIL: guard mutation escaped detection: {label}")
            return 1

    print("PASS: V25 canonical MSI publication remains handle-owned/delete-on-close until same-handle verification commits it")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
