#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Mep/MepRecognition.cs"
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25/MepTakeoffCommands.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepRecognitionSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/CUBICOST-MEP-RECOGNITION-LOCATE.md"
errors = []


def require(text, token, label):
    if token not in text:
        errors.append(f"{label}: missing {token!r}")


def forbid(text, token, label):
    if token in text:
        errors.append(f"{label}: forbidden {token!r}")


for path in (CORE, ADAPTER, SMOKE, REG, DOC):
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
registration = REG.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")

for token in (
    "public sealed class MepRecognitionRule",
    "public sealed class MepRecognitionProfile",
    "public static class MepRecognitionProfiles",
    "MepRecognitionStatus.Ambiguous",
    "rule.Priority > highestPriority",
    "rule.Priority < highestPriority",
    "!SameClassification(first, topMatches[i])",
    "MepRecognitionSource.LayerOrBlockName",
    '"mep.cable-tray"',
    '"mep.cable"',
):
    require(core, token, "Core recognition contract")

require(adapter, "MepRecognitionProfiles.CreateDefault()", "adapter shared profile")
require(adapter, "RecognitionProfile.Recognize(snapshot.Layer, blockName)", "adapter profile invocation")
require(adapter, '[CommandMethod("QS3DMEPCLASHLOCATE", CommandFlags.UsePickSet)]', "Locate command")
require(adapter, "new PromptIntegerOptions(", "native pair-number prompt")
require(adapter, "document.Editor.GetInteger(prompt)", "native integer input")
require(adapter, "CadHandleService.Resolve(document, new[] { clash.LeftElementId, clash.RightElementId })", "fresh pair Handle resolution")
require(adapter, "if (liveIds.Count != 2)", "exact pair guard")
require(adapter, "document.Editor.SetImpliedSelection(new List<ObjectId>(liveIds).ToArray())", "all-or-nothing implied selection")
require(adapter, "MaxLocateReviewPairs = 200", "bounded Locate review")

for removed_private_classifier in (
    "private static bool TryClassifyMep",
    "private static string StructuralCategory",
    "private static string ArchitecturalCategory",
    "private static bool ContainsAny",
):
    forbid(adapter, removed_private_classifier, "adapter classification centralization")

for forbidden in (
    "OpenMode.ForWrite",
    "ProjectContextCoordinator.GetOrCreate",
    "ProjectContextCoordinator.SetCurrent",
    "QsdbProjectStore",
    "AppendEntity",
    "Erase(",
    "TransformBy(",
    "BooleanOperation(",
    "Task.Run",
    "Parallel.For",
):
    forbid(adapter, forbidden, "read-only Locate boundary")

for token in (
    "DefaultProfilePriorityAndCase();",
    "BlockNameRecognition();",
    "ExplicitPriority();",
    "AmbiguityFailsClosed();",
    "UnmatchedFailsClosed();",
):
    require(smoke, token, "recognition smoke")
require(registration, "MepRecognitionSmoke.Run();", "smoke registration")

require(doc, "QS3DMEPCLASHLOCATE", "Locate documentation")
require(doc, "Ambiguous", "ambiguity documentation")
require(doc, "both", "all-or-nothing selection documentation")
require(doc, "PENDING_LOCAL / DO_NOT_RETRY_REMOTE", "local evidence boundary")

if errors:
    print("Cubicost MEP recognition/Locate preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost MEP recognition/Locate preflight: PASS")
