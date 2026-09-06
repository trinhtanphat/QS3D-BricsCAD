#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "assert-v25-release-package-identity.ps1"


def fail(message: str) -> None:
    raise SystemExit(f"preflight-v25-release-package-held-product-version: {message}")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"missing {label}: {token}")


def validate(text: str) -> None:
    informational_lookup = "$_.AttributeType.FullName -eq 'System.Reflection.AssemblyInformationalVersionAttribute'"
    require(text, informational_lookup, "held informational-version attribute lookup")
    require(text, "ProductVersion = $informationalVersion", "held ProductVersion identity result")
    require(text, "$pluginIdentity.ProductVersion", "plugin ProductVersion comparison")
    require(text, "$coreIdentity.ProductVersion", "Core ProductVersion comparison")
    require(text, "does not match package productVersion", "ProductVersion mismatch failure")
    require(text, "PluginProductVersion = $pluginIdentity.ProductVersion", "plugin ProductVersion evidence")
    require(text, "CoreProductVersion = $coreIdentity.ProductVersion", "Core ProductVersion evidence")
    require(text, "ReflectionOnlyLoad($bytes)", "exact-held-byte reflection-only inspection")
    require(text, "GetCustomAttributesData()", "non-executing informational-version metadata read")

    forbidden = (
        "[Diagnostics.FileVersionInfo]::GetVersionInfo($Held.Path)",
        "[Diagnostics.FileVersionInfo]::GetVersionInfo($pluginHeld.Path)",
        "[Diagnostics.FileVersionInfo]::GetVersionInfo($coreHeld.Path)",
        "[Reflection.AssemblyName]::GetAssemblyName($Held.Path)",
        "[Reflection.Assembly]::LoadFile($Held.Path)",
        "[Reflection.Assembly]::LoadFrom($Held.Path)",
    )
    for token in forbidden:
        if token in text:
            fail(f"pathname semantic reopen or executable load is forbidden: {token}")

    assembly_check = "if ($pluginIdentity.AssemblyVersion -ne $packageVersion -or $coreIdentity.AssemblyVersion -ne $packageVersion)"
    product_check = "if (-not [string]::Equals($pluginIdentity.ProductVersion, $productVersion, [StringComparison]::Ordinal) -or"
    core_product_check = "-not [string]::Equals($coreIdentity.ProductVersion, $productVersion, [StringComparison]::Ordinal))"
    require(text, assembly_check, "managed AssemblyVersion equality")
    require(text, product_check, "plugin ProductVersion equality")
    require(text, core_product_check, "Core ProductVersion equality")

    # Mutation/adversarial controls: every identity limb must be independently necessary.
    mutations = {
        "informational attribute lookup removed": text.replace(
            informational_lookup,
            "$_.AttributeType.FullName -eq 'System.Reflection.AssemblyTitleAttribute'",
            1,
        ),
        "plugin product equality removed": text.replace(product_check, "if ($false -or", 1),
        "core product equality removed": text.replace(core_product_check, "$false)", 1),
        "held-byte inspection removed": text.replace("ReflectionOnlyLoad($bytes)", "ReflectionOnlyLoad([byte[]]::new(0))", 1),
    }
    for label, mutant in mutations.items():
        if mutant == text:
            fail(f"mutation fixture did not alter source: {label}")
        try:
            validate_without_mutations(mutant)
        except SystemExit:
            continue
        fail(f"mutation unexpectedly passed: {label}")


def validate_without_mutations(text: str) -> None:
    require(
        text,
        "$_.AttributeType.FullName -eq 'System.Reflection.AssemblyInformationalVersionAttribute'",
        "held informational-version attribute lookup",
    )
    require(text, "ReflectionOnlyLoad($bytes)", "exact-held-byte reflection-only inspection")
    require(text, "if (-not [string]::Equals($pluginIdentity.ProductVersion, $productVersion, [StringComparison]::Ordinal) -or", "plugin ProductVersion equality")
    require(text, "-not [string]::Equals($coreIdentity.ProductVersion, $productVersion, [StringComparison]::Ordinal))", "Core ProductVersion equality")


def main() -> None:
    text = TARGET.read_text(encoding="utf-8")
    validate(text)
    print("PASS V25 release package held ProductVersion identity guard")


if __name__ == "__main__":
    main()
