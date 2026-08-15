#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/MepExactClashReviewCommands.cs"
DOC = ROOT / "docs/CUBICOST-MEP-EXACT-REVIEW-HIGHLIGHT-V25.md"
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
    print("Cubicost exact clash highlight preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
doc = DOC.read_text(encoding="utf-8")

for token in (
    '[CommandMethod("QS3DMEPEXACTCLASHHIGHLIGHT", CommandFlags.UsePickSet)]',
    "snapshots.Count != 2",
    "MepRecognitionProfiles.CreateDefault()",
    "CadHandleService.Resolve(document",
    "ids.Count != 2",
    "OpenMode.ForRead",
    "left.CheckInterference(right)",
    "left.Highlight()",
    "right.Highlight()",
    "highlightApplied = true",
    "document.Editor.SetImpliedSelection",
    "document.Editor.GetString",
    "finally",
    "if (highlightApplied",
    "UnhighlightBestEffort",
    "entity.Unhighlight()",
    "if (rightHighlighted",
    "if (leftHighlighted",
):
    require(source, token, "exact review source")

for forbidden in (
    "OpenMode.ForWrite",
    "BooleanOperation(",
    "Clone(",
    "AppendEntity",
    "Erase(",
    "TransformBy(",
    "ProjectContextCoordinator.GetOrCreate",
    "ProjectContextCoordinator.SetCurrent",
    "QsdbProjectStore",
    "Task.Run",
    "Parallel.For",
    "highlightOwned",
):
    forbid(source, forbidden, "read-only highlight boundary")

for token in (
    "QS3DMEPEXACTCLASHHIGHLIGHT",
    "CheckInterference",
    "Unhighlight",
    "PENDING_LOCAL / DO_NOT_RETRY_REMOTE",
    "ownership token",
    "not a PASS requirement",
):
    require(doc, token, "exact review documentation")

for forbidden in (
    "acquired ownership of both highlights",
    "removes only highlight state owned by this command",
):
    forbid(doc, forbidden, "truthful highlight contract")

if errors:
    print("Cubicost exact clash highlight preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Cubicost exact clash highlight preflight: PASS")
