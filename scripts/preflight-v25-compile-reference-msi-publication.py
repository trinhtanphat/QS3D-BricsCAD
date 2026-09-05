#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "acquire-v25-compile-references.ps1"
text = SOURCE.read_text(encoding="utf-8")

required = [
    "Open-PinnedMsiReadLock -Path $staging -ExpectedSha256 $expected",
    "$stagingAdmission.Stream.CopyTo($publishedStream)",
    "$publishedStream.Flush($true)",
    "Open-PinnedMsiReadLock -Path $msi -ExpectedSha256 $expected",
]
for token in required:
    if token not in text:
        raise SystemExit(f"missing V25 MSI held-publication contract token: {token}")

forbidden = [
    "if (-not (Test-PinnedMsiGeneration -Path $staging",
    "Remove-Item -LiteralPath $msi -Force",
    "[IO.File]::Move($staging, $msi)",
]
for token in forbidden:
    if token in text:
        raise SystemExit(f"unsafe pathname publication contract remains: {token}")

copy_pos = text.index("$stagingAdmission.Stream.CopyTo($publishedStream)")
dispose_pos = text.find("$stagingAdmission.Stream.Dispose()")
if dispose_pos != -1 and dispose_pos < copy_pos:
    raise SystemExit("staging admission is released before canonical publication")

readmit_pos = text.index("Open-PinnedMsiReadLock -Path $msi -ExpectedSha256 $expected")
if readmit_pos < copy_pos:
    raise SystemExit("canonical destination re-admission must follow publication")

print("PASS V25 compile-reference MSI held-generation publication")
