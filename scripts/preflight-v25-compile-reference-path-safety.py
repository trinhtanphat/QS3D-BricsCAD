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
        "ExtractDir must not be a filesystem root",
        "ExtractDir must not equal or contain MsiPath",
        "ExtractDir must not equal or contain the MSI cache directory",
        "No destructive filesystem mutation may occur before the path-overlap guards above.",
        "Remove-Item -LiteralPath $extract -Recurse -Force",
    )
    for token in required_tokens:
        require(token in source, f"missing V25 acquisition path-safety contract token: {token}")

    cleanup_index = source.index("Remove-Item -LiteralPath $extract -Recurse -Force")
    root_guard_index = source.index("ExtractDir must not be a filesystem root")
    msi_guard_index = source.index("ExtractDir must not equal or contain MsiPath")
    cache_guard_index = source.index("ExtractDir must not equal or contain the MSI cache directory")
    require(root_guard_index < cleanup_index, "filesystem-root guard occurs after recursive cleanup")
    require(msi_guard_index < cleanup_index, "MSI containment guard occurs after recursive cleanup")
    require(cache_guard_index < cleanup_index, "cache containment guard occurs after recursive cleanup")

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
        "Invoke-WebRequest -Uri $candidate.Url -OutFile $msi" in source,
        "download destination semantics changed unexpectedly",
    )
    require(
        "Get-AuthenticodeSignature -FilePath $msi" in source,
        "Authenticode validation was removed from V25 acquisition",
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
    print(" - recursive extraction cleanup is preceded by root/MSI/cache containment guards")
    print(" - normal shared-CI disjoint paths and safe cache-child extraction remain accepted")
    print(" - filesystem-root, MSI-containing and cache-containing extraction layouts fail closed")
    print(" - hash/signature/version/download/timeout acquisition semantics remain source-guarded")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
