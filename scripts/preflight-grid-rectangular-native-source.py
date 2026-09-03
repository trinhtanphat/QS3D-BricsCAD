#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
builder_path = ROOT / "src/QS3D.BricsCAD.V25/Cad/RectangularGridNativeSourceBuilder.cs"
command_path = ROOT / "src/QS3D.BricsCAD.V25/RectangularGridCommands.cs"
errors = []

if not builder_path.is_file():
    errors.append("missing RectangularGridNativeSourceBuilder.cs")
    builder = ""
else:
    builder = builder_path.read_text(encoding="utf-8")

if not command_path.is_file():
    errors.append("missing RectangularGridCommands.cs")
    command = ""
else:
    command = command_path.read_text(encoding="utf-8")

for token in (
    "GridSystemPlanner.PlanRectangular(input)",
    "MaxStationsPerFamily = 200",
    "NormalizeSystemKey(request.SystemKey)",
    "ValidateCount(request.UCount, \"U\")",
    "ValidateCount(request.VCount, \"V\")",
    "ValidateSpacing(request.USpacingM, \"U\")",
    "ValidateSpacing(request.VSpacingM, \"V\")",
    "ProjectContextCoordinator.RequireBackingStoreUnchanged",
    "ProjectStateSnapshot.Capture(project)",
    "ValidateExistingSources(document.Database, transaction, project, systemKey, desiredIds)",
    "element.SourceHandles.Count != 1",
    "line.OwnerId != database.CurrentSpaceId",
    "desiredIds.Contains(x.Id)",
    "transaction.GetObject(source.ObjectId, OpenMode.ForWrite, false)",
    "entity.Erase();",
    "new ProjectElement(plan.ElementId, ElementCategory.Grid",
    "element.SourceHandles.Clear();",
    "element.SourceHandles.Add(handle);",
    "rollback.Restore(project)",
    "new AggregateException(operationError, restoreError)",
    "transaction.Commit();",
):
    if token not in builder:
        errors.append("rectangular Grid native source contract missing: " + token)

validate = builder.find("ValidateExistingSources(document.Database, transaction, project, systemKey, desiredIds)")
erase = builder.find("entity.Erase();", validate + 1 if validate >= 0 else 0)
commit = builder.find("transaction.Commit();", erase + 1 if erase >= 0 else 0)
restore = builder.find("rollback.Restore(project)", commit + 1 if commit >= 0 else 0)
if min(validate, erase, commit, restore) < 0 or not validate < erase < commit < restore:
    errors.append("complete previous source validation must precede erase, CAD commit must remain inside semantic rollback boundary")

if "GeneratedNativeSourceGuard" in builder or "MarkGenerated" in builder:
    errors.append("primary authored Grid LINEs must not be classified as derived QS3D generated output")

for token in (
    '[CommandMethod("QS3DGRIDRECT")]',
    "TryPromptRectangularRequest(document, out var request)",
    "RectangularGridNativeSourceBuilder.Build(document, project, request)",
    "PaletteCoordinator.RefreshProject()",
    "PaletteCoordinator.SetStatus(status)",
    "native + semantic rectangular Grid đã commit; một phần UI không thể đồng bộ",
):
    if token not in command:
        errors.append("rectangular Grid command contract missing: " + token)

build_call = command.find("RectangularGridNativeSourceBuilder.Build(document, project, request)")
ui_refresh = command.find("PaletteCoordinator.RefreshProject()", build_call + 1 if build_call >= 0 else 0)
ui_status = command.find("PaletteCoordinator.SetStatus(status)", build_call + 1 if build_call >= 0 else 0)
if min(build_call, ui_refresh, ui_status) < 0 or not build_call < ui_refresh or not build_call < ui_status:
    errors.append("post-commit Grid UI synchronization must remain after native+semantic Build returns")

print("QS3D rectangular Grid native source preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: rectangular Grid authoring consumes the canonical planner, bounds stable station identity, validates complete old source ownership before erase, rolls ProjectState back on CAD failure, and isolates post-commit UI synchronization.")
