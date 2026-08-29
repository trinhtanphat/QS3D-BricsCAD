#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "assert-v26-release-package-identity.ps1"
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26.yml"


def read(path: Path) -> str:
    if not path.is_file():
        raise RuntimeError(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def validate_helper(text: str) -> list[str]:
    errors: list[str] = []
    required = (
        "Set-StrictMode -Version Latest",
        "$script:MaxMetadataBytes = 65536",
        "Resolve-OrdinaryNonReparseFile",
        "$item.Attributes -band [IO.FileAttributes]::ReparsePoint",
        "$cursor.Attributes -band [IO.FileAttributes]::ReparsePoint",
        "Get-HeldStreamingSha256",
        "[Security.Cryptography.SHA256]::Create()",
        "Open-LockedStableFile",
        "[IO.File]::Open",
        "[IO.FileShare]::Read",
        "$hash = Get-HeldStreamingSha256 -Stream $stream -Label $Label",
        "LastWriteUtcTicks",
        "Sha256 = $hash",
        "Read-BoundedStrictUtf8Stream",
        "$stream.Length -gt $script:MaxMetadataBytes",
        "[byte[]]::new([int]$stream.Length)",
        "[Text.UTF8Encoding]::new($false, $true)",
        "[Text.DecoderFallbackException]",
        "ConvertFrom-Json -ErrorAction Stop",
        "BricsCAD V26 x64",
        "net8.0-windows",
        "[string]::Equals(('v' + [string]$metadata.productVersion), $ReleaseTag, [StringComparison]::Ordinal)",
        "Assert-LockedPathBinding -Held $pluginHeld",
        "[Reflection.AssemblyName]::GetAssemblyName($pluginHeld.Path)",
        "Assert-LockedPathBinding -Held $coreHeld",
        "[Reflection.AssemblyName]::GetAssemblyName($coreHeld.Path)",
        "$heldFiles[$index].Stream.Dispose()",
    )
    for token in required:
        if token not in text:
            errors.append(f"V26 release identity helper missing required safety token: {token}")

    metadata_lock = text.find("$metadataHeld = Open-LockedStableFile")
    plugin_lock = text.find("$pluginHeld = Open-LockedStableFile", metadata_lock)
    core_lock = text.find("$coreHeld = Open-LockedStableFile", plugin_lock)
    metadata_guard = text.find("Assert-LockedPathBinding -Held $metadataHeld", core_lock)
    metadata_read = text.find("$metadataText = Read-BoundedStrictUtf8Stream -Held $metadataHeld", metadata_guard)
    json_parse = text.find("ConvertFrom-Json -ErrorAction Stop", metadata_read)
    plugin_pre = text.find("Assert-LockedPathBinding -Held $pluginHeld", json_parse)
    plugin_read = text.find("GetAssemblyName($pluginHeld.Path)", plugin_pre)
    plugin_post = text.find("Assert-LockedPathBinding -Held $pluginHeld", plugin_read)
    core_pre = text.find("Assert-LockedPathBinding -Held $coreHeld", plugin_post)
    core_read = text.find("GetAssemblyName($coreHeld.Path)", core_pre)
    core_post = text.find("Assert-LockedPathBinding -Held $coreHeld", core_read)
    dispose = text.find("$heldFiles[$index].Stream.Dispose()", core_post)

    ordered = (
        metadata_lock,
        plugin_lock,
        core_lock,
        metadata_guard,
        metadata_read,
        json_parse,
        plugin_pre,
        plugin_read,
        plugin_post,
        core_pre,
        core_read,
        core_post,
        dispose,
    )
    if min(ordered) < 0 or list(ordered) != sorted(ordered):
        errors.append(
            "V26 identity safety order must lock metadata/plugin/core before consumption, read metadata from the held stream, consume AssemblyName under locked pathname assertions, then dispose"
        )

    lock_function = text.find("function Open-LockedStableFile")
    file_open = text.find("[IO.File]::Open(", lock_function)
    share_read = text.find("[IO.FileShare]::Read", file_open)
    held_hash = text.find("$hash = Get-HeldStreamingSha256 -Stream $stream -Label $Label", share_read)
    after_hash = text.find("$afterHash = Resolve-OrdinaryNonReparseFile", held_hash)
    state_hash = text.find("Sha256 = $hash", after_hash)
    if min(lock_function, file_open, share_read, held_hash, after_hash, state_hash) < 0 or not (
        lock_function < file_open < share_read < held_hash < after_hash < state_hash
    ):
        errors.append(
            "V26 held generation admission must open with FileShare.Read, fingerprint the held stream, re-resolve, and publish that held SHA-256 state"
        )

    for forbidden in (
        "Get-StableFileState",
        "Assert-StableFileState",
        "Read-BoundedStrictUtf8File",
        "Get-Content -LiteralPath $MetadataPath -Raw | ConvertFrom-Json",
        "[Text.Encoding]::UTF8.GetString",
        "Get-FileHash",
    ):
        if forbidden in text:
            errors.append(f"V26 release identity helper contains superseded/unsafe transient parsing shortcut: {forbidden}")
    return errors


def package_identity_call(text: str) -> str:
    helper_call = "assert-v26-release-package-identity.ps1"
    start = text.find(helper_call)
    if start < 0:
        return ""
    line_start = text.rfind("\n", 0, start) + 1
    end_marker = " | Out-Null"
    end = text.find(end_marker, start)
    if end < 0:
        return text[line_start:]
    return text[line_start : end + len(end_marker)]


def validate_workflow(text: str) -> list[str]:
    errors: list[str] = []
    helper_call = "assert-v26-release-package-identity.ps1"
    call = package_identity_call(text)
    if not call:
        errors.append("V26 release workflow must route package identity validation through the bounded helper")
    for token in (
        "-MetadataPath 'dist\\QS3D-BricsCAD-V26\\PACKAGE-METADATA.json'",
        "-PluginPath 'dist\\QS3D-BricsCAD-V26\\QS3D.BricsCAD.V26.dll'",
        "-CorePath 'dist\\QS3D-BricsCAD-V26\\QS3D.Core.dll'",
        "-ReleaseTag $env:RELEASE_TAG",
    ):
        if token not in call:
            errors.append(f"V26 release workflow helper call missing exact parameter binding: {token}")
    if helper_call in text and text.count(helper_call) != 1:
        errors.append("V26 release workflow must contain exactly one package identity helper invocation")
    if "Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json" in text:
        errors.append("V26 release workflow must not retain the raw unbounded metadata parser")
    if "GetAssemblyName((Resolve-Path 'dist\\QS3D-BricsCAD-V26" in text:
        errors.append("V26 release workflow must not bypass helper ordinary-file checks with inline AssemblyName parsing")
    return errors


try:
    helper = read(HELPER)
    workflow = read(WORKFLOW)
except Exception as exc:
    print(f"ERROR: {exc}")
    sys.exit(1)

errors = validate_helper(helper)
errors.extend(validate_workflow(workflow))

helper_mutations = {
    "metadata size bound": helper.replace("$stream.Length -gt $script:MaxMetadataBytes", "$false", 1),
    "strict UTF-8 decoder": helper.replace("[Text.UTF8Encoding]::new($false, $true)", "[Text.Encoding]::UTF8", 1),
    "leaf reparse rejection": helper.replace("$item.Attributes -band [IO.FileAttributes]::ReparsePoint", "$item.Attributes -band [IO.FileAttributes]::Normal", 1),
    "parent reparse rejection": helper.replace("$cursor.Attributes -band [IO.FileAttributes]::ReparsePoint", "$cursor.Attributes -band [IO.FileAttributes]::Normal", 1),
    "streaming fingerprint": helper.replace("[Security.Cryptography.SHA256]::Create()", "[Security.Cryptography.MD5]::Create()", 1),
    "generation share mode": helper.replace("[IO.FileShare]::Read", "[IO.FileShare]::Write", 1),
    "held fingerprint": helper.replace("$hash = Get-HeldStreamingSha256 -Stream $stream -Label $Label", "$hash = 'UNBOUND'", 1),
    "metadata generation lock": helper.replace("$metadataHeld = Open-LockedStableFile", "$metadataHeld = Resolve-OrdinaryNonReparseFile", 1),
    "plugin generation lock": helper.replace("$pluginHeld = Open-LockedStableFile", "$pluginHeld = Resolve-OrdinaryNonReparseFile", 1),
    "core generation lock": helper.replace("$coreHeld = Open-LockedStableFile", "$coreHeld = Resolve-OrdinaryNonReparseFile", 1),
    "held metadata consumption": helper.replace("$metadataText = Read-BoundedStrictUtf8Stream -Held $metadataHeld", "$metadataText = Get-Content $MetadataPath -Raw", 1),
    "plugin locked AssemblyName": helper.replace("GetAssemblyName($pluginHeld.Path)", "GetAssemblyName($PluginPath)", 1),
    "core locked AssemblyName": helper.replace("GetAssemblyName($coreHeld.Path)", "GetAssemblyName($CorePath)", 1),
    "finally disposal": helper.replace("$heldFiles[$index].Stream.Dispose()", "# generation lock disposal removed", 1),
}
for label, mutated in helper_mutations.items():
    if mutated == helper:
        errors.append(f"mutation fixture did not modify helper for {label}")
    elif not validate_helper(mutated):
        errors.append(f"mutation escaped V26 release identity safety guard: {label}")

call = package_identity_call(workflow)
workflow_mutations = {
    "shared helper call": workflow.replace("assert-v26-release-package-identity.ps1", "missing-v26-release-identity-helper.ps1", 1),
    "metadata binding": workflow.replace("-MetadataPath 'dist\\QS3D-BricsCAD-V26\\PACKAGE-METADATA.json'", "-MetadataPath 'PACKAGE-METADATA.json'", 1),
    "plugin binding": workflow.replace("-PluginPath 'dist\\QS3D-BricsCAD-V26\\QS3D.BricsCAD.V26.dll'", "-PluginPath 'QS3D.BricsCAD.V26.dll'", 1),
    "core binding": workflow.replace("-CorePath 'dist\\QS3D-BricsCAD-V26\\QS3D.Core.dll'", "-CorePath 'QS3D.Core.dll'", 1),
    "release tag binding": workflow.replace(call, call.replace("-ReleaseTag $env:RELEASE_TAG", "-ReleaseTag 'v0.0.0'", 1), 1),
}
for label, mutated in workflow_mutations.items():
    if mutated == workflow:
        errors.append(f"mutation fixture did not modify workflow for {label}")
    elif not validate_workflow(mutated):
        errors.append(f"mutation escaped V26 release workflow routing guard: {label}")

print("QS3D V26 release identity input-safety preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: V26 release package identity is bounded, strict-UTF8, ordinary-file/reparse guarded, SHA-256 bound to held FileShare.Read generations through semantic consumption, and the manual release workflow is mutation-locked to the exact shared-helper invocation.")
