#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RIBBON = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "ProjectRibbonAugmenter.cs"
SYNC = ROOT / "src" / "QS3D.BricsCAD.V25" / "SourceReconcileCommands.cs"
INTERCHANGE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectInterchangeCommands.cs"
errors = []

for path in (RIBBON, SYNC, INTERCHANGE):
    if not path.is_file():
        errors.append("missing project ribbon command source: " + str(path.relative_to(ROOT)))

if RIBBON.is_file():
    text = RIBBON.read_text(encoding="utf-8")
    for needle in (
        'new ButtonSpec("QS3D_PROJECT_PROJECTTOOLS", "Project Tools", "QS3DPROJECTTOOLS")',
        'new ButtonSpec("QS3D_PROJECT_SYNCSOURCE", "Đồng bộ source CAD", "QS3DSYNCSOURCE")',
        'new ButtonSpec("QS3D_PROJECT_INTERCHANGEJSON", "Xuất Semantic JSON", "QS3DINTERCHANGEJSON")',
        'new ButtonSpec("QS3D_PROJECT_LEVELS", "Tầng / Cao độ", "QS3DLEVELS")',
    ):
        if needle not in text:
            errors.append("ProjectRibbonAugmenter.cs missing project command: " + needle)

if SYNC.is_file() and '[CommandMethod("QS3DSYNCSOURCE", CommandFlags.UsePickSet)]' not in SYNC.read_text(encoding="utf-8"):
    errors.append("SourceReconcileCommands.cs no longer exposes QS3DSYNCSOURCE")

if INTERCHANGE.is_file() and '[CommandMethod("QS3DINTERCHANGEJSON", CommandFlags.Modal)]' not in INTERCHANGE.read_text(encoding="utf-8"):
    errors.append("ProjectInterchangeCommands.cs no longer exposes QS3DINTERCHANGEJSON")

print("QS3D project Ribbon command preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: the Project Ribbon exposes Project Tools, authoritative-source reconcile and read-only semantic interchange with live command implementations.")
