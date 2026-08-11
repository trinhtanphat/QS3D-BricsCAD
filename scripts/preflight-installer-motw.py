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
    "$commands = Assert-PackageIntegrity -Directory $package",
    "$destination = Join-Path $stage $name",
    "Copy-Item -LiteralPath $source -Destination $destination -Force",
    "Unblock-File -LiteralPath $destination -ErrorAction Stop",
    "Move-Item -LiteralPath $stage -Destination $installFull",
)
for token in required:
    if token not in text:
        errors.append("installer MOTW lifecycle missing token: " + token)

integrity = text.find("$commands = Assert-PackageIntegrity -Directory $package")
copy = text.find("Copy-Item -LiteralPath $source -Destination $destination -Force")
unblock = text.find("Unblock-File -LiteralPath $destination -ErrorAction Stop")
commit = text.find("Move-Item -LiteralPath $stage -Destination $installFull")
if min(integrity, copy, unblock, commit) < 0 or not integrity < copy < unblock < commit:
    errors.append("installer must verify package integrity before copy, clear MOTW only on staged verified payload, then commit the staging directory")

if "Get-ChildItem" in text[unblock - 200:unblock + 200] if unblock >= 0 else False:
    errors.append("installer must not broadly unblock arbitrary files; only exact staged payload paths are allowed")

print("QS3D installer MOTW preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: verified package payloads are copied to staging, only exact staged files have Mark-of-the-Web cleared, and the staged directory is committed only afterward.")
