#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/WallQuantityCommands.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml.cs"

errors = []

def require(text, token, label):
    if token not in text:
        errors.append(f"{label}: missing {token!r}")

command = COMMAND.read_text(encoding="utf-8")
xaml = XAML.read_text(encoding="utf-8")
code = CODE.read_text(encoding="utf-8")

require(command, '[CommandMethod("QS3DWALLQTY"', "command")
require(command, "new WallQuantityWindow(document)", "command")
require(code, "ProjectContextCoordinator.TryGetReadOnly", "read-only project")
require(code, "ProjectStateSnapshot.CreateDetachedCopy", "detached snapshot")
require(code, "RegenerateDirty(snapshot)", "detached regen")
require(code, "ProjectQuantityReportBuilder.Detail(snapshot)", "canonical detail report")
require(code, "XlsxQuantityExporter.Export", "xlsx export")
require(code, "ElementCategory.StructuralWall", "wall filter")
require(code, "ElementCategory.ArchitecturalWall", "wall filter")
require(code, "ElementCategory.GlassWall", "wall filter")
require(code, "ElementCategory.WallPier", "wall filter")
require(code, "DocumentBoundWindowLifetime.Attach", "document lifetime")
require(code, "_sourceProjectId", "project identity")
require(code, "_suppressFilterEvents = true;\n            InitializeComponent();", "initialization guard")
require(xaml, 'x:Name="WallList"', "wall browser")
require(xaml, 'x:Name="TakeoffGrid"', "detail grid")
require(xaml, 'x:Name="SelectedThicknessText"', "selected facts")
require(xaml, 'x:Name="TotalNetText"', "totals")

for forbidden in (
    "ProjectContextCoordinator.GetOrCreate",
    "ProjectContextCoordinator.SetCurrent",
    "QsdbProjectStore",
    "TransactionManager",
    "StartTransaction(",
):
    if forbidden in code:
        errors.append(f"read-only boundary: forbidden token {forbidden!r}")

try:
    ET.parse(XAML)
except ET.ParseError as exc:
    errors.append(f"XAML XML parse failed: {exc}")

if errors:
    print("wall quantity preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("wall quantity preflight: PASS")
