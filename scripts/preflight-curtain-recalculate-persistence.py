#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs"

errors = []
if not SOURCE.is_file():
    errors.append("missing CurtainWallWindow source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find("private void OnRecalculateClick")
    end = text.find("private void OnCommandClick", start)
    if start < 0 or end < 0:
        errors.append("cannot isolate Curtain OnRecalculateClick")
    else:
        body = text[start:end]
        required = [
            "ProjectStateSnapshot.Capture(project)",
            "var dirtyStateChanged = false",
            "var beforeDirty = element.Dirty",
            "element.MarkDirty(ElementDirtyFlags.Quantity)",
            "if (element.Dirty != beforeDirty) dirtyStateChanged = true",
            "if (dirtyStateChanged) project.Touch()",
            "RegenerateDirty(project)",
            "RestoreOrThrow(project, rollback, operationError",
        ]
        for token in required:
            if token not in body:
                errors.append("Curtain recalculate missing persistence token: " + token)
        touch = body.find("if (dirtyStateChanged) project.Touch()")
        regen = body.find("RegenerateDirty(project)")
        snapshot = body.find("ProjectStateSnapshot.Capture(project)")
        dirty = body.find("element.MarkDirty(ElementDirtyFlags.Quantity)")
        if not (0 <= snapshot < dirty < touch < regen):
            errors.append("Curtain recalculate must snapshot before dirty mutation and Touch before regeneration")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Curtain recalculate persists newly introduced dirty state even when regeneration makes zero progress, with snapshot rollback preserved.")
