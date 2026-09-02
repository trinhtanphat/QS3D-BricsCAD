#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
builder_path = ROOT / "src/QS3D.BricsCAD.V25/Cad/RadialGridNativeSourceBuilder.cs"
command_path = ROOT / "src/QS3D.BricsCAD.V25/RadialGridCommands.cs"
errors = []

builder = builder_path.read_text(encoding="utf-8") if builder_path.is_file() else ""
command = command_path.read_text(encoding="utf-8") if command_path.is_file() else ""
if not builder: errors.append("missing RadialGridNativeSourceBuilder.cs")
if not command: errors.append("missing RadialGridCommands.cs")

for token in (
    "GridSystemPlanner.PlanRadial(input)",
    "GridSystemMaterializationPlan.Create(planned)",
    "MaxStationsPerFamily = 200",
    "GRIDRAD:",
    'SystemKindProperty = "QS3D.GridSystem.Kind"',
    'element.Properties[SystemKindProperty] = "RADIAL"',
    "ProjectContextCoordinator.RequireBackingStoreUnchanged",
    "ProjectStateSnapshot.Capture(project)",
    "ValidateExistingSources(document.Database, transaction, project, systemKey, desiredIds)",
    "element.SourceHandles.Count != 1",
    "entity.OwnerId != database.CurrentSpaceId",
    'family == "RAY" && !(entity is Line)',
    'family == "RING" && !(entity is Arc)',
    "desiredIds.Contains(x.Id)",
    "transaction.GetObject(source.ObjectId, OpenMode.ForWrite, false)",
    "entity.Erase();",
    "new Line(",
    "new Arc(",
    "element.SourceHandles.Clear();",
    "element.SourceHandles.Add(handle);",
    "rollback.Restore(project)",
    "new AggregateException(operationError, restoreError)",
    "transaction.Commit();",
):
    if token not in builder:
        errors.append("radial Grid native source contract missing: " + token)

validate = builder.find("ValidateExistingSources(document.Database, transaction, project, systemKey, desiredIds)")
erase = builder.find("entity.Erase();", validate + 1 if validate >= 0 else 0)
commit = builder.find("transaction.Commit();", erase + 1 if erase >= 0 else 0)
restore = builder.find("rollback.Restore(project)", commit + 1 if commit >= 0 else 0)
if min(validate, erase, commit, restore) < 0 or not validate < erase < commit < restore:
    errors.append("complete radial source validation must precede erase, and CAD commit must remain inside semantic rollback boundary")

if "GeneratedNativeSourceGuard" in builder or "MarkGenerated" in builder:
    errors.append("primary authored radial Grid LINE/ARC sources must not be classified as derived QS3D generated output")

for token in (
    '[CommandMethod("QS3DGRIDRADIAL")]',
    "TryPromptRequest(document, out var request)",
    "RadialGridNativeSourceBuilder.Build(document, project, request)",
    "PaletteCoordinator.RefreshProject()",
    "PaletteCoordinator.SetStatus(status)",
    "native + semantic radial Grid đã commit; một phần UI không thể đồng bộ",
):
    if token not in command:
        errors.append("radial Grid command contract missing: " + token)

build_call = command.find("RadialGridNativeSourceBuilder.Build(document, project, request)")
ui_refresh = command.find("PaletteCoordinator.RefreshProject()", build_call + 1 if build_call >= 0 else 0)
ui_status = command.find("PaletteCoordinator.SetStatus(status)", build_call + 1 if build_call >= 0 else 0)
if min(build_call, ui_refresh, ui_status) < 0 or not build_call < ui_refresh or not build_call < ui_status:
    errors.append("post-commit radial Grid UI synchronization must remain after native+semantic Build returns")

print("QS3D radial Grid native source preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: radial Grid authoring consumes canonical PlanRadial output, materializes LINE rays + ARC rings, validates complete old ownership before erase, rolls ProjectState back on CAD failure, and isolates post-commit UI synchronization.")
