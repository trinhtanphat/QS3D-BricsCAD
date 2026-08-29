#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts/assert-v26-release-package-identity.ps1"

OPEN_LOCK = "Open-LockedStableFile"
SHARE_READ = "[IO.FileShare]::Read"
HELD_HASH = "Get-HeldStreamingSha256"
HELD_METADATA = "Read-BoundedStrictUtf8Stream -Held $metadataHeld"
PLUGIN_ASSEMBLY = "GetAssemblyName($pluginHeld.Path)"
CORE_ASSEMBLY = "GetAssemblyName($coreHeld.Path)"
PLUGIN_ASSERT = "Assert-LockedPathBinding -Held $pluginHeld"
CORE_ASSERT = "Assert-LockedPathBinding -Held $coreHeld"
DISPOSE = "$heldFiles[$index].Stream.Dispose()"
OLD_CAPTURE = "Get-StableFileState"
OLD_METADATA_REOPEN = "Read-BoundedStrictUtf8File"


def validate(text: str) -> list[str]:
    failures: list[str] = []
    required = (
        OPEN_LOCK,
        SHARE_READ,
        HELD_HASH,
        "System.Collections.Generic.List[object]",
        HELD_METADATA,
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
            "held-generation ordering must be lock metadata/plugin/core -> consume metadata -> consume plugin/core identities -> dispose"
        )

    if text.count(PLUGIN_ASSERT) < 2:
        failures.append("plugin pathname binding must be asserted before and after AssemblyName consumption")
    if text.count(CORE_ASSERT) < 2:
        failures.append("Core pathname binding must be asserted before and after AssemblyName consumption")
    return failures


def main() -> int:
    source = TARGET.read_text(encoding="utf-8")
    failures = validate(source)

    mutation_tokens = (
        OPEN_LOCK,
        SHARE_READ,
        HELD_HASH,
        HELD_METADATA,
        PLUGIN_ASSEMBLY,
        CORE_ASSEMBLY,
        DISPOSE,
    )
    for token in mutation_tokens:
        mutated = source.replace(token, "MUTATED-V26-PACKAGE-GENERATION")
        if not validate(mutated):
            failures.append(f"mutation probe escaped V26 package held-generation guard: {token}")

    # Recreate the old vulnerable metadata reopen shape and prove the guard rejects it.
    vulnerable = source.replace(
        HELD_METADATA,
        "Read-BoundedStrictUtf8File -File $metadataFile",
        1,
    )
    if not validate(vulnerable):
        failures.append("transient metadata reopen mutation escaped held-generation guard")

    if failures:
        print("V26 release package held-generation preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V26 release package identity consumers remain bound to held admitted file generations.")
    print(" - metadata, plugin, and Core generations are locked before semantic consumption")
    print(" - package metadata is read from its held stream")
    print(" - AssemblyName pathname consumers execute while write/delete/replace are denied")
    print(" - all generation locks are released only after cross-identity validation")
    return 0


if __name__ == "__main__":
    sys.exit(main())
