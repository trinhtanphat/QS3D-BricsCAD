#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/MepTakeoffCommands.cs"
DOC = ROOT / "docs/CUBICOST-MEP-ADAPTER-V25.md"
errors = []


def require(text, token, label):
    if token not in text:
        errors.append(f"{label}: missing {token!r}")


def forbid(text, token, label):
    if token in text:
        errors.append(f"{label}: forbidden {token!r}")


for path in (SOURCE, DOC):
    if not path.exists():
        errors.append(f"missing required file: {path.relative_to(ROOT)}")

if errors:
    print("Cubicost MEP adapter preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")

require(source, '[CommandMethod("QS3DMEPTAKEOFF", CommandFlags.UsePickSet)]', "takeoff command")
require(source, '[CommandMethod("QS3DMEPCLASH", CommandFlags.UsePickSet)]', "clash command")
require(source, "EntitySnapshotReader.ReadCurrentSelection(document)", "canonical selection reader")
require(source, "CadUnitService.GetPolicy(document)", "canonical unit policy")
require(source, "snapshot.LengthDrawingUnits", "real native length metric")
require(source, "units.AreaToSquareMeters", "area conversion")
require(source, "units.VolumeToCubicMeters", "volume conversion")
require(source, "CadHandleService.Resolve(document, selectedByHandle.Keys)", "live handle resolution")
require(source, "StartOpenCloseTransaction()", "read transaction")
require(source, "OpenMode.ForRead", "read-only entity open")
require(source, "entity.GeometricExtents", "native extents")
require(source, "new AxisAlignedBox(", "Core clash envelope")
require(source, "new MepQuantityService().Aggregate", "Core MEP aggregation")
require(source, "new ClashDetectionService().Detect", "Core clash detection")
require(source, "if (!TryCreateMepElement", "unclassified skip")
require(source, "IsMep(disciplineById", "MEP-pair filter")

for forbidden in (
    "OpenMode.ForWrite",
    "ProjectContextCoordinator.GetOrCreate",
    "ProjectContextCoordinator.SetCurrent",
    "ExistingProjectMutationContext",
    "ProjectStateSnapshot",
    "QsdbProjectStore",
    "AppendEntity",
    "AppendEntityToModelSpace",
    "Erase(",
    "TransformBy(",
    "BooleanOperation(",
    "Task.Run",
    "Parallel.For",
):
    forbid(source, forbidden, "read-only adapter boundary")

for fake_length in (
    "DistanceTo(extents.MaxPoint)",
    "Math.Sqrt((extents.MaxPoint",
    "bounding-box diagonal",
):
    forbid(source, fake_length, "no bounding-box quantity length")

require(doc, "QS3DMEPTAKEOFF", "takeoff documentation")
require(doc, "QS3DMEPCLASH", "clash documentation")
require(doc, "PENDING_LOCAL / DO_NOT_RETRY_REMOTE", "local qualification boundary")
require(doc, "never invents length", "quantity integrity documentation")
require(doc, "OpenMode.ForRead", "read-only documentation")

if errors:
    print("Cubicost MEP adapter preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost MEP adapter preflight: PASS")
