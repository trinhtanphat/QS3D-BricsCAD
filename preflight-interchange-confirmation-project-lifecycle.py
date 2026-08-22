#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
GUARD = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeConfirmationGuard.cs"
IMPORT = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectInterchangeImportCommands.cs"
errors = []

if not GUARD.is_file():
    errors.append("missing InterchangeConfirmationGuard.cs")
else:
    text = GUARD.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
        "ReferenceEquals(currentProject, reviewedProject)",
        "currentProject.ChangeVersion != reviewedChangeVersion",
    ):
        if token not in text:
            errors.append("interchange confirmation freshness missing token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("interchange confirmation freshness must not create/cache replacement project state")

if not IMPORT.is_file():
    errors.append("missing ProjectInterchangeImportCommands.cs")
else:
    text = IMPORT.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.GetOrCreate(document)" not in text:
        errors.append("interchange import bootstrap semantics changed unexpectedly; initial import may intentionally initialize a project")
    if "InterchangeConfirmationGuard.RequireFresh(" not in text:
        errors.append("generic interchange import must re-check target freshness after policy review")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: interchange import may bootstrap initially, but post-preview confirmation verifies the same live project read-only without creating replacement state.")
