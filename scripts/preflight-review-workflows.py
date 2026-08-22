#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
review = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
errors = []

if not review.is_file():
    errors.append("missing ReviewCommands.cs")
else:
    text = review.read_text(encoding="utf-8")
    required = (
        "trackedCategories.Count > 1",
        "Selection đang trộn nhiều semantic category",
        "Selection đang trộn source đã capture",
        "new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase)",
        'AuditTrail.ForProject(project).Record("recognition.skip"',
        "QS3D Recognition skip",
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
    )
    for token in required:
        if token not in text:
            errors.append("Review workflow safety contract missing: " + token)
    if "catch { skipped++; }" in text:
        errors.append("auto recognition must not silently swallow failed semantic captures")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Build3D fails closed on partial/mixed semantic selections and auto-recognition skips are auditable.")
