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

    if major == "V25":
        required_calls = (
            "$source = Assert-SafeInputDirectory",
            "$sampleSource = Assert-SafeInputDirectory",
            "V25 build artifact $name",
            "release script $script",
            "package launcher $launcherName",
            "synthetic sample $sampleName",
            "Get-SafeSourceFiles -SourceRoot (Join-Path $root 'src/QS3D.BricsCAD.V25')",
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
    return found


for path, label in ((V25, "V25 package script"), (V26, "V26 package script"), (RUNBOOK, "runbook")):
    if not path.is_file():
        errors.append(f"missing {label}: {path.relative_to(ROOT)}")

v25 = V25.read_text(encoding="utf-8") if V25.is_file() else ""
v26 = V26.read_text(encoding="utf-8") if V26.is_file() else ""
runbook = RUNBOOK.read_text(encoding="utf-8") if RUNBOOK.is_file() else ""

errors.extend(inspect_script(v25, "V25"))
errors.extend(inspect_script(v26, "V26"))

for text, major in ((v25, "V25"), (v26, "V26")):
    before(text, "$source = Assert-SafeInputDirectory", "foreach ($name in $required)", major)
    before(text, "Assert-SafeInputFile -Path (Join-Path $source $name)", "Copy-Item -LiteralPath $path", major)
    require(text, "$item.Attributes -band [IO.FileAttributes]::ReparsePoint", major)
    require(text, "Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop", major)

require(runbook, "Lane-Key: `issue-4386`", "runbook")
require(runbook, "ordinary non-reparse", "runbook")
require(runbook, "before any packaging output mutation", "runbook")
require(runbook, "V25", "runbook")
require(runbook, "V26", "runbook")

# Mutation checks: removing the input binder or restoring recursive source scanning
# must make the reference inspector reject the candidate.
mutations = (
    ("V25", v25.replace("$source = Assert-SafeInputDirectory", "$source = Get-CanonicalFullPath", 1)),
    ("V26", v26.replace("$generator = Assert-SafeInputFile", "$generator = Get-CanonicalFullPath", 1)),
    ("V25", v25.replace("Get-SafeSourceFiles -SourceRoot (Join-Path $root 'src/QS3D.BricsCAD.V25') -RepositoryRoot $root -Extension '.cs'", "Get-ChildItem (Join-Path $root 'src/QS3D.BricsCAD.V25') -Recurse -Filter '*.cs'", 1)),
    ("V26", v26.replace("Get-SafeSourceFiles -SourceRoot (Join-Path $root 'src/QS3D.BricsCAD.V26') -RepositoryRoot $root -Extension '.cs'", "Get-ChildItem (Join-Path $root 'src/QS3D.BricsCAD.V26') -Recurse -Filter '*.cs'", 1)),
)
for major, mutated in mutations:
    if not inspect_script(mutated, major):
        errors.append(f"{major}: mutation probe unexpectedly remained accepted")

if errors:
    for error in errors:
        print(f"FAIL {error}")
    sys.exit(1)

print("PASS package source-input filesystem safety")
