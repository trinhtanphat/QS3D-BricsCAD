#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "acquire-v25-compile-references.ps1"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


def require_before(text: str, first: str, second: str, label: str) -> None:
    left = text.find(first)
    right = text.find(second)
    if left < 0 or right < 0 or left >= right:
        raise SystemExit(f"FAIL: {label}: expected {first!r} before {second!r}")


def validate(text: str) -> None:
    require(text, "function Assert-NoExistingReparseComponent", "reparse helper")
    require(text, "[IO.FileAttributes]::ReparsePoint", "reparse attribute check")
    require(text, "function Get-OrdinaryFileOrNull", "ordinary-file helper")
    require(text, "Assert-NoExistingReparseComponent -Path $cacheDir", "cache path guard")
    require(text, "Assert-NoExistingReparseComponent -Path $msi", "MSI path guard")
    require(text, "Assert-NoExistingReparseComponent -Path $extract", "extract path guard")
    require(text, "$item = Get-OrdinaryFileOrNull -Path $msi", "MSI ordinary-file trust")
    require(text, "Get-FileHash -LiteralPath $msi -Algorithm SHA256", "pinned hash")
    require(text, "Get-AuthenticodeSignature -FilePath $msi", "Authenticode validation")
    require(text, "ProductVersion", "MSI version validation")
    require(text, "ProductName", "MSI name validation")
    require(text, "$process.WaitForExit(900000)", "bounded extraction")
    require(text, "Stop-OwnedProcessTree -Process $process", "PID-scoped cleanup")

    destructive = "Remove-Item -LiteralPath $extract -Recurse -Force"
    require_before(text, "Assert-NoExistingReparseComponent -Path $cacheDir", destructive,
                   "cache reparse guard before recursive cleanup")
    require_before(text, "Assert-NoExistingReparseComponent -Path $msi", destructive,
                   "MSI reparse guard before recursive cleanup")
    require_before(text, "Assert-NoExistingReparseComponent -Path $extract", destructive,
                   "extract reparse guard before recursive cleanup")
    require_before(text, "$item = Get-OrdinaryFileOrNull -Path $msi", "Get-FileHash -LiteralPath $msi",
                   "ordinary MSI check before hash trust")


def expect_rejected(original: str, mutated: str, label: str) -> None:
    try:
        validate(mutated)
    except SystemExit:
        return
    raise SystemExit(f"FAIL: mutation probe was accepted: {label}")


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    validate(text)

    expect_rejected(
        text,
        text.replace("Assert-NoExistingReparseComponent -Path $extract -Label 'ExtractDir'", "# removed", 1),
        "removed extract reparse guard",
    )
    expect_rejected(
        text,
        text.replace("$item = Get-OrdinaryFileOrNull -Path $msi -Label 'BricsCAD V25 MSI'", "$item = Get-Item -LiteralPath $msi", 1),
        "removed ordinary MSI trust boundary",
    )
    expect_rejected(
        text,
        text.replace("Assert-NoExistingReparseComponent -Path $cacheDir -Label 'MSI cache directory'", "# delayed cache guard", 1)
            .replace(destructive := "Remove-Item -LiteralPath $extract -Recurse -Force -ErrorAction SilentlyContinue",
                     destructive + "\nAssert-NoExistingReparseComponent -Path $cacheDir -Label 'MSI cache directory'", 1),
        "moved cache guard after recursive cleanup",
    )

    print("PASS V25 compile-reference acquisition path/cache safety contract")


if __name__ == "__main__":
    main()
