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


def require_order(text, tokens, label):
    positions = []
    for token in tokens:
        pos = text.find(token)
        if pos < 0:
            errors.append(f"{label}: missing ordered token {token!r}")
            return
        positions.append(pos)
    if positions != sorted(positions):
        errors.append(f"{label}: unsafe order {tokens!r}")


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
require(code, "ResolveCurrentRow(currentProject, displayedView)", "locate current-row revalidation")
require(code, "currentProject.FindElement(elementId)", "locate semantic identity")
require(code, "IsWallCategory(currentElement.Category)", "locate current wall category")
require(code, "ProjectQuantityReportBuilder.Detail(currentSnapshot, new[] { elementId })", "locate detached current detail")
require(code, "SourceHandleResolver.Resolve(currentProject, currentRow.ElementIds)", "locate current handles")
require(code, "QS3D.BricsCAD.V25.Cad.CadHandleService.Select(_document, handles)", "native CAD select")
require(code, '_document.SendStringToExecute("QS3DZOOMSELECTED ', "native CAD zoom")
require(xaml, 'x:Name="WallList"', "wall browser")
require(xaml, 'x:Name="TakeoffGrid"', "detail grid")
require(xaml, 'x:Name="SelectedThicknessText"', "selected facts")
require(xaml, 'x:Name="TotalNetText"', "totals")
require(xaml, 'x:Name="AutoRevealCheck"', "auto 3D reveal")
require(xaml, 'Content="Định vị 3D"', "explicit 3D locate")
require(xaml, 'MouseDoubleClick="OnWallListDoubleClick"', "wall list double-click locate")
require(xaml, 'MouseDoubleClick="OnGridDoubleClick"', "detail double-click locate")

locate_start = code.find("private void LocateSelected")
locate_end = code.find("private QuantityReportRow ResolveCurrentRow", locate_start)
if locate_start < 0 or locate_end < 0:
    errors.append("locate flow: cannot isolate LocateSelected")
else:
    locate = code[locate_start:locate_end]
    require_order(
        locate,
        [
            'EnsureCurrentProject("định vị Tường trong View 3D")',
            "ResolveCurrentRow(currentProject, displayedView)",
            "SourceHandleResolver.Resolve(currentProject, currentRow.ElementIds)",
            "CadHandleService.Select(_document, handles)",
            'SendStringToExecute("QS3DZOOMSELECTED ',
        ],
        "locate revalidation/select/zoom order",
    )

for forbidden in (
    "ProjectContextCoordinator.GetOrCreate",
    "ProjectContextCoordinator.SetCurrent",
    "QsdbProjectStore",
    "TransactionManager",
    "StartTransaction(",
    "ProjectStateSnapshot.Capture(",
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
