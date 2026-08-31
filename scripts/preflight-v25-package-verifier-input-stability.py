#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "verify-v25-package.ps1"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL v25 package verifier input stability: missing {label}: {token}")


def require_count(text: str, token: str, expected: int, label: str) -> None:
    actual = text.count(token)
    if actual != expected:
        raise SystemExit(
            f"FAIL v25 package verifier input stability: {label} count changed: expected {expected}, got {actual}"
        )


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        raise SystemExit(f"FAIL v25 package verifier input stability: forbidden {label}: {token}")


def require_order(text: str, first: str, second: str, label: str) -> None:
    a = text.find(first)
    b = text.find(second)
    if a < 0 or b < 0 or a >= b:
        raise SystemExit(f"FAIL v25 package verifier input stability: ordering violated for {label}")


def require_last_order(text: str, first: str, second: str, label: str) -> None:
    a = text.rfind(first)
    b = text.rfind(second)
    if a < 0 or b < 0 or a >= b:
        raise SystemExit(f"FAIL v25 package verifier input stability: final ordering violated for {label}")


def validate(text: str) -> None:
    for token, label in (
        ("function Assert-NoReparseAncestors", "ancestor reparse admission"),
        ("function Resolve-OrdinaryNonReparseFile", "ordinary file admission"),
        ("function Get-StableFileState", "stable generation capture"),
        ("$secondHash = Get-FileStreamSha256", "second fingerprint"),
        ("function Assert-StableFileState", "post-consumption state assertion"),
        ("$currentHash = Get-FileStreamSha256", "fresh assertion fingerprint"),
        ("function Open-StableReadStream", "generation-bound stream open"),
        ("[IO.FileShare]::Read", "write/delete sharing denial"),
        ("$streamHash = Get-StreamSha256 -Stream $stream", "opened-handle fingerprint"),
        ("function Read-BoundedStrictUtf8State", "bounded checksum reader"),
        ("[Text.UTF8Encoding]::new($false, $true)", "strict UTF-8 decoder"),
        ("$zipState = Get-StableFileState -Path $ZipPath", "ZIP state binding"),
        ("$checksumState = Get-StableFileState -Path $ChecksumPath", "checksum state binding"),
        ("Read-BoundedStrictUtf8State -State $checksumState", "bound checksum read"),
        ("[string]$zipState.Sha256", "checksum compares admitted ZIP hash"),
        ("$zipStream = Open-StableReadStream -State $zipState", "bound archive stream"),
        ("[IO.Compression.ZipArchive]::new($zipStream", "archive consumes admitted stream"),
        ("$null = Assert-StableFileState -Expected $zipState", "ZIP post-consumption recheck"),
        ("$null = Assert-StableFileState -Expected $checksumState", "checksum post-consumption recheck"),
        ("Convert-ToSafeArchivePath", "archive path safety"),
        ("OrdinalIgnoreCase", "case-collision guard"),
        ("SHA256SUMS.txt coverage count does not match archive file count", "manifest exact coverage"),
        ("Get-StreamSha256 -Stream $entryStream", "entry stream hashing"),
    ):
        require(text, token, label)

    checksum_recheck = "$null = Assert-StableFileState -Expected $checksumState -Label 'V25 package checksum'"
    require_count(
        text,
        checksum_recheck,
        2,
        "checksum generation rechecks (after checksum consumption and after archive consumption)",
    )

    for token, label in (
        ("Get-Content -LiteralPath $resolvedChecksum", "unbounded path-reopening checksum read"),
        ("Get-FileHash -LiteralPath $resolvedZip", "path-reopening ZIP hash"),
        ("[IO.Compression.ZipFile]::OpenRead", "path-reopening archive open"),
        ("Resolve-Path -LiteralPath $ZipPath", "path-only ZIP admission"),
    ):
        forbid(text, token, label)

    require_order(text, "$zipState = Get-StableFileState", "$zipStream = Open-StableReadStream", "ZIP capture before archive open")
    require_order(text, "$checksumState = Get-StableFileState", "Read-BoundedStrictUtf8State -State $checksumState", "checksum capture before read")
    require_order(text, "$zipStream = Open-StableReadStream", "[IO.Compression.ZipArchive]::new($zipStream", "stable stream before archive")
    require_order(text, "$archive.Dispose()", "$null = Assert-StableFileState -Expected $zipState", "archive disposal before final ZIP recheck")
    require_last_order(text, "$archive.Dispose()", checksum_recheck, "archive disposal before final checksum recheck")


def mutation_lock(text: str) -> None:
    mutations = {
        "second fingerprint": text.replace("$secondHash = Get-FileStreamSha256 -File $second -Label $Label", "$secondHash = $firstHash", 1),
        "assertion fingerprint": text.replace("$currentHash = Get-FileStreamSha256 -File $current -Label $Label", "$currentHash = [string]$Expected.Sha256", 1),
        "opened stream fingerprint": text.replace("$streamHash = Get-StreamSha256 -Stream $stream", "$streamHash = [string]$State.Sha256", 1),
        "strict checksum read": text.replace("Read-BoundedStrictUtf8State -State $checksumState", "Get-Content -LiteralPath $checksumState.Path -Raw", 1),
        "bound archive stream": text.replace("$zipStream = Open-StableReadStream -State $zipState -Label 'V25 package ZIP'", "$zipStream = [IO.File]::OpenRead($zipState.Path)", 1),
        "ZIP post-check": text.replace("$null = Assert-StableFileState -Expected $zipState -Label 'V25 package ZIP'", "$null = $zipState", 1),
        "checksum post-check": text.replace("$null = Assert-StableFileState -Expected $checksumState -Label 'V25 package checksum'", "$null = $checksumState", 1),
    }
    for label, mutated in mutations.items():
        try:
            validate(mutated)
        except SystemExit:
            continue
        raise SystemExit(f"FAIL v25 package verifier input stability: mutation escaped guard: {label}")


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    validate(text)
    mutation_lock(text)
    print("PASS v25 package verifier input stability")


if __name__ == "__main__":
    main()
