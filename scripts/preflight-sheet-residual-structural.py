#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(relative):
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing required source: " + relative)
        return ""
    return path.read_text(encoding="utf-8")


structural = read("src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs")
snapshots = read("src/QS3D.BricsCAD.V25/Cad/EntitySnapshotReader.cs")
bootstrapper = read("src/QS3D.BricsCAD.V25/Updates/UpdateBootstrapper.cs")
commands = read("src/QS3D.BricsCAD.V25/Updates/UpdateCommands.cs")

for token, message in {
    "entity is Arc arc": "Beam ARC source routing missing",
    "entity is Circle circle": "round structural CIRCLE source routing missing",
    "CadPolylinePathReader.ReadOpenWcsXy": "curved Beam POLYLINE tessellation missing",
    "SampleCircularPath": "curved/round Beam circular tessellation missing",
    "BooleanOperation(BooleanOperationType.BoolUnite": "curved Beam segmented solid union missing",
    "BuildClosedProfilePrism(document, project, circle": "Slab/Column CIRCLE extrusion routing missing",
    "solid.CreateExtrudedSolid(profile": "closed structural profile extrusion missing",
}.items():
    if token not in structural:
        errors.append(message)

if "if (entity is Circle circle)" not in snapshots or "Math.PI * circle.Radius * circle.Radius" not in snapshots:
    errors.append("CIRCLE snapshot area metric missing")

if "UpdateCenterWindowHost.Show" in bootstrapper:
    errors.append("automatic update discovery must remain non-modal")
if "UpdateCenterWindowHost.Show();" not in commands:
    errors.append("explicit QS3DUPDATE command must still open Update Center")
if "AutomaticUpdateFound += OnAutomaticUpdateFound" not in bootstrapper:
    errors.append("automatic non-modal update notification subscription missing")

if errors:
    print("QS3D Sheet residual structural/update preflight FAILED")
    for error in errors:
        print("ERROR:", error)
    raise SystemExit(1)

print("PASS: curved/round structural sources and non-modal automatic update discovery are source-guarded.")
raise SystemExit(0)
