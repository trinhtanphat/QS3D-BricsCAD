#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CONTRACTS = ROOT / "src/QS3D.Core/Interoperability/Sap2000/Sap2000Contracts.cs"
BRIDGE = ROOT / "src/QS3D.BricsCAD.V25/Sap2000/Sap2000ComBridge.cs"
GEOMETRY = ROOT / "src/QS3D.BricsCAD.V25/Sap2000/Sap2000GeometryExporter.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Sap2000/Sap2000Commands.cs"
V25 = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
V26 = ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"
errors = []

for path in (CONTRACTS, BRIDGE, GEOMETRY, COMMANDS, V25, V26):
    if not path.is_file():
        errors.append("missing required SAP2000 integration path: " + str(path.relative_to(ROOT)))

if not errors:
    contracts = CONTRACTS.read_text(encoding="utf-8")
    bridge = BRIDGE.read_text(encoding="utf-8")
    geometry = GEOMETRY.read_text(encoding="utf-8")
    commands = COMMANDS.read_text(encoding="utf-8")
    v25 = V25.read_text(encoding="utf-8")
    v26 = V26.read_text(encoding="utf-8")

    for token in (
        "public struct Sap2000Point3",
        "public sealed class Sap2000FrameMember",
        "public sealed class Sap2000AreaMember",
        "public sealed class Sap2000ExportPlan",
    ):
        if token not in contracts:
            errors.append("Core SAP2000 contract missing token: " + token)

    for token in (
        'HelperProgId = "SAP2000v1.Helper"',
        'SapObjectProgId = "CSI.SAP2000.API.SapObject"',
        'Invoke(helper, "GetObject", SapObjectProgId)',
        'Invoke(helper, "CreateObjectProgID", SapObjectProgId)',
        'InvokeStatus(sapObject, "ApplicationStart")',
        'InvokeStatusWithArguments(frameObject, "AddByCoord", arguments)',
        'InvokeStatusWithArguments(areaObject, "AddByCoord", arguments)',
        'InvokeStatus(analyze, "RunAnalysis")',
        'InvokeStatus(file, "Save", path)',
        "Marshal.FinalReleaseComObject",
    ):
        if token not in bridge:
            errors.append("late-bound SAP2000 OAPI bridge missing token: " + token)

    if "ApplicationExit" in bridge:
        errors.append("QS3D SAP bridge must not close a user-owned SAP2000 process")

    for token in (
        "document.Editor.GetSelection(options)",
        "entity as Line",
        "entity as Polyline",
        "polyline.Closed",
        "GetMetresPerDrawingUnit(document.Database.Insunits)",
        "UnitsValue.Millimeters",
        "UnitsValue.Meters",
    ):
        if token not in geometry:
            errors.append("SAP2000 CAD geometry contract missing token: " + token)

    for command in (
        "QS3DSAPCONNECT",
        "QS3DSAPNEW",
        "QS3DSAPEXPORT",
        "QS3DSAPSAVE",
        "QS3DSAPRUN",
        "QS3DSAPRESET",
    ):
        token = '[CommandMethod("%s", CommandFlags.Modal)]' % command
        if token not in commands:
            errors.append("missing SAP2000 command registration: " + command)

    if "Sap2000GeometryExporter.BuildFromSelection(document)" not in commands:
        errors.append("SAP export command must build a detached export plan before OAPI mutation")
    if "InitializeBlankModel();" not in commands:
        errors.append("blank-model creation must remain an explicit command path")
    if "System.Windows.MessageBoxButton.YesNo" not in commands:
        errors.append("destructive blank-model command must require explicit confirmation")

    project_text = v25 + "\n" + v26
    forbidden_references = (
        '<Reference Include="SAP2000v1',
        '<Reference Include="SAP2000',
        '<PackageReference Include="SAP2000',
    )
    for token in forbidden_references:
        if token in project_text:
            errors.append("proprietary CSI compile-time dependency is forbidden: " + token)

    if '..\\QS3D.BricsCAD.V25\\**\\*.cs' not in v26:
        errors.append("V26 must continue linking shared V25 host source so SAP commands ship in both hosts")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: SAP2000 OAPI integration is late-bound, non-destructive by default, unit-aware, and shared by V25/V26 without proprietary CSI references.")
