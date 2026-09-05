#!/usr/bin/env python3
"""Fail-closed source guard for Windows Installer COM lifetime during V26 MSI admission."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "acquire-v26-compile-references.ps1"
text = SOURCE.read_text(encoding="utf-8")

required_literals = [
    "function Release-WindowsInstallerComObjectQuietly",
    "[Runtime.InteropServices.Marshal]::IsComObject($Value)",
    "[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($Value)",
    "function Close-WindowsInstallerViewQuietly",
    "[void]$View.Close()",
    "$installer = $null",
    "$database = $null",
    "$versionView = $null",
    "$versionRecord = $null",
    "$nameView = $null",
    "$nameRecord = $null",
    "Release-WindowsInstallerComObjectQuietly -Value $nameRecord",
    "Close-WindowsInstallerViewQuietly -View $nameView",
    "Release-WindowsInstallerComObjectQuietly -Value $versionRecord",
    "Close-WindowsInstallerViewQuietly -View $versionView",
    "Release-WindowsInstallerComObjectQuietly -Value $database",
    "Release-WindowsInstallerComObjectQuietly -Value $installer",
]

missing = [literal for literal in required_literals if literal not in text]
if missing:
    print("ERROR: V26 MSI admission COM-release guard missing required source contract:")
    for literal in missing:
        print(f" - {literal}")
    sys.exit(1)

admission_match = re.search(
    r"function Open-AdmittedV26Installer \{(?P<body>.*?)\n\}\n\nfunction Get-SingleV26InstallerAdmission",
    text,
    flags=re.DOTALL,
)
if not admission_match:
    print("ERROR: could not locate Open-AdmittedV26Installer.")
    sys.exit(1)

body = admission_match.group("body")
create_index = body.find("$installer = New-Object -ComObject WindowsInstaller.Installer")
cleanup_tokens = [
    "Release-WindowsInstallerComObjectQuietly -Value $nameRecord",
    "Close-WindowsInstallerViewQuietly -View $nameView",
    "Release-WindowsInstallerComObjectQuietly -Value $versionRecord",
    "Close-WindowsInstallerViewQuietly -View $versionView",
    "Release-WindowsInstallerComObjectQuietly -Value $database",
    "Release-WindowsInstallerComObjectQuietly -Value $installer",
]
cleanup_indices = [body.find(token) for token in cleanup_tokens]
after_metadata_index = body.find("$afterMetadata = Get-OrdinaryFileOrNull")
return_index = body.find("return [pscustomobject]@{")

if create_index < 0 or any(index < 0 for index in cleanup_indices):
    print("ERROR: V26 MSI admission COM objects are not created and released under the guarded lifecycle.")
    sys.exit(1)

if cleanup_indices != sorted(cleanup_indices):
    print("ERROR: V26 MSI admission COM objects must be released in reverse dependency order.")
    sys.exit(1)

if not (create_index < cleanup_indices[0] < cleanup_indices[-1] < after_metadata_index < return_index):
    print("ERROR: Windows Installer COM cleanup must complete before post-metadata stability validation and admission return.")
    sys.exit(1)

cleanup_region = body[create_index:after_metadata_index]
if not re.search(r"try\s*\{.*?New-Object -ComObject WindowsInstaller\.Installer.*?\}\s*finally\s*\{.*?FinalReleaseComObject|try\s*\{.*?New-Object -ComObject WindowsInstaller\.Installer.*?\}\s*finally\s*\{.*?Release-WindowsInstallerComObjectQuietly", cleanup_region, flags=re.DOTALL):
    print("ERROR: Windows Installer metadata access must be enclosed by try/finally cleanup.")
    sys.exit(1)

print("PASS: V26 MSI admission releases Windows Installer COM views, records, database, and installer before msiexec can run.")
