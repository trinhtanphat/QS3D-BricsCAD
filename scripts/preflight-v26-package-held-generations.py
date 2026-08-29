#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts/package-v26.ps1"


def validate(text: str) -> list[str]:
    errors: list[str] = []
    required = (
        "Open-HeldPackageInput",
        "[IO.FileShare]::Read",
        "Copy-HeldPackageInput",
        "Read-HeldPackageText",
        "Invoke-WithHeldPackageInput",
        "Open-HeldStagedManagedFile",
        "Assert-HeldPathBinding",
        "$heldPlugin.Stream.Dispose()",
        "$heldCore.Stream.Dispose()",
    )
    for token in required:
        if token not in text:
            errors.append(f"V26 package constructor missing held-generation marker: {token}")

    forbidden = (
        "Copy-Item -LiteralPath $path -Destination (Join-Path $dist $name)",
        "Copy-Item -LiteralPath $samplePath -Destination (Join-Path $sampleDestination $sampleName)",
        "Get-Content -LiteralPath $ProjectPath -Raw",
        "Get-Content -LiteralPath $Path -Raw",
    )
    for token in forbidden:
        if token in text:
            errors.append(f"V26 package constructor retains pathname reopen/copy shortcut: {token}")

    artifact_copy = text.find("Copy-HeldPackageInput", text.find("foreach ($name in $required)"))
    command_read = text.find("Read-HeldPackageText", text.find("function Add-CommandMethodsFromSource"))
    plugin_lock = text.find("$heldPlugin = Open-HeldStagedManagedFile")
    signature = text.find("Get-AuthenticodeSignature", plugin_lock)
    plugin_assembly = text.find("GetAssemblyName($heldPlugin.Path)", signature)
    core_lock = text.find("$heldCore = Open-HeldStagedManagedFile", plugin_assembly)
    core_assembly = text.find("GetAssemblyName($heldCore.Path)", core_lock)
    plugin_dispose = text.find("$heldPlugin.Stream.Dispose()", core_assembly)
    core_dispose = text.find("$heldCore.Stream.Dispose()", core_assembly)

    if artifact_copy < 0:
        errors.append("required V26 build artifacts are not streamed from held admitted generations")
    if command_read < 0:
        errors.append("V26 command source is not consumed from a held admitted generation")
    if min(plugin_lock, signature, plugin_assembly, core_lock, core_assembly, plugin_dispose, core_dispose) < 0:
        errors.append("staged plugin/Core identity consumers are not protected by held generation locks")
    elif not (plugin_lock < signature < plugin_assembly < core_lock < core_assembly < plugin_dispose and core_assembly < core_dispose):
        errors.append("staged managed identity ordering must lock -> consume Authenticode/AssemblyName/ProductVersion -> dispose")

    return errors


def main() -> int:
    if not TARGET.is_file():
        print("FAIL: missing scripts/package-v26.ps1")
        return 1
    source = TARGET.read_text(encoding="utf-8")
    failures = validate(source)

    mutation_tokens = (
        "Open-HeldPackageInput",
        "[IO.FileShare]::Read",
        "Copy-HeldPackageInput",
        "Read-HeldPackageText",
        "Invoke-WithHeldPackageInput",
        "Open-HeldStagedManagedFile",
        "Assert-HeldPathBinding",
    )
    for token in mutation_tokens:
        if token not in source:
            continue
        mutated = source.replace(token, "MUTATED-V26-PACKAGE-GENERATION", 1)
        if not validate(mutated):
            failures.append(f"mutation probe escaped V26 package held-generation guard: {token}")

    if failures:
        print("V26 package held-generation preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V26 package source and staged managed-identity consumers remain bound to held admitted file generations.")
    print(" - build artifacts/samples are streamed from held source handles")
    print(" - project/command text is read from held bounded streams")
    print(" - generator inputs remain locked while transformed")
    print(" - staged plugin/Core identities are consumed while write/delete/replace is denied")
    return 0


if __name__ == "__main__":
    sys.exit(main())
