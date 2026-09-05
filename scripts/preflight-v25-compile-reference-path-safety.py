#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path, PureWindowsPath

SCRIPT = Path(__file__).resolve().with_name("acquire-v25-compile-references.ps1")
STAGING_OPEN = "$stagingAdmission = Open-PinnedMsiReadLock -Path $staging -ExpectedSha256 $expected"
DESTINATION_READMIT = "$publishedAdmission = Open-PinnedMsiReadLock -Path $msi -ExpectedSha256 $expected"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def norm(path: str) -> PureWindowsPath:
    return PureWindowsPath(path)


def equal(left: PureWindowsPath, right: PureWindowsPath) -> bool:
    return str(left).casefold() == str(right).casefold()


def within(candidate: PureWindowsPath, parent: PureWindowsPath) -> bool:
    if equal(candidate, parent):
        return True
    candidate_parts = [part.casefold() for part in candidate.parts]
    parent_parts = [part.casefold() for part in parent.parts]
    return len(candidate_parts) > len(parent_parts) and candidate_parts[: len(parent_parts)] == parent_parts


def extraction_is_unsafe(msi: str, extract: str) -> bool:
    msi_path = norm(msi)
    extract_path = norm(extract)
    cache_dir = msi_path.parent
    extract_root = PureWindowsPath(extract_path.anchor)
    return (
        equal(extract_path, extract_root)
        or within(msi_path, extract_path)
        or within(cache_dir, extract_path)
    )


def main() -> int:
    source = SCRIPT.read_text(encoding="utf-8")

    required_tokens = (
        "function Get-CanonicalAbsolutePath",
        "function Test-CanonicalPathEqual",
        "function Test-CanonicalPathWithin",
        "function Assert-NoExistingReparseComponent",
        "function Open-PinnedMsiReadLock",
        "function Test-PinnedMsiGeneration",
        "[IO.FileShare]::Read",
        "$sha.ComputeHash($stream)",
        "ExtractDir must not be a filesystem root",
        "ExtractDir must not equal or contain MsiPath",
        "ExtractDir must not equal or contain the MSI cache directory",
        "Assert-NoExistingReparseComponent -Path $extract -Label 'ExtractDir'",
        "The extraction root is single-use.",
        "if (Test-Path -LiteralPath $extract)",
        "ExtractDir unexpectedly already exists; refusing pathname reuse",
        "New-Item -ItemType Directory -Path $extract | Out-Null",
        "Invoke-WebRequest -Uri $candidate.Url -OutFile $staging",
        STAGING_OPEN,
        "[IO.FileMode]::CreateNew",
        "$stagingAdmission.Stream.CopyTo($publishedStream)",
        "$publishedStream.Flush($true)",
        DESTINATION_READMIT,
        "Canonical MSI destination appeared before held-generation publication; refusing destructive replacement.",
        "$msiState = Open-PinnedMsiReadLock -Path $msi",
        "Get-AuthenticodeSignature -FilePath $msiState.Path",
    )
    for token in required_tokens:
        require(token in source, f"missing V25 acquisition path-safety contract token: {token}")

    for forbidden, label in (
        ("Get-FileHash", "pathname MSI hashing must remain retired"),
        ("Invoke-WebRequest -Uri $candidate.Url -OutFile $msi", "remote MSI bytes must not be downloaded directly to the canonical cache pathname"),
        ("Remove-Item -LiteralPath $extract -Recurse", "fresh-only ExtractDir must never be recursively deleted/reused by pathname"),
        ("New-Item -ItemType Directory -Path $extract -Force", "fresh ExtractDir creation must not use -Force because a raced-in generation must fail"),
        ("Test-PinnedMsiGeneration -Path $staging", "staging admission must remain held rather than being temporary"),
        ("[IO.File]::Move($staging, $msi)", "canonical publication must not re-resolve the staging pathname"),
        ("Remove-Item -LiteralPath $msi -Force", "canonical publication must not destructively remove an unbound destination"),
        ("[IO.FileMode]::OpenOrCreate", "canonical publication must be fresh-only"),
    ):
        require(forbidden not in source, label)

    root_guard_index = source.index("ExtractDir must not be a filesystem root")
    msi_guard_index = source.index("ExtractDir must not equal or contain MsiPath")
    cache_guard_index = source.index("ExtractDir must not equal or contain the MSI cache directory")
    reparse_guard_index = source.index("Assert-NoExistingReparseComponent -Path $extract -Label 'ExtractDir'")
    absent_guard_index = source.index("if (Test-Path -LiteralPath $extract)")
    create_index = source.index("New-Item -ItemType Directory -Path $extract | Out-Null")
    require(root_guard_index < absent_guard_index, "filesystem-root guard occurs after fresh-root admission")
    require(msi_guard_index < absent_guard_index, "MSI containment guard occurs after fresh-root admission")
    require(cache_guard_index < absent_guard_index, "cache containment guard occurs after fresh-root admission")
    require(reparse_guard_index < absent_guard_index, "reparse-component guard occurs after fresh-root admission")
    require(absent_guard_index < create_index, "existing ExtractDir refusal must precede fresh creation")

    download_index = source.index("Invoke-WebRequest -Uri $candidate.Url -OutFile $staging")
    staged_index = source.index(STAGING_OPEN, download_index)
    fresh_publish_index = source.index("[IO.FileMode]::CreateNew", staged_index)
    copy_index = source.index("$stagingAdmission.Stream.CopyTo($publishedStream)", fresh_publish_index)
    flush_index = source.index("$publishedStream.Flush($true)", copy_index)
    published_index = source.index(DESTINATION_READMIT, flush_index)
    final_lock_index = source.index("$msiState = Open-PinnedMsiReadLock -Path $msi", published_index)
    signature_index = source.index("Get-AuthenticodeSignature -FilePath $msiState.Path", final_lock_index)
    require(
        download_index < staged_index < fresh_publish_index < copy_index < flush_index < published_index < final_lock_index < signature_index,
        "V25 acquisition must stage -> held-admit -> fresh-copy -> durable-flush -> canonical re-admit -> final lock -> Authenticode",
    )
    staging_dispose_index = source.find("$stagingAdmission.Stream.Dispose()", staged_index)
    require(staging_dispose_index < 0 or staging_dispose_index > published_index,
            "staged admission must remain held until canonical destination is re-admitted")

    safe_cases = (
        (r"D:\a\QS3D-BricsCAD\QS3D-BricsCAD\.cache\bricscad\BricsCAD-V25.2.10-x64.msi", r"D:\a\_temp\BricsCAD-V25-managed-references"),
        (r"C:\cache\bricscad\v25.msi", r"C:\work\extract\v25"),
        (r"C:\cache\v25.msi", r"C:\cache\extract"),
    )
    for msi, extract in safe_cases:
        require(not extraction_is_unsafe(msi, extract), f"safe sibling/disjoint layout rejected by contract model: {msi} / {extract}")

    unsafe_cases = (
        (r"C:\cache\v25.msi", "C:/"),
        (r"C:\cache\v25.msi", r"C:\cache\v25.msi"),
        (r"C:\cache\v25.msi", r"C:\cache"),
        (r"C:\cache\nested\v25.msi", r"C:\cache"),
        (r"C:\CACHE\v25.msi", r"c:\cache"),
    )
    for msi, extract in unsafe_cases:
        require(extraction_is_unsafe(msi, extract), f"unsafe extraction overlap was not rejected by contract model: {msi} / {extract}")

    require(source.count("New-Item -ItemType Directory -Path $extract | Out-Null") == 1,
            "fresh extraction root must be created through one non-Force path")
    require("^25\\.2\\.10(?:\\.|$)" in source, "pinned V25.2.10 MSI identity validation was removed")
    require("WaitForExit(900000)" in source, "MSI extraction timeout contract changed unexpectedly")

    print("PASS: V25 compile-reference acquisition path-safety regression")
    print(" - root/MSI/cache containment and reparse guards precede fresh-root admission")
    print(" - pre-existing or raced ExtractDir generations fail closed; recursive pathname cleanup is forbidden")
    print(" - normal shared-CI disjoint paths and safe cache-child extraction remain accepted")
    print(" - filesystem-root, MSI-containing and cache-containing extraction layouts fail closed")
    print(" - remote bytes stay held while copied into a fresh-only canonical destination and durably flushed")
    print(" - canonical bytes are re-admitted before the staging generation is released")
    print(" - final Authenticode consumption remains bound to the held MSI state")
    print(" - version and bounded extraction semantics remain source-guarded")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
