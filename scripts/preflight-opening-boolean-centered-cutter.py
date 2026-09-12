#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs"
text = path.read_text(encoding="utf-8")
errors = []

create = text.find("cutter.CreateBox(cutterWidth, cutterDepth, cutterHeight);")
rotate = text.find("cutter.TransformBy(Matrix3d.Rotation(item.Angle", create)
target = text.find("cutter.TransformBy(Matrix3d.Displacement(new Vector3d(item.Target.X, item.Target.Y, item.Target.Z)))", create)
if min(create, rotate, target) < 0 or not (create < rotate < target):
    errors.append("physical opening cutter must create the centered box, rotate it, then place it at the planned target")

for forbidden in (
    "-cutterWidth / 2d",
    "-cutterDepth / 2d",
    "-cutterHeight / 2d",
):
    if forbidden in text[create:target if target >= 0 else None]:
        errors.append("physical opening cutter must not apply a duplicate half-extent translation: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)
print("PASS: physical opening cutter preserves BricsCAD V25 centered CreateBox placement before rotation/target translation.")
