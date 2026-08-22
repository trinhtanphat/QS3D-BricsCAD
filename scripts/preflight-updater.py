#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "scripts/update-v25.ps1",
    "scripts/new-v25-update-manifest.ps1",
    "scripts/install-v25-autoload.ps1",
    "scripts/package-v25.ps1",
    "scripts/sign-v25.ps1",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing updater/release file: " + relative)

checks = {
    "scripts/update-v25.ps1": [
        "[ValidatePattern('^https://')]",
        "ExpectedSignerThumbprint",
        "AllowedPackageHost",
        "MaxPackageSizeMB",
        "embedded credentials",
        "Refusing downgrade",
        "AllowSameVersion",
        "65536",
        "Get-FileHash -LiteralPath $zipPath -Algorithm SHA256",
        "Assert-PackageRoot",
        "Get-AuthenticodeSignature",
        "Downloaded QS3D plugin signer mismatch",
        "SHA256SUMS.txt",
        "$name.Split('/')",
        "Unsafe SHA256SUMS entry",
        "StartsWith($packageRoot",
        "install-v25-autoload.ps1",
        "RequireSigned = $true",
        "ExpectedSignerThumbprint = $expectedSigner",
        "Get-Process -Name bricscad",
        "finally",
        "Remove-Item -LiteralPath $tempRoot",
    ],
    "scripts/new-v25-update-manifest.ps1": [
        "PackageUri",
        "ExpectedSignerThumbprint",
        "embedded credentials",
        "PACKAGE-METADATA.json",
        "Get-AuthenticodeSignature",
        "QS3D signer mismatch",
        "Get-FileHash -LiteralPath $zip -Algorithm SHA256",
        "schemaVersion = 1",
        "signerThumbprint = $expectedSigner",
    ],
    "scripts/install-v25-autoload.ps1": [
        "ExpectedSignerThumbprint",
        "SignerThumbprint",
        "Get-AuthenticodeSignature",
        "QS3D plugin signer mismatch",
        "$name.Split('/')",
        "Unsafe SHA256SUMS entry",
        "StartsWith($packageRoot",
        "update-v25.ps1",
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

updater = ROOT / "scripts/update-v25.ps1"
if updater.is_file():
    text = updater.read_text(encoding="utf-8").lower()
    forbidden = [
        "http://",
        "-skipcertificatecheck",
        "trustallcertificates",
        "certificatepolicy",
        "executionpolicy bypass",
    ]
    for token in forbidden:
        if token in text:
            errors.append("updater contains forbidden insecure token: " + token)

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

print("PASS: secure V25 updater uses HTTPS origin controls, downgrade/version guards, package size + SHA-256 verification, internal hash validation, pinned Authenticode publisher verification, atomic installer reuse and a verified release-manifest generator.")
