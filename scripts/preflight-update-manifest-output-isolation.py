#!/usr/bin/env python3
import ntpath
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "scripts" / "new-v25-update-manifest.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def output_isolated(package_directory: str, package_zip: str, output_path: str) -> bool:
    package = ntpath.normcase(ntpath.normpath(package_directory)).rstrip("\\/")
    package_root = package + "\\"
    package_zip = ntpath.normcase(ntpath.normpath(package_zip))
    output = ntpath.normcase(ntpath.normpath(output_path))
    if ntpath.splitext(output)[1].lower() != ".json":
        return False
    if output == package or output.startswith(package_root):
        return False
    if output == package_zip:
        return False
    return True


def managed_identity_valid(metadata_version: str, metadata_product_version: str, plugin: dict, core: dict) -> bool:
    for dll in (plugin, core):
        if dll.get("assemblyVersion") != metadata_version:
            return False
        if dll.get("productVersion") != metadata_product_version:
            return False
    return True


def main() -> int:
    if not MANIFEST.is_file():
        raise AssertionError("missing scripts/new-v25-update-manifest.ps1")
    text = MANIFEST.read_text(encoding="utf-8")

    required_tokens = (
        "$package = Resolve-OrdinaryNonReparseDirectory -Path $PackageDirectory",
        "$packagePath = $package.FullName.TrimEnd",
        "$packageRoot = $packagePath + [IO.Path]::DirectorySeparatorChar",
        "$zip = Resolve-OrdinaryNonReparseFile -Path $PackageZip",
        "$zipPath = $zip.FullName",
        "$outputFull = [IO.Path]::GetFullPath($OutputPath)",
        "[IO.Path]::GetExtension($outputFull), '.json', [StringComparison]::OrdinalIgnoreCase",
        "$outputFull.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)",
        "OutputPath must be outside PackageDirectory",
        "OutputPath must not alias PackageZip.",
        "$metadataState = Get-StableFileState",
        "$zipState = Get-StableFileState",
        "$payloadFiles[$name] = Resolve-OrdinaryNonReparseFile",
        "$payloadStates[$name] = Get-StableFileState",
        "Assert-AuthenticodeSigner -Path $payloadFiles[$name].FullName",
        "$managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')",
        "Read-ManagedAssemblyVersion -Path $path -Label $name",
        "Read-ManagedProductVersion -Path $payloadFiles[$name].FullName -Label $name",
        "does not match signed $name assembly version",
        "does not match signed $name product version",
        "[StringComparison]::Ordinal",
        "Assert-ZipPayloadMatchesSignedStaging -ZipFile $zip -PackageRoot $package",
        "$zip = Assert-StableFileState -Expected $zipState",
        "$zipHash = [string]$zipState.Sha256",
        "$PSCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')",
        "[IO.File]::WriteAllText($stagePath",
        "[IO.File]::Replace($stage.FullName, $outputFull, $backupPath, $true)",
        "[IO.File]::Move($stage.FullName, $outputFull)",
    )
    for token in required_tokens:
        require(token in text, "update manifest guard missing token: " + token)

    for forbidden in (
        "Assert-AuthenticodeSigner -Path (Join-Path $package $name)",
        "Assert-ZipPayloadMatchesSignedStaging -ZipPath $zip -PackageRoot $package",
        "Get-FileHash -LiteralPath $zip -Algorithm SHA256",
        "Get-FileHash -LiteralPath $zip.FullName -Algorithm SHA256",
        "$manifest | ConvertTo-Json | Set-Content -LiteralPath $outputFull -Encoding UTF8",
    ):
        require(forbidden not in text, "update manifest retained unsafe/legacy routing token: " + forbidden)

    package = r"C:\release\QS3D-BricsCAD-V25"
    package_zip = r"C:\release\QS3D-BricsCAD-V25.zip"
    output_cases = (
        (r"C:\release\QS3D-BricsCAD-V25.update.json", True, "sibling manifest"),
        (r"D:\artifacts\QS3D.update.json", True, "external manifest"),
        (r"C:\release\QS3D-BricsCAD-V25\update.json", False, "nested staging manifest"),
        (r"C:\release\QS3D-BricsCAD-V25\PACKAGE-METADATA.json", False, "staged metadata alias"),
        (package_zip, False, "package ZIP alias"),
        (r"C:\release\QS3D-BricsCAD-V25.sha256", False, "non-JSON output"),
        (r"C:\release\QS3D-BricsCAD-V25-copy\update.json", True, "similarly-prefixed sibling tree"),
    )
    for output, expected, label in output_cases:
        actual = output_isolated(package, package_zip, output)
        require(actual is expected, f"manifest output isolation mismatch for {label}: expected {expected}, got {actual}")

    identity_cases = (
        ("0.1.0.0", "0.1.0-preview.2", {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"}, {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"}, True, "canonical plugin/Core identity"),
        ("0.1.0.0", "0.1.0-preview.2", {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"}, {"assemblyVersion": "0.2.0.0", "productVersion": "0.1.0-preview.2"}, False, "Core assembly mismatch"),
        ("0.1.0.0", "0.1.0-preview.2", {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"}, {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.3"}, False, "Core productVersion mismatch"),
        ("0.1.0.0", "0.1.0-preview.2", {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-PREVIEW.2"}, {"assemblyVersion": "0.1.0.0", "productVersion": "0.1.0-preview.2"}, False, "plugin productVersion case mismatch"),
    )
    for version, product_version, plugin, core, expected, label in identity_cases:
        actual = managed_identity_valid(version, product_version, plugin, core)
        require(actual is expected, f"manifest managed identity mismatch for {label}: expected {expected}, got {actual}")

    package_guard = text.find("$package = Resolve-OrdinaryNonReparseDirectory -Path $PackageDirectory")
    zip_guard = text.find("$zip = Resolve-OrdinaryNonReparseFile -Path $PackageZip")
    extension_guard = text.find("[IO.Path]::GetExtension($outputFull), '.json', [StringComparison]::OrdinalIgnoreCase")
    staging_guard = text.find("$outputFull.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)")
    zip_alias_guard = text.find("OutputPath must not alias PackageZip.")
    metadata_state = text.find("$metadataState = Get-StableFileState")
    zip_state = text.find("$zipState = Get-StableFileState")
    payload_guard = text.find("$payloadFiles[$name] = Resolve-OrdinaryNonReparseFile")
    payload_state = text.find("$payloadStates[$name] = Get-StableFileState")
    signer_check = text.find("Assert-AuthenticodeSigner -Path $payloadFiles[$name].FullName")
    managed_loop = text.find("$managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')")
    product_compare = text.find("does not match signed $name product version")
    zip_binding = text.find("Assert-ZipPayloadMatchesSignedStaging -ZipFile $zip -PackageRoot $package")
    zip_recheck = text.find("$zip = Assert-StableFileState -Expected $zipState", zip_binding)
    zip_hash = text.find("$zipHash = [string]$zipState.Sha256", zip_recheck)
    manifest_create = text.find("$manifest = [ordered]@{")
    should_process = text.find("$PSCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')")
    stage_write = text.find("[IO.File]::WriteAllText($stagePath")
    positions = (package_guard, zip_guard, extension_guard, staging_guard, zip_alias_guard, metadata_state, zip_state, payload_guard, payload_state, signer_check, managed_loop, product_compare, zip_binding, zip_recheck, zip_hash, manifest_create, should_process, stage_write)
    require(min(positions) >= 0, "manifest output/identity/verification ordering token is missing")
    require(
        package_guard < zip_guard < extension_guard < staging_guard < zip_alias_guard < metadata_state < zip_state < payload_guard < payload_state < signer_check < managed_loop < product_compare < zip_binding < zip_recheck < zip_hash < manifest_create < should_process < stage_write,
        "manifest path/output isolation and both managed identities must precede state-bound ZIP verification/hash derivation and atomic manifest publication",
    )

    print("PASS: update manifest generation requires ordinary non-reparse package inputs, isolated external JSON output, stable input generations and exact metadata identity across both signed managed DLLs before ZIP/staging verification and atomic publication.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
