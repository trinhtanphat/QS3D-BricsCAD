#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "scripts/update-v25.ps1",
    "scripts/new-v25-update-manifest.ps1",
    "scripts/finalize-v25-signed-package.ps1",
    "scripts/install-v25-autoload.ps1",
    "scripts/uninstall-v25-autoload.ps1",
    "scripts/package-v25.ps1",
    "scripts/sign-v25.ps1",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing updater/release file: " + relative)

signed_payload_tokens = [
    "QS3D.BricsCAD.V25.dll",
    "QS3D.Core.dll",
    "install-v25-autoload.ps1",
    "uninstall-v25-autoload.ps1",
    "update-v25.ps1",
]


def read(relative):
    path = ROOT / relative
    return path.read_text(encoding="utf-8") if path.is_file() else ""


def require(text, token, label):
    if token not in text:
        errors.append(label + " missing updater guard/token: " + token)


updater = read("scripts/update-v25.ps1")
manifest = read("scripts/new-v25-update-manifest.ps1")
finalizer = read("scripts/finalize-v25-signed-package.ps1")
installer = read("scripts/install-v25-autoload.ps1")
uninstaller = read("scripts/uninstall-v25-autoload.ps1")
package = read("scripts/package-v25.ps1")
signer = read("scripts/sign-v25.ps1")

for label, text in (
    ("scripts/update-v25.ps1", updater),
    ("scripts/new-v25-update-manifest.ps1", manifest),
    ("scripts/finalize-v25-signed-package.ps1", finalizer),
    ("scripts/install-v25-autoload.ps1", installer),
):
    for token in signed_payload_tokens:
        require(text, token, label + " signed payload")

for token in (
    "[ValidatePattern('^https://')]",
    "ExpectedSignerThumbprint",
    "AllowedPackageHost",
    "MaxPackageSizeMB",
    "MaxExpandedPackageSizeMB",
    "MaxArchiveEntries",
    "function Invoke-BoundedHttpsDownload",
    "Invoke-BoundedHttpsDownload -Address $manifestAddress",
    "Invoke-BoundedHttpsDownload -Address $packageAddress",
    "embedded credentials",
    "function Read-InstalledVersion",
    "function Read-InstalledProductVersion",
    "function Convert-ToStrictSemVer",
    "function Compare-StrictSemVer",
    "schemaVersion -ne 2",
    "Refusing product-version downgrade",
    "Assert-SafeArchive",
    "Assert-PackageRoot",
    "Assert-AuthenticodeSigner",
    "Read-SignedPluginVersion",
    "Downloaded PACKAGE-METADATA.json is missing productVersion",
    "does not match signed plugin product version",
    "does not match manifest productVersion",
    "Installed QS3D productVersion changed during update preparation",
    "SHA256SUMS.txt",
    "Unsafe SHA256SUMS entry",
    "RequireSigned = $true",
    "ExpectedSignerThumbprint = $expectedSigner",
    "Get-Process -Name bricscad",
    "$updateMutex = Enter-Qs3dUpdateMutex",
    "Exit-Qs3dUpdateMutex -Mutex $updateMutex",
    "Remove-Item -LiteralPath $tempRoot",
):
    require(updater, token, "scripts/update-v25.ps1")

for token in (
    "PackageUri",
    "ExpectedSignerThumbprint",
    "function Convert-ToStrictSemVerText",
    "function Read-ManagedAssemblyVersion",
    "function Read-ManagedProductVersion",
    "function Get-StreamingSha256",
    "function Get-StableFileState",
    "function Assert-StableFileState",
    "managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')",
    "PACKAGE-METADATA is missing productVersion",
    "Resolve-OrdinaryNonReparseDirectory",
    "Resolve-OrdinaryNonReparseFile",
    "Read-BoundedStrictUtf8File",
    "$metadataState = Get-StableFileState",
    "$zipState = Get-StableFileState",
    "$payloadFiles[$name] = Resolve-OrdinaryNonReparseFile",
    "$payloadStates[$name] = Get-StableFileState",
    "Assert-AuthenticodeSigner -Path $payloadFiles[$name].FullName",
    "Assert-ZipPayloadMatchesSignedStaging -ZipFile $zip -PackageRoot $package",
    "Package ZIP payload does not match signed staging file",
    "$zip = Assert-StableFileState -Expected $zipState",
    "$zipHash = [string]$zipState.Sha256",
    "schemaVersion = 2",
    "productVersion = $signedPluginProductVersion",
    "signerThumbprint = $expectedSigner",
    "[IO.File]::WriteAllText($stagePath",
    "[IO.File]::Replace($stage.FullName, $outputFull, $backupPath, $true)",
):
    require(manifest, token, "scripts/new-v25-update-manifest.ps1")
if "schemaVersion = 1" in manifest:
    errors.append("new-v25-update-manifest.ps1 must not regress to legacy schemaVersion 1")
if "Get-FileHash -LiteralPath $zip.FullName -Algorithm SHA256" in manifest:
    errors.append("new-v25-update-manifest.ps1 must derive the published ZIP hash from its admitted stable state, not reopen the ZIP through Get-FileHash")

for token in (
    "ExpectedSignerThumbprint",
    "Assert-AuthenticodeSigner",
    "function Read-ManagedAssemblyVersion",
    "function Read-ManagedProductVersion",
    "managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')",
    "PACKAGE-METADATA productVersion",
    "signedExecutablePayload",
    "signedPayloadSignerThumbprint",
    "SHA256SUMS.txt",
    "Compress-Archive",
    "ZIP SHA256",
):
    require(finalizer, token, "scripts/finalize-v25-signed-package.ps1")

for token in (
    "ExpectedSignerThumbprint",
    "Assert-PackageIntegrity",
    "Assert-PackageIdentity",
    "Get-DemandLoadSnapshot",
    "Restore-DemandLoadSnapshot",
    "$registrySnapshots",
    ".qs3d-stage-",
    ".backup-",
    "$payloadCommitted",
    "$originalError",
    "$rollbackFailures",
    "for ($index = $registrySnapshots.Count - 1; $index -ge 0; $index--)",
    "Move-Item -LiteralPath $backup -Destination $installFull",
    "elseif ($payloadCommitted -and (Test-Path -LiteralPath $installFull))",
    "throw $originalError",
    "$updateMutex = Enter-Qs3dUpdateMutex",
    "Exit-Qs3dUpdateMutex -Mutex $updateMutex",
):
    require(installer, token, "scripts/install-v25-autoload.ps1")

for token in (
    "Assert-InstallDirectorySafeToRemove",
    "PACKAGE-METADATA.json",
    "QS3D.BricsCAD.V25.dll",
    "BricsCAD V25 x64",
    "$registryPlan = @()",
    "Get-RegistryTreeSnapshot",
    "Restore-RegistryTreeSnapshot",
    ".qs3d-uninstall-",
    "Move-Item -LiteralPath $installFull -Destination $quarantine -ErrorAction Stop",
    "Move-Item -LiteralPath $quarantine -Destination $installFull -ErrorAction Stop",
    "Remove-Item -LiteralPath $entry.Target.AppKey -Recurse -Force -ErrorAction Stop",
    "throw $originalError",
    "$updateMutex = Enter-Qs3dUpdateMutex",
    "Exit-Qs3dUpdateMutex -Mutex $updateMutex",
):
    require(uninstaller, token, "scripts/uninstall-v25-autoload.ps1")

for token in (
    "update-v25.ps1",
    "[Reflection.AssemblyName]::GetAssemblyName",
    "version = $assemblyVersion.ToString()",
    "pluginSignerThumbprint",
    "Installer/updater never weaken BricsCAD security settings.",
):
    require(package, token, "scripts/package-v25.ps1")
for token in ("HashAlgorithm SHA256", "TimestampServer", "Get-AuthenticodeSignature", "Code Signing", "'.ps1'", "'.dll'"):
    require(signer, token, "scripts/sign-v25.ps1")

for label, text in (("updater", updater), ("installer", installer), ("uninstaller", uninstaller)):
    lower = text.lower()
    for token in ("-skipcertificatecheck", "trustallcertificates", "certificatepolicy", "executionpolicy bypass"):
        if token in lower:
            errors.append(label + " contains forbidden insecure token: " + token)
    if "Stop-Process" in text or "taskkill" in text or ".Kill(" in text:
        errors.append(label + " must never force-terminate BricsCAD/processes")

installed_state = updater.find("$installedVersion = Read-InstalledVersion -Directory $InstallDirectory")
product_state = updater.find("$installedProductVersion = Read-InstalledProductVersion -Directory $InstallDirectory")
should_process = updater.find("$PSCmdlet.ShouldProcess($InstallDirectory")
archive_check = updater.find("Assert-SafeArchive -ZipPath $zipPath")
extraction = updater.find("Expand-Archive -LiteralPath $zipPath")
package_check = updater.find("Assert-PackageRoot -Directory $extractRoot")
signed_version = updater.find("$signedPluginVersion = Read-SignedPluginVersion")
metadata_check = updater.find("$packageVersion -ne $signedPluginVersion")
installer_execute = updater.find("& $installer @arguments")
if min(installed_state, product_state, should_process) < 0 or not (installed_state < product_state < should_process):
    errors.append("updater must reconcile installed assembly/product identities before mutation approval")
if min(archive_check, extraction) < 0 or archive_check > extraction:
    errors.append("updater must validate archive paths/expanded limits before Expand-Archive")
if min(package_check, signed_version, metadata_check, installer_execute) < 0 or not (package_check < signed_version < metadata_check < installer_execute):
    errors.append("updater must verify signatures, signed plugin identity and metadata binding before installer execution")

manifest_package_guard = manifest.find("$package = Resolve-OrdinaryNonReparseDirectory -Path $PackageDirectory")
manifest_zip_guard = manifest.find("$zip = Resolve-OrdinaryNonReparseFile -Path $PackageZip")
manifest_metadata_guard = manifest.find("$metadataFile = Resolve-OrdinaryNonReparseFile")
manifest_metadata_state = manifest.find("$metadataState = Get-StableFileState")
manifest_zip_state = manifest.find("$zipState = Get-StableFileState")
manifest_payload_guard = manifest.find("$payloadFiles[$name] = Resolve-OrdinaryNonReparseFile")
manifest_payload_state = manifest.find("$payloadStates[$name] = Get-StableFileState")
manifest_metadata_read = manifest.find("$metadataText = Read-BoundedStrictUtf8File")
manifest_metadata_recheck = manifest.find("Assert-StableFileState -Expected $metadataState", manifest_metadata_read)
manifest_signer = manifest.find("Assert-AuthenticodeSigner -Path $payloadFiles[$name].FullName")
manifest_identity = manifest.find("$managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')")
manifest_zip_verify = manifest.find("Assert-ZipPayloadMatchesSignedStaging -ZipFile $zip -PackageRoot $package")
manifest_zip_recheck = manifest.find("$zip = Assert-StableFileState -Expected $zipState", manifest_zip_verify)
manifest_hash = manifest.find("$zipHash = [string]$zipState.Sha256", manifest_zip_recheck)
manifest_publish = manifest.find("[IO.File]::WriteAllText($stagePath")
manifest_positions = (
    manifest_package_guard,
    manifest_zip_guard,
    manifest_metadata_guard,
    manifest_metadata_state,
    manifest_zip_state,
    manifest_payload_guard,
    manifest_payload_state,
    manifest_metadata_read,
    manifest_metadata_recheck,
    manifest_signer,
    manifest_identity,
    manifest_zip_verify,
    manifest_zip_recheck,
    manifest_hash,
    manifest_publish,
)
if min(manifest_positions) < 0 or not (
    manifest_package_guard
    < manifest_zip_guard
    < manifest_metadata_guard
    < manifest_metadata_state
    < manifest_zip_state
    < manifest_payload_guard
    < manifest_payload_state
    < manifest_metadata_read
    < manifest_metadata_recheck
    < manifest_signer
    < manifest_identity
    < manifest_zip_verify
    < manifest_zip_recheck
    < manifest_hash
    < manifest_publish
):
    errors.append("manifest generation must ordinary-file bind and stable-state capture package/ZIP/metadata/payload before bounded metadata materialization, then revalidate identities and ZIP generation before publishing the admitted ZIP hash atomically")

finalizer_signer = finalizer.find("Assert-AuthenticodeSigner -Path $path")
finalizer_identity = finalizer.find("$managedIdentityNames = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')")
finalizer_mutation = finalizer.find("$PSCmdlet.ShouldProcess($zip")
if min(finalizer_signer, finalizer_identity, finalizer_mutation) < 0 or not (finalizer_signer < finalizer_identity < finalizer_mutation):
    errors.append("signed package finalization must validate signer and both managed identities before metadata/ZIP mutation")

install_snapshot = installer.find("$registrySnapshots = @(")
install_swap = installer.find("Move-Item -LiteralPath $stage -Destination $installFull")
install_registry = installer.find("New-ItemProperty -Path $target.AppKey -Name 'Loader'")
install_catch = installer.find("$originalError = $_")
install_restore = installer.find("Restore-DemandLoadSnapshot -Snapshot $registrySnapshots[$index]")
install_payload_restore = installer.find("Move-Item -LiteralPath $backup -Destination $installFull", install_catch)
install_rethrow = installer.find("throw $originalError")
if min(install_snapshot, install_swap, install_registry, install_catch, install_restore, install_payload_restore, install_rethrow) < 0 or not (
    install_snapshot < install_swap < install_registry < install_catch < install_restore < install_payload_restore < install_rethrow
):
    errors.append("installer must snapshot before payload/registry mutation and rollback registry/payload before rethrow")

uninstall_identity = uninstaller.find("Assert-InstallDirectorySafeToRemove -Directory $InstallDirectory")
uninstall_plan = uninstaller.find("$registryPlan = @()")
uninstall_quarantine = uninstaller.find("Move-Item -LiteralPath $installFull -Destination $quarantine -ErrorAction Stop")
uninstall_registry = uninstaller.find("Remove-Item -LiteralPath $entry.Target.AppKey -Recurse -Force -ErrorAction Stop")
uninstall_restore_files = uninstaller.find("Move-Item -LiteralPath $quarantine -Destination $installFull -ErrorAction Stop")
uninstall_restore_registry = uninstaller.find("Restore-RegistryTreeSnapshot -Snapshot $removedSnapshots[$index]")
if min(uninstall_identity, uninstall_plan, uninstall_quarantine, uninstall_registry, uninstall_restore_files, uninstall_restore_registry) < 0:
    errors.append("uninstaller must validate identity, snapshot plan, quarantine files and provide file/registry rollback")
elif not (uninstall_identity < uninstall_plan < uninstall_quarantine < uninstall_registry < uninstall_restore_files < uninstall_restore_registry):
    errors.append("uninstaller transaction ordering must validate/snapshot before mutation and restore files before registry on failure")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: secure V25 update uses bounded HTTPS, bounded/reparse-safe generation-stable manifest inputs with atomic publication, schema-2 dual managed identity binding, signed/hash-verified packages, shared update serialization, transactional install rollback and quarantine-safe uninstall rollback.")
