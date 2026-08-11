#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
INSTALLER = ROOT / "scripts" / "install-v25-autoload.ps1"
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

print("QS3D installer package-root preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: installer resolves the package root after parameter binding, falls back through the executing script path, and never passes an empty default PackageDirectory to Resolve-Path.")
