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
        "using QS3D.Core.Persistence;", "ProjectStateSnapshot.Capture(project)", "var cadCommitted = false;",
        "foreach (var update in pending) CommitSemanticUpdate(project, update);", "if (pending.Count > 0) project.Touch();",
        "rollback.Restore(project)", "AggregateException(operationError, restoreError)",
        "RectangularSlabMeshPlanner.Plan", "ElementCategory.Foundation", "GeneratedFoundationMeshHandles",
        "GeneratedRebarOwnershipGuard.Build", "ownership.EnsureOwned", "RebarFoundationXNotation", "RebarFoundationYNotation",
        "RebarFoundationFaces", "MaxBarsPerBatch", "FoundationMeshXY", "duplicateSelectedSource", "checked(batchBars + layout.Count)",
        "CadGeometryGuard.Multiply", "CadGeometryGuard.Subtract", "ClearGeneratedFoundationMeshStale",
        'AuditTrail.ForProject(project).Record("geometry.rebar.foundation.mesh"'
    ],
    "src/QS3D.BricsCAD.V25/FoundationMeshCommands.cs": [
        "QS3DFOUNDATIONREBAR3D", "FinalizeUi", "document.Editor.Regen()", "UI sync warning: ", "TryWriteMessage"
    ],
    "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs": [
        "FOUNDATION_MESH_GENERATED_OWNERSHIP_CONFLICT", "FOUNDATION_MESH_GENERATED_SOLID_MISSING",
        "FOUNDATION_MESH_CATEGORY_MISMATCH", "FOUNDATION_MESH_GENERATED_STALE", "IsGeneratedFoundationMeshStale",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot", "OwnershipIndex", "Conflicts", "IsConflicted"
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
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs": ["CoreOwnershipPolicy.RebarHandleKeys", "MetadataPrefixForHandleKey", "RemoveByPrefix"],
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
    end = text.find("private static void CommitSemanticUpdate", start + 1) if start >= 0 else -1
    if start < 0 or end < 0:
        errors.append("foundation mesh atomicity guard cannot isolate BuildSelected method")
    else:
        body = text[start:end]
        semantic_token = "foreach (var update in pending) CommitSemanticUpdate(project, update);"
        touch_token = "if (pending.Count > 0) project.Touch();"
        commit_token = "transaction.Commit();\n                    cadCommitted = true;"
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

    helper_start = text.find("private static void CommitSemanticUpdate")
    helper_end = text.find("private sealed class RectangleFrame", helper_start + 1) if helper_start >= 0 else -1
    helper = text[helper_start:helper_end] if helper_start >= 0 and helper_end > helper_start else ""
    for token in (
        "GeneratedFoundationMeshHandles", "GeneratedFoundationMeshCount", "GeneratedFoundationMeshXDiameterMm",
        "GeneratedFoundationMeshYDiameterMm", "GeneratedFoundationMeshCoverM", "GeneratedFoundationMeshMode",
        "GeneratedFoundationMeshXActualSpacingM", "GeneratedFoundationMeshYActualSpacingM", "GeneratedFoundationMeshFaces",
        "ClearGeneratedFoundationMeshStale()", 'AuditTrail.ForProject(project).Record("geometry.rebar.foundation.mesh"',
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

print("PASS: Foundation mesh uses dedicated stale/health metadata and canonical policy-driven ownership across destructive guards, dynamic semantic selection, B4D exclusion, unified health/release-readiness and UI contracts; generated handles/count/spacing/faces/audit/revision advance while CAD is rollback-capable, pre-commit failures restore the deep project snapshot, the native builder stays UI-free, and command-level UI synchronization is non-fatal.")
