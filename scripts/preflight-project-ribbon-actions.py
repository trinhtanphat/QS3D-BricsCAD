#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RIBBON = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "ProjectRibbonAugmenter.cs"
SYNC = ROOT / "src" / "QS3D.BricsCAD.V25" / "SourceReconcileCommands.cs"
INTERCHANGE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectInterchangeCommands.cs"
INTERCHANGE_VALIDATE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectInterchangeValidationCommands.cs"
GRID = ROOT / "src" / "QS3D.BricsCAD.V25" / "GridCommands.cs"
GRID_NUMBER = ROOT / "src" / "QS3D.BricsCAD.V25" / "GridNamingCommands.cs"
GRID_ANNOTATION = ROOT / "src" / "QS3D.BricsCAD.V25" / "GridAnnotationCommands.cs"
errors = []

for path in (RIBBON, SYNC, INTERCHANGE, INTERCHANGE_VALIDATE, GRID, GRID_NUMBER, GRID_ANNOTATION):
    if not path.is_file():
        errors.append("missing project ribbon command source: " + str(path.relative_to(ROOT)))

if RIBBON.is_file():
    text = RIBBON.read_text(encoding="utf-8")
    for needle in (
        'new ButtonSpec("QS3D_PROJECT_PROJECTTOOLS", "Project Tools", "QS3DPROJECTTOOLS")',
        'new ButtonSpec("QS3D_PROJECT_SYNCSOURCE", "Đồng bộ source CAD", "QS3DSYNCSOURCE")',
        'new ButtonSpec("QS3D_PROJECT_INTERCHANGEJSON", "Xuất Semantic JSON", "QS3DINTERCHANGEJSON")',
        'new ButtonSpec("QS3D_PROJECT_INTERCHANGEVALIDATE", "Kiểm tra Semantic JSON", "QS3DINTERCHANGEVALIDATE")',
        'new ButtonSpec("QS3D_PROJECT_LEVELS", "Tầng / Cao độ", "QS3DLEVELS")',
        'new ButtonSpec("QS3D_PROJECT_GRID", "Grid / Trục", "QS3DGRID")',
        'new ButtonSpec("QS3D_PROJECT_GRIDNUMBER", "Đánh số Grid", "QS3DGRIDNUMBER")',
        'new ButtonSpec("QS3D_PROJECT_GRIDANNOTATE", "Gắn nhãn Grid", "QS3DGRIDANNOTATE")',
        'new ButtonSpec("QS3D_PROJECT_GRIDANNOTATEALL", "Gắn nhãn tất cả Grid", "QS3DGRIDANNOTATEALL")',
    ):
        if needle not in text:
            errors.append("ProjectRibbonAugmenter.cs missing project command: " + needle)

if SYNC.is_file() and '[CommandMethod("QS3DSYNCSOURCE", CommandFlags.UsePickSet)]' not in SYNC.read_text(encoding="utf-8"):
    errors.append("SourceReconcileCommands.cs no longer exposes QS3DSYNCSOURCE")

if INTERCHANGE.is_file() and '[CommandMethod("QS3DINTERCHANGEJSON", CommandFlags.Modal)]' not in INTERCHANGE.read_text(encoding="utf-8"):
    errors.append("ProjectInterchangeCommands.cs no longer exposes QS3DINTERCHANGEJSON")

if INTERCHANGE_VALIDATE.is_file() and '[CommandMethod("QS3DINTERCHANGEVALIDATE", CommandFlags.Modal)]' not in INTERCHANGE_VALIDATE.read_text(encoding="utf-8"):
    errors.append("ProjectInterchangeValidationCommands.cs no longer exposes QS3DINTERCHANGEVALIDATE")

if GRID.is_file() and '[CommandMethod("QS3DGRID", CommandFlags.UsePickSet)]' not in GRID.read_text(encoding="utf-8"):
    errors.append("GridCommands.cs no longer exposes QS3DGRID")

if GRID_NUMBER.is_file() and '[CommandMethod("QS3DGRIDNUMBER", CommandFlags.Modal)]' not in GRID_NUMBER.read_text(encoding="utf-8"):
    errors.append("GridNamingCommands.cs no longer exposes QS3DGRIDNUMBER")

if GRID_ANNOTATION.is_file():
    text = GRID_ANNOTATION.read_text(encoding="utf-8")
    if '[CommandMethod("QS3DGRIDANNOTATE", CommandFlags.UsePickSet)]' not in text:
        errors.append("GridAnnotationCommands.cs no longer exposes QS3DGRIDANNOTATE")
    if '[CommandMethod("QS3DGRIDANNOTATEALL", CommandFlags.Modal)]' not in text:
        errors.append("GridAnnotationCommands.cs no longer exposes QS3DGRIDANNOTATEALL")

print("QS3D project Ribbon command preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: the Project Ribbon exposes Project Tools, source reconcile, read-only semantic export/validation and the guarded Grid capture/numbering/native-annotation workflow with live command implementations.")
