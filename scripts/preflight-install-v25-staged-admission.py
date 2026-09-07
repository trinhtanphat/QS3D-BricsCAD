#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INSTALLER = ROOT / "scripts" / "install-v25-autoload.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def function_body(text: str, start_marker: str, next_marker: str) -> str:
    start = text.find(start_marker)
    end = text.find(next_marker, start + len(start_marker)) if start >= 0 else -1
    require(start >= 0 and end > start, f"Could not isolate PowerShell function: {start_marker}")
    return text[start:end]


def mutate_scoped(text: str, marker: str, start_marker: str | None = None, next_marker: str | None = None) -> str:
    if start_marker is None:
        require(marker in text, f"Mutation probe could not find required marker: {marker}")
        return text.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)

    require(next_marker is not None, "Scoped mutation requires a terminating function marker.")
    start = text.find(start_marker)
    end = text.find(next_marker, start + len(start_marker)) if start >= 0 else -1
    require(start >= 0 and end > start, f"Mutation probe could not isolate PowerShell function: {start_marker}")
    scoped = text[start:end]
    require(marker in scoped, f"Mutation probe could not find scoped marker: {marker}")
    mutated_scoped = scoped.replace(marker, "__QS3D_MUTATION_REMOVED__", 1)
    return text[:start] + mutated_scoped + text[end:]


def validate(text: str) -> None:
    staged = function_body(
        text,
        "function Assert-StagedPayloadAdmission {",
        "function Convert-ToStrictSemVerIdentity {",
    )

    require("Hashes = $manifestHashes" in text,
            "Package integrity admission must retain the already-validated manifest hash snapshot.")
    require("Commands = $commands" in text,
            "Package integrity admission must retain the admitted command snapshot.")
    require("$packageAdmission = Assert-PackageIntegrity -Directory $package" in text,
            "Installer must keep the package admission result instead of discarding validated identity.")
    require("$commands = @($packageAdmission.Commands)" in text,
            "DemandLoad commands must derive from the admitted package snapshot.")

    require("$manifestStream = [IO.File]::Open($manifest, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)" in text,
            "Manifest admission must hold one generation while hashing and parsing SHA256SUMS.txt.")
    require("$manifestSha256 = ([BitConverter]::ToString($sha.ComputeHash($manifestStream)))" in text,
            "Manifest bytes must be hashed from the held stream.")
    require("$manifestStream.Position = 0" in text and "$reader = [IO.StreamReader]::new($manifestStream" in text,
            "Manifest parsing must rewind and read the same held stream generation.")
    require("$manifestHashes.Add('SHA256SUMS.txt', $manifestSha256)" in text,
            "The staged manifest must be bound to the exact held manifest generation.")

    stage_call = "Assert-StagedPayloadAdmission -Directory $stage"
    require(stage_call in text, "Installer must validate staged payload bytes before commit.")
    require("-AdmittedHashes $packageAdmission.Hashes" in text,
            "Staged admission must compare against the frozen manifest hashes from source admission.")
    require("Assert-PackageIdentity -Directory $Directory" in staged,
            "Staged admission must re-bind package identity to staged DLL/metadata bytes.")
    require("Get-FileHash -LiteralPath $path -Algorithm SHA256" in staged,
            "Staged admission must hash staged payload bytes.")
    require("Assert-AuthenticodeSigner -Path $path" in staged,
            "Staged executable payloads must have signer admission after staging.")
    require("$stageRootItem = Get-Item -LiteralPath $stageRootPath -Force -ErrorAction Stop" in staged,
            "Staged admission must inspect the exact stage root before trusting descendants.")
    require("$stageRootItem.Attributes -band [IO.FileAttributes]::ReparsePoint" in staged,
            "Staged admission must reject a reparse-backed stage root.")
    require("$stageChildren = @(Get-ChildItem -LiteralPath $Directory -Force -ErrorAction Stop)" in staged,
            "Staged admission must enumerate every top-level stage entry, not silently skip directories.")
    require("$stageFile.Attributes -band [IO.FileAttributes]::ReparsePoint" in staged,
            "Staged admission must reject reparse-backed staged entries.")
    require("$relative.Contains('/')" in staged,
            "Standalone installer staging must reject nested entries instead of silently admitting hidden subtrees.")

    copy_pos = text.find("Copy-Item -LiteralPath $source -Destination $destination -Force")
    stage_pos = text.find(stage_call)
    commit_pos = text.find("Move-Item -LiteralPath $stage -Destination $installFull")
    require(copy_pos >= 0 and stage_pos > copy_pos,
            "Staged admission must occur after source bytes have been copied into the stage.")
    require(commit_pos > stage_pos,
            "Staged admission must occur before the stage is committed into InstallDirectory.")


text = INSTALLER.read_text(encoding="utf-8")
validate(text)

STAGED_START = "function Assert-StagedPayloadAdmission {"
STAGED_END = "function Convert-ToStrictSemVerIdentity {"

for marker, scope in (
    ("Hashes = $manifestHashes", None),
    ("$packageAdmission = Assert-PackageIntegrity -Directory $package", None),
    ("$commands = @($packageAdmission.Commands)", None),
    ("$manifestStream = [IO.File]::Open($manifest, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)", None),
    ("$manifestHashes.Add('SHA256SUMS.txt', $manifestSha256)", None),
    ("Assert-StagedPayloadAdmission -Directory $stage", None),
    ("-AdmittedHashes $packageAdmission.Hashes", None),
    ("Assert-PackageIdentity -Directory $Directory", "staged"),
    ("Get-FileHash -LiteralPath $path -Algorithm SHA256", "staged"),
    ("Assert-AuthenticodeSigner -Path $path", "staged"),
    ("$stageRootItem = Get-Item -LiteralPath $stageRootPath -Force -ErrorAction Stop", "staged"),
    ("$stageRootItem.Attributes -band [IO.FileAttributes]::ReparsePoint", "staged"),
    ("$stageChildren = @(Get-ChildItem -LiteralPath $Directory -Force -ErrorAction Stop)", "staged"),
    ("$stageFile.Attributes -band [IO.FileAttributes]::ReparsePoint", "staged"),
):
    if scope == "staged":
        mutated = mutate_scoped(text, marker, STAGED_START, STAGED_END)
    else:
        mutated = mutate_scoped(text, marker)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

print("PASS V25 installer staged payload admission fence")
