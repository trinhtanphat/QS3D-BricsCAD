#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "scripts/package-v25.ps1"
V26 = ROOT / "scripts/package-v26.ps1"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/package-source-input-safety.md"

errors = []


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"{label}: missing required contract token: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        errors.append(f"{label}: unsafe legacy token remains: {token}")


def before(text: str, first: str, second: str, label: str) -> None:
    left = text.find(first)
    right = text.find(second)
    if left < 0 or right < 0 or left >= right:
        errors.append(f"{label}: expected ordering {first!r} before {second!r}")


def inspect_script(text: str, major: str) -> list[str]:
    found: list[str] = []
    for token in (
        "function Assert-SafeInputPathAncestors",
        "function Assert-SafeInputDirectory",
        "function Assert-SafeInputFile",
        "function Get-SafeSourceFiles",
        "[IO.FileAttributes]::ReparsePoint",
        "Test-PathEqualOrContained -Path $fullPath -Container $repo",
        "-not ($item -is [IO.FileInfo])",
    ):
        if token not in text:
            found.append(f"{major}: missing {token}")

    held_tokens = (
        "function Open-HeldPackageInput",
        "[IO.FileShare]::Read",
        "function Assert-HeldPathBinding",
        "function Copy-HeldPackageInput",
        "function Read-HeldPackageText",
    )
    for token in held_tokens:
        if token not in text:
            found.append(f"{major}: missing {token}")

    if major == "V25":
        required_calls = (
            "$source = Assert-SafeInputDirectory",
            "$sampleSource = Assert-SafeInputDirectory",
            "V25 build artifact $name",
            "release script $script",
            "package launcher $launcherName",
            "synthetic sample $sampleName",
            "Get-SafeSourceFiles -SourceRoot (Join-Path $root 'src/QS3D.BricsCAD.V25')",
            "Copy-HeldPackageInput -SourcePath $path",
            "Copy-HeldPackageInput -SourcePath $scriptPath",
            "Copy-HeldPackageInput -SourcePath $launcherPath",
            "Copy-HeldPackageInput -SourcePath $samplePath",
            "Read-HeldSourceText -Path $_.FullName",
        )
        legacy = "Get-ChildItem (Join-Path $root 'src/QS3D.BricsCAD.V25') -Recurse -Filter '*.cs'"
    else:
        required_calls = (
            "$source = Assert-SafeInputDirectory",
            "$generator = Assert-SafeInputFile",
            "$sampleSource = Assert-SafeInputDirectory",
            "V26 build artifact $name",
            "V26 generator input $sourceScript",
            "synthetic sample $sampleName",
            "Get-SafeSourceFiles -SourceRoot $v25Root",
            "Get-SafeSourceFiles -SourceRoot (Join-Path $root 'src/QS3D.BricsCAD.V26')",
            "function Invoke-WithHeldPackageInput",
        )
        legacy = "Get-ChildItem (Join-Path $root 'src/QS3D.BricsCAD.V26') -Recurse -Filter '*.cs'"

    for token in required_calls:
        if token not in text:
            found.append(f"{major}: missing {token}")
    if legacy in text:
        found.append(f"{major}: legacy recursive source scan remains")

    source_bind = text.find("$source = Assert-SafeInputDirectory")
    output_mutation = text.find("New-Item -ItemType Directory -Path $distRoot -Force")
    if source_bind < 0 or output_mutation < 0 or source_bind >= output_mutation:
        found.append(f"{major}: source admission must happen before package output mutation")

    common_forbidden = (
        "Get-Content -LiteralPath $ProjectPath -Raw",
        "Copy-Item -LiteralPath $path -Destination (Join-Path $dist $name)",
        "Copy-Item -LiteralPath $samplePath -Destination",
    )
    for forbidden_token in common_forbidden:
        if forbidden_token in text:
            found.append(f"{major}: pathname reopen/copy generation shortcut remains: {forbidden_token}")

    if major == "V25":
        for forbidden_token in (
            "Get-Content -LiteralPath $_.FullName -Raw",
            "Copy-Item -LiteralPath $scriptPath -Destination",
            "Copy-Item -LiteralPath $launcherPath -Destination",
        ):
            if forbidden_token in text:
                found.append(f"V25: pathname reopen/copy generation shortcut remains: {forbidden_token}")
    else:
        for forbidden_token in (
            "Get-Content -LiteralPath $Path -Raw",
        ):
            if forbidden_token in text:
                found.append(f"V26: pathname reopen/copy generation shortcut remains: {forbidden_token}")

    artifact_admit = text.find(f"Assert-SafeInputFile -Path (Join-Path $source $name)")
    held_copy = text.find("Copy-HeldPackageInput -SourcePath $path", artifact_admit)
    if artifact_admit < 0 or held_copy <= artifact_admit:
        found.append(f"{major}: held build-artifact copy must follow ordinary/non-reparse admission")
    return found


for path, label in ((V25, "V25 package script"), (V26, "V26 package script"), (RUNBOOK, "runbook")):
    if not path.is_file():
        errors.append(f"missing {label}: {path.relative_to(ROOT)}")

v25 = V25.read_text(encoding="utf-8") if V25.is_file() else ""
v26 = V26.read_text(encoding="utf-8") if V26.is_file() else ""
runbook = RUNBOOK.read_text(encoding="utf-8") if RUNBOOK.is_file() else ""

errors.extend(inspect_script(v25, "V25"))
errors.extend(inspect_script(v26, "V26"))

before(v25, "$source = Assert-SafeInputDirectory", "foreach ($name in $required)", "V25")
before(v25, "Assert-SafeInputFile -Path (Join-Path $source $name)", "Copy-HeldPackageInput -SourcePath $path", "V25")
before(v26, "$source = Assert-SafeInputDirectory", "foreach ($name in $required)", "V26")
before(v26, "Assert-SafeInputFile -Path (Join-Path $source $name)", "Copy-HeldPackageInput -SourcePath $path", "V26")
for text, major in ((v25, "V25"), (v26, "V26")):
    require(text, "$item.Attributes -band [IO.FileAttributes]::ReparsePoint", major)
    require(text, "Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop", major)

require(runbook, "Lane-Key: `issue-4386`", "runbook")
require(runbook, "ordinary non-reparse", "runbook")
require(runbook, "before any packaging output mutation", "runbook")
require(runbook, "V25", "runbook")
require(runbook, "V26", "runbook")

# Mutation checks: removing an input binder, restoring recursive source scanning,
# or reverting either package path to pathname copy must make inspection reject it.
mutations = (
    ("V25", v25.replace("$source = Assert-SafeInputDirectory", "$source = Get-CanonicalFullPath", 1)),
    ("V26", v26.replace("$generator = Assert-SafeInputFile", "$generator = Get-CanonicalFullPath", 1)),
    ("V25", v25.replace("Get-SafeSourceFiles -SourceRoot (Join-Path $root 'src/QS3D.BricsCAD.V25') -RepositoryRoot $root -Extension '.cs'", "Get-ChildItem (Join-Path $root 'src/QS3D.BricsCAD.V25') -Recurse -Filter '*.cs'", 1)),
    ("V26", v26.replace("Get-SafeSourceFiles -SourceRoot (Join-Path $root 'src/QS3D.BricsCAD.V26') -RepositoryRoot $root -Extension '.cs'", "Get-ChildItem (Join-Path $root 'src/QS3D.BricsCAD.V26') -Recurse -Filter '*.cs'", 1)),
    ("V25", v25.replace("Copy-HeldPackageInput -SourcePath $path -DestinationPath (Join-Path $dist $name) -Label (\"V25 build artifact $name\")", "Copy-Item -LiteralPath $path -Destination (Join-Path $dist $name)", 1)),
    ("V26", v26.replace("Copy-HeldPackageInput -SourcePath $path -DestinationPath (Join-Path $dist $name) -Label (\"V26 build artifact $name\")", "Copy-Item -LiteralPath $path -Destination (Join-Path $dist $name)", 1)),
)
for major, mutated in mutations:
    if not inspect_script(mutated, major):
        errors.append(f"{major}: mutation probe unexpectedly remained accepted")

if errors:
    for error in errors:
        print(f"FAIL {error}")
    sys.exit(1)

print("PASS package source-input filesystem safety")
