#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
REVIEW = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
REVISION = ROOT / "src/QS3D.BricsCAD.V25/Services/RevisionCoordinator.cs"
errors = []

for path in (REVIEW, REVISION):
    if not path.is_file():
        errors.append("missing review lifecycle source: " + str(path.relative_to(ROOT)))

if REVIEW.is_file():
    text = REVIEW.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
        "var previewProject = ProjectStateSnapshot.CreateDetachedCopy(project);",
        "ProjectRebarScheduleBuilder.Build(previewProject)",
        'ExistingProjectMutationContext.Require(doc, "Revision baseline")',
        "ProjectContextCoordinator.TryGetReadOnly(doc, out _)",
        "RevisionCoordinator.CaptureCurrent(doc)",
    ):
        if token not in text:
            errors.append("ReviewCommands missing lifecycle token: " + token)
    bbs_start = text.find('CommandMethod("QS3DBBSVIEW"')
    recognize_start = text.find('CommandMethod("QS3DRECOGNIZE"')
    if bbs_start >= 0 and recognize_start > bbs_start:
        bbs = text[bbs_start:recognize_start]
        if "ProjectContextCoordinator.GetOrCreate(doc)" in bbs:
            errors.append("BBS View must not create/cache project state merely to display a schedule.")

    diff_start = text.find('CommandMethod("QS3DREVDIFF"')
    locate_start = text.find("private static int LocateCurrentElement", diff_start)
    if diff_start < 0 or locate_start <= diff_start:
        errors.append("cannot isolate QS3DREVDIFF review lifecycle")
    else:
        diff = text[diff_start:locate_start]
        for token in (
            "ProjectContextCoordinator.TryGetReadOnly(doc, out _)",
            "RevisionCoordinator.LoadBaseline(doc)",
            "RevisionCoordinator.CaptureCurrent(doc)",
            "new QuantityRevisionReport().Build(before, after)",
            "new RevisionWindow(doc, before, after, rows, locate)",
        ):
            if token not in diff:
                errors.append("Revision Diff missing read-only review token: " + token)
        for forbidden in (
            "ProjectContextCoordinator.GetOrCreate",
            "ExistingProjectMutationContext",
            "AuditTrail.ForProject",
            "TryRecordRevisionCompare",
            "Regenerate(project)",
            "ProjectContextCoordinator.Save(",
            "ProjectContextCoordinator.TrySavePending",
            ".Touch(",
            ".Record(",
        ):
            if forbidden in diff:
                errors.append("Revision Diff must remain read-only; forbidden mutation surface: " + forbidden)

    if "TryRecordRevisionCompare" in text:
        errors.append("Revision compare audit helper must not reintroduce project mutation for QS3DREVDIFF")

if REVISION.is_file():
    text = REVISION.read_text(encoding="utf-8")
    for token in (
        'ExistingProjectMutationContext.Require(document, "Revision baseline")',
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
        "CaptureCurrent(ProjectState project)",
    ):
        if token not in text:
            errors.append("RevisionCoordinator missing lifecycle token: " + token)
    current_start = text.find("public static RevisionSnapshot CaptureCurrent(Document document)")
    project_overload = text.find("public static RevisionSnapshot CaptureCurrent(ProjectState project)")
    if current_start >= 0 and project_overload > current_start:
        current = text[current_start:project_overload]
        if "ProjectContextCoordinator.GetOrCreate(document)" in current:
            errors.append("CaptureCurrent(Document) must never create/cache project state.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: BBS View and Revision Diff use read-only/detached snapshots without mutating compare telemetry, Revision baseline requires an existing canonical project, and current revision capture never creates project state.")
