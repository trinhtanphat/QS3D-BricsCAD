#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/GridNamingCommands.cs"
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/GridAnnotationBuilder.cs"
errors = []

for path in (COMMANDS, BUILDER):
    if not path.is_file():
        errors.append("missing Grid renumber lifecycle file: " + str(path.relative_to(ROOT)))

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DGRIDNUMBER"',
        "CaptureAnnotatedGridIds(project, orderedIds)",
        "ProjectStateSnapshot.Capture(project)",
        "GridNamingService.Renumber(project, orderedIds, options)",
        "GridAnnotationBuilder.HandlesKey",
        "GridAnnotationBuilder.Build(document, project, ResolveGridElements(project, annotatedIds))",
        "rollback.Restore(project)",
        "if (annotatedIds.Count > 0)",
        "element.Category != ElementCategory.Grid",
        "result.ContainsKey(element.Id)",
    ):
        if token not in text:
            errors.append("Grid renumber annotation contract missing: " + token)

    if "result.TryAdd(" in text:
        errors.append("Grid renumber helper must remain .NET Framework 4.8 compatible; Dictionary.TryAdd is not allowed")

    capture = text.find("annotatedIds = CaptureAnnotatedGridIds(project, orderedIds);")
    snapshot = text.find("var rollback = ProjectStateSnapshot.Capture(project);")
    rename = text.find("assignments = GridNamingService.Renumber(project, orderedIds, options);")
    audit = text.find("AuditTrail.ForProject(project).Record(")
    rebuild = text.find("GridAnnotationBuilder.Build(document, project, ResolveGridElements(project, annotatedIds));")
    finalize = text.find("FinalizeUi(document, assignments, options);")
    if min(capture, snapshot, rename, audit, rebuild, finalize) < 0 or not (capture < snapshot < rename < audit < rebuild < finalize):
        errors.append("Grid renumber ordering must remain capture-intent -> snapshot -> semantic rename -> audit -> annotation rebuild -> UI")

    try_start = text.find("try\n            {", snapshot)
    catch_start = text.find("catch (Exception operationError)", rename)
    if try_start < 0 or catch_start < 0 or not (try_start < rename < rebuild < catch_start):
        errors.append("Grid annotation rebuild must stay inside the semantic rollback try/catch")

if BUILDER.is_file():
    text = BUILDER.read_text(encoding="utf-8")
    for token in (
        "var rollback = ProjectStateSnapshot.Capture(project);",
        "using (var transaction = document.Database.TransactionManager.StartTransaction())",
        "foreach (var element in elements) ReplaceOne(document, transaction, project, element);",
        "GeneratedGeometryService.RequireMatchingOwnership(entity, project, element",
        "transaction.Commit();",
        "rollback.Restore(project)",
    ):
        if token not in text:
            errors.append("Grid annotation builder rollback/ownership contract missing: " + token)

print("QS3D Grid renumber annotation lifecycle preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: QS3DGRIDNUMBER preserves optional annotation intent, refreshes visible labels through an ownership-guarded CAD transaction, and rolls semantic state back if the rebuild fails.")
