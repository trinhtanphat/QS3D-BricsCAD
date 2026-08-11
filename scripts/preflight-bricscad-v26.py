#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(rel):
    path = ROOT / rel
    if not path.is_file():
        errors.append(f"missing required V26 compatibility file: {rel}")
        return ""
    return path.read_text(encoding="utf-8")


def require(text, token, label):
    if token not in text:
        errors.append(f"{label} missing required token: {token}")


v25 = read("src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj")
v26 = read("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj")
entry = read("src/QS3D.BricsCAD.V26/PluginEntry.cs")
update_stub = read("src/QS3D.BricsCAD.V26/Updates/UpdateCommands.cs")
workflow = read(".github/workflows/bricscad-v26.yml")
runtime = read("scripts/test-bricscad-v26-runtime.ps1")
qualification = read("docs/LOCAL-V26-QUALIFICATION.md")
core = read("src/QS3D.Core/QS3D.Core.csproj")

for token in (
    "<TargetFramework>net48</TargetFramework>",
    "QS3D.BricsCAD.V25",
    "BRICSCAD_V25_DIR",
):
    require(v25, token, "V25 project")

for token in (
    "<TargetFramework>net8.0-windows</TargetFramework>",
    "<AssemblyName>QS3D.BricsCAD.V26</AssemblyName>",
    "<RootNamespace>QS3D.BricsCAD.V25</RootNamespace>",
    "BRICSCAD_V26_DIR",
    "..\\QS3D.BricsCAD.V25\\**\\*.cs",
    "..\\QS3D.BricsCAD.V25\\PluginEntry.cs",
    "..\\QS3D.BricsCAD.V25\\Updates\\**\\*.cs",
    "<Reference Include=\"BrxMgd\">",
    "<Reference Include=\"TD_Mgd\">",
    "<Private>false</Private>",
    "ValidateBricsCadV26References",
):
    require(v26, token, "V26 project")

if "BRICSCAD_V25_DIR" in v26:
    errors.append("V26 project must never resolve managed references through BRICSCAD_V25_DIR")
if "net48" in v26:
    errors.append("V26 project must not fall back to net48")
if "QS3D-BricsCAD-V25.update.json" in v26:
    errors.append("V26 project must not embed the V25 updater channel")

require(entry, "public sealed class PluginEntry : IExtensionApplication", "V26 PluginEntry")
if "UpdateBootstrapper" in entry or ".Updates" in entry:
    errors.append("V26 PluginEntry must not start the V25 updater until a V26 signed channel is qualified")

for token in ("QS3DUPDATE", "one-click update is intentionally disabled", "Do not install a V25 update package"):
    require(update_stub, token, "V26 update safety stub")
if "UpdateCenterWindowHost" in update_stub or "UpdateCoordinator" in update_stub:
    errors.append("V26 update safety stub must not invoke the V25 update implementation")

for token in (
    "workflow_dispatch:",
    "github.event_name == 'workflow_dispatch'",
    "runs-on: [self-hosted, windows, x64, bricscad-v26]",
    "BRICSCAD_V26_DIR",
    "dotnet-version: \"8.0.x\"",
    "preflight-bricscad-v26.py",
    "QS3D.BricsCAD.V26.csproj",
    "test-bricscad-v26-runtime.ps1",
):
    require(workflow, token, "V26 workflow")
for forbidden in ("\n  push:", "\n  pull_request:", "\n  schedule:", "\n  workflow_run:"):
    if forbidden in workflow:
        errors.append("V26 workflow must remain manual-only; forbidden trigger: " + forbidden.strip())

for token in (
    "FileMajorPart -ne 26",
    "QS3D.BricsCAD.V26.dll",
    "BrxMgd.dll",
    "TD_Mgd.dll",
    "QS3DRUNTIMEPROBE",
    "ribbon_ready",
    "palette_visible",
    "QS3D_RUNTIME_RESULT",
):
    require(runtime, token, "V26 runtime gate")
if "QS3D.BricsCAD.V25.dll" in runtime:
    errors.append("V26 runtime gate must reject/circumvent V25 adapter binaries, not load them")

for token in (
    "LOCAL_ONLY",
    "DO_NOT_RETRY_REMOTE",
    "net8.0-windows",
    "BRICSCAD_V26_DIR",
    "QS3D.BricsCAD.V26.dll",
    "BricsCAD V26",
    ".NET 8",
):
    require(qualification, token, "V26 local qualification")

require(core, "<TargetFramework>netstandard2.0</TargetFramework>", "Core project")

print("QS3D BricsCAD V26 source compatibility preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: V25 remains net48; V26 is isolated on net8.0-windows with V26-only refs, manual CI, runtime identity checks, and no V25 one-click updater cross-load.")