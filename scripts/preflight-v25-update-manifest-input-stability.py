#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "new-v25-update-manifest.ps1"


def read_target() -> str:
    if not TARGET.is_file():
        raise RuntimeError("missing scripts/new-v25-update-manifest.ps1")
    return TARGET.read_text(encoding="utf-8")


def validate(text: str) -> list[str]:
    errors: list[str] = []
    required = (
        "function Get-StreamingSha256",
        "[Security.Cryptography.SHA256]::Create()",
        "function Get-StableFileState",
        "$currentHash = Get-StreamingSha256 -File $current -Label $Label",
        "function Assert-StableFileState",
        "LastWriteUtcTicks",
        "Sha256 = $currentHash",
        "function Get-SafeStagedFiles",
        "[Collections.Generic.Stack[string]]::new()",
        "Signed staging package contains a reparse-backed entry",
        "$stagedFiles = @(Get-SafeStagedFiles -Root $PackageRoot)",
        "$metadataState = Get-StableFileState",
        "$zipState = Get-StableFileState",
        "$payloadStates",
        "Assert-StableFileState -Expected $metadataState",
        "Assert-StableFileState -Expected $zipState",
    )
    for token in required:
        if token not in text:
            errors.append(f"V25 update-manifest input stability missing required token: {token}")

    first_hash = text.find("Get-StreamingSha256 -File $file")
    second_resolve = text.find("$current = Resolve-OrdinaryNonReparseFile")
    second_hash = text.find("$currentHash = Get-StreamingSha256 -File $current -Label $Label")
    published_hash = text.find("Sha256 = $currentHash")
    if min(first_hash, second_resolve, second_hash, published_hash) < 0 or not first_hash < second_resolve < second_hash < published_hash:
        errors.append("stable file-state capture must hash, re-resolve, re-hash, then publish the revalidated state")

    traversal_start = text.find("function Get-SafeStagedFiles")
    traversal_end = text.find("function Assert-ZipPayloadMatchesSignedStaging", traversal_start)
    if traversal_start < 0 or traversal_end < 0:
        errors.append("safe staging traversal helper must precede ZIP/staging verification")
    else:
        traversal = text[traversal_start:traversal_end]
        if "-Recurse" in traversal:
            errors.append("safe staging traversal must not recurse before admitting each directory entry")
        for token in (
            "Get-ChildItem -LiteralPath $directory.FullName -Force -ErrorAction Stop",
            "[IO.FileAttributes]::ReparsePoint",
            "if ($item.PSIsContainer)",
            "$pending.Push($item.FullName)",
            "if (-not ($item -is [IO.FileInfo]))",
            "Resolve-OrdinaryNonReparseFile -Path $item.FullName",
        ):
            if token not in traversal:
                errors.append(f"safe staging traversal missing fail-closed token: {token}")

    metadata_capture = text.find("$metadataState = Get-StableFileState")
    metadata_read = text.find("$metadataText = Read-BoundedStrictUtf8File")
    metadata_recheck = text.find("Assert-StableFileState -Expected $metadataState", metadata_read)
    json_parse = text.find("ConvertFrom-Json -ErrorAction Stop", metadata_recheck)
    if min(metadata_capture, metadata_read, metadata_recheck, json_parse) < 0 or not metadata_capture < metadata_read < metadata_recheck < json_parse:
        errors.append("metadata order must be stable capture -> bounded read -> stability recheck -> JSON parse")

    zip_capture = text.find("$zipState = Get-StableFileState")
    zip_validation = text.find("Assert-ZipPayloadMatchesSignedStaging -ZipFile", zip_capture)
    zip_recheck = text.find("Assert-StableFileState -Expected $zipState", zip_validation)
    if min(zip_capture, zip_validation, zip_recheck) < 0 or not zip_capture < zip_validation < zip_recheck:
        errors.append("package ZIP must be state-bound before ZIP/staging verification and rechecked afterward")

    if "Get-FileHash -LiteralPath $stagedByName[$name]" in text:
        errors.append("staged ZIP parity must not reopen an admitted path through Get-FileHash without stable-state revalidation")
    if "Get-ChildItem -LiteralPath $PackageRoot.FullName -File -Recurse" in text:
        errors.append("staging enumeration must reject reparse directories before descent instead of direct recursive traversal")
    return errors


try:
    target = read_target()
except Exception as exc:
    print(f"ERROR: {exc}")
    sys.exit(1)

errors = validate(target)
print("QS3D V25 update-manifest input-generation stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

mutations = {
    "second fingerprint": target.replace("$currentHash = Get-StreamingSha256 -File $current -Label $Label", "$currentHash = $hash", 1),
    "metadata recheck": target.replace("Assert-StableFileState -Expected $metadataState", "# removed metadata state recheck", 1),
    "ZIP recheck": target.replace("Assert-StableFileState -Expected $zipState", "# removed ZIP state recheck", 1),
    "safe traversal": target.replace("$stagedFiles = @(Get-SafeStagedFiles -Root $PackageRoot)", "$stagedFiles = @(Get-ChildItem -LiteralPath $PackageRoot.FullName -File -Recurse -Force)", 1),
    "reparse rejection": target.replace("throw \"Signed staging package contains a reparse-backed entry: $($item.FullName)\"", "continue", 1),
}
for label, mutated in mutations.items():
    if mutated == target:
        print(f"ERROR: mutation fixture did not modify target for {label}")
        sys.exit(1)
    if not validate(mutated):
        print(f"ERROR: mutation escaped V25 update-manifest input-stability guard: {label}")
        sys.exit(1)

print("PASS: V25 update-manifest metadata, signed staging, and ZIP inputs remain bound to stable ordinary-file generations with fail-closed staging traversal across trust and identity consumption.")
