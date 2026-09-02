#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Domain/ProjectElement.cs",
    "src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/GridAnnotationBuilder.cs",
    "src/QS3D.BricsCAD.V25/GeneratedGeometryHealthCommands.cs",
    "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs",
    "src/QS3D.BricsCAD.V25/WallJunctionSnapCommands.cs",
    "tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs",
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs",
]
for rel in required:
    if not (ROOT / rel).is_file(): errors.append("missing generated-geometry lifecycle file: " + rel)

element = ROOT / "src/QS3D.Core/Domain/ProjectElement.cs"
if element.is_file():
    text = element.read_text(encoding="utf-8")
    for needle in (
        "GeneratedGeometryStateKey", "GeneratedGeometryStaleReasonKey",
        "GeneratedSolidStateKey", "GeneratedRebarStateKey", "GeneratedShapeRebarStateKey",
        "GeneratedTieRebarStateKey", "GeneratedBeamStirrupStateKey",
        "GeneratedSolidStaleSnapshotKey", "GeneratedRebarStaleSnapshotKey", "GeneratedShapeRebarStaleSnapshotKey",
        "GeneratedTieRebarStaleSnapshotKey", "GeneratedBeamStirrupStaleSnapshotKey",
        "MarkGeneratedGeometryStale", "IsGeneratedGeometryStale", "IsGeneratedSolidStale", "IsGeneratedRebarStale",
        "IsGeneratedShapeRebarStale", "IsGeneratedTieRebarStale", "IsGeneratedBeamStirrupStale",
        "ClearGeneratedSolidStale", "ClearGeneratedRebarStale", "ClearGeneratedShapeRebarStale",
        "ClearGeneratedTieRebarStale", "ClearGeneratedBeamStirrupStale", "ClearGeneratedGeometryStale",
        "Semantic/source state changed.", "Remove(stateKey)", "Remove(snapshotKey)",
    ):
        if needle not in text: errors.append("ProjectElement generated lifecycle guard missing: " + needle)
    if "MarkGeneratedGeometryStale(\"Semantic/source state changed.\")" not in text:
        errors.append("MarkDirty must propagate semantic/source edits into generated stale state")

health = ROOT / "src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs"
if health.is_file():
    text = health.read_text(encoding="utf-8")
    for needle in (
        "GENERATED_SOLID_STALE", "REBAR_GENERATED_STALE", "SHAPE_REBAR_GENERATED_STALE",
        "TIE_REBAR_GENERATED_STALE", "BEAM_STIRRUP_GENERATED_STALE",
        "IsGeneratedSolidStale", "IsGeneratedRebarStale", "IsGeneratedShapeRebarStale",
        "IsGeneratedTieRebarStale", "IsGeneratedBeamStirrupStale",
    ):
        if needle not in text: errors.append("generated stale health missing: " + needle)

host = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs"
if host.is_file() and "ClearGeneratedSolidStale" not in host.read_text(encoding="utf-8"):
    errors.append("GeneratedGeometryService must clear host stale state after successful replacement metadata commit")

invalidator = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs"
if invalidator.is_file():
    text = invalidator.read_text(encoding="utf-8")
    for needle in (
        "CoreOwnershipPolicy.RebarHandleKeys", "MetadataPrefixForHandleKey", "RemoveByPrefix",
        "ClearGeneratedGeometryStale",
        "EnsureCompleteLiveHandleSets(document, project, targets, rebarOwnership, curtainOwnership, curtainPanelOwnership);",
        "ParseExpectedHandles", "CadHandleService.NormalizeHexHandle",
        "ResolveCompleteSet", "ids.Count != expected.Count",
        "Refusing destructive invalidation before any generated geometry is erased.",
        "Refusing partial destructive invalidation.",
    ):
        if needle not in text: errors.append("dependent generated-geometry invalidation missing: " + needle)

    strict_index = text.find("EnsureCompleteLiveHandleSets(document, project, targets, rebarOwnership, curtainOwnership, curtainPanelOwnership);")
    mutation_index = text.find("GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);")
    if strict_index < 0 or mutation_index < 0 or strict_index >= mutation_index:
        errors.append("dependent generated-geometry invalidation must validate every expected live handle set before the first destructive replacement")

    for fail_open in ("if (ids.Count == 0) continue;", "if (ids.Count == 0) return;"):
        if fail_open in text:
            errors.append("dependent generated-geometry invalidation still silently skips missing CAD handles: " + fail_open)

grid = ROOT / "src/QS3D.BricsCAD.V25/Cad/GridAnnotationBuilder.cs"
if grid.is_file():
    text = grid.read_text(encoding="utf-8")
    for needle in (
        "ValidatePrevious", "CadHandleService.NormalizeHexHandle", "OpenMode.ForRead",
        "result.Count != expected.Count",
        "Refusing destructive replacement before any Grid annotation is erased.",
        "Refusing partial destructive replacement",
        "var authoritativeOwnerId = source.OwnerId;",
        "ValidatePrevious(document.Database, transaction, project, element, authoritativeOwnerId);",
        "ErasePrevious(transaction, project, element, previous, authoritativeOwnerId);",
    ):
        if needle not in text: errors.append("Grid annotation exact-set/owner-space replacement guard missing: " + needle)

    owner_index = text.find("var authoritativeOwnerId = source.OwnerId;")
    validate_index = text.find("var previous = ValidatePrevious(document.Database, transaction, project, element, authoritativeOwnerId);")
    erase_index = text.find("ErasePrevious(transaction, project, element, previous, authoritativeOwnerId);")
    metadata_index = text.find("element.Properties[HandlesKey] = string.Join(\";\", generatedHandles);")
    if owner_index < 0 or validate_index < 0 or erase_index < 0 or metadata_index < 0 or not (owner_index < validate_index < erase_index < metadata_index):
        errors.append("Grid annotation replacement must resolve authoritative owner, validate the complete previous handle/owner set before erase, and replace metadata only afterwards")

    for fail_open in (
        "allowMissing: true",
        "if (id.IsNull || !id.IsValid) continue;",
        "if (entity == null || entity.IsErased) continue;",
    ):
        if fail_open in text:
            errors.append("Grid annotation replacement still silently skips missing CAD handles: " + fail_open)

ownership = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs"
if ownership.is_file():
    text = ownership.read_text(encoding="utf-8")
    for needle in (
        "CoreOwnershipPolicy.IsOwnerSlot", "CoreOwnershipPolicy.IsRebarOwnerSlot",
        "CoreOwnershipPolicy.RebarHandleKeys", "SourceHandles", "Refusing destructive erase",
    ):
        if needle not in text: errors.append("cross-set generated ownership guard missing: " + needle)

workspace = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs"
if workspace.is_file():
    text = workspace.read_text(encoding="utf-8")
    for needle in ("element.SetProperty(key, next)",):
        if needle not in text: errors.append("Workspace semantic edit must flow through stale-aware element mutation: " + needle)
    if "element.MarkDirty(ElementDirtyFlags.All)" in text:
        errors.append("Workspace must preserve ProjectElement.SetProperty property-specific dirty/geometry invalidation")

wall_snap = ROOT / "src/QS3D.BricsCAD.V25/WallJunctionSnapCommands.cs"
if wall_snap.is_file():
    text = wall_snap.read_text(encoding="utf-8")
    for needle in ("GeneratedDependentGeometryInvalidator.Prepare", "invalidation.CommitMetadata", "ElementDirtyFlags.Geometry | ElementDirtyFlags.Quantity"):
        if needle not in text: errors.append("wall snap generated invalidation contract missing: " + needle)

command = ROOT / "src/QS3D.BricsCAD.V25/GeneratedGeometryHealthCommands.cs"
if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in ('CommandMethod("QS3DGENERATEDHEALTH"', "GeneratedGeometryStaleHealthService().Inspect"):
        if needle not in text: errors.append("generated health command missing: " + needle)

smoke = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs"
if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "GeneratedOutputsBecomeStaleAfterSemanticEdit();", "ReplacedHandlesRemainAsObsoleteMetadataUntilExplicitClear();",
        "CurtainPanelObsoleteMarkerIsQueryPure();", "ExplicitClearPreservesOtherStaleKinds();", "StaleHealthReportsAllGeneratedKinds();",
        "GeneratedTieRebarHandles", "GeneratedBeamStirrupHandles",
    ):
        if needle not in text: errors.append("generated stale regression missing: " + needle)

registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
if registration.is_file() and "GeneratedGeometryStaleSmoke.Run();" not in registration.read_text(encoding="utf-8"):
    errors.append("GeneratedGeometryStaleSmoke is not registered")

print("QS3D generated-geometry lifecycle preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: stale snapshots, exact live-handle and owner-space prevalidation before destructive invalidation/replacement, query-pure obsolete markers until explicit cleanup, cross-set ownership, UI mutation path, health command and regression coverage are present.")
