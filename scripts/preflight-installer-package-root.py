#!/usr/bin/env python3
from pathlib import Path
import sys

# RED contract: the CMD must reject an incomplete/not-extracted package before PowerShell starts.
ROOT = Path(__file__).resolve().parents[1]
INSTALLER = ROOT / "scripts" / "install-v25-autoload.ps1"
CMD = ROOT / "scripts" / "INSTALL-QS3D.cmd"
PACKAGE = ROOT / "scripts" / "package-v25.ps1"
errors = []

if not INSTALLER.is_file():
    errors.append("missing scripts/install-v25-autoload.ps1")
    text = ""
else:
    text = INSTALLER.read_text(encoding="utf-8")

required = (
    "[string]$PackageDirectory,",
    "$scriptDirectory = $PSScriptRoot",
    "$MyInvocation.MyCommand.Path",
    "if ([string]::IsNullOrWhiteSpace($PackageDirectory))",
    "$PackageDirectory = $scriptDirectory",
    "PackageDirectory could not be resolved from the installer script location. Pass -PackageDirectory explicitly.",
    "$package = (Resolve-Path -LiteralPath $PackageDirectory).Path",
)
for token in required:
    if token not in text:
        errors.append("installer package-root fallback missing token: " + token)

if "[string]$PackageDirectory = $PSScriptRoot" in text:
    errors.append("installer must not bind PackageDirectory directly to PSScriptRoot in the param block")

fallback_index = text.find("$scriptDirectory = $PSScriptRoot")
assignment_index = text.find("$PackageDirectory = $scriptDirectory")
resolve_index = text.find("$package = (Resolve-Path -LiteralPath $PackageDirectory).Path")
if min(fallback_index, assignment_index, resolve_index) < 0 or not fallback_index < assignment_index < resolve_index:
    errors.append("installer must resolve its script directory and fill an empty PackageDirectory before Resolve-Path")

if not CMD.is_file():
    errors.append("missing scripts/INSTALL-QS3D.cmd")
    cmd_text = ""
else:
    cmd_text = CMD.read_text(encoding="utf-8")

cmd_required = (
    'set "QS3D_INSTALLER=%~dp0install-v25-autoload.ps1"',
    'if not exist "%QS3D_INSTALLER%" goto :missing_companion',
    ':missing_companion',
    'Extract All / Giai nen tat ca',
    'Keep INSTALL-QS3D.cmd and install-v25-autoload.ps1 in the same extracted folder.',
)
for token in cmd_required:
    if token not in cmd_text:
        errors.append("installer CMD extract/companion guard missing token: " + token)

companion_check_index = cmd_text.find('if not exist "%QS3D_INSTALLER%" goto :missing_companion')
powershell_index = cmd_text.find("powershell.exe")
if min(companion_check_index, powershell_index) < 0 or not companion_check_index < powershell_index:
    errors.append("INSTALL-QS3D.cmd must reject a missing companion script before launching PowerShell")

if not PACKAGE.is_file():
    errors.append("missing scripts/package-v25.ps1")
    package_text = ""
else:
    package_text = PACKAGE.read_text(encoding="utf-8")
for token in ("INSTALL-QS3D.cmd", "install-v25-autoload.ps1"):
    if token not in package_text:
        errors.append("V25 package script must ship installer companion together: " + token)

print("QS3D installer package-root preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: installer resolves the package root after parameter binding; INSTALL-QS3D.cmd rejects an incompletely extracted package before PowerShell, tells the user to Extract All, and the V25 package contract ships the CMD + install-v25-autoload.ps1 companions together.")
