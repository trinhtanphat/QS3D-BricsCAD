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


def require(token: str, message: str) -> None:
    if token not in text:
        errors.append(message)


# The installer must copy the complete already-admitted package, not a hand-maintained
# subset. packageAdmission.Hashes contains the exact source-admitted file set including
# SHA256SUMS.txt, so it is the authority for staging and MOTW removal.
required = (
    "$packageAdmission = Assert-PackageIntegrity -Directory $package",
    "$commands = @($packageAdmission.Commands)",
    "$admittedNames = [string[]]@($packageAdmission.Hashes.Keys)",
    "[Array]::Sort($admittedNames, [StringComparer]::Ordinal)",
    "foreach ($relative in $admittedNames)",
    "$source = Join-Path $package ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))",
    "$destination = Join-Path $stage ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))",
    "Copy-Item -LiteralPath $source -Destination $destination -Force",
    "Unblock-File -LiteralPath $destination -ErrorAction Stop",
    "Assert-NoZoneIdentifier -Path $destination",
    "Assert-StagedPayloadAdmission -Directory $stage -AdmittedHashes $packageAdmission.Hashes",
    "Assert-UnblockedAdmittedPayload -Directory $stage -AdmittedHashes $packageAdmission.Hashes",
    "Move-Item -LiteralPath $stage -Destination $installFull",
    "Assert-StagedPayloadAdmission -Directory $installFull -AdmittedHashes $packageAdmission.Hashes",
    "Assert-UnblockedAdmittedPayload -Directory $installFull -AdmittedHashes $packageAdmission.Hashes",
)
for token in required:
    require(token, "installer complete-package/MOTW lifecycle missing token: " + token)

# Verification must inspect the ADS after Unblock-File. This is the same fail-closed
# contract as the owner's manual Get-Item -Stream Zone.Identifier check.
zone_tokens = (
    "function Assert-NoZoneIdentifier {",
    "Get-Item -LiteralPath $Path -Stream Zone.Identifier -ErrorAction SilentlyContinue",
    "Mark-of-the-Web is still present after Unblock-File",
    "function Assert-UnblockedAdmittedPayload {",
    "$relativePath in $AdmittedHashes.Keys",
    "Assert-NoZoneIdentifier -Path $path",
)
for token in zone_tokens:
    require(token, "installer post-unblock verification missing token: " + token)

integrity = text.find("$packageAdmission = Assert-PackageIntegrity -Directory $package")
copy = text.find("Copy-Item -LiteralPath $source -Destination $destination -Force")
unblock = text.find("Unblock-File -LiteralPath $destination -ErrorAction Stop")
zone_verify = text.find("Assert-NoZoneIdentifier -Path $destination", unblock + 1)
staged_admission = text.find("Assert-StagedPayloadAdmission -Directory $stage -AdmittedHashes $packageAdmission.Hashes")
staged_unblocked = text.find("Assert-UnblockedAdmittedPayload -Directory $stage -AdmittedHashes $packageAdmission.Hashes")
commit = text.find("Move-Item -LiteralPath $stage -Destination $installFull")
installed_admission = text.find("Assert-StagedPayloadAdmission -Directory $installFull -AdmittedHashes $packageAdmission.Hashes")
installed_unblocked = text.find("Assert-UnblockedAdmittedPayload -Directory $installFull -AdmittedHashes $packageAdmission.Hashes")
if min(integrity, copy, unblock, zone_verify, staged_admission, staged_unblocked, commit, installed_admission, installed_unblocked) < 0 or not (
    integrity < copy < unblock < zone_verify < staged_admission < staged_unblocked < commit < installed_admission < installed_unblocked
):
    errors.append(
        "installer must admit package integrity, copy every admitted file, unblock+verify it, re-admit and verify the staged tree, atomically commit it, then re-admit and re-verify the installed tree"
    )

# Regression model: Samples/ and installer/bootstrap helpers are release payload too.
# A fixed top-level whitelist silently drops these files and must not return.
for forbidden in (
    "$payload = @(",
    "$seen.Count -ne 8",
    "$relative.Contains('/')",
):
    if forbidden in text:
        errors.append("installer must not retain fixed/top-level-only staging contract: " + forbidden)

print("QS3D installer MOTW preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: the complete manifest-admitted V25 release payload is staged, every staged file is unblocked and verified free of Zone.Identifier, the staged bytes are re-admitted, and the committed install tree is re-admitted/reverified before registration.")
