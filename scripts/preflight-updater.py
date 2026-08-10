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

checks = {
    "scripts/update-v25.ps1": [
        "[ValidatePattern('^https://')]",
        "ExpectedSignerThumbprint",
        "AllowedPackageHost",
        "MaxPackageSizeMB",
        "MaxExpandedPackageSizeMB",
        "MaxArchiveEntries",
        "$SignedPayloadNames",
        "embedded credentials",
        "Read-InstalledVersion",
        "Installed QS3D plugin assembly version is unreadable",
        "does not match installed plugin assembly version",
        "Refusing update until installed state is repaired",
        "Refusing downgrade",
        "AllowSameVersion",
        "65536",
        "Get-FileHash -LiteralPath $zipPath -Algorithm SHA256",
        "Assert-SafeArchive",
        "System.IO.Compression.ZipFile",
        "Unsafe package archive entry",
        "Assert-PackageRoot",
        "Assert-AuthenticodeSigner",
        "Read-SignedPluginVersion",
        "[Reflection.AssemblyName]::GetAssemblyName",
        "$signedPluginVersion",
        "does not match manifest version",
        "metadata version",
        "Refusing replay/downgrade metadata substitution",
        "SHA256SUMS.txt",
        "$name.Split('/')",
        "Unsafe SHA256SUMS entry",
        "StartsWith($packageRoot",
        "RequireSigned = $true",
        "ExpectedSignerThumbprint = $expectedSigner",
        "Get-Process -Name bricscad",
        "finally",
        "Remove-Item -LiteralPath $tempRoot",
    ],
    "scripts/new-v25-update-manifest.ps1": [
        "PackageUri",
        "ExpectedSignerThumbprint",
        "$SignedPayloadNames",
        "embedded credentials",
        "PACKAGE-METADATA.json",
        "Assert-AuthenticodeSigner",
        "Read-PluginAssemblyVersion",
        "$signedPluginVersion",
        "does not match signed QS3D plugin assembly version",
        "version = $signedPluginVersion.ToString()",
        "Assert-ZipPayloadMatchesSignedStaging",
        "Zipped QS3D executable payload",
        "Package ZIP payload does not match signed staging file",
        "Get-FileHash -LiteralPath $zip -Algorithm SHA256",
        "schemaVersion = 1",
        "signerThumbprint = $expectedSigner",
    ],
    "scripts/finalize-v25-signed-package.ps1": [
        "ExpectedSignerThumbprint",
        "$SignedPayloadNames",
        "Assert-AuthenticodeSigner",
        "Read-PluginAssemblyVersion",
        "$signedPluginVersion",
        "does not match signed QS3D plugin assembly version",
        "signedPluginAssemblyVersion",
        "signedExecutablePayload",
        "signedPayloadSignerThumbprint",
        "SHA256SUMS.txt",
        "Compress-Archive",
        "ZIP SHA256",
    ],
    "scripts/install-v25-autoload.ps1": [
        "ExpectedSignerThumbprint",
        "$signedPayloadNames",
        "Assert-AuthenticodeSigner",
        "Required executable payload is missing",
        "SHA256SUMS.txt",
        "$name.Split('/')",
        "Unsafe SHA256SUMS entry",
        "StartsWith($packageRoot",
        "Get-Process -Name bricscad",
        ".qs3d-stage-",
        ".backup-",
        "Get-DemandLoadSnapshot",
        "Get-RegistryValueSnapshot",
        "Get-RegistryValuesSnapshot",
        "Restore-DemandLoadSnapshot",
        "$registrySnapshots",
        "$payloadCommitted",
        "$originalError",
        "$rollbackFailures",
        "for ($index = $registrySnapshots.Count - 1; $index -ge 0; $index--)",
        "Remove-Item -LiteralPath $installFull -Recurse -Force",
        "Move-Item -LiteralPath $backup -Destination $installFull",
        "throw $originalError",
    ],
    "scripts/uninstall-v25-autoload.ps1": [
        "Assert-InstallDirectorySafeToRemove",
        "Join-Path $env:LOCALAPPDATA 'QS3D'",
        "PACKAGE-METADATA.json",
        "QS3D.BricsCAD.V25.dll",
        "BricsCAD V25 x64",
        "Refusing recursive removal",
        "$installFull = Assert-InstallDirectorySafeToRemove",
        "$root = 'HKCU:\\Software\\Bricsys\\BricsCAD'",
        "Remove-Item -LiteralPath $installFull -Recurse -Force",
    ],
    "scripts/package-v25.ps1": [
        "update-v25.ps1",
        "[Reflection.AssemblyName]::GetAssemblyName",
        "version = $assemblyVersion.ToString()",
        "pluginSignerThumbprint",
        "Installer/updater never weaken BricsCAD security settings.",
        "Get-ChildItem $dist -Recurse -File",
        ".Replace([IO.Path]::DirectorySeparatorChar, '/')",
    ],
    "scripts/sign-v25.ps1": [
        "HashAlgorithm SHA256",
        "TimestampServer",
        "Get-AuthenticodeSignature",
        "Code Signing",
        "'.ps1'",
        "'.dll'",
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing updater guard/token: " + needle)

for relative in (
    "scripts/update-v25.ps1",
    "scripts/new-v25-update-manifest.ps1",
    "scripts/finalize-v25-signed-package.ps1",
    "scripts/install-v25-autoload.ps1",
):
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for token in signed_payload_tokens:
        if token not in text:
            errors.append(relative + " must cover signed executable payload: " + token)

updater = ROOT / "scripts/update-v25.ps1"
if updater.is_file():
    text = updater.read_text(encoding="utf-8")
    lower = text.lower()
    for token in ("http://", "-skipcertificatecheck", "trustallcertificates", "certificatepolicy", "executionpolicy bypass"):
        if token in lower:
            errors.append("updater contains forbidden insecure token: " + token)
    installed_state = text.find("$installedVersion = Read-InstalledVersion -Directory $InstallDirectory")
    downgrade_check = text.find("if ($targetVersion -lt $installedVersion)")
    should_process = text.find("$PSCmdlet.ShouldProcess($InstallDirectory")
    if min(installed_state, downgrade_check, should_process) < 0 or not (installed_state < downgrade_check < should_process):
        errors.append("updater must reconcile installed DLL/metadata before downgrade/same-version decisions and before mutation")
    archive_check = text.find("Assert-SafeArchive -ZipPath $zipPath")
    extraction = text.find("Expand-Archive -LiteralPath $zipPath")
    if archive_check < 0 or extraction < 0 or archive_check > extraction:
        errors.append("updater must validate archive paths/expanded limits before Expand-Archive")
    package_check = text.find("Assert-PackageRoot -Directory $extractRoot")
    signed_version = text.find("$signedPluginVersion = Read-SignedPluginVersion")
    metadata_check = text.find("$packageVersion -ne $signedPluginVersion")
    installer_execute = text.find("& $installer @arguments")
    if package_check < 0 or signed_version < 0 or metadata_check < 0 or installer_execute < 0:
        errors.append("updater must verify signatures, signed plugin version, metadata binding, then execute installer")
    elif not (package_check < signed_version < metadata_check < installer_execute):
        errors.append("updater version binding must happen after signature verification and before installer execution")

manifest = ROOT / "scripts/new-v25-update-manifest.ps1"
if manifest.is_file():
    text = manifest.read_text(encoding="utf-8")
    signer_check = text.find("Assert-AuthenticodeSigner -Path (Join-Path $package $name)")
    signed_version = text.find("$signedPluginVersion = Read-PluginAssemblyVersion")
    verification = text.find("Assert-ZipPayloadMatchesSignedStaging -ZipPath $zip")
    package_hash = text.find("$zipHash =")
    if min(signer_check, signed_version, verification, package_hash) < 0 or not (signer_check < signed_version < verification < package_hash):
        errors.append("manifest generation must bind version to signed plugin before verifying/hashing the ZIP")

finalizer = ROOT / "scripts/finalize-v25-signed-package.ps1"
if finalizer.is_file():
    text = finalizer.read_text(encoding="utf-8")
    signer_check = text.find("Assert-AuthenticodeSigner -Path $path")
    signed_version = text.find("$signedPluginVersion = Read-PluginAssemblyVersion")
    should_process = text.find("$PSCmdlet.ShouldProcess($zip")
    if min(signer_check, signed_version, should_process) < 0 or not (signer_check < signed_version < should_process):
        errors.append("signed package finalization must validate signer and signed plugin version before mutating metadata/ZIP")

installer = ROOT / "scripts/install-v25-autoload.ps1"
if installer.is_file():
    original = installer.read_text(encoding="utf-8")
    lower = original.lower()
    for token in ("secureload 0", "setvar('secureload'", 'setvar("secureload"'):
        if token in lower:
            errors.append("installer must not weaken SECURELOAD: " + token)
    snapshot = original.find("$registrySnapshots = @($targets | ForEach-Object { Get-DemandLoadSnapshot")
    payload_swap = original.find("Move-Item -LiteralPath $stage -Destination $installFull")
    registry_write = original.find("New-ItemProperty -Path $target.AppKey -Name 'Loader'")
    catch_block = original.find("$originalError = $_")
    registry_rollback = original.find("Restore-DemandLoadSnapshot -Snapshot $registrySnapshots[$index]")
    payload_rollback = original.find("Move-Item -LiteralPath $backup -Destination $installFull", catch_block)
    rethrow = original.find("throw $originalError")
    if min(snapshot, payload_swap, registry_write, catch_block, registry_rollback, payload_rollback, rethrow) < 0:
        errors.append("installer must snapshot DemandLoad state and rollback registry/payload on any failure")
    elif not (snapshot < payload_swap < registry_write < catch_block < registry_rollback < payload_rollback < rethrow):
        errors.append("installer transactional ordering must snapshot before mutation and rollback before rethrow")
    if "elseif ($payloadCommitted -and (Test-Path -LiteralPath $installFull))" not in original:
        errors.append("fresh-install failure must remove the newly committed payload")

uninstaller = ROOT / "scripts/uninstall-v25-autoload.ps1"
if uninstaller.is_file():
    original = uninstaller.read_text(encoding="utf-8")
    safety = original.find("$installFull = Assert-InstallDirectorySafeToRemove -Directory $InstallDirectory")
    registry_scan = original.find("$root = 'HKCU:\\Software\\Bricsys\\BricsCAD'")
    recursive_delete = original.find("Remove-Item -LiteralPath $installFull -Recurse -Force")
    if min(safety, registry_scan, recursive_delete) < 0 or not (safety < registry_scan < recursive_delete):
        errors.append("uninstaller must validate install scope/package identity before registry or recursive file deletion")
    if "IndexOf('\\QS3D\\'" in original:
        errors.append("uninstaller must scope normal deletion to the canonical LocalAppData/QS3D root, not any path containing a QS3D segment")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: secure V25 updates reconcile installed DLL/metadata before downgrade decisions; installer rollback stays transactional; uninstall validates canonical scope and QS3D package identity before destructive cleanup.")
