#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "acquire-v25-compile-references.ps1"

STAGING_OPEN = "$stagingAdmission = Open-PinnedMsiReadLock -Path $staging -ExpectedSha256 $expected"
OWNED_OPEN = "$publishedStream = Open-OwnedMsiPublication -Path $msi"
DELETE_ARM = "Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $true"
DELETE_CLEAR = "Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $false"
DESTINATION_READMIT = "$publishedAdmission = Open-PinnedMsiReadLock -Path $msi -ExpectedSha256 $expected"
REQUIRED = [
    STAGING_OPEN,
    "CreateFileW",
    "public const uint DELETE = 0x00010000;",
    "public const uint CREATE_NEW = 1;",
    OWNED_OPEN,
    DELETE_ARM,
    "$stagingAdmission.Stream.CopyTo($publishedStream)",
    "$publishedStream.Flush($true)",
    "$publishedStream.Position = 0",
    "$publishedHashBytes = $publishedSha.ComputeHash($publishedStream)",
    DELETE_CLEAR,
    DESTINATION_READMIT,
    "Canonical MSI destination appeared before held-generation publication; refusing destructive replacement.",
]
FORBIDDEN = [
    "if (-not (Test-PinnedMsiGeneration -Path $staging",
    "Remove-Item -LiteralPath $msi -Force",
    "[IO.File]::Move($staging, $msi)",
    "[IO.FileMode]::OpenOrCreate",
    "[IO.FileOptions]::DeleteOnClose",
]


def validate(source: str) -> None:
    for token in REQUIRED:
        if token not in source:
            raise ValueError(f"missing V25 MSI held-publication contract token: {token}")
    for token in FORBIDDEN:
        if token in source:
            raise ValueError(f"unsafe pathname/publication contract remains: {token}")

    staging_open = source.index(STAGING_OPEN)
    owned_open = source.index(OWNED_OPEN, staging_open)
    arm_pos = source.index(DELETE_ARM, owned_open)
    copy_pos = source.index("$stagingAdmission.Stream.CopyTo($publishedStream)", arm_pos)
    flush_pos = source.index("$publishedStream.Flush($true)", copy_pos)
    rewind_pos = source.index("$publishedStream.Position = 0", flush_pos)
    hash_pos = source.index("$publishedHashBytes = $publishedSha.ComputeHash($publishedStream)", rewind_pos)
    clear_pos = source.index(DELETE_CLEAR, hash_pos)
    readmit_pos = source.index(DESTINATION_READMIT, clear_pos)
    if not (
        staging_open < owned_open < arm_pos < copy_pos < flush_pos < rewind_pos
        < hash_pos < clear_pos < readmit_pos
    ):
        raise ValueError(
            "held staging admission, fresh handle-owned publication, explicit delete arm, same-handle verification, explicit commit and re-admission ordering changed"
        )

    dispose_pos = source.find("$stagingAdmission.Stream.Dispose()", staging_open)
    if dispose_pos != -1 and dispose_pos < readmit_pos:
        raise ValueError("staging admission is released before canonical destination re-admission")


def expect_rejected(label: str, mutated: str) -> None:
    try:
        validate(mutated)
    except (ValueError, IndexError):
        return
    raise SystemExit(f"mutation unexpectedly accepted: {label}")


text = SOURCE.read_text(encoding="utf-8")
validate(text)

mutations = {
    "premature staging release": text.replace(
        "$stagingAdmission.Stream.Position = 0",
        "$stagingAdmission.Stream.Dispose()\n            $stagingAdmission.Stream.Position = 0",
        1,
    ),
    "reusable destination open": text.replace("public const uint CREATE_NEW = 1;", "public const uint OPEN_ALWAYS = 4;", 1),
    "pathname staging publication": text.replace(
        "$stagingAdmission.Stream.CopyTo($publishedStream)",
        "[IO.File]::Move($staging, $msi)",
        1,
    ),
    "missing explicit delete arm": text.replace(DELETE_ARM, "# delete disposition arm removed", 1),
    "non-durable publication": text.replace("$publishedStream.Flush($true)", "$publishedStream.Flush()", 1),
    "missing same-handle verification": text.replace(
        "$publishedHashBytes = $publishedSha.ComputeHash($publishedStream)",
        "$publishedHashBytes = @()",
        1,
    ),
    "missing explicit commit": text.replace(DELETE_CLEAR, "# delete disposition commit removed", 1),
    "missing post-publication re-admission": text.replace(DESTINATION_READMIT, "$publishedAdmission = $null", 1),
}
for label, mutated in mutations.items():
    if mutated == text:
        raise SystemExit(f"mutation fixture did not change production source: {label}")
    expect_rejected(label, mutated)

print("PASS V25 compile-reference MSI held-generation publication")