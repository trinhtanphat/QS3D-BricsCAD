#!/usr/bin/env python3
"""Fail closed if V25 signed ZIP verification is detached from publication identity."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FINALIZER = ROOT / "scripts" / "finalize-v25-signed-package.ps1"

NATIVE_RENAME = "SetFileInformationByHandle"
RENAME_INFO = "FileRenameInfo"
PINVOKE = "private static extern bool SetFileInformationByHandle("
RENAME_CLASS = "public const int FileRenameInfo = 3;"
NATIVE_CALL = "[QS3DV25HeldZipPublication]::SetFileInformationByHandleFileRenameInfo("
HELD_HELPER = "function Publish-HeldVerifiedZipGeneration {"
HELD_STREAM_BIND = "$null = $HeldZip.Stream"
HELD_HASH_BIND = "$expectedHash = [string]$HeldZip.Sha256"
VERIFY_HELD = "Assert-HeldVerifiedZipStable -HeldZip $HeldZip"
PUBLISH_CALL = "Publish-HeldVerifiedZipGeneration -HeldZip $heldZip"
COMMIT_DISPOSE = "$transactionCommitted = $true\n    $heldZip.Stream.Dispose()\n    $heldZip = $null"
PATH_MOVE = "[IO.File]::Move($tempZip, $zip)"
PATH_REPLACE = "[IO.File]::Replace($tempZip, $zip, $zipBackup, $true)"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    require(NATIVE_RENAME in text and RENAME_INFO in text,
            "V25 signed ZIP publication must use a handle-bound native rename primitive.")
    require(PINVOKE in text and RENAME_CLASS in text,
            "V25 signed ZIP publication must retain the exact SetFileInformationByHandle/FileRenameInfo interop contract.")
    helper = text.find(HELD_HELPER)
    require(helper >= 0,
            "V25 finalizer must publish through one held verified ZIP generation helper.")
    held_stream = text.find(HELD_STREAM_BIND, helper)
    held_hash = text.find(HELD_HASH_BIND, helper)
    require(held_stream > helper and held_hash > helper,
            "V25 publication helper must bind the exact held ZIP stream and admitted SHA-256 identity.")
    verify = text.find(VERIFY_HELD, helper)
    native_call = text.find(NATIVE_CALL, verify)
    require(verify > helper and native_call > verify,
            "V25 staged ZIP generation must be revalidated while held immediately before the exact handle-bound native publication call.")

    call = text.find(PUBLISH_CALL)
    require(call >= 0,
            "V25 final publish path must route through held-generation publication.")
    require(PATH_MOVE not in text and PATH_REPLACE not in text,
            "V25 verified staged ZIP must never be published by closing and reopening its pathname.")

    staged_hash = text.find("$stagedZipHash =")
    require(staged_hash >= 0 and staged_hash < call,
            "V25 finalizer must establish staged ZIP identity before publication.")
    commit_dispose = text.find(COMMIT_DISPOSE, call)
    require(commit_dispose > call,
            "V25 held staged ZIP must remain alive through transaction commit and then have an explicit lifetime boundary.")


text = FINALIZER.read_text(encoding="utf-8")
validate(text)

for marker in (
    PINVOKE,
    RENAME_CLASS,
    NATIVE_CALL,
    HELD_HELPER,
    HELD_STREAM_BIND,
    HELD_HASH_BIND,
    VERIFY_HELD,
    PUBLISH_CALL,
    COMMIT_DISPOSE,
):
    require(marker in text, f"Mutation probe could not find required marker: {marker}")
    mutated = text.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

print("PASS V25 signed ZIP generation-bound publication fence")