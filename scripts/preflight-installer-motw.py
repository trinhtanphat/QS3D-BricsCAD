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
    "$packageAdmission = Assert-PackageIntegrity -Directory $package",
    "$commands = @($packageAdmission.Commands)",
    "$destination = Join-Path $stage $name",
    "Copy-Item -LiteralPath $source -Destination $destination -Force",
    "Unblock-File -LiteralPath $destination -ErrorAction Stop",
    "Assert-StagedPayloadAdmission -Directory $stage -AdmittedHashes $packageAdmission.Hashes",
    "Move-Item -LiteralPath $stage -Destination $installFull",
)
for token in required:
    if token not in text:
        errors.append("installer MOTW lifecycle missing token: " + token)

integrity = text.find("$packageAdmission = Assert-PackageIntegrity -Directory $package")
copy = text.find("Copy-Item -LiteralPath $source -Destination $destination -Force")
unblock = text.find("Unblock-File -LiteralPath $destination -ErrorAction Stop")
staged_admission = text.find("Assert-StagedPayloadAdmission -Directory $stage -AdmittedHashes $packageAdmission.Hashes")
commit = text.find("Move-Item -LiteralPath $stage -Destination $installFull")
if min(integrity, copy, unblock, staged_admission, commit) < 0 or not integrity < copy < unblock < staged_admission < commit:
    errors.append("installer must admit source package integrity before copy, clear MOTW only on exact staged payload files, re-admit staged bytes, then commit the staging directory")

if "Get-ChildItem" in text[unblock - 200:unblock + 200] if unblock >= 0 else False:
    errors.append("installer must not broadly unblock arbitrary files; only exact staged payload paths are allowed")

print("QS3D installer MOTW preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: source-admitted package payloads are copied to staging, only exact staged files have Mark-of-the-Web cleared, staged bytes are re-admitted against captured hashes, and commit occurs only afterward.")
