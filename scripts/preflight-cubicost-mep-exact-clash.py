#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/MepExactClashCommands.cs"
DOC = ROOT / "docs/CUBICOST-MEP-EXACT-CLASH-V25.md"
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
    print("Cubicost exact MEP clash preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")

for token in (
    '[CommandMethod("QS3DMEPEXACTCLASH", CommandFlags.UsePickSet)]',
    "EntitySnapshotReader.ReadCurrentSelection(document)",
    "CadHandleService.Resolve(document, snapshotByHandle.Keys)",
    "MepRecognitionProfiles.CreateDefault()",
    "recognition.Status != MepRecognitionStatus.Matched",
    "as Solid3d",
    "solid.GeometricExtents",
    "ExtentsMayIntersect",
    "CheckInterference(right.Solid)",
    "MaxRecognizedSolids",
    "MaxBroadPhasePairs",
    "StartOpenCloseTransaction()",
    "OpenMode.ForRead",
):
    require(source, token, "exact clash source contract")

for forbidden in (
    "OpenMode.ForWrite",
    "BooleanOperation(",
    "AppendEntity",
    "AppendEntityToModelSpace",
    "Erase(",
    "TransformBy(",
    ".Clone(",
    ".Copy(",
    "ProjectContextCoordinator.GetOrCreate",
    "ProjectContextCoordinator.SetCurrent",
    "QsdbProjectStore",
    "Task.Run",
    "Parallel.For",
):
    forbid(source, forbidden, "read-only exact clash boundary")

for token in (
    "QS3DMEPEXACTCLASH",
    "Solid3d.CheckInterference",
    "500 recognized",
    "100,000",
    "PENDING_LOCAL / DO_NOT_RETRY_REMOTE",
):
    require(doc, token, "exact clash documentation")

if errors:
    print("Cubicost exact MEP clash preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost exact MEP clash preflight: PASS")
