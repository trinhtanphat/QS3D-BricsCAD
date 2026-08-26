#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[1]
SCRIPT = REPO_ROOT / "scripts" / "import-v25-signing-certificate.ps1"
MAX_SOURCE_BYTES = 128 * 1024


def fail(message: str) -> None:
    raise SystemExit(f"::error::{message}")


def load_source() -> str:
    try:
        stat = SCRIPT.stat()
    except OSError as exc:
        fail(f"cannot stat signing PFX importer: {exc}")
    if not SCRIPT.is_file() or SCRIPT.is_symlink():
        fail("signing PFX importer must be an ordinary non-symlink file")
    if stat.st_size > MAX_SOURCE_BYTES:
        fail(f"signing PFX importer unexpectedly exceeds {MAX_SOURCE_BYTES} bytes")
    try:
        return SCRIPT.read_bytes().decode("utf-8", errors="strict")
    except (OSError, UnicodeDecodeError) as exc:
        fail(f"cannot read signing PFX importer as strict UTF-8: {exc}")


def require(source: str, needle: str, label: str) -> int:
    index = source.find(needle)
    if index < 0:
        fail(f"missing {label}: {needle}")
    return index


def require_before(source: str, first: str, second: str, label: str) -> None:
    first_index = require(source, first, label + " first token")
    second_index = require(source, second, label + " second token")
    if first_index >= second_index:
        fail(f"{label}: expected {first!r} before {second!r}")


def main() -> None:
    source = load_source()

    require(source, "$maxPfxDecodedBytes = 1048576", "decoded PFX limit")
    require(source, "$maxPfxBase64Chars = 1398104", "encoded PFX limit")
    require(source, "if ($encodedPfx.Length -gt $maxPfxBase64Chars)", "pre-decode encoded-size guard")
    require_before(
        source,
        "if ($encodedPfx.Length -gt $maxPfxBase64Chars)",
        "[Convert]::FromBase64String($encodedPfx)",
        "encoded-size rejection must precede base64 allocation",
    )
    require(
        source,
        "if ($bytes.Length -lt 256 -or $bytes.Length -gt $maxPfxDecodedBytes)",
        "decoded-size guard",
    )

    require(source, "function Assert-SafeTempDirectory", "temp-directory validator")
    require(source, "-PathType Container", "temp-directory container check")
    require(source, "[IO.FileAttributes]::ReparsePoint", "reparse-point rejection")
    require(source, "Temporary directory must not be a filesystem root", "filesystem-root rejection")
    require_before(
        source,
        "$tempRoot = Assert-SafeTempDirectory -Path $tempRootCandidate",
        "$pfxPath = Join-Path $tempRoot",
        "temp root must be validated before choosing the secret path",
    )

    require(source, "function Assert-SafeTempFile", "temp-file validator")
    require(source, "Temporary PFX escaped the validated temporary directory", "temp-file containment check")
    require(source, "-PathType Leaf", "temp-file leaf check")
    require(source, "-not ($item -is [IO.FileInfo])", "regular-file check")
    require_before(
        source,
        "[IO.File]::WriteAllBytes($pfxPath, $bytes)",
        "$pfxPath = Assert-SafeTempFile -Path $pfxPath -TempRoot $tempRoot",
        "written PFX must be revalidated before import",
    )
    require_before(
        source,
        "$pfxPath = Assert-SafeTempFile -Path $pfxPath -TempRoot $tempRoot",
        "Import-PfxCertificate",
        "temp PFX validation must precede certificate import",
    )

    require(source, "Remove-ImportedCertificates -Thumbprints $importedNewThumbprints", "ephemeral certificate rollback")
    require(source, "[Array]::Clear($bytes, 0, $bytes.Length)", "decoded secret zeroing")
    require(source, "Remove-Item -LiteralPath $pfxPath -Force -ErrorAction SilentlyContinue", "temporary PFX cleanup")

    forbidden = (
        "Get-ChildItem -Path Cert:\\CurrentUser\\My | Remove-Item",
        "Remove-Item -Path Cert:\\CurrentUser\\My",
        "taskkill",
    )
    for token in forbidden:
        if token in source:
            fail(f"signing PFX importer contains forbidden broad cleanup primitive: {token}")

    print("V25 signing PFX input/temp-path safety preflight: PASS")


if __name__ == "__main__":
    main()
