#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.BricsCAD.V25/Cad/CadGeometryGuard.cs": [
        "public static double Subtract", "public static double Multiply", "public static double Hypot3"
    ],
    "src/QS3D.Core/Rebar/RectangularRebarLayoutPlanner.cs": [
        "MaxBars = 10000", "projectedBars", "Rectangular rebar layout exceeds the supported bar count"
    ],
    "src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs": [
        "MaxBarsPerElement = 1200", "MaxBarsPerBatch = 4000", "CadGeometryGuard.Midpoint",
        "CadGeometryGuard.Subtract", "CadGeometryGuard.Multiply", "CreateVerticalBar",
        "MaxBarsPerBatch - layout.BarCenters.Count"
    ],
    "src/QS3D.BricsCAD.V25/Cad/ColumnTieSolidBuilder.cs": [
        "CadGeometryGuard.Midpoint", "CadGeometryGuard.Subtract", "CadGeometryGuard.Multiply",
        "CadGeometryGuard.Hypot3", "MaxTiesPerBatch - layout.ElevationsM.Count"
    ],
    "src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs": [
        "duplicateSelectedSource", "đang thuộc nhiều QS3D element", "CadGeometryGuard.Hypot3",
        "CadGeometryGuard.Subtract", "CadGeometryGuard.Multiply", "geometry.rebar.beam.stirrup",
        "MaxStirrupsPerBatch - layout.Count"
    ],
    "tests/QS3D.Core.SmokeTests/RectangularRebarSafetySmoke.cs": [
        "NormalPerimeterLayoutIsDeterministic", "ExcessiveAllocationIsRejected", "ExtremeFiniteSectionKeepsFiniteCenters"
    ],
    "tests/QS3D.Core.SmokeTests/RectangularRebarSafetyRegistration.cs": [
        "ModuleInitializer", "RectangularRebarSafetySmoke.Run()"
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing rebar numeric safety file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing guard/token: " + needle)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: column longitudinal/tie and beam stirrup CAD pipelines have bounded allocation, finite transforms, duplicate-source protection and registered regression coverage.")
