#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INSTALLER = ROOT / "scripts" / "install-v25-autoload.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def validate(text: str) -> None:
    require("function Assert-StagedPayloadAdmission {" in text,
            "Installer must define staged-payload admission for the bytes that will be committed.")
    require("Hashes = $manifestHashes" in text,
            "Package integrity admission must retain the already-validated manifest hash snapshot.")
    require("Commands = $commands" in text,
            "Package integrity admission must retain the admitted command snapshot.")
    require("$packageAdmission = Assert-PackageIntegrity -Directory $package" in text,
            "Installer must keep the package admission result instead of discarding validated identity.")
    require("$commands = @($packageAdmission.Commands)" in text,
            "DemandLoad commands must derive from the admitted package snapshot.")

    stage_call = "Assert-StagedPayloadAdmission -Directory $stage"
    require(stage_call in text, "Installer must validate staged payload bytes before commit.")
    require("-AdmittedHashes $packageAdmission.Hashes" in text,
            "Staged admission must compare against the frozen manifest hashes from source admission.")
    require("Assert-PackageIdentity -Directory $Directory" in text,
            "Staged admission must re-bind package identity to staged DLL/metadata bytes.")
    require("Get-FileHash -LiteralPath $path -Algorithm SHA256" in text,
            "Staged admission must hash staged payload bytes.")
    require("Assert-AuthenticodeSigner -Path $path" in text,
            "Staged executable payloads must have signer admission after staging.")

    copy_pos = text.find("Copy-Item -LiteralPath $source -Destination $destination -Force")
    stage_pos = text.find(stage_call)
    commit_pos = text.find("Move-Item -LiteralPath $stage -Destination $installFull")
    require(copy_pos >= 0 and stage_pos > copy_pos,
            "Staged admission must occur after source bytes have been copied into the stage.")
    require(commit_pos > stage_pos,
            "Staged admission must occur before the stage is committed into InstallDirectory.")


text = INSTALLER.read_text(encoding="utf-8")
validate(text)

for marker in (
    "Hashes = $manifestHashes",
    "$packageAdmission = Assert-PackageIntegrity -Directory $package",
    "$commands = @($packageAdmission.Commands)",
    "Assert-StagedPayloadAdmission -Directory $stage",
    "-AdmittedHashes $packageAdmission.Hashes",
    "Assert-PackageIdentity -Directory $Directory",
):
    require(marker in text, f"Mutation probe could not find required marker: {marker}")
    mutated = text.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

print("PASS V25 installer staged payload admission fence")