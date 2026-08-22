#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INSTALLER = ROOT / "scripts" / "install-v25-autoload.ps1"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing installer source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def replacement_allowed(*, exists: bool, force: bool, is_directory: bool, qs3d_identity: bool) -> bool:
    if not exists:
        return True
    if not force:
        return False
    return is_directory and qs3d_identity


def public_semver_identity(value: str) -> str:
    return value.split("+", 1)[0]


def package_product_versions_allowed(metadata_version: str, dll_versions: tuple[str, ...]) -> bool:
    if not dll_versions:
        return False
    metadata_public = public_semver_identity(metadata_version)
    return (
        all(public_semver_identity(version) == metadata_public for version in dll_versions)
        and len(set(dll_versions)) == 1
    )


def main() -> int:
    installer = read(INSTALLER)

    require(installer, "function Convert-ToStrictSemVerIdentity", "strict product SemVer parser")
    require(installer, "function Assert-PackageIdentity", "package identity boundary")
    require(installer, "PACKAGE-METADATA product must be QS3D", "product binding")
    require(installer, "PACKAGE-METADATA target must be BricsCAD V25 x64", "target binding")
    require(installer, "PACKAGE-METADATA is missing version", "assembly metadata requirement")
    require(installer, "PACKAGE-METADATA is missing productVersion", "product-version metadata requirement")
    require(installer, "[Version]::Parse([string]$metadata.version)", "metadata AssemblyVersion parse")
    require(installer, "Convert-ToStrictSemVerIdentity -Value ([string]$metadata.productVersion)", "metadata strict SemVer parse")
    require(installer, "$metadataAssemblyVersion.Build -ne $metadataProductVersion.Patch", "assembly/product core binding")

    require(installer, "$text.IndexOf('+')", "SemVer build-metadata boundary")
    require(installer, "PublicText = $publicText", "public SemVer identity")
    require(installer, "$dllProductVersion.PublicText", "DLL public product identity")
    require(installer, "$metadataProductVersion.PublicText", "metadata public product identity")
    require(installer, "$fullDllProductVersion = $null", "full DLL ProductVersion provenance baseline")
    require(installer, "Package managed DLL product versions disagree", "mixed-revision DLL refusal")
    if "$dllProductVersion.Text, $metadataProductVersion.Text" in installer:
        raise AssertionError("installer must not compare full DLL ProductVersion directly to canonical PACKAGE-METADATA productVersion")

    reported = "0.1.0-preview.3+a99038557bfeab6fd8945cb28a0f890c46480184"
    cases = (
        ("0.1.0-preview.3", (reported, reported), True, "owner-reported SDK source revision metadata"),
        ("0.1.0-preview.3", ("0.1.0-preview.3", "0.1.0-preview.3"), True, "no build metadata"),
        ("0.1.0-preview.3", ("0.1.0-preview.4+abc", "0.1.0-preview.4+abc"), False, "public prerelease mismatch"),
        ("0.1.0-preview.3", ("0.1.0-preview.3+aaa", "0.1.0-preview.3+bbb"), False, "mixed source revision metadata"),
    )
    for metadata_version, dll_versions, expected, label in cases:
        actual = package_product_versions_allowed(metadata_version, dll_versions)
        if actual is not expected:
            raise AssertionError(f"product-version policy model mismatch for {label}: expected {expected}, got {actual}")

    require(installer, "foreach ($name in @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll'))", "both managed DLL identity checks")
    require(installer, "[Reflection.AssemblyName]::GetAssemblyName($path).Version", "DLL AssemblyVersion read")
    require(installer, "$assemblyVersion -ne $metadataAssemblyVersion", "DLL/metadata AssemblyVersion equality")
    require(installer, "[Diagnostics.FileVersionInfo]::GetVersionInfo($path).ProductVersion", "DLL ProductVersion read")
    require(installer, "does not match PACKAGE-METADATA productVersion", "DLL/metadata public ProductVersion equality")

    # A forced upgrade must prove the existing destructive target is QS3D-owned.
    require(installer, "function Assert-ExistingInstallDirectorySafeToReplace", "existing install replacement boundary")
    require(installer, "if (-not (Test-Path -LiteralPath $Directory -PathType Container))", "non-directory replacement refusal")
    require(installer, "Assert-PackageIdentity -Directory $Directory", "existing target QS3D identity validation")
    require(installer, "Refusing to replace existing InstallDirectory because it is not a valid QS3D V25 installation", "foreign directory refusal")
    require(installer, "Assert-ExistingInstallDirectorySafeToReplace -Directory $installFull", "replacement guard invocation")

    policy_cases = (
        ({"exists": False, "force": False, "is_directory": False, "qs3d_identity": False}, True, "first install"),
        ({"exists": True, "force": False, "is_directory": True, "qs3d_identity": True}, False, "existing install without force"),
        ({"exists": True, "force": True, "is_directory": False, "qs3d_identity": True}, False, "forced file target"),
        ({"exists": True, "force": True, "is_directory": True, "qs3d_identity": False}, False, "forced foreign directory"),
        ({"exists": True, "force": True, "is_directory": True, "qs3d_identity": True}, True, "forced verified QS3D replacement"),
    )
    for kwargs, expected, label in policy_cases:
        actual = replacement_allowed(**kwargs)
        if actual is not expected:
            raise AssertionError(f"replacement policy model mismatch for {label}: expected {expected}, got {actual}")

    integrity_call = installer.find("$commands = Assert-PackageIntegrity -Directory $package")
    identity_call = installer.find("Assert-PackageIdentity -Directory $package")
    targets_call = installer.find("$targets = @(Get-RegistryTargets")
    stage_create = installer.find("New-Item -ItemType Directory -Path $stage")
    replacement_guard = installer.find("Assert-ExistingInstallDirectorySafeToReplace -Directory $installFull")
    backup_assign = installer.find("$backup = $installFull + '.backup-'")
    backup_move = installer.find("Move-Item -LiteralPath $installFull -Destination $backup")
    registry_write = installer.find("New-Item -Path $target.AppKey -Force")
    positions = (
        integrity_call,
        identity_call,
        targets_call,
        stage_create,
        replacement_guard,
        backup_assign,
        backup_move,
        registry_write,
    )
    if min(positions) < 0 or not (
        integrity_call < identity_call < targets_call < stage_create < replacement_guard < backup_assign < backup_move < registry_write
    ):
        raise AssertionError(
            "hash/signature integrity -> source package identity -> target discovery/staging -> existing target identity -> backup move -> registry mutation ordering is required"
        )

    # Preserve the existing install security/transaction contracts.
    require(installer, "SHA256SUMS.txt", "hash manifest verification")
    require(installer, "Assert-AuthenticodeSigner", "optional/required publisher verification")
    require(installer, "Get-RunningBricsCADProcessDetails", "running BricsCAD refusal diagnostics")
    require(installer, "Close all BricsCAD processes before installing or upgrading QS3D", "running host refusal")
    require(installer, "Unblock-File -LiteralPath $destination -ErrorAction Stop", "MOTW clearing after verified copy")
    require(installer, "Get-DemandLoadSnapshot", "registry rollback snapshot")
    require(installer, "Restore-DemandLoadSnapshot", "registry rollback restore")
    require(installer, "Assert-DemandLoadRegistration", "post-write DemandLoad readback")
    require(installer, "$backup = $installFull + '.backup-'", "payload rollback backup")
    require(installer, "throw $originalError", "original install failure propagation")

    print(
        "PASS: V25 installer accepts SDK SemVer build metadata only when public product identity matches, rejects mixed managed-DLL revisions, and preserves hashed/signed transactional install safeguards."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
