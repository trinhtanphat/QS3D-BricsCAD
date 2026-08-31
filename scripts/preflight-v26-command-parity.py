#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

COMMAND_PATTERN = re.compile(r'\[CommandMethod\("([^\"]+)"')
EXPECTED_UPDATE_VERSION_COMMANDS = {"QS3DUPDATE", "QSUPDATE", "QS3DVER", "QSVER"}
LINKED_V25_UPDATE_SOURCES = {
    "SemanticReleaseVersion.cs",
    "UpdateBootstrapper.cs",
    "UpdateCenterWindow.cs",
    "UpdateCoordinator.cs",
    "UpdatePreferences.cs",
    "UpdateSettingsCommands.cs",
}


def read(rel):
    path = ROOT / rel
    if not path.is_file():
        errors.append(f"missing required V26 command-parity file: {rel}")
        return ""
    return path.read_text(encoding="utf-8")


def command_names(text):
    return {match.group(1).upper() for match in COMMAND_PATTERN.finditer(text)}


v25_update_commands = read("src/QS3D.BricsCAD.V25/Updates/UpdateCommands.cs")
v26_update_commands = read("src/QS3D.BricsCAD.V26/Updates/UpdateCommands.cs")
v26_project = read("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj")
package = read("scripts/package-v26.ps1")
qualification = read("docs/LOCAL-V26-QUALIFICATION.md")

v25_commands = command_names(v25_update_commands)
v26_commands = command_names(v26_update_commands)

missing_v25 = EXPECTED_UPDATE_VERSION_COMMANDS - v25_commands
if missing_v25:
    errors.append("V25 update/version command contract is missing: " + ", ".join(sorted(missing_v25)))

missing_v26 = EXPECTED_UPDATE_VERSION_COMMANDS - v26_commands
if missing_v26:
    errors.append("V26 update/version command parity is missing: " + ", ".join(sorted(missing_v26)))

extra_v26 = v26_commands - v25_commands
if extra_v26:
    errors.append("V26 update command surface has unreviewed V25 parity drift: " + ", ".join(sorted(extra_v26)))

for command in EXPECTED_UPDATE_VERSION_COMMANDS:
    if f'CommandMethod("{command}"' not in v26_update_commands:
        errors.append(f"V26 source does not register expected command {command}")

for token in (
    "loaded QS3D V26 assembly",
    "V26 GitHub Releases channel",
    "UpdateCenterWindowHost.Show();",
    "RuntimeDiagnosticsCommands",
):
    if token not in v26_update_commands:
        errors.append(f"V26 update/version implementation missing host-major-safe token: {token}")

for token in ("BricsCAD V25", "QS3D-BricsCAD-V25", "update-v25.ps1"):
    if token in v26_update_commands:
        errors.append(f"V26 update/version implementation leaked V25-specific token: {token}")

# The V26 project deliberately excludes V25 Updates/** and opts only a reviewed
# host-neutral subset back in. The package command inventory must mirror that
# compile surface instead of scanning excluded V25 command source.
for token in (
    "..\\QS3D.BricsCAD.V25\\Updates\\**\\*.cs",
    "Updates\\UpdateSettingsCommands.cs",
):
    if token not in v26_project:
        errors.append(f"V26 project compile-surface contract missing: {token}")

for linked_source in LINKED_V25_UPDATE_SOURCES:
    token = f"'{linked_source}'"
    if token not in package:
        errors.append(f"V26 package command inventory missing linked updater source: {linked_source}")

for token in (
    "function Add-CommandMethodsFromSource",
    "function Get-SafeSourceFiles",
    "$_ .Name -ne 'PluginEntry.cs'".replace("$_ .", "$_."),
    "StartsWith((Join-Path $v25Root 'Updates')",
    "Required V26 command was not discovered from compiled source",
    "Get-SafeSourceFiles -SourceRoot (Join-Path $root 'src/QS3D.BricsCAD.V26') -RepositoryRoot $root -Extension '.cs'",
):
    if token not in package:
        errors.append(f"V26 package command inventory guard missing: {token}")

legacy_scan = "Get-ChildItem (Join-Path $root 'src/QS3D.BricsCAD.V25') -Recurse -Filter '*.cs' | ForEach-Object"
if legacy_scan in package:
    errors.append("V26 package still scans the entire V25 source tree as if every file were compiled into V26")

unsafe_v26_scan = "Get-ChildItem (Join-Path $root 'src/QS3D.BricsCAD.V26') -Recurse -Filter '*.cs'"
if unsafe_v26_scan in package:
    errors.append("V26 package command inventory must use reparse-safe source traversal")

for token in ("LOCAL_ONLY", "DO_NOT_RETRY_REMOTE", "net8.0-windows"):
    if token not in qualification:
        errors.append(f"V26 qualification boundary missing expected token: {token}")

print("QS3D V26 compiled-command parity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: V26 exposes the reviewed V25 update/version aliases and package COMMANDS.txt "
    "is derived from the V26 compile surface rather than excluded V25-only command source."
)
