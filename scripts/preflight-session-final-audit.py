#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required_files = [
    "src/QS3D.Core/Rebar/BeamLongitudinalRebarPlanner.cs",
    "tests/QS3D.Core.SmokeTests/BeamRebarRegressionSmoke.cs",
    "tests/QS3D.Core.SmokeTests/BeamRebarSmokeRegistration.cs",
    "src/QS3D.Core/Rebar/OrthogonalRebarMatPlanner.cs",
    "src/QS3D.BricsCAD.V25/Cad/RebarMatSolidBuilder.cs",
    "src/QS3D.Core/Diagnostics/GeneratedRebarMatHealthService.cs",
    "src/QS3D.BricsCAD.V25/RebarMatCommands.cs",
    "src/QS3D.BricsCAD.V25/RebarMatHealthCommands.cs",
    "scripts/preflight-rebar-mat.py",
    "docs/REBAR-MAT3D.md",
]
for relative in required_files:
    if not (ROOT / relative).is_file():
        errors.append("missing final-audit file: " + relative)

checks = {
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs": ["GeneratedRebarMatHandles"],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedTieRebarOwnershipGuard.cs": ["GeneratedRebarMatHandles"],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs": ["GeneratedRebarMatHandles", "GeneratedRebarMatMode"],
    "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs": ["longitudinal rebar", "BeamLongitudinalBars", "ColumnVerticalBars", "REBAR_GENERATED_STALE", "GeneratedRebarMatHandles"],
    "src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs": ["GeneratedBeamStirrupHealthService", "GeneratedRebarMatHealthService", "GeneratedBeamStirrupHandles", "GeneratedRebarMatHandles"],
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml": ["QS3DBEAMREBAR3D", "QS3DREBARSTIRRUP3D", "QS3DREBARTIES3D", "QS3DREBARMAT3D", "QS3DREBARMATHEALTH", "QS3DREBARHEALTHALL"],
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs": ["QS3DBEAMREBAR3D", "QS3DREBARSTIRRUP3D", "QS3DREBARTIES3D", "QS3DREBARMAT3D", "QS3DREBARMATHEALTH", "QS3DREBARHEALTHALL"],
    "src/QS3D.Core/Rebar/RectangularRebarLayoutPlanner.cs": ["MaxBars", "requestedBars", "interpolation delta overflowed"],
    "tests/QS3D.Core.SmokeTests/BeamRebarSmokeRegistration.cs": ["BeamRebarRegressionSmoke.Run();"],
    "tests/QS3D.Core.SmokeTests/OrthogonalRebarMatSmokeRegistration.cs": ["OrthogonalRebarMatSmoke.Run();"],
    "docs/COMMANDS.md": ["QS3DBEAMREBAR3D", "QS3DREBARMAT3D", "QS3DREBARHEALTHALL", "REBAR-MAT3D.md"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing checked file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing contract token: " + needle)

hub = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
if hub.is_file():
    text = hub.read_text(encoding="utf-8")
    for wrong in ('Tag="QS3DBEAMSTIRRUP3D"', 'Tag="QS3DBEAMSTIRRUPHEALTH"'):
        if wrong in text:
            errors.append("Domain Hub contains stale/nonexistent command tag: " + wrong)

commands = []
commands_root = ROOT / "src/QS3D.BricsCAD.V25"
if commands_root.is_dir():
    for path in commands_root.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
upper = [x.upper() for x in commands]
if len(upper) != len(set(upper)):
    duplicates = sorted({x for x in upper if upper.count(x) > 1})
    errors.append("duplicate CommandMethod names: " + ", ".join(duplicates))
for required_command in ("QS3DBEAMREBAR3D", "QS3DREBARSTIRRUP3D", "QS3DREBARTIES3D", "QS3DREBARMAT3D", "QS3DREBARMATHEALTH", "QS3DREBARHEALTHALL"):
    if required_command not in upper:
        errors.append("missing command: " + required_command)

workflows = ROOT / ".github/workflows"
if workflows.is_dir():
    for path in workflows.glob("*.yml"):
        text = path.read_text(encoding="utf-8")
        if re.search(r"(?m)^\s*(push|pull_request|pull_request_target|schedule)\s*:", text):
            errors.append("workflow must remain manual-only: " + path.relative_to(ROOT).as_posix())
        if "workflow_dispatch" not in text:
            errors.append("workflow missing workflow_dispatch: " + path.relative_to(ROOT).as_posix())

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: session-audit Beam/Mat rebar, ownership, Health-All, UI command parity, smoke registration and manual-only workflow contracts are present.")
