#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Rebar/RebarShapePath.cs",
    "src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/ShapeRebarGeometryCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/ModelReviewService.cs",
    "src/QS3D.BricsCAD.V25/ModelReviewCommands.cs",
    "tests/QS3D.Core.SmokeTests/RebarShapeGeometrySmoke.cs",
]
for rel in required:
    if not (ROOT / rel).exists(): errors.append("missing: " + rel)
owners = {}
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    for command in re.findall(r'\[CommandMethod\("([^\"]+)"', text): owners.setdefault(command.upper(), []).append(str(path.relative_to(ROOT)))
for command, paths in sorted(owners.items()):
    if len(paths) > 1: errors.append("duplicate CommandMethod " + command + ": " + ", ".join(paths))
for command in ("QS3DREBAR3DSHAPE", "QS3DHIGHLIGHT", "QS3DUNHIGHLIGHT", "QS3DFOCUS", "QS3DISOLATE", "QS3DUNISOLATE"):
    if command not in owners: errors.append("missing command: " + command)
shape = (ROOT / "src/QS3D.Core/Rebar/RebarShapePath.cs").read_text(encoding="utf-8") if (ROOT / "src/QS3D.Core/Rebar/RebarShapePath.cs").exists() else ""
for needle in ("RebarShapeLegsM", "RebarShapeTurnsDeg", "MaxLegs", "ValidateTotal", 'code == "11"', 'code == "21"', 'code == "31"'):
    if needle not in shape: errors.append("shape guard missing: " + needle)
builder = (ROOT / "src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs").read_text(encoding="utf-8") if (ROOT / "src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs").exists() else ""
for needle in ("MaxBarsPerElement", "MaxBarsPerBatch", "ProjectRebarScheduleBuilder.Build", "RebarShapePathBuilder.Build", "BoolUnite", "GeneratedShapeRebarHandles", "RebarCoverM"):
    if needle not in builder: errors.append("shape rebar builder guard missing: " + needle)
review = (ROOT / "src/QS3D.BricsCAD.V25/Cad/ModelReviewService.cs").read_text(encoding="utf-8") if (ROOT / "src/QS3D.BricsCAD.V25/Cad/ModelReviewService.cs").exists() else ""
for needle in ("SetImpliedSelection", "Highlight()", "Unhighlight()", "ClearHighlight"):
    if needle not in review: errors.append("review guard missing: " + needle)
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/RebarShapeGeometrySmoke.cs").read_text(encoding="utf-8") if (ROOT / "tests/QS3D.Core.SmokeTests/RebarShapeGeometrySmoke.cs").exists() else ""
for needle in ("[ModuleInitializer]", "LShape();", "UShape();", "CustomTurns();", "RejectsMissingDimensions();", "RejectsLengthMismatch();"):
    if needle not in smoke: errors.append("shape smoke missing: " + needle)
print("QS3D advanced geometry preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: shape-path rebar and transient review integration markers are present.")
