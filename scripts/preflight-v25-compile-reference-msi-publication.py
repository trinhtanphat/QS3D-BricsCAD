#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "acquire-v25-compile-references.ps1"

STAGING_OPEN = "$stagingAdmission = Open-PinnedMsiReadLock -Path $staging -ExpectedSha256 $expected"
DESTINATION_READMIT = "$publishedAdmission = Open-PinnedMsiReadLock -Path $msi -ExpectedSha256 $expected"
REQUIRED = [
    STAGING_OPEN,
    "[IO.FileMode]::CreateNew",
    "$stagingAdmission.Stream.CopyTo($publishedStream)",
    "$publishedStream.Flush($true)",
    DESTINATION_READMIT,
    "Canonical MSI destination appeared before held-generation publication; refusing destructive replacement.",
]
FORBIDDEN = [
    "if (-not (Test-PinnedMsiGeneration -Path $staging",
    "Remove-Item -LiteralPath $msi -Force",
    "[IO.File]::Move($staging, $msi)",
    "[IO.FileMode]::OpenOrCreate",
]


def validate(source: str) -> None:
    for token in REQUIRED:
        if token not in source:
            raise ValueError(f"missing V25 MSI held-publication contract token: {token}")
    for token in FORBIDDEN:
        if token in source:
            raise ValueError(f"unsafe pathname publication contract remains: {token}")

    staging_open = source.index(STAGING_OPEN)
    create_new = source.index("[IO.FileMode]::CreateNew", staging_open)
    copy_pos = source.index("$stagingAdmission.Stream.CopyTo($publishedStream)", create_new)
    flush_pos = source.index("$publishedStream.Flush($true)", copy_pos)
    readmit_pos = source.index(DESTINATION_READMIT, flush_pos)
    if not (staging_open < create_new < copy_pos < flush_pos < readmit_pos):
        raise ValueError("held staging admission, fresh publication, durable flush and re-admission ordering changed")

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
    "non-fresh destination open": text.replace("[IO.FileMode]::CreateNew", "[IO.FileMode]::OpenOrCreate", 1),
    "pathname staging publication": text.replace(
        "$stagingAdmission.Stream.CopyTo($publishedStream)",
        "[IO.File]::Move($staging, $msi)",
        1,
    ),
    "non-durable publication": text.replace("$publishedStream.Flush($true)", "$publishedStream.Flush()", 1),
    "missing post-publication re-admission": text.replace(DESTINATION_READMIT, "$publishedAdmission = $null", 1),
}
for label, mutated in mutations.items():
    if mutated == text:
        raise SystemExit(f"mutation fixture did not change production source: {label}")
    expect_rejected(label, mutated)

print("PASS V25 compile-reference MSI held-generation publication")
