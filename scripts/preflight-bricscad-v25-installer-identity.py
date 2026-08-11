#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
INSTALLER = ROOT / "scripts" / "install-bricscad-v25.ps1"
errors = []


def require(condition, message):
    if not condition:
        errors.append(message)


def is_v25_identity(product_name, product_version):
    return bool(
        re.search(r"\bBricsCAD\b", product_name or "", re.IGNORECASE)
        and re.match(r"^25(?:\.|$)", product_version or "")
    )


for name, version in (
    ("BricsCAD", "25"),
    ("BricsCAD Ultimate", "25.2.10"),
    ("BricsCAD Pro", "25.1.07.1"),
):
    require(is_v25_identity(name, version), f"identity model unexpectedly rejected V25 MSI: {name!r} {version!r}")

for name, version in (
    ("Other CAD", "25.2.10"),
    ("BricsCAD", "24.2.10"),
    ("BricsCAD", "26.0.1"),
    ("BricsCAD", "250.1"),
    ("", "25.2.10"),
    ("BricsCAD", ""),
):
    require(not is_v25_identity(name, version), f"identity model unexpectedly accepted non-V25 MSI: {name!r} {version!r}")

if not INSTALLER.is_file():
    errors.append("missing scripts/install-bricscad-v25.ps1")
    source = ""
else:
    source = INSTALLER.read_text(encoding="utf-8")

required_tokens = (
    "Get-FileHash -Algorithm SHA256 -LiteralPath $MsiPath",
    "Get-AuthenticodeSignature -LiteralPath $MsiPath",
    "New-Object -ComObject WindowsInstaller.Installer",
    "$windowsInstaller.OpenDatabase($MsiPath, 0)",
    "SELECT `Value` FROM `Property` WHERE `Property`=''ProductName''",
    "SELECT `Value` FROM `Property` WHERE `Property`=''ProductVersion''",
    "$productName -notmatch '(?i)\\bBricsCAD\\b'",
    "$productVersion -notmatch '^25(?:\\.|$)'",
    "MSI ProductName does not identify BricsCAD",
    "MSI ProductVersion does not identify BricsCAD V25",
    "Verified MSI identity: $productName $productVersion",
    'Start-Process -FilePath "msiexec.exe"',
)
for token in required_tokens:
    require(token in source, f"V25 installer identity guard missing token: {token}")

if source:
    hash_index = source.find("Get-FileHash -Algorithm SHA256 -LiteralPath $MsiPath")
    signature_index = source.find("Get-AuthenticodeSignature -LiteralPath $MsiPath")
    identity_index = source.find("New-Object -ComObject WindowsInstaller.Installer")
    name_guard_index = source.find("MSI ProductName does not identify BricsCAD")
    version_guard_index = source.find("MSI ProductVersion does not identify BricsCAD V25")
    execute_index = source.find('Start-Process -FilePath "msiexec.exe"')
    require(
        min(hash_index, signature_index, identity_index, name_guard_index, version_guard_index, execute_index) >= 0
        and hash_index < signature_index < identity_index < name_guard_index < version_guard_index < execute_index,
        "V25 installer must hash/signature-check and validate MSI product identity before msiexec",
    )

if errors:
    print("BricsCAD V25 installer identity preflight FAILED")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("BricsCAD V25 installer identity preflight passed.")
