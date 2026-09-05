#!/usr/bin/env python3
"""Require V26 assembly identity semantics to consume admitted held file generations."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "assert-v26-release-package-identity.ps1"


def main() -> int:
    source = SCRIPT.read_text(encoding="utf-8")
    failures: list[str] = []

    required_existing = (
        "function Open-LockedStableFile",
        "function Get-HeldStreamingSha256",
        "[IO.FileShare]::Read",
        "$pluginHeld = Open-LockedStableFile",
        "$coreHeld = Open-LockedStableFile",
        "PluginSha256 = $pluginHeld.Sha256",
        "CoreSha256 = $coreHeld.Sha256",
    )
    for token in required_existing:
        if token not in source:
            failures.append(f"held-generation admission regressed; missing: {token}")

    forbidden_path_reopens = (
        "GetAssemblyName($pluginHeld.Path)",
        "GetAssemblyName($coreHeld.Path)",
        "GetAssemblyName($Held.Path)",
    )
    for token in forbidden_path_reopens:
        if token in source:
            failures.append(
                f"managed assembly semantics still reopen admitted input by pathname: {token}"
            )

    helper_start = source.find("function Get-HeldAssemblyVersion")
    plugin_call = source.find("$pluginVersion = Get-HeldAssemblyVersion -Held $pluginHeld")
    core_call = source.find("$coreVersion = Get-HeldAssemblyVersion -Held $coreHeld")

    if helper_start < 0:
        failures.append("missing held-generation assembly semantic helper")
        helper = ""
    else:
        next_function = source.find("\nfunction ", helper_start + len("function Get-HeldAssemblyVersion"))
        helper = source[helper_start:next_function] if next_function > helper_start else source[helper_start:]

    required_helper = (
        "$Held.Stream.Position = 0",
        "$Held.Stream.Length -gt [int]::MaxValue",
        "[byte[]]::new([int]$Held.Stream.Length)",
        "$Held.Stream.Read(",
        "$Held.Stream.ReadByte()",
        "[Reflection.Assembly]::ReflectionOnlyLoad($bytes)",
        ".GetName().Version",
    )
    for token in required_helper:
        if token not in helper:
            failures.append(f"held-byte assembly inspection contract is incomplete; missing: {token}")

    if helper and "GetAssemblyName(" in helper:
        failures.append("held assembly helper must not reopen a pathname through AssemblyName.GetAssemblyName")
    if helper and ("CreateNew" in helper or "$snapshotPath" in helper or "GetTemp" in helper):
        failures.append("held assembly helper must not introduce a temporary pathname generation")

    if plugin_call < 0 or core_call < 0:
        failures.append("plugin/Core semantic consumers must both use Get-HeldAssemblyVersion")

    dispose_marker = source.find("$heldFiles[$index].Stream.Dispose()")
    version_match = source.find("if ($pluginVersion -ne $packageVersion -or $coreVersion -ne $packageVersion)")
    if dispose_marker < 0 or version_match < 0 or dispose_marker < version_match:
        failures.append("held input streams must remain live through cross-assembly version equality checks")

    if "continue-on-error" in source.lower():
        failures.append("release package identity must not hide held-generation failures")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: V26 package assembly semantics consume the exact admitted held bytes with no semantic pathname reopen")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
