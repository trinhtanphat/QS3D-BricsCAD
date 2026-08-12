#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SNAP = ROOT / "src/QS3D.BricsCAD.V25/WallJunctionSnapCommands.cs"
INVALIDATOR = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs"
SNAPSHOT = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
errors = []

for path in (SNAP, INVALIDATOR, SNAPSHOT):
    if not path.is_file():
        errors.append("missing Wall Snap atomicity file: " + str(path.relative_to(ROOT)))

if SNAP.is_file():
    text = SNAP.read_text(encoding="utf-8")
    for token in (
        "ProjectStateSnapshot.Capture(project)",
        "var cadCommitted = false;",
        "GeneratedDependentGeometryInvalidator.Prepare(document, transaction, project, touchedOwners)",
        "invalidation.CommitMetadata();",
        'element.Properties["LengthM"] = updatedLengthsM[element.Id].ToString("R", CultureInfo.InvariantCulture);',
        "element.MarkDirty(ElementDirtyFlags.Geometry | ElementDirtyFlags.Quantity);",
        "ClearPreview(project);",
        'AuditTrail.ForProject(project).Record("wall.junction.snap.apply"',
        "transaction.Commit();",
        "cadCommitted = true;",
        "rollback.Restore(project)",
        "Wall Snap failed before CAD commit and project rollback also failed.",
    ):
        if token not in text:
            errors.append("Wall Snap atomicity contract missing: " + token)

    prepare = text.find("GeneratedDependentGeometryInvalidator.Prepare(document, transaction, project, touchedOwners)")
    edit = text.find("polyline.SetPointAt(edit.VertexIndex, new Point2d(x, y));")
    metadata = text.find("invalidation.CommitMetadata();")
    length = text.find('element.Properties["LengthM"] = updatedLengthsM[element.Id].ToString("R", CultureInfo.InvariantCulture);')
    clear = text.find("ClearPreview(project);", length)
    audit = text.find('AuditTrail.ForProject(project).Record("wall.junction.snap.apply"', length)
    commit = text.find("transaction.Commit();", audit)
    committed = text.find("cadCommitted = true;", commit)
    if min(prepare, edit, metadata, length, clear, audit, commit, committed) < 0 or not (prepare < edit < metadata < length < clear < audit < commit < committed):
        errors.append("Wall Snap ordering must remain prepare -> CAD edit -> metadata/semantic mutation -> audit -> CAD commit")

    catch_pos = text.find("catch (Exception operationError)", committed)
    restore = text.find("rollback.Restore(project)", catch_pos)
    regen = text.find("document.Editor.Regen();", restore)
    if min(catch_pos, restore, regen) < 0 or not (committed < catch_pos < restore < regen):
        errors.append("Wall Snap rollback must precede post-commit UI/regen work")

if INVALIDATOR.is_file():
    text = INVALIDATOR.read_text(encoding="utf-8")
    for token in (
        'RemoveByPrefix(element, "GeneratedSolid")',
        'RemoveByPrefix(element, "PhysicalOpeningCut")',
        "element.ClearGeneratedGeometryStale();",
    ):
        if token not in text:
            errors.append("Wall Snap invalidation must clear generated-solid/physical-cut lifecycle metadata: " + token)

if SNAPSHOT.is_file():
    text = SNAPSHOT.read_text(encoding="utf-8")
    for token in (
        "target.AuditEvents.Clear();",
        "target.Metadata.Clear();",
        "target.Elements.Clear();",
        "target.RestorePersistenceState(source.Dirty, source.UpdatedUtc);",
    ):
        if token not in text:
            errors.append("ProjectStateSnapshot no longer covers Wall Snap semantic rollback state: " + token)

print("QS3D Wall Snap atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Wall Snap keeps source edits, generated-output/physical-cut invalidation, LengthM/dirty state, preview cleanup and audit inside one CAD+semantic rollback boundary; snapshot element persistence state is restored through the target copy path and UI work is post-commit only.")
