#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts/package-v25.ps1"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/v25-package-held-generations.md"
errors: list[str] = []


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"{label}: missing required token: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        errors.append(f"{label}: forbidden pathname-reopen token remains: {token}")


if not SCRIPT.is_file():
    errors.append("missing scripts/package-v25.ps1")
if not RUNBOOK.is_file():
    errors.append("missing V25 held-generation runbook")

text = SCRIPT.read_text(encoding="utf-8") if SCRIPT.is_file() else ""
runbook = RUNBOOK.read_text(encoding="utf-8") if RUNBOOK.is_file() else ""

for token in (
    "function Open-HeldPackageInput",
    "[IO.FileShare]::Read",
    "function Assert-HeldPathBinding",
    "function Read-HeldPackageText",
    "function Copy-HeldPackageInput",
    "function Read-HeldSourceText",
    "$script:MaxPackageTextBytes = 8MB",
    "$script:StrictUtf8 = [Text.UTF8Encoding]::new($false, $true)",
    "Copy-HeldPackageInput -SourcePath $path -DestinationPath (Join-Path $dist $name)",
    "Copy-HeldPackageInput -SourcePath $scriptPath",
    "Copy-HeldPackageInput -SourcePath $launcherPath",
    "Copy-HeldPackageInput -SourcePath $samplePath",
    "Read-HeldSourceText -Path $_.FullName -Label 'V25 command source'",
    "[xml]$project = Read-HeldPackageText -Held $held -Label 'project file'",
):
    require(text, token, "V25 package script")

for token in (
    "Get-Content -LiteralPath $ProjectPath -Raw",
    "Get-Content -LiteralPath $_.FullName -Raw",
    "Copy-Item -LiteralPath $path -Destination (Join-Path $dist $name)",
    "Copy-Item -LiteralPath $scriptPath -Destination",
    "Copy-Item -LiteralPath $launcherPath -Destination",
    "Copy-Item -LiteralPath $samplePath -Destination",
):
    forbid(text, token, "V25 package script")

for token in (
    "Lane-Key: `issue-4592`",
    "held read-only generation",
    "FileShare.Read",
    "same-path replacement",
    "No licensed BricsCAD runtime evidence",
):
    require(runbook, token, "runbook")

# Deterministic mutation probes: each regression must remove a contract token that
# the reference inspector requires, proving the guard fails closed on shortcuts.
mutations = (
    text.replace("[IO.FileShare]::Read", "[IO.FileShare]::Write", 1),
    text.replace("Copy-HeldPackageInput -SourcePath $path -DestinationPath (Join-Path $dist $name)", "Copy-Item -LiteralPath $path -Destination (Join-Path $dist $name)", 1),
    text.replace("Read-HeldSourceText -Path $_.FullName -Label 'V25 command source'", "Get-Content -LiteralPath $_.FullName -Raw", 1),
    text.replace("[xml]$project = Read-HeldPackageText -Held $held -Label 'project file'", "[xml]$project = Get-Content -LiteralPath $ProjectPath -Raw", 1),
)
required = (
    "[IO.FileShare]::Read",
    "Copy-HeldPackageInput -SourcePath $path -DestinationPath (Join-Path $dist $name)",
    "Read-HeldSourceText -Path $_.FullName -Label 'V25 command source'",
    "[xml]$project = Read-HeldPackageText -Held $held -Label 'project file'",
)
for index, mutated in enumerate(mutations):
    if required[index] in mutated:
        errors.append(f"mutation probe {index + 1} unexpectedly retained its required held-generation token")

if errors:
    for error in errors:
        print(f"FAIL {error}")
    sys.exit(1)

print("PASS V25 package held-generation source contract")