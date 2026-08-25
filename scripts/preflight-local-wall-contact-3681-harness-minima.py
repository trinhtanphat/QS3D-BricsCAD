#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HARNESS_FILES = (
    ROOT / "tests" / "QS3D.BricsCAD.V25.LocalQualification" / "WallContact3681SourceFixGateCommands.cs",
    ROOT / "tests" / "QS3D.BricsCAD.V25.LocalQualification" / "WallContact3681QualificationCommands.cs",
)


def fail(message: str) -> None:
    print("ERROR: #3681 harness minimum-corner preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


for path in HARNESS_FILES:
    text = path.read_text(encoding="utf-8")
    label = path.name
    required = (
        "var extents = solid.GeometricExtents;",
        "var desiredMin = new Point3d(x, y, z);",
        "solid.TransformBy(Matrix3d.Displacement(desiredMin - extents.MinPoint));",
    )
    for token in required:
        if token not in text:
            fail(label + " does not align CreateBox to the requested minimum corner: missing " + token)
    if "solid.TransformBy(Matrix3d.Displacement(new Vector3d(x, y, z)));" in text:
        fail(label + " still assumes Solid3d.CreateBox origin semantics")

print("PASS: both #3681 V25 harness CreateBox helpers align requested minima from native GeometricExtents")
