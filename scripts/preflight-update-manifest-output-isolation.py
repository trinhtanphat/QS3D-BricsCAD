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


def main() -> int:
    if not MANIFEST.is_file():
        raise AssertionError("missing scripts/new-v25-update-manifest.ps1")
    text = MANIFEST.read_text(encoding="utf-8")

    required_tokens = (
        "$packagePath = [IO.Path]::GetFullPath($package).TrimEnd",
        "$packageRoot = $packagePath + [IO.Path]::DirectorySeparatorChar",
        "$zipPath = [IO.Path]::GetFullPath($zip)",
        "$outputFull = [IO.Path]::GetFullPath($OutputPath)",
        "[IO.Path]::GetExtension($outputFull), '.json', [StringComparison]::OrdinalIgnoreCase",
        "$outputFull.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)",
        "OutputPath must be outside PackageDirectory",
        "OutputPath must not alias PackageZip.",
        "Assert-AuthenticodeSigner -Path (Join-Path $package $name)",
        "Assert-ZipPayloadMatchesSignedStaging -ZipPath $zip -PackageRoot $package",
        "Get-FileHash -LiteralPath $zip -Algorithm SHA256",
        "$PSCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')",
        "$manifest | ConvertTo-Json | Set-Content -LiteralPath $outputFull -Encoding UTF8",
    )
    for token in required_tokens:
        require(token in text, "update manifest output guard missing token: " + token)

    package = r"C:\release\QS3D-BricsCAD-V25"
    package_zip = r"C:\release\QS3D-BricsCAD-V25.zip"
    cases = (
        (r"C:\release\QS3D-BricsCAD-V25.update.json", True, "sibling manifest"),
        (r"D:\artifacts\QS3D.update.json", True, "external manifest"),
        (r"C:\release\QS3D-BricsCAD-V25\update.json", False, "nested staging manifest"),
        (r"C:\release\QS3D-BricsCAD-V25\PACKAGE-METADATA.json", False, "staged metadata alias"),
        (package_zip, False, "package ZIP alias"),
        (r"C:\release\QS3D-BricsCAD-V25.sha256", False, "non-JSON output"),
        (r"C:\release\QS3D-BricsCAD-V25-copy\update.json", True, "similarly-prefixed sibling tree"),
    )
    for output, expected, label in cases:
        actual = output_isolated(package, package_zip, output)
        require(actual is expected, f"manifest output isolation mismatch for {label}: expected {expected}, got {actual}")

    extension_guard = text.find("[IO.Path]::GetExtension($outputFull), '.json', [StringComparison]::OrdinalIgnoreCase")
    staging_guard = text.find("$outputFull.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)")
    zip_alias_guard = text.find("OutputPath must not alias PackageZip.")
    signer_check = text.find("Assert-AuthenticodeSigner -Path (Join-Path $package $name)")
    zip_binding = text.find("Assert-ZipPayloadMatchesSignedStaging -ZipPath $zip -PackageRoot $package")
    zip_hash = text.find("Get-FileHash -LiteralPath $zip -Algorithm SHA256")
    should_process = text.find("$PSCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')")
    output_write = text.find("$manifest | ConvertTo-Json | Set-Content -LiteralPath $outputFull -Encoding UTF8")
    positions = (extension_guard, staging_guard, zip_alias_guard, signer_check, zip_binding, zip_hash, should_process, output_write)
    require(min(positions) >= 0, "manifest output/verification ordering token is missing")
    require(
        extension_guard < staging_guard < zip_alias_guard < signer_check < zip_binding < zip_hash < should_process < output_write,
        "manifest output isolation must precede signer/ZIP verification and all output mutation",
    )

    print(
        "PASS: update manifest generation requires an external .json output distinct from the package ZIP before signed staging/ZIP verification and before manifest output mutation."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
