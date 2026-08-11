#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/FoundationMeshCommands.cs",
    "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs",
    "src/QS3D.BricsCAD.V25/FoundationMeshHealthCommands.cs",
    "tests/QS3D.Core.SmokeTests/FoundationMeshHealthSmoke.cs",
    "tests/QS3D.Core.SmokeTests/FoundationMeshHealthSmokeRegistration.cs",
    "docs/FOUNDATION-REBAR3D.md",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing foundation-mesh file: " + relative)

checks = {
    "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs": [
        "using QS3D.Core.Persistence;", "using QS3D.Core.Geometry;", "ProjectStateSnapshot.Capture(project)", "var cadCommitted = false;",
        "foreach (var update in pending) CommitSemanticUpdate(project, update);", "if (pending.Count > 0) project.Touch();",
        "rollback.Restore(project)", "AggregateException(operationError, restoreError)",
        "RectangularSlabMeshPlanner.Plan", "PolygonalSlabMeshPlanner.Plan", "ElementCategory.Foundation", "GeneratedFoundationMeshHandles",
        "GeneratedRebarOwnershipGuard.Build", "ownership.EnsureOwned", "RebarFoundationXNotation", "RebarFoundationYNotation",
        "RebarFoundationFaces", "MaxBarsPerBatch", "ReserveBatchBars(ref batchBars, layout.Count)", "ReserveBatchBars(ref batchBars, polygonLayout.Count)",
        "FoundationMeshXY", "RectangleLocalXY", "PolygonGlobalXY", "GeneratedFoundationMeshFootprintMode",
        "duplicateSelectedSource", "checked(batchBars + count)", "CadGeometryGuard.Multiply", "CadGeometryGuard.Subtract",
        "ReadPolygonFootprint", "ValidateCommonFootprint", "polygonal Foundation mesh chưa hỗ trợ bulge/curved boundary",
        "polyline.Normal.Z < 1d - 1e-9d", "ClearGeneratedFoundationMeshStale",
        "ErasePrevious(document, transaction, project, element, ownership)",
        "GeneratedRebarNativeOwnershipService.MarkFreshGeneratedHandles(document, transaction, project, element, HandlesKey",
        'AuditTrail.ForProject(project).Record('
    ],
    "src/QS3D.BricsCAD.V25/FoundationMeshCommands.cs": [
        "QS3DFOUNDATIONREBAR3D", "closed straight plan-view POLYLINE", "Rectangle giữ local X/Y", "polygon dùng drawing X/Y",
        "FinalizeUi", "document.Editor.Regen()", "UI sync warning: ", "TryWriteMessage"
    ],
    "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs": [
        "FOUNDATION_MESH_GENERATED_OWNERSHIP_CONFLICT", "FOUNDATION_MESH_GENERATED_SOLID_MISSING",
        "FOUNDATION_MESH_CATEGORY_MISMATCH", "FOUNDATION_MESH_GENERATED_STALE", "IsGeneratedFoundationMeshStale",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot", "OwnershipIndex", "Conflicts", "IsConflicted",
        "GeneratedFoundationMeshFootprintMode", "FOUNDATION_MESH_FOOTPRINT_MODE_INVALID", "RectangleLocalXY", "PolygonGlobalXY"
    ],
    "src/QS3D.BricsCAD.V25/FoundationMeshHealthCommands.cs": ["QS3DFOUNDATIONREBARHEALTH"],
    "src/QS3D.BricsCAD.V25/RebarMeshSetupCommands.cs": ["ElementCategory.Foundation"],
    "src/QS3D.BricsCAD.V25/UI/RebarMeshSetupWindow.xaml.cs": [
        "ElementCategory.Foundation", "RebarFoundationXNotation", "RebarFoundationYNotation", "RebarFoundationCoverM",
        "RebarFoundationFaces", "RebarFoundationXClosestToFace", "D16@200"
    ],
    "src/QS3D.BricsCAD.V25/UI/Rebar3DHubWindow.xaml": ["QS3DFOUNDATIONREBAR3D", "QS3DFOUNDATIONREBARHEALTH", "QS3DREBARMESHSETUP"],
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml": ["QS3DFOUNDATIONREBAR3D", "QS3DFOUNDATIONREBARHEALTH", "QS3DREBARMESHSETUP"],
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs": ["QS3DFOUNDATIONREBAR3D", "QS3DFOUNDATIONREBARHEALTH", "QS3DREBARMESHSETUP"],
    "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs": [
        "GeneratedFoundationMeshHandles", "RebarHandleKeys", "IsOwnerSlot", "IsRebarOwnerSlot",
        "EnumerateOwnerHandles", "CollectOwnerHandles", "TryFindOwner"
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs": ["CoreOwnershipPolicy.IsOwnerSlot", "CoreOwnershipPolicy.IsRebarOwnerSlot", "CoreOwnershipPolicy.RebarHandleKeys"],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedTieRebarOwnershipGuard.cs": ["CoreOwnershipPolicy.IsOwnerSlot", "CoreOwnershipPolicy.IsRebarOwnerSlot", "CoreOwnershipPolicy.RebarHandleKeys"],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameOwnershipGuard.cs": ["CoreOwnershipPolicy.IsOwnerSlot", "GeneratedCurtainFrameHandles"],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs": [
        "CoreOwnershipPolicy.RebarHandleKeys", "MetadataPrefixForHandleKey", "RemoveByPrefix",
        "GeneratedRebarNativeOwnershipService.RequireMatchingOwnership"
    ],
    "src/QS3D.Core/Domain/ProjectElement.cs": [
        "GeneratedFoundationMeshStateKey", "GeneratedFoundationMeshStaleSnapshotKey", "IsGeneratedFoundationMeshStale", "ClearGeneratedFoundationMeshStale"
    ],
    "src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs": ["FOUNDATION_MESH_GENERATED_STALE"],
    "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs": ["GeneratedHandleOwnershipPolicy.RebarHandleKeys", "REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT"],
    "src/QS3D.Core/Diagnostics/GeneratedRebarModeHealthService.cs": ["FoundationMeshXY", "GeneratedFoundationMeshHandles", "GeneratedSlabMeshHandles", "GeneratedWallMeshHandles"],
    "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs": ["GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)"],
    "src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs": ["GeneratedFoundationMeshHealthService", "FoundationMeshSolidBuilder.HandlesKey"],
    "src/QS3D.BricsCAD.V25/HealthAllCommands.cs": ["GeneratedFoundationMeshHealthService", "FoundationMeshSolidBuilder.HandlesKey"],
    "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs": ["GeneratedFoundationMeshHealthService().Inspect", "GeneratedRebarModeHealthService().Inspect", "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)"],
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs": ["CollectGeneratedHandles(project)", "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)"],
    "src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs": ["0.005d", "beam horizontal tolerance"],
    "tests/QS3D.Core.SmokeTests/FoundationMeshHealthSmoke.cs": [
        "IsGeneratedFoundationMeshStale", "GeneratedRebarModeHealthService", "GeneratedRebarOwnershipHealthService",
        "DetectsLaterOwnerConflictAndFutureGeneratedSlot", "GeneratedFutureMeshHandles"
    ],
    "tests/QS3D.Core.SmokeTests/FoundationMeshHealthSmokeRegistration.cs": ["FoundationMeshHealthSmoke.Run();"],
    "tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipSmoke.cs": ["FoundationMeshGeneratedHandleResolvesOwner", "FutureGeneratedOwnerSlotResolvesOwner", "GeneratedFoundationMeshHandles"],
    "tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs": ["FOUNDATION_MESH_GENERATED_STALE", "GeneratedFoundationMeshHandles"],
    "tests/QS3D.Core.SmokeTests/GeneratedOutputHealthStaleSmoke.cs": ["FoundationMeshUsesSnapshotState"],
    "docs/FOUNDATION-REBAR3D.md": [
        "RectangleLocalXY", "PolygonGlobalXY", "PolygonalSlabMeshPlanner", "GeneratedFoundationMeshFootprintMode",
        "Curved/bulged boundaries", "holes/islands/multiple outer loops", "exact-SHA licensed BricsCAD V25 qualification"
    ],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing checked file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing guard/token: " + needle)

foundation_builder = ROOT / "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs"
if foundation_builder.is_file():
    text = foundation_builder.read_text(encoding="utf-8")
    start = text.find("public static FoundationMeshBuildResult BuildSelected(Document document, ProjectState project)")
    end = text.find("private static PendingUpdate CreateUpdate", start + 1) if start >= 0 else -1
    if start < 0 or end < 0:
        errors.append("foundation mesh atomicity guard cannot isolate BuildSelected method")
    else:
        body = text[start:end]
        semantic_token = "foreach (var update in pending) CommitSemanticUpdate(project, update);"
        touch_token = "if (pending.Count > 0) project.Touch();"
        commit_token = "transaction.Commit();\n                    cadCommitted = true;"
        erase_token = "ErasePrevious(document, transaction, project, element, ownership)"
        semantic = body.find(semantic_token)
        touch = body.find(touch_token, semantic if semantic >= 0 else 0)
        commit = body.find(commit_token)
        restore = body.find("rollback.Restore(project)")
        if min(semantic, touch, commit, restore) < 0:
            errors.append("foundation mesh atomicity ordering tokens are incomplete")
        elif not semantic < touch < commit < restore:
            errors.append("Foundation mesh must commit handles/metadata/revision before CAD commit and restore project state only on the pre-commit failure path")
        if commit >= 0 and semantic_token in body[commit + len(commit_token):]:
            errors.append("Foundation mesh still mutates generated semantic ownership after CAD commit")
        if body.count(semantic_token) != 1 or body.count(commit_token) != 1:
            errors.append("Foundation mesh requires exactly one semantic replacement phase and one CAD commit/flag boundary")
        if "Editor.Regen(" in body:
            errors.append("Foundation native mesh builder must remain UI-free; viewport regen belongs to FoundationMeshCommands post-commit FinalizeUi")
        rectangle_reserve = body.find("ReserveBatchBars(ref batchBars, layout.Count)")
        rectangle_erase = body.find(erase_token, rectangle_reserve if rectangle_reserve >= 0 else 0)
        if rectangle_reserve < 0 or rectangle_erase < 0 or rectangle_reserve > rectangle_erase:
            errors.append("Rectangle Foundation mesh must reserve the batch limit before project-aware destructive replacement")
        polygon_reserve = body.find("ReserveBatchBars(ref batchBars, polygonLayout.Count)")
        polygon_erase = body.find(erase_token, polygon_reserve if polygon_reserve >= 0 else 0)
        if polygon_reserve < 0 or polygon_erase < 0 or polygon_reserve > polygon_erase:
            errors.append("Polygon Foundation mesh must reserve the batch limit before project-aware destructive replacement")

    helper_start = text.find("private static void CommitSemanticUpdate")
    helper_end = text.find("private sealed class RectangleFrame", helper_start + 1) if helper_start >= 0 else -1
    helper = text[helper_start:helper_end] if helper_start >= 0 and helper_end > helper_start else ""
    for token in (
        "Properties[HandlesKey]", "GeneratedFoundationMeshCount", "GeneratedFoundationMeshXDiameterMm",
        "GeneratedFoundationMeshYDiameterMm", "GeneratedFoundationMeshCoverM", "GeneratedFoundationMeshMode",
        "GeneratedFoundationMeshFootprintMode", "GeneratedFoundationMeshXActualSpacingM", "GeneratedFoundationMeshYActualSpacingM", "GeneratedFoundationMeshFaces",
        "ClearGeneratedFoundationMeshStale()", 'AuditTrail.ForProject(project).Record(', "update.FootprintMode",
    ):
        if token not in helper:
            errors.append("Foundation mesh semantic commit helper missing metadata/audit contract: " + token)

resolver = ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs"
if resolver.is_file():
    resolver_text = resolver.read_text(encoding="utf-8")
    if '"GeneratedFoundationMeshHandles"' in resolver_text:
        errors.append("semantic ownership resolver must discover Foundation/future generated slots dynamically through GeneratedHandleOwnershipPolicy")

review = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
if review.is_file():
    review_text = review.read_text(encoding="utf-8")
    collect_start = review_text.find("private static HashSet<string> CollectGeneratedHandles")
    if collect_start >= 0 and "property.Value.Split" in review_text[collect_start:]:
        errors.append("B4D must use canonical CollectOwnerHandles instead of duplicating generated-handle parsing")

setup = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarMeshSetupWindow.xaml.cs"
if setup.is_file():
    text = setup.read_text(encoding="utf-8")
    if "first.DiameterMm - second.DiameterMm" in text:
        errors.append("Mesh Setup still blocks independent direction diameters")

commands = []
commands_root = ROOT / "src/QS3D.BricsCAD.V25"
if commands_root.is_dir():
    for path in commands_root.rglob("*.cs"):
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8")))
upper = [x.upper() for x in commands]
for required_command in ("QS3DFOUNDATIONREBAR3D", "QS3DFOUNDATIONREBARHEALTH", "QS3DREBARMESHSETUP"):
    if required_command not in upper:
        errors.append("missing command: " + required_command)
if len(upper) != len(set(upper)):
    duplicates = sorted({x for x in upper if upper.count(x) > 1})
    errors.append("duplicate CommandMethod names: " + ", ".join(duplicates))

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Foundation mesh preserves rectangle/polygon behavior, reserves batch limits before project-aware native-ownership replacement, commits semantic ownership while CAD is rollback-capable, and retains health/audit/runtime qualification gates.")
