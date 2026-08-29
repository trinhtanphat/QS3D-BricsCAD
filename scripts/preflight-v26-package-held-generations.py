#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts/package-v26.ps1"


def validate(text: str) -> list[str]:
    errors: list[str] = []
    required = (
        "function Open-HeldPackageInput",
        "[IO.FileShare]::Read",
        "function Copy-HeldPackageInput",
        "function Read-HeldPackageText",
        "function Invoke-WithHeldPackageInput",
        "function Open-HeldStagedManagedFile",
        "function Assert-HeldPathBinding",
        "Copy-HeldPackageInput -SourcePath $path",
        "Read-HeldPackageText -Held $held -Label 'V26 command source'",
        "Invoke-WithHeldPackageInput -Path $generator",
        "$heldPlugin = Open-HeldStagedManagedFile",
        "$heldCore = Open-HeldStagedManagedFile",
        "GetAssemblyName($heldPlugin.Path)",
        "GetAssemblyName($heldCore.Path)",
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

    artifact_loop = text.find("foreach ($name in $required)")
    artifact_copy = text.find("Copy-HeldPackageInput -SourcePath $path", artifact_loop)
    command_function = text.find("function Add-CommandMethodsFromSource")
    command_read = text.find("Read-HeldPackageText -Held $held -Label 'V26 command source'", command_function)
    generator_hold = text.find("Invoke-WithHeldPackageInput -Path $generator")
    generator_source_hold = text.find("Invoke-WithHeldPackageInput -Path $sourceScriptPath", generator_hold)
    plugin_lock = text.find("$heldPlugin = Open-HeldStagedManagedFile")
    core_lock = text.find("$heldCore = Open-HeldStagedManagedFile", plugin_lock)
    signature = text.find("Get-AuthenticodeSignature -FilePath $heldPlugin.Path", core_lock)
    plugin_assembly = text.find("GetAssemblyName($heldPlugin.Path)", signature)
    core_assembly = text.find("GetAssemblyName($heldCore.Path)", plugin_assembly)
    core_dispose = text.find("$heldCore.Stream.Dispose()", core_assembly)
    plugin_dispose = text.find("$heldPlugin.Stream.Dispose()", core_assembly)

    if artifact_loop < 0 or artifact_copy <= artifact_loop:
        errors.append("required V26 build artifacts are not streamed from held admitted generations")
    if command_function < 0 or command_read <= command_function:
        errors.append("V26 command source is not consumed from a held admitted generation")
    if generator_hold < 0 or generator_source_hold <= generator_hold:
        errors.append("V26 transformer and generator source inputs are not held during transformation")

    ordered = (
        plugin_lock,
        core_lock,
        signature,
        plugin_assembly,
        core_assembly,
        core_dispose,
        plugin_dispose,
    )
    if min(ordered) < 0:
        errors.append("staged plugin/Core identity consumers are not protected by held generation locks")
    elif not (plugin_lock < core_lock < signature < plugin_assembly < core_assembly < core_dispose and core_assembly < plugin_dispose):
        errors.append("staged managed identity ordering must lock plugin/Core before semantic consumers and dispose only after cross-identity validation")

    share_pos = text.find("[IO.FileShare]::Read", text.find("function Open-HeldPackageInput"))
    binding_pos = text.find("Assert-HeldPathBinding -Held $held", text.find("function Copy-HeldPackageInput"))
    copy_pos = text.find("$held.Stream.CopyTo($output)", binding_pos)
    post_copy_binding = text.find("Assert-HeldPathBinding -Held $held", copy_pos)
    if min(share_pos, binding_pos, copy_pos, post_copy_binding) < 0 or not (share_pos < binding_pos < copy_pos < post_copy_binding):
        errors.append("held package copy must use FileShare.Read and assert pathname binding before/after streaming copy")

    return errors


def main() -> int:
    if not TARGET.is_file():
        print("FAIL: missing scripts/package-v26.ps1")
        return 1
    source = TARGET.read_text(encoding="utf-8")
    failures = validate(source)

    mutation_tokens = (
        "function Open-HeldPackageInput",
        "[IO.FileShare]::Read",
        "function Copy-HeldPackageInput",
        "function Read-HeldPackageText",
        "function Invoke-WithHeldPackageInput",
        "function Open-HeldStagedManagedFile",
        "function Assert-HeldPathBinding",
        "Copy-HeldPackageInput -SourcePath $path",
        "Read-HeldPackageText -Held $held -Label 'V26 command source'",
        "Invoke-WithHeldPackageInput -Path $generator",
        "GetAssemblyName($heldPlugin.Path)",
        "GetAssemblyName($heldCore.Path)",
    )
    for token in mutation_tokens:
        if token not in source:
            failures.append(f"mutation source marker missing: {token}")
            continue
        mutated = source.replace(token, "MUTATED-V26-PACKAGE-GENERATION")
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
    print(" - generator and source-script inputs remain locked while transformed")
    print(" - staged plugin/Core identities are consumed while write/delete/replace is denied")
    return 0


if __name__ == "__main__":
    sys.exit(main())
