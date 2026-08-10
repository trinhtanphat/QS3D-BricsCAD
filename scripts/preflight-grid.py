#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = {
    "src/QS3D.BricsCAD.V25/GridCommands.cs": [
        'CommandMethod("QS3DGRID"',
        "CommandFlags.UsePickSet",
        'ElementCategory.Grid',
        'SemanticCaptureService.Capture(document, ElementCategory.Grid)',
        'string.Equals(entityType, "Line"',
        'string.Equals(entityType, "Arc"',
        'LengthDrawingUnits.HasValue',
        'double.IsNaN(x.LengthDrawingUnits.Value)',
        'double.IsInfinity(x.LengthDrawingUnits.Value)',
        'reference/takeoff semantic',
        'không sinh native 3D',
    ],
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml": [
        'Header="Lưới Trục" Tag="Grid"',
    ],
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs": [
        'case ElementCategory.Grid: return "QS3DGRID";',
    ],
    "src/QS3D.Core/Services/StructuralRegenerator.cs": [
        'public sealed class GenericTakeoffRegenerator',
        'category == ElementCategory.CustomQuantity || category == ElementCategory.Grid',
        'element.SetQuantity("LengthM", length);',
        'element.SetQuantity("AreaM2", area);',
        'element.SetQuantity("Count", 1d);',
    ],
    "src/QS3D.Core/Services/RegenerationEngine.cs": [
        'new GenericTakeoffRegenerator()',
    ],
}

for relative, needles in required.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing Grid dependency: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing Grid contract: " + needle)

command_root = ROOT / "src/QS3D.BricsCAD.V25"
commands = []
if command_root.is_dir():
    for path in command_root.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text))
if commands.count("QS3DGRID") != 1:
    errors.append("QS3DGRID must be declared exactly once, found " + str(commands.count("QS3DGRID")))

native = ROOT / "src/QS3D.BricsCAD.V25/Cad/NativeBuildCapability.cs"
if native.is_file():
    text = native.read_text(encoding="utf-8")
    support_block = text.split("public static bool Supports", 1)[-1]
    if "ElementCategory.Grid" in support_block:
        errors.append("Grid is a semantic reference/takeoff category and must not be advertised as native 3D buildable")

source = ROOT / "src/QS3D.BricsCAD.V25/GridCommands.cs"
if source.is_file():
    text = source.read_text(encoding="utf-8")
    for forbidden in (
        "Solid3d",
        "AppendEntity",
        "BoolSubtract",
        "CreateExtrudedSolid",
        "GeneratedSolidHandle",
    ):
        if forbidden in text:
            errors.append("QS3DGRID must stay non-destructive semantic reference capture; forbidden native token: " + forbidden)

print("QS3D Grid semantic preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Lưới Trục routes to QS3DGRID, accepts guarded LINE/ARC source, captures semantic Grid, regenerates deterministic takeoff quantities, and remains non-native-3D.")
