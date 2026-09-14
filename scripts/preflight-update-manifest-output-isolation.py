#!/usr/bin/env python3
import ntpath
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WRAPPER = ROOT / "scripts" / "new-v25-update-manifest.ps1"
VALIDATION_CORE = ROOT / "scripts" / "new-v25-update-manifest-validation-core.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def read(path: Path, label: str) -> str:
    require(path.is_file(), f"missing {label}: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


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
    wrapper = read(WRAPPER, "V25 update-manifest wrapper")
    validation = read(VALIDATION_CORE, "V25 update-manifest validation core")

    required_validation_tokens = (
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
    )
    for token in required_validation_tokens:
        require(token in validation, "update manifest validation guard missing token: " + token)

    for forbidden in (
        "Assert-AuthenticodeSigner -Path (Join-Path $package $name)",
        "Assert-ZipPayloadMatchesSignedStaging -ZipPath $zip -PackageRoot $package",
        "Get-FileHash -LiteralPath $zip -Algorithm SHA256",
        "Get-FileHash -LiteralPath $zip.FullName -Algorithm SHA256",
        "$manifest | ConvertTo-Json | Set-Content -LiteralPath $outputFull -Encoding UTF8",
    ):
        require(forbidden not in validation, "validation core retained unsafe/legacy routing token: " + forbidden)

    # The validation core may contain its legacy publisher, but the public wrapper must
    # invoke it only under -WhatIf and then publish through held-generation authority.
    for token in (
        "$validationCorePath = Join-Path $PSScriptRoot 'new-v25-update-manifest-validation-core.ps1'",
        ". $validationCorePath",
        "-WhatIf 6>$null",
        "$wrapperCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')",
        "OpenOwnedDirectory($preOutputParentPath)",
        "OpenOwnedExisting($preOutputFull)",
        "PublishOwnedGenerationInDirectory",
        "RollbackOwnedGenerationInDirectory",
        "ReadOwnedGenerationBytes($stageOwned",
    ):
        require(token in wrapper, "split wrapper missing publication/delegation token: " + token)
    for forbidden in (
        "[IO.File]::WriteAllText($stagePath",
        "[IO.File]::Replace($stage.FullName, $outputFull",
        "[IO.File]::Move($stage.FullName, $outputFull",
        "& $validationCorePath",
    ):
        require(forbidden not in wrapper, "public wrapper retained pathname/unsafe delegation token: " + forbidden)

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

    # Ordering is now checked inside the validation owner; publication authority is
    # separately checked in the public wrapper rather than by concatenating files.
    ordered = (
        "$package = Resolve-OrdinaryNonReparseDirectory -Path $PackageDirectory",
        "$zip = Resolve-OrdinaryNonReparseFile -Path $PackageZip",
        "[IO.Path]::GetExtension($outputFull), '.json', [StringComparison]::OrdinalIgnoreCase",
        "$outputFull.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)",
        "OutputPath must not alias PackageZip.",
        "$metadataState = Get-StableFileState",
        "$zipState = Get-StableFileState",
        "$payloadFiles[$name] = Resolve-OrdinaryNonReparseFile",
        "$payloadStates[$name] = Get-StableFileState",
        "Assert-AuthenticodeSigner -Path $payloadFiles[$name].FullName",
        "$managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')",
        "does not match signed $name product version",
        "Assert-ZipPayloadMatchesSignedStaging -ZipFile $zip -PackageRoot $package",
        "$zip = Assert-StableFileState -Expected $zipState",
        "$zipHash = [string]$zipState.Sha256",
        "$manifest = [ordered]@{",
        "$PSCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')",
    )
    positions = [validation.find(token) for token in ordered]
    require(min(positions) >= 0 and positions == sorted(positions), "validation core path/output isolation and both managed identities must precede state-bound ZIP verification/hash derivation and any legacy publisher")

    validation_call = wrapper.find(". $validationCorePath")
    should_process = wrapper.find("$wrapperCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')")
    stage_create = wrapper.find("OpenOwnedStaging($stagePath)")
    publish = wrapper.find("PublishOwnedGenerationInDirectory")
    verify = wrapper.find("ReadOwnedGenerationBytes($stageOwned", publish)
    require(min(validation_call, should_process, stage_create, publish, verify) >= 0 and validation_call < should_process < stage_create < publish < verify,
            "wrapper must complete non-publishing validation before held-generation staging, publication and same-generation verification")

    print("PASS: split update-manifest validation requires isolated external JSON output and stable managed identities before wrapper-owned held-generation publication.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
