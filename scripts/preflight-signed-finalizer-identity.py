#!/usr/bin/env python3
import ntpath
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FINALIZER = ROOT / "scripts" / "finalize-v25-signed-package.ps1"


def check(ok: bool, message: str) -> None:
    if not ok:
        raise AssertionError(message)


def isolated(package: str, output: str) -> bool:
    package = ntpath.normcase(ntpath.normpath(package)).rstrip("\\/")
    output = ntpath.normcase(ntpath.normpath(output))
    return ntpath.splitext(output)[1].lower() == ".zip" and output != package and not output.startswith(package + "\\")


def first(text: str, tokens: tuple[str, ...]) -> int:
    values = [text.find(token) for token in tokens if text.find(token) >= 0]
    return min(values) if values else -1


def main() -> int:
    check(FINALIZER.is_file(), "missing scripts/finalize-v25-signed-package.ps1")
    text = FINALIZER.read_text(encoding="utf-8")

    package_tokens = (
        "$packagePath = Assert-SafeContainedDirectory -Path $PackageDirectory -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'",
        "$package = Assert-SafeContainedDirectory -Path $PackageDirectory -RepositoryRoot $repositoryRoot -Label 'PackageDirectory'",
    )
    root_tokens = (
        "$packageRoot = $packagePath + [IO.Path]::DirectorySeparatorChar",
        "$packageRoot = $package + [IO.Path]::DirectorySeparatorChar",
    )
    check(any(token in text for token in package_tokens), "missing contained package initialization")
    check(any(token in text for token in root_tokens), "missing isolated package-root initialization")

    common = (
        "$SignedPayloadNames = @(",
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
        "if (-not $PSCmdlet.ShouldProcess($zip, 'Finalize signed QS3D V25 package and rebuild ZIP'))",
    )
    for token in common:
        check(token in text, "signed finalizer guard missing token: " + token)

    for package, output, expected in (
        (r"C:\release\pkg", r"C:\release\pkg.zip", True),
        (r"C:\release\pkg", r"C:\release\pkg\nested.zip", False),
        (r"C:\release\pkg", r"C:\release\output.bin", False),
        (r"C:\release\pkg", r"C:\release\pkg-copy\x.zip", True),
    ):
        check(isolated(package, output) is expected, "signed finalizer output-isolation model drift")

    package = first(text, package_tokens)
    extension = text.find("[IO.Path]::GetExtension($zip), '.zip', [StringComparison]::OrdinalIgnoreCase")
    outside = text.find("$zip.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)")
    signer = text.find("Assert-AuthenticodeSigner -Path $path -ExpectedSigner $expectedSigner")
    product = text.find("PACKAGE-METADATA product must be QS3D.")
    managed = text.find("$managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')")
    product_version = text.find("does not match signed $name product version")
    approval = text.find("if (-not $PSCmdlet.ShouldProcess($zip, 'Finalize signed QS3D V25 package and rebuild ZIP'))")
    identity_order = (package, extension, outside, signer, product, managed, product_version, approval)
    check(min(identity_order) >= 0 and list(identity_order) == sorted(identity_order), "containment/signer/managed identity must all precede approval")

    atomic_tokens = (
        "$metadataStage = New-SiblingTempPath",
        "Write-Utf8NoBomText -Path $metadataStage",
        "[IO.File]::Replace($metadataStage, $metadataPath, $metadataBackup, $true)",
        "[IO.File]::Move($hashManifest, $manifestBackup)",
        "[IO.File]::WriteAllLines($manifestStage",
        "[IO.File]::Move($manifestStage, $hashManifest)",
        "Compress-Archive -Path (Join-Path $package '*') -DestinationPath $tempZip -CompressionLevel Optimal",
        "Assert-ZipMatchesPackage -ZipPath $tempZip -PackageRoot $package",
        "$stagedZipHash = Assert-ZipManifestIntegrity -ZipPath $tempZip",
        "$heldZip = Open-HeldVerifiedZipGeneration -Path $tempZip -ExpectedSha256 $stagedZipHash",
        "Publish-HeldVerifiedZipGeneration -HeldZip $heldZip -TargetPath $zip -ReplaceIfExists $zipExistedBeforePublish",
        "[QS3DV25HeldZipPublication]::SetFileInformationByHandleFileRenameInfo(",
        "$installedZipHash = [string]$heldZip.Sha256",
        "$transactionCommitted = $true",
        "restore original manifest",
        "restore original metadata",
        "Rollback also failed",
    )
    for token in atomic_tokens:
        check(token in text, "atomic signed-finalizer contract missing: " + token)

    for forbidden in (
        "[IO.File]::Replace($tempZip, $zip, $zipBackup, $true)",
        "[IO.File]::Move($tempZip, $zip)",
        "Get-FileHash -LiteralPath $zip -Algorithm SHA256",
    ):
        check(forbidden not in text, "superseded pathname publication/identity token remains: " + forbidden)

    zip_remove = text.find("Remove-Item -LiteralPath $zip -Force")
    rollback_marker = text.find("$originalError = $_")
    check(zip_remove < 0 or (rollback_marker >= 0 and rollback_marker < zip_remove), "atomic publication may delete PackageZip only during rollback")

    ordered = (
        approval,
        text.find("$metadataStage = New-SiblingTempPath"),
        text.find("[IO.File]::Replace($metadataStage, $metadataPath, $metadataBackup, $true)"),
        text.find("[IO.File]::Move($hashManifest, $manifestBackup)"),
        text.find("[IO.File]::Move($manifestStage, $hashManifest)"),
        text.find("Compress-Archive -Path (Join-Path $package '*') -DestinationPath $tempZip -CompressionLevel Optimal"),
        text.find("Assert-ZipMatchesPackage -ZipPath $tempZip -PackageRoot $package"),
        text.find("$stagedZipHash = Assert-ZipManifestIntegrity -ZipPath $tempZip"),
        text.find("$heldZip = Open-HeldVerifiedZipGeneration -Path $tempZip -ExpectedSha256 $stagedZipHash"),
        text.find("Publish-HeldVerifiedZipGeneration -HeldZip $heldZip -TargetPath $zip -ReplaceIfExists $zipExistedBeforePublish"),
        text.find("$transactionCommitted = $true"),
    )
    check(min(ordered) >= 0 and list(ordered) == sorted(ordered), "atomic held-generation publication order drift")

    print("PASS: signed V25 finalizer identity/containment precedes failure-atomic held-generation publication")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
