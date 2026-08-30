#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path, PureWindowsPath

SCRIPT = Path(__file__).resolve().with_name("acquire-v25-compile-references.ps1")


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
        "No destructive filesystem mutation may occur before the path-overlap and",
        "reparse-component guards above.",
        "Remove-Item -LiteralPath $extract -Recurse -Force",
        "Invoke-WebRequest -Uri $candidate.Url -OutFile $staging",
        "Test-PinnedMsiGeneration -Path $staging",
        "[IO.File]::Move($staging, $msi)",
        "Test-PinnedMsiGeneration -Path $msi -Label 'Published BricsCAD V25 MSI'",
        "$msiState = Open-PinnedMsiReadLock -Path $msi",
        "Get-AuthenticodeSignature -FilePath $msiState.Path",
    )
    for token in required_tokens:
        require(token in source, f"missing V25 acquisition path-safety contract token: {token}")

    require("Get-FileHash" not in source, "pathname MSI hashing must remain retired")
    require(
        "Invoke-WebRequest -Uri $candidate.Url -OutFile $msi" not in source,
        "remote MSI bytes must not be downloaded directly to the canonical cache pathname",
    )

    cleanup_index = source.index("Remove-Item -LiteralPath $extract -Recurse -Force")
    root_guard_index = source.index("ExtractDir must not be a filesystem root")
    msi_guard_index = source.index("ExtractDir must not equal or contain MsiPath")
    cache_guard_index = source.index("ExtractDir must not equal or contain the MSI cache directory")
    reparse_guard_index = source.index("Assert-NoExistingReparseComponent -Path $extract -Label 'ExtractDir'")
    require(root_guard_index < cleanup_index, "filesystem-root guard occurs after recursive cleanup")
    require(msi_guard_index < cleanup_index, "MSI containment guard occurs after recursive cleanup")
    require(cache_guard_index < cleanup_index, "cache containment guard occurs after recursive cleanup")
    require(reparse_guard_index < cleanup_index, "reparse-component guard occurs after recursive cleanup")

    download_index = source.index("Invoke-WebRequest -Uri $candidate.Url -OutFile $staging")
    staged_index = source.index("Test-PinnedMsiGeneration -Path $staging", download_index)
    publish_index = source.index("[IO.File]::Move($staging, $msi)", staged_index)
    published_index = source.index("Test-PinnedMsiGeneration -Path $msi -Label 'Published BricsCAD V25 MSI'", publish_index)
    final_lock_index = source.index("$msiState = Open-PinnedMsiReadLock -Path $msi", published_index)
    signature_index = source.index("Get-AuthenticodeSignature -FilePath $msiState.Path", final_lock_index)
    require(
        download_index < staged_index < publish_index < published_index < final_lock_index < signature_index,
        "V25 acquisition must stage -> held-admit -> publish -> canonical re-admit -> final lock -> Authenticode",
    )

    safe_cases = (
        (
            r"D:\a\QS3D-BricsCAD\QS3D-BricsCAD\.cache\bricscad\BricsCAD-V25.2.10-x64.msi",
            r"D:\a\_temp\BricsCAD-V25-managed-references",
        ),
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
        require(extraction_is_unsafe(msi, extract), f"unsafe recursive-cleanup overlap was not rejected by contract model: {msi} / {extract}")

    require(
        source.count("Remove-Item -LiteralPath $extract -Recurse -Force") == 1,
        "unexpected additional recursive extraction cleanup bypasses the guarded path",
    )
    require(
        "^25\\.2\\.10(?:\\.|$)" in source,
        "pinned V25.2.10 MSI identity validation was removed",
    )
    require(
        "WaitForExit(900000)" in source,
        "MSI extraction timeout contract changed unexpectedly",
    )

    print("PASS: V25 compile-reference acquisition path-safety regression")
    print(" - recursive extraction cleanup is preceded by root/MSI/cache containment and reparse guards")
    print(" - normal shared-CI disjoint paths and safe cache-child extraction remain accepted")
    print(" - filesystem-root, MSI-containing and cache-containing extraction layouts fail closed")
    print(" - remote bytes are staged and held-verified before canonical publication/re-admission")
    print(" - final Authenticode consumption remains bound to the held MSI state")
    print(" - version and bounded extraction semantics remain source-guarded")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
