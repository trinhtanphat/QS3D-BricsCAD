#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts/assert-v26-release-package-identity.ps1"

OPEN_LOCK = "Open-LockedStableFile"
SHARE_READ = "[IO.FileShare]::Read"
HELD_HASH = "Get-HeldStreamingSha256"
HELD_METADATA = "Read-BoundedStrictUtf8Stream -Held $metadataHeld"
PROBE_INIT = "Initialize-AssemblyVersionProbe"
PROBE_STREAM = "$Held.Stream.CopyToAsync($process.StandardInput.BaseStream)"
PLUGIN_ASSEMBLY = "$pluginVersion = Get-HeldAssemblyVersion -Held $pluginHeld -Probe $assemblyProbe -Label 'V26 plugin assembly'"
CORE_ASSEMBLY = "$coreVersion = Get-HeldAssemblyVersion -Held $coreHeld -Probe $assemblyProbe -Label 'V26 Core assembly'"
PLUGIN_ASSERT = "Assert-LockedPathBinding -Held $pluginHeld"
CORE_ASSERT = "Assert-LockedPathBinding -Held $coreHeld"
DISPOSE = "$heldFiles[$index].Stream.Dispose()"
OLD_CAPTURE = "Get-StableFileState"
OLD_METADATA_REOPEN = "Read-BoundedStrictUtf8File"
OLD_ASSEMBLY_REOPEN = "[Reflection.AssemblyName]::GetAssemblyName"


def validate(text: str) -> list[str]:
    failures: list[str] = []
    required = (
        OPEN_LOCK,
        SHARE_READ,
        HELD_HASH,
        "System.Collections.Generic.List[object]",
        HELD_METADATA,
        PROBE_INIT,
        PROBE_STREAM,
        "30000 - [int]$deadline.ElapsedMilliseconds",
        PLUGIN_ASSEMBLY,
        CORE_ASSEMBLY,
        PLUGIN_ASSERT,
        CORE_ASSERT,
        DISPOSE,
    )
    for token in required:
        if token not in text:
            failures.append(f"V26 package identity verifier missing held-generation marker: {token}")

    if OLD_CAPTURE in text:
        failures.append("V26 package identity verifier retains transient capture/reopen state helper")
    if OLD_METADATA_REOPEN in text:
        failures.append("V26 package metadata is reopened by pathname instead of consumed from its held stream")
    if OLD_ASSEMBLY_REOPEN in text or "AssemblyName.GetAssemblyName" in text:
        failures.append("V26 package assembly semantics are reopened by pathname instead of consumed from held-stream bytes")

    metadata_lock = text.find("$metadataHeld = Open-LockedStableFile")
    plugin_lock = text.find("$pluginHeld = Open-LockedStableFile", metadata_lock)
    core_lock = text.find("$coreHeld = Open-LockedStableFile", plugin_lock)
    metadata_read = text.find(HELD_METADATA, core_lock)
    plugin_read = text.find(PLUGIN_ASSEMBLY, metadata_read)
    core_read = text.find(CORE_ASSEMBLY, plugin_read)
    dispose = text.find(DISPOSE, core_read)
    if not (
        0 <= metadata_lock < plugin_lock < core_lock < metadata_read < plugin_read < core_read < dispose
    ):
        failures.append(
            "held-generation ordering must be lock metadata/plugin/core -> consume metadata -> consume plugin/core identities from held streams -> dispose"
        )

    probe_function = text.find("function Get-HeldAssemblyVersion")
    probe_stream = text.find(PROBE_STREAM, probe_function)
    probe_complete = text.find("$Held.Stream.Position -ne $Held.Stream.Length", probe_stream)
    probe_wait = text.find("$process.WaitForExit($exitWait)", probe_complete)
    probe_reset = text.find("$Held.Stream.Position = 0", probe_wait)
    if not (
        0 <= probe_function < probe_stream < probe_complete < probe_wait < probe_reset
    ):
        failures.append(
            "held-generation assembly probe must stream exact held bytes, verify complete input, wait within the shared budget, and reset the held stream"
        )

    if text.count(PLUGIN_ASSERT) < 2:
        failures.append("plugin pathname binding must be asserted around held-stream semantic consumption")
    if text.count(CORE_ASSERT) < 2:
        failures.append("Core pathname binding must be asserted around held-stream semantic consumption")
    return failures


def main() -> int:
    source = TARGET.read_text(encoding="utf-8")
    failures = validate(source)

    mutation_tokens = (
        OPEN_LOCK,
        SHARE_READ,
        HELD_HASH,
        HELD_METADATA,
        PROBE_INIT,
        PROBE_STREAM,
        PLUGIN_ASSEMBLY,
        CORE_ASSEMBLY,
        DISPOSE,
    )
    for token in mutation_tokens:
        mutated = source.replace(token, "MUTATED-V26-PACKAGE-GENERATION")
        if mutated == source:
            failures.append(f"mutation fixture did not modify V26 package source: {token}")
        elif not validate(mutated):
            failures.append(f"mutation probe escaped V26 package held-generation guard: {token}")

    vulnerable_metadata = source.replace(
        HELD_METADATA,
        "Read-BoundedStrictUtf8File -File $metadataFile",
        1,
    )
    if vulnerable_metadata == source:
        failures.append("metadata reopen mutation fixture did not modify V26 package source")
    elif not validate(vulnerable_metadata):
        failures.append("transient metadata reopen mutation escaped held-generation guard")

    vulnerable_plugin = source.replace(
        PLUGIN_ASSEMBLY,
        "$pluginVersion = [Reflection.AssemblyName]::GetAssemblyName($PluginPath).Version",
        1,
    )
    if vulnerable_plugin == source:
        failures.append("plugin pathname reopen mutation fixture did not modify V26 package source")
    elif not validate(vulnerable_plugin):
        failures.append("plugin pathname semantic reopen mutation escaped held-generation guard")

    vulnerable_core = source.replace(
        CORE_ASSEMBLY,
        "$coreVersion = [Reflection.AssemblyName]::GetAssemblyName($CorePath).Version",
        1,
    )
    if vulnerable_core == source:
        failures.append("Core pathname reopen mutation fixture did not modify V26 package source")
    elif not validate(vulnerable_core):
        failures.append("Core pathname semantic reopen mutation escaped held-generation guard")

    if failures:
        print("V26 release package held-generation preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V26 release package identity consumers remain bound to held admitted file generations.")
    print(" - metadata, plugin, and Core generations are locked before semantic consumption")
    print(" - package metadata is read from its held stream")
    print(" - plugin/Core assembly semantics are parsed from bytes streamed from their exact held generations")
    print(" - semantic probe execution is bounded and pathname AssemblyName reopen regressions are rejected")
    print(" - all generation locks are released only after cross-identity validation")
    return 0


if __name__ == "__main__":
    sys.exit(main())
