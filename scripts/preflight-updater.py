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
        "Refusing downgrade",
        "AllowSameVersion",
        "65536",
        "Get-FileHash -LiteralPath $zipPath -Algorithm SHA256",
        "Assert-SafeArchive",
        "System.IO.Compression.ZipFile",
        "Unsafe package archive entry",
        "Assert-PackageRoot",
        "Assert-AuthenticodeSigner",
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
    archive_check = text.find("Assert-SafeArchive -ZipPath $zipPath")
    extraction = text.find("Expand-Archive -LiteralPath $zipPath")
    if archive_check < 0 or extraction < 0 or archive_check > extraction:
        errors.append("updater must validate archive paths/expanded limits before Expand-Archive")
    package_check = text.find("Assert-PackageRoot -Directory $extractRoot")
    installer_execute = text.find("& $installer @arguments")
    if package_check < 0 or installer_execute < 0 or package_check > installer_execute:
        errors.append("all downloaded executable payload signatures must be pinned before installer execution")

manifest = ROOT / "scripts/new-v25-update-manifest.ps1"
if manifest.is_file():
    text = manifest.read_text(encoding="utf-8")
    verification = text.find("Assert-ZipPayloadMatchesSignedStaging -ZipPath $zip")
    package_hash = text.find("$zipHash =")
    if verification < 0 or package_hash < 0 or verification > package_hash:
        errors.append("update manifest generation must verify signed ZIP payload before hashing manifest package")

installer = ROOT / "scripts/install-v25-autoload.ps1"
if installer.is_file():
    text = installer.read_text(encoding="utf-8").lower()
    for token in ("secureload 0", "setvar('secureload'", 'setvar("secureload"'):
        if token in text:
            errors.append("installer must not weaken SECURELOAD: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: secure V25 updates pin both DLLs plus install/update/uninstall scripts, validate ZIP path/count/expanded bounds before extraction, finalize signed staging and publish manifests only for matching signed ZIP payloads.")
