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
    walker = function_body(
        text,
        "function Get-SafePayloadFiles {",
        "function Assert-StagedPayloadAdmission {",
    )
    staged = function_body(
        text,
        "function Assert-StagedPayloadAdmission {",
        "function Assert-NoZoneIdentifier {",
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

    require("[Collections.Generic.Queue[string]]::new()" in walker,
            "Recursive staged admission must walk directories explicitly instead of trusting broad recursive traversal.")
    require("$pending.Enqueue($rootPath)" in walker and "$pending.Enqueue([string]$entry.FullName)" in walker,
            "Recursive staged admission must enqueue only directories it has inspected.")
    require("$entry.Attributes -band [IO.FileAttributes]::ReparsePoint" in walker,
            "Recursive staged admission must reject every reparse-backed descendant before traversal.")
    require("$entry.PSIsContainer" in walker and "$entry -is [IO.FileInfo]" in walker,
            "Recursive staged admission must distinguish ordinary directories from ordinary files.")

    require("$stageFiles = @(Get-SafePayloadFiles -Directory $Directory)" in staged,
            "Staged admission must validate the complete recursively staged file set.")
    require("Get-FileHash -LiteralPath $path -Algorithm SHA256" in staged,
            "Staged admission must hash every staged payload file.")
    require("$AdmittedHashes.ContainsKey($relative)" in staged,
            "Every staged file must be bound to the frozen source-admission hash set.")
    require("foreach ($relative in $AdmittedHashes.Keys)" in staged and "if (-not $seen.Contains([string]$relative))" in staged,
            "Every frozen admitted payload path must be present in the staged tree.")
    require("$seen.Count -ne $AdmittedHashes.Count" in staged,
            "Staged admission must require exact set cardinality without a fixed file count.")
    require("Assert-PackageIdentity -Directory $Directory" in staged,
            "Staged admission must re-bind package identity to staged DLL/metadata bytes.")
    require("Assert-AuthenticodeSigner -Path $path" in staged,
            "Staged executable payloads must have signer admission after staging.")
    require("'install-v25-autoload.ps1'" in staged,
            "The complete staged package must retain the signed installer bootstrap itself.")
    require("$relative.Contains('/')" not in staged,
            "Staged admission must allow manifest-authorized nested payload paths such as Samples/.")
    require("$seen.Count -ne 8" not in staged,
            "Staged admission must not hard-code the historical eight-file payload.")

    stage_call = "Assert-StagedPayloadAdmission -Directory $stage -AdmittedHashes $packageAdmission.Hashes"
    install_call = "Assert-StagedPayloadAdmission -Directory $installFull -AdmittedHashes $packageAdmission.Hashes"
    require(text.count(stage_call) >= 2,
            "Installer must validate staged payload bytes after copying and again immediately before commit.")
    require(install_call in text,
            "Installer must re-admit the committed InstallDirectory before DemandLoad registration.")

    copy_pos = text.find("Copy-Item -LiteralPath $source -Destination $destination -Force")
    first_stage_pos = text.find(stage_call)
    backup_pos = text.find("Move-Item -LiteralPath $installFull -Destination $backup")
    final_stage_pos = text.rfind(stage_call)
    commit_pos = text.find("Move-Item -LiteralPath $stage -Destination $installFull")
    installed_pos = text.find(install_call)
    require(copy_pos >= 0 and first_stage_pos > copy_pos,
            "Initial staged admission must occur after source bytes have been copied into the stage.")
    require(backup_pos > first_stage_pos,
            "Existing-install validation/backup must occur after the initial staged admission.")
    require(final_stage_pos > backup_pos,
            "Final staged admission must revalidate the stage after any existing-install backup window.")
    require(commit_pos > final_stage_pos,
            "Final staged admission must occur immediately before the stage is committed into InstallDirectory.")
    require(installed_pos > commit_pos,
            "Committed InstallDirectory must be re-admitted after the atomic stage move and before registration.")


text = INSTALLER.read_text(encoding="utf-8")
validate(text)

WALKER_START = "function Get-SafePayloadFiles {"
WALKER_END = "function Assert-StagedPayloadAdmission {"
STAGED_START = "function Assert-StagedPayloadAdmission {"
STAGED_END = "function Assert-NoZoneIdentifier {"

for marker, scope in (
    ("Hashes = $manifestHashes", None),
    ("$packageAdmission = Assert-PackageIntegrity -Directory $package", None),
    ("$commands = @($packageAdmission.Commands)", None),
    ("$manifestStream = [IO.File]::Open($manifest, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)", None),
    ("$manifestHashes.Add('SHA256SUMS.txt', $manifestSha256)", None),
    ("[Collections.Generic.Queue[string]]::new()", "walker"),
    ("$entry.Attributes -band [IO.FileAttributes]::ReparsePoint", "walker"),
    ("$stageFiles = @(Get-SafePayloadFiles -Directory $Directory)", "staged"),
    ("Get-FileHash -LiteralPath $path -Algorithm SHA256", "staged"),
    ("$AdmittedHashes.ContainsKey($relative)", "staged"),
    ("foreach ($relative in $AdmittedHashes.Keys)", "staged"),
    ("$seen.Count -ne $AdmittedHashes.Count", "staged"),
    ("Assert-PackageIdentity -Directory $Directory", "staged"),
    ("Assert-AuthenticodeSigner -Path $path", "staged"),
    ("Assert-StagedPayloadAdmission -Directory $installFull -AdmittedHashes $packageAdmission.Hashes", None),
):
    if scope == "walker":
        mutated = mutate_scoped(text, marker, WALKER_START, WALKER_END)
    elif scope == "staged":
        mutated = mutate_scoped(text, marker, STAGED_START, STAGED_END)
    else:
        mutated = mutate_scoped(text, marker)
    try:
        validate(mutated)
    except SystemExit:
        pass
    else:
        raise SystemExit(f"Mutation probe unexpectedly passed after removing: {marker}")

print("PASS V25 installer recursive staged payload admission fence")
