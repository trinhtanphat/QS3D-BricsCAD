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
        "[IO.FileAttributes]::ReparsePoint",
        "Read-BoundedStrictUtf8File",
        "[IO.File]::Open",
        "$stream.Length -gt $script:MaxMetadataBytes",
        "[byte[]]::new([int]$stream.Length)",
        "New-Object System.Text.UTF8Encoding($false, $true)",
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

    metadata_guard = text.find("$metadataFile = Resolve-OrdinaryNonReparseFile")
    metadata_read = text.find("$metadataText = Read-BoundedStrictUtf8File")
    json_parse = text.find("ConvertFrom-Json -ErrorAction Stop")
    plugin_guard = text.find("$pluginFile = Resolve-OrdinaryNonReparseFile")
    plugin_read = text.find("GetAssemblyName($pluginFile.FullName)")
    core_guard = text.find("$coreFile = Resolve-OrdinaryNonReparseFile")
    core_read = text.find("GetAssemblyName($coreFile.FullName)")
    if min(metadata_guard, metadata_read, json_parse) < 0 or not metadata_guard < metadata_read < json_parse:
        errors.append("V26 metadata safety order must be ordinary-file guard -> bounded strict-UTF8 read -> JSON parse")
    if min(plugin_guard, plugin_read) < 0 or plugin_guard >= plugin_read:
        errors.append("V26 plugin assembly must be ordinary/non-reparse before AssemblyName parsing")
    if min(core_guard, core_read) < 0 or core_guard >= core_read:
        errors.append("V26 Core assembly must be ordinary/non-reparse before AssemblyName parsing")

    for forbidden in (
        "Get-Content -LiteralPath $MetadataPath -Raw | ConvertFrom-Json",
        "[Text.Encoding]::UTF8.GetString",
    ):
        if forbidden in text:
            errors.append(f"V26 release identity helper contains unsafe parsing shortcut: {forbidden}")
    return errors


def validate_workflow(text: str, require_helper_call: bool) -> list[str]:
    errors: list[str] = []
    helper_call = "assert-v26-release-package-identity.ps1"
    if require_helper_call and helper_call not in text:
        errors.append("V26 release workflow must route package identity validation through the bounded helper")
    if helper_call in text:
        for token in (
            "-MetadataPath",
            "-PluginPath",
            "-CorePath",
            "-ReleaseTag $env:RELEASE_TAG",
        ):
            if token not in text:
                errors.append(f"V26 release workflow helper call missing parameter binding: {token}")
        if "Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json" in text:
            errors.append("V26 release workflow must not retain the raw unbounded metadata parser once routed through the helper")
    return errors


try:
    helper = read(HELPER)
    workflow = read(WORKFLOW)
except Exception as exc:
    print(f"ERROR: {exc}")
    sys.exit(1)

errors = validate_helper(helper)
# The workflow integration is a second commit in this carrier. Once present, this
# guard locks its parameter bindings and forbids regression to the old raw parser.
errors.extend(validate_workflow(workflow, require_helper_call=False))

# Deterministic mutation probes: each critical input/resource gate must be load-bearing.
mutations = {
    "metadata size bound": helper.replace("$stream.Length -gt $script:MaxMetadataBytes", "$false", 1),
    "strict UTF-8 decoder": helper.replace("New-Object System.Text.UTF8Encoding($false, $true)", "[Text.Encoding]::UTF8", 1),
    "reparse rejection": helper.replace("[IO.FileAttributes]::ReparsePoint", "[IO.FileAttributes]::Normal", 1),
    "metadata ordinary-file binding": helper.replace("$metadataFile = Resolve-OrdinaryNonReparseFile", "$metadataFile = Get-Item", 1),
    "plugin ordinary-file binding": helper.replace("$pluginFile = Resolve-OrdinaryNonReparseFile", "$pluginFile = Get-Item", 1),
    "core ordinary-file binding": helper.replace("$coreFile = Resolve-OrdinaryNonReparseFile", "$coreFile = Get-Item", 1),
}
for label, mutated in mutations.items():
    if mutated == helper:
        errors.append(f"mutation fixture did not modify helper for {label}")
    elif not validate_helper(mutated):
        errors.append(f"mutation escaped V26 release identity safety guard: {label}")

print("QS3D V26 release identity input-safety preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: V26 release package identity helper is bounded, strict-UTF8, ordinary-file/reparse guarded, and mutation-resistant; workflow bindings are checked when integrated.")
