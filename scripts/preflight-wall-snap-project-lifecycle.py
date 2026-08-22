#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "WallJunctionSnapCommands.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
errors = []

if not COMMAND.is_file():
    errors.append("missing WallJunctionSnapCommands.cs")
else:
    text = COMMAND.read_text(encoding="utf-8")
    preview = 'ExistingProjectMutationContext.Require(document, "Wall Snap Preview")'
    apply = 'ExistingProjectMutationContext.Require(document, "Wall Snap Apply")'
    for token in (preview, apply):
        if token not in text:
            errors.append("Wall Snap lifecycle missing token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("Wall Snap Preview/Apply must not create/cache project state")

    preview_region_start = text.find('[CommandMethod("QS3DWALLSNAPPREVIEW"')
    apply_region_start = text.find('[CommandMethod("QS3DWALLSNAPAPPLY"')
    build_plan_start = text.find("private static SnapPlan BuildPlan")
    if min(preview_region_start, apply_region_start, build_plan_start) < 0:
        errors.append("cannot isolate Wall Snap command regions")
    else:
        preview_region = text[preview_region_start:apply_region_start]
        apply_region = text[apply_region_start:build_plan_start]
        if preview not in preview_region:
            errors.append("QS3DWALLSNAPPREVIEW must bind canonical existing project state")
        if apply not in apply_region:
            errors.append("QS3DWALLSNAPAPPLY must bind canonical existing project state")
        for token in (
            "ProjectStateSnapshot.Capture(project)",
            "GeneratedDependentGeometryInvalidator.Prepare",
            "transaction.Commit()",
            "rollback.Restore(project)",
        ):
            if token not in apply_region:
                errors.append("Wall Snap Apply rollback/native boundary drift; missing token: " + token)

if not INBOX.is_file():
    errors.append("missing LOCAL-AGENT-INBOX.md")
else:
    inbox = INBOX.read_text(encoding="utf-8")
    for token in (
        "LOCAL-007 — physical L/T/X wall junction output",
        "QS3DWALLSNAPPREVIEW",
        "QS3DWALLSNAPAPPLY",
        "must not create/cache a replacement project",
    ):
        if token not in inbox:
            errors.append("LOCAL-007 Wall Snap runtime handoff missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Wall Snap Preview/Apply require canonical existing project state, preserve apply rollback boundaries, and have an explicit LOCAL-007 V25 lifecycle scenario.")
