#!/usr/bin/env python3
import ntpath
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FINALIZER = ROOT / "scripts" / "finalize-v25-signed-package.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def identity_valid(metadata, plugin, core) -> bool:
    if metadata.get("product") != "QS3D" or metadata.get("target") != "BricsCAD V25 x64":
        return False
    version = str(metadata.get("version") or "").strip()
    product_version = str(metadata.get("productVersion") or "").strip()
    if not version or not product_version:
        return False
    for dll in (plugin, core):
        if dll.get("assemblyVersion") != version or dll.get("productVersion") != product_version:
            return False
    return True


def output_isolated(package_directory: str, package_zip: str) -> bool:
    package = ntpath.normcase(ntpath.normpath(package_directory)).rstrip("\\/")
    output = ntpath.normcase(ntpath.normpath(package_zip))
    if ntpath.splitext(output)[1].lower() != ".zip":
        return False
    package_root = package + "\\"
    return output != package and not output.startswith(package_root)


def main() -> int:
    if not FINALIZER.is_file():
        raise AssertionError("missing scripts/finalize-v25-signed-package.ps1")
    text = FINALIZER.read_text(encoding="utf-8")

    required_tokens = (
        "$SignedPayloadNames = @(",
        "$packagePath = [IO.Path]::GetFullPath($package).TrimEnd",
        "$packageRoot = $packagePath + [IO.Path]::DirectorySeparatorChar",
        "[IO.Path]::GetExtension($zip), '.zip', [StringComparison]::OrdinalIgnoreCase",
        "$zip.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)",
        "PackageZip must be outside PackageDirectory",
        "Assert-AuthenticodeSigner -Path $path -ExpectedSigner $expectedSigner",
        "PACKAGE-METADATA product must be QS3D.",
        "PACKAGE-METADATA target must be BricsCAD V25 x64.",
        "PACKAGE-METADATA is missing version.",
        "PACKAGE-METADATA is missing productVersion.",
        "[Version]::Parse([string]$metadata.version)",
        "$metadataProductVersion = ([string]$metadata.productVersion).Trim()",
        "$managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')",
        "Read-ManagedAssemblyVersion -Path $path -Label $name",
        "Read-ManagedProductVersion -Path $path -Label $name",
        "does not match signed $name assembly version",
        "does not match signed $name product version",
        "[StringComparison]::Ordinal",
        "$metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $metadataPath -Encoding UTF8",
        "Remove-Item -LiteralPath $hashManifest -Force",
        "Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -CompressionLevel Optimal",
    )
    for token in required_tokens:
        require(token in text, "signed finalizer guard missing token: " + token)

    identity_cases = (
        (
            {"product": "QS3D", "target": "BricsCAD V25 x64", "version": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            True,
            "canonical identity",
        ),
        (
            {"product": "OTHER", "target": "BricsCAD V25 x64", "version": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            False,
            "product substitution",
        ),
        (
            {"product": "QS3D", "target": "BricsCAD V26 x64", "version": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            False,
            "target substitution",
        ),
        (
            {"product": "QS3D", "target": "BricsCAD V25 x64", "version": "0.2.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            False,
            "assembly metadata substitution",
        ),
        (
            {"product": "QS3D", "target": "BricsCAD V25 x64", "version": "0.1.0.0", "productVersion": "0.1.0-preview.3"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            False,
            "product-version metadata substitution",
        ),
        (
            {"product": "QS3D", "target": "BricsCAD V25 x64", "version": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.2.0.0", "productVersion": "0.1.0-preview.2"},
            False,
            "Core assembly mismatch",
        ),
        (
            {"product": "QS3D", "target": "BricsCAD V25 x64", "version": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"},
            {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-PREVIEW.2"},
            False,
            "Core product-version case mismatch",
        ),
    )
    for metadata, plugin, core, expected, label in identity_cases:
        actual = identity_valid(metadata, plugin, core)
        require(actual is expected, f"signed finalizer identity model mismatch for {label}: expected {expected}, got {actual}")

    output_cases = (
        (r"C:\release\QS3D-BricsCAD-V25", r"C:\release\QS3D-BricsCAD-V25.zip", True, "sibling ZIP"),
        (r"C:\release\QS3D-BricsCAD-V25", r"C:\release\QS3D-BricsCAD-V25\release.zip", False, "nested ZIP"),
        (r"C:\release\QS3D-BricsCAD-V25", r"C:\release\QS3D-BricsCAD-V25\QS3D.Core.dll", False, "payload-file output"),
        (r"C:\release\QS3D-BricsCAD-V25", r"C:\release\output.bin", False, "non-ZIP output"),
        (r"C:\release\QS3D-BricsCAD-V25", r"C:\release\QS3D-BricsCAD-V25-copy\release.zip", True, "similarly-prefixed sibling tree"),
    )
    for package, output, expected, label in output_cases:
        actual = output_isolated(package, output)
        require(actual is expected, f"signed finalizer output model mismatch for {label}: expected {expected}, got {actual}")

    extension_guard_pos = text.find("[IO.Path]::GetExtension($zip), '.zip', [StringComparison]::OrdinalIgnoreCase")
    output_guard_pos = text.find("$zip.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)")
    signature_pos = text.find("Assert-AuthenticodeSigner -Path $path -ExpectedSigner $expectedSigner")
    product_pos = text.find("PACKAGE-METADATA product must be QS3D.")
    managed_loop_pos = text.find("$managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')")
    product_version_compare_pos = text.find("does not match signed $name product version")
    should_process_pos = text.find("if (-not $PSCmdlet.ShouldProcess($zip, 'Finalize signed QS3D V25 package and rebuild ZIP'))")
    metadata_write_pos = text.find("$metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $metadataPath -Encoding UTF8")
    hash_remove_pos = text.find("Remove-Item -LiteralPath $hashManifest -Force")
    output_remove_pos = text.find("Remove-Item -LiteralPath $zip -Force")
    zip_pos = text.find("Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -CompressionLevel Optimal")
    positions = (
        extension_guard_pos,
        output_guard_pos,
        signature_pos,
        product_pos,
        managed_loop_pos,
        product_version_compare_pos,
        should_process_pos,
        metadata_write_pos,
        hash_remove_pos,
        output_remove_pos,
        zip_pos,
    )
    require(min(positions) >= 0, "signed finalizer output/identity/publication ordering token is missing")
    require(
        extension_guard_pos < output_guard_pos < signature_pos < product_pos < managed_loop_pos < product_version_compare_pos < should_process_pos < metadata_write_pos < hash_remove_pos < output_remove_pos < zip_pos,
        "signed finalizer must isolate output, verify signatures/identity, then gate mutations before output cleanup/compression",
    )

    print(
        "PASS: signed V25 finalization requires an external .zip output, binds canonical product/target/version/productVersion to both signed managed DLLs, and performs output cleanup/compression only after isolation, signer, identity and ShouldProcess gates."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
