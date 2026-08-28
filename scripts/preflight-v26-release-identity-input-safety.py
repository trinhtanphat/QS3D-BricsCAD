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
        "Get-StreamingSha256",
        "[Security.Cryptography.SHA256]::Create()",
        "$currentHash = Get-StreamingSha256 -File $current -Label $Label",
        "Get-StableFileState",
        "Assert-StableFileState",
        "LastWriteUtcTicks",
        "Sha256 = $currentHash",
        "Read-BoundedStrictUtf8File",
        "[IO.File]::Open",
        "[IO.FileShare]::Read",
        "$stream.Length -gt $script:MaxMetadataBytes",
        "[byte[]]::new([int]$stream.Length)",
        "[Text.UTF8Encoding]::new($false, $true)",
        "[Text.DecoderFallbackException]",
        "ConvertFrom-Json -ErrorAction Stop",
        "BricsCAD V26 x64",
        "net8.0-windows",
        "[string]::Equals(('v' + [string]$metadata.productVersion), $ReleaseTag, [StringComparison]::Ordinal)",
        "[Reflection.AssemblyName]::GetAssemblyName($pluginFile.FullName)",
        "[Reflection.AssemblyName]::GetAssemblyName($coreFile.FullName)",
    )
    for token in required:
        if token not in text:
            errors.append(f"V26 release identity helper missing required safety token: {token}")

    metadata_state = text.find("$metadataState = Get-StableFileState")
    plugin_state = text.find("$pluginState = Get-StableFileState")
    core_state = text.find("$coreState = Get-StableFileState")
    metadata_guard = text.find("$metadataFile = Resolve-OrdinaryNonReparseFile")
    metadata_read = text.find("$metadataText = Read-BoundedStrictUtf8File")
    metadata_recheck = text.find("Assert-StableFileState -Expected $metadataState")
    json_parse = text.find("ConvertFrom-Json -ErrorAction Stop")
    plugin_guard = text.find("$pluginFile = Resolve-OrdinaryNonReparseFile")
    plugin_read = text.find("GetAssemblyName($pluginFile.FullName)")
    plugin_recheck = text.find("Assert-StableFileState -Expected $pluginState")
    core_guard = text.find("$coreFile = Resolve-OrdinaryNonReparseFile")
    core_read = text.find("GetAssemblyName($coreFile.FullName)")
    core_recheck = text.find("Assert-StableFileState -Expected $coreState")

    if min(metadata_state, metadata_guard, metadata_read, metadata_recheck, json_parse) < 0 or not metadata_state < metadata_guard < metadata_read < metadata_recheck < json_parse:
        errors.append("V26 metadata safety order must be stable-state capture -> ordinary-file guard -> bounded strict-UTF8 read -> stability recheck -> JSON parse")
    if min(plugin_state, plugin_guard, plugin_read, plugin_recheck) < 0 or not plugin_state < plugin_guard < plugin_read < plugin_recheck:
        errors.append("V26 plugin assembly safety order must be stable-state capture -> ordinary-file guard -> AssemblyName parsing -> stability recheck")
    if min(core_state, core_guard, core_read, core_recheck) < 0 or not core_state < core_guard < core_read < core_recheck:
        errors.append("V26 Core assembly safety order must be stable-state capture -> ordinary-file guard -> AssemblyName parsing -> stability recheck")

    stable_capture = text.find("function Get-StableFileState")
    first_hash = text.find("Get-StreamingSha256 -File $file")
    second_resolve = text.find("$current = Resolve-OrdinaryNonReparseFile")
    second_hash = text.find("$currentHash = Get-StreamingSha256 -File $current -Label $Label")
    state_hash = text.find("Sha256 = $currentHash")
    if min(stable_capture, first_hash, second_resolve, second_hash, state_hash) < 0 or not stable_capture < first_hash < second_resolve < second_hash < state_hash:
        errors.append("V26 stable file-state capture must fingerprint, re-resolve, re-fingerprint, and publish the revalidated SHA-256 state")

    stability_assert = text.find("function Assert-StableFileState")
    actual_capture = text.find("$actual = Get-StableFileState", stability_assert)
    hash_compare = text.find("$Expected.Sha256", stability_assert)
    if min(stability_assert, actual_capture, hash_compare) < 0 or not stability_assert < actual_capture < hash_compare:
        errors.append("V26 stability assertion must recapture file state and compare the SHA-256 fingerprint")

    for forbidden in (
        "Get-Content -LiteralPath $MetadataPath -Raw | ConvertFrom-Json",
        "[Text.Encoding]::UTF8.GetString",
        "Get-FileHash",
    ):
        if forbidden in text:
            errors.append(f"V26 release identity helper contains unsafe parsing/fingerprinting shortcut: {forbidden}")
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
    "revalidated fingerprint": helper.replace("$currentHash = Get-StreamingSha256 -File $current -Label $Label", "$currentHash = $hash", 1),
    "stable metadata capture": helper.replace("$metadataState = Get-StableFileState", "$metadataState = Resolve-OrdinaryNonReparseFile", 1),
    "stable plugin capture": helper.replace("$pluginState = Get-StableFileState", "$pluginState = Resolve-OrdinaryNonReparseFile", 1),
    "stable core capture": helper.replace("$coreState = Get-StableFileState", "$coreState = Resolve-OrdinaryNonReparseFile", 1),
    "metadata post-read recheck": helper.replace("Assert-StableFileState -Expected $metadataState", "# removed metadata stability recheck", 1),
    "plugin post-read recheck": helper.replace("Assert-StableFileState -Expected $pluginState", "# removed plugin stability recheck", 1),
    "core post-read recheck": helper.replace("Assert-StableFileState -Expected $coreState", "# removed core stability recheck", 1),
    "metadata ordinary-file binding": helper.replace("$metadataFile = Resolve-OrdinaryNonReparseFile", "$metadataFile = Get-Item", 1),
    "plugin ordinary-file binding": helper.replace("$pluginFile = Resolve-OrdinaryNonReparseFile", "$pluginFile = Get-Item", 1),
    "core ordinary-file binding": helper.replace("$coreFile = Resolve-OrdinaryNonReparseFile", "$coreFile = Get-Item", 1),
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
print("PASS: V26 release package identity is bounded, strict-UTF8, ordinary-file/reparse guarded, SHA-256 state-bound before/after consumption, and the manual release workflow is mutation-locked to the exact shared-helper invocation.")
