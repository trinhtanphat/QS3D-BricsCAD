#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Mep/MepRecognition.cs"
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25/MepTakeoffCommands.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepRecognitionSmoke.cs"
DOC = ROOT / "docs/CUBICOST-MEP-RECOGNITION-LOCATE.md"
errors = []


def require(text, token, label):
    if token not in text:
        errors.append(f"{label}: missing {token!r}")


def forbid(text, token, label):
    if token in text:
        errors.append(f"{label}: forbidden {token!r}")


for path in (CORE, ADAPTER, SMOKE, DOC):
    if not path.exists():
        errors.append(f"missing required file: {path.relative_to(ROOT)}")

if errors:
    print("Cubicost MEP recognition/Locate preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

core = CORE.read_text(encoding="utf-8")
adapter = ADAPTER.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")

for token in (
    "public sealed class MepRecognitionRule",
    "public sealed class MepRecognitionProfile",
    "public enum MepRecognitionStatus",
    "Ambiguous",
    "highestPriority",
    "SameClassification",
    "public static MepRecognitionProfile CreateDefault()",
    "MepRecognitionSource.LayerOrBlockName",
):
    require(core, token, "Core recognition contract")

for token in (
    '[CommandMethod("QS3DMEPCLASHLOCATE", CommandFlags.UsePickSet)]',
    "MepRecognitionProfiles.CreateDefault()",
    "RecognitionProfile.Recognize(snapshot.Layer, blockName)",
    "recognition.Status != MepRecognitionStatus.Matched",
    "CadHandleService.SelectIfAny(document",
    "MaxLocateReviewPairs",
    "PromptIntegerOptions",
):
    require(adapter, token, "V25 recognition/Locate adapter")

for forbidden in (
    "private static bool ContainsAny(",
    "private static string ClassificationText(",
    "private static string StructuralCategory(",
    "private static string ArchitecturalCategory(",
    "OpenMode.ForWrite",
    "AppendEntity",
    "Erase(",
    "TransformBy(",
    "BooleanOperation(",
    "ProjectContextCoordinator.GetOrCreate",
    "ProjectContextCoordinator.SetCurrent",
    "Task.Run",
    "Parallel.For",
):
    forbid(adapter, forbidden, "adapter safety/configurability boundary")

for token in (
    "DefaultProfilePriorityAndCase",
    "BlockNameRecognition",
    "ExplicitPriority",
    "AmbiguityFailsClosed",
    "UnmatchedFailsClosed",
    "MepRecognitionStatus.Ambiguous",
):
    require(smoke, token, "recognition smoke coverage")

for token in (
    "QS3DMEPCLASHLOCATE",
    "fail-closed",
    "CadHandleService.SelectIfAny",
    "PENDING_LOCAL / DO_NOT_RETRY_REMOTE",
):
    require(doc, token, "recognition/Locate documentation")

if errors:
    print("Cubicost MEP recognition/Locate preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost MEP recognition/Locate preflight: PASS")
