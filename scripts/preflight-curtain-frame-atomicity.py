#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

builders = (
    "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs",
)

for relative in builders:
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing curtain frame builder: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    required = (
        "ProjectStateSnapshot.Capture(project)",
        "ApplyPendingUpdate(project, update)",
        "if (pending.Count > 0) project.Touch();",
        "transaction.Commit();",
        "rollback.Restore(project)",
        "new AggregateException(operationError, restoreError)",
        "ErasePrevious(document, transaction, element, ownership)",
    )
    for needle in required:
        if needle not in text:
            errors.append(relative + " missing frame atomicity contract: " + needle)

    apply = text.find("foreach (var update in pending) ApplyPendingUpdate(project, update);")
    touch = text.find("if (pending.Count > 0) project.Touch();", apply)
    commit = text.find("transaction.Commit();", apply)
    catch = text.find("catch (Exception operationError)", commit)
    restore = text.find("rollback.Restore(project)", catch)
    if min(apply, touch, commit, catch, restore) < 0 or not (apply < touch < commit < catch < restore):
        errors.append(relative + " must publish project metadata/audit before CAD commit and restore project state if the CAD transaction fails")

    after_commit = text[commit + len("transaction.Commit();"):]
    helper = after_commit.find("private static void ApplyPendingUpdate")
    if helper < 0:
        errors.append(relative + " missing ApplyPendingUpdate helper")
    else:
        between = after_commit[:helper]
        if "GeneratedCurtainFrameHandles" in between or "AuditTrail.ForProject(project).Record" in between:
            errors.append(relative + " must not publish generated-frame metadata/audit after CAD commit")

    erase = text.find("ErasePrevious(document, transaction, element, ownership)")
    if erase < 0 or erase > commit:
        errors.append(relative + " previous generated frames must be erased inside the same CAD transaction as replacement creation")

line = ROOT / builders[0]
if line.is_file():
    text = line.read_text(encoding="utf-8")
    for needle in (
        'AuditTrail.ForProject(project).Record("geometry.curtain.frames"',
        'update.Element.Properties["GeneratedCurtainFrameConfigFingerprint"]',
        "update.Element.ClearGeneratedCurtainFrameStale();",
    ):
        if needle not in text:
            errors.append(builders[0] + " missing LINE frame publication marker: " + needle)

path = ROOT / builders[1]
if path.is_file():
    text = path.read_text(encoding="utf-8")
    for needle in (
        'AuditTrail.ForProject(project).Record("geometry.curtain.path.frames"',
        'update.Element.Properties["GeneratedCurtainFrameSourceKind"] = "OpenPolyline"',
        'update.Element.Properties["GeneratedCurtainFramePathSegmentCount"]',
        'update.Element.Properties["GeneratedCurtainFrameMappedFrameCount"]',
        "update.Element.ClearGeneratedCurtainFrameStale();",
    ):
        if needle not in text:
            errors.append(builders[1] + " missing path-frame publication marker: " + needle)

print("QS3D curtain frame cross-layer atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: LINE and open/bulged path curtain frame replacement publishes ownership/fingerprint/audit before CAD commit under a full project snapshot, so semantic failure aborts CAD and CAD commit failure restores project state. Whole QS3DCURTAIN3D host+frame orchestration remains a separate transaction-family boundary.")
