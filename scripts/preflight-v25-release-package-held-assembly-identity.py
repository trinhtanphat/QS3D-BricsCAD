#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VALIDATOR = ROOT / "scripts" / "assert-v25-release-package-identity.ps1"


def validate(source: str) -> list[str]:
    errors: list[str] = []
    assembly_equality = (
        "$pluginIdentity.AssemblyVersion -ne $packageVersion -or "
        "$coreIdentity.AssemblyVersion -ne $packageVersion"
    )
    required = [
        "$script:MaxAssemblyBytes = 134217728",
        "function Open-HeldAssemblyFile",
        "function Read-HeldAssemblyBytes",
        "function Get-HeldAssemblyIdentity",
        "QS3D.BricsCAD.V25.dll",
        "QS3D.Core.dll",
        "$packageVersion = [Version]::Parse([string]$metadata.version)",
        "$pluginHeld = Open-HeldAssemblyFile",
        "$coreHeld = Open-HeldAssemblyFile",
        "$pluginIdentity = Get-HeldAssemblyIdentity",
        "$coreIdentity = Get-HeldAssemblyIdentity",
        "[Reflection.Assembly]::ReflectionOnlyLoad($bytes)",
        assembly_equality,
        "AssemblyVersion = $packageVersion.ToString()",
        "$pluginHeld.Stream.Dispose()",
        "$coreHeld.Stream.Dispose()",
    ]
    for token in required:
        if token not in source:
            errors.append(f"missing held V25 assembly identity contract token: {token}")

    forbidden = [
        "AssemblyName]::GetAssemblyName($plugin",
        "AssemblyName]::GetAssemblyName($core",
        "Reflection.Assembly]::LoadFile(",
        "Reflection.Assembly]::LoadFrom(",
    ]
    for token in forbidden:
        if token in source:
            errors.append(f"pathname/executable assembly semantic reopen is forbidden: {token}")

    ordering = [
        "$held = Open-HeldMetadataFile",
        "$pluginHeld = Open-HeldAssemblyFile",
        "$coreHeld = Open-HeldAssemblyFile",
        "$packageVersion = [Version]::Parse([string]$metadata.version)",
        "$pluginIdentity = Get-HeldAssemblyIdentity",
        "$coreIdentity = Get-HeldAssemblyIdentity",
        assembly_equality,
    ]
    positions = [source.find(token) for token in ordering]
    if all(position >= 0 for position in positions) and positions != sorted(positions):
        errors.append("held V25 metadata/plugin/Core admission and AssemblyVersion checks are out of order")

    return errors


def assert_rejects_mutation(source: str, old: str, new: str, label: str) -> None:
    if old not in source:
        raise SystemExit(f"guard self-check could not find mutation anchor: {label}")
    mutated = source.replace(old, new, 1)
    if not validate(mutated):
        raise SystemExit(f"guard failed to reject mutation: {label}")


def main() -> int:
    source = VALIDATOR.read_text(encoding="utf-8")
    errors = validate(source)
    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        return 1

    assert_rejects_mutation(
        source,
        "[Reflection.Assembly]::ReflectionOnlyLoad($bytes)",
        "[Reflection.AssemblyName]::GetAssemblyName($Held.Path)",
        "pathname semantic reopen",
    )
    assert_rejects_mutation(
        source,
        "$pluginIdentity.AssemblyVersion -ne $packageVersion -or $coreIdentity.AssemblyVersion -ne $packageVersion",
        "$pluginIdentity.AssemblyVersion -ne $packageVersion",
        "Core assembly version equality",
    )
    assert_rejects_mutation(
        source,
        "$coreHeld.Stream.Dispose()",
        "# core held stream disposal removed",
        "Core generation lifetime cleanup",
    )

    print("PASS: V25 release package identity binds metadata, plugin, and Core AssemblyVersion to held generations")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
