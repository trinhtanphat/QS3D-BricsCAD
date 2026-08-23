#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
CAD = V25 / "Cad"

FILES = {
    "selection": CAD / "WallJunctionSelectionReader.cs",
    "ownership": CAD / "GeneratedWallJunctionNativeOwnershipService.cs",
    "builder": CAD / "WallJunctionSolidBuilder.cs",
    "health": CAD / "GeneratedWallJunctionRuntimeHealthService.cs",
    "commands": V25 / "WallJunctionPhysicalCommands.cs",
    "source_guard": CAD / "GeneratedNativeSourceGuard.cs",
    "invalidator": CAD / "GeneratedDependentGeometryInvalidator.cs",
    "line_builder": CAD / "WallSolidBuilder.cs",
    "path_builder": CAD / "PolylineWallSolidBuilder.cs",
    "health_aggregator": CAD / "GeneratedSolidRuntimeHealthService.cs",
    "health_all": V25 / "HealthAllCommands.cs",
    "release": V25 / "ReleaseReadinessCommands.cs",
    "ribbon": V25 / "Ribbon" / "RibbonBootstrapper.cs",
    "catalog": V25 / "Services" / "StartCenterCommandCatalog.cs",
    "workspace": V25 / "UI" / "WorkspacePanel.xaml",
    "workspace_code": V25 / "UI" / "WorkspacePanel.xaml.cs",
    "doc": ROOT / "docs" / "WALL-JUNCTION-OWNERSHIP.md",
}

errors = []
texts = {}
for name, path in FILES.items():
    if not path.is_file():
        errors.append("missing wall-junction native materialization file: " + str(path.relative_to(ROOT)))
        continue
    texts[name] = path.read_text(encoding="utf-8")


def require(name, *tokens):
    text = texts.get(name, "")
    for token in tokens:
        if token not in text:
            errors.append(FILES[name].name + " missing contract token: " + token)


require(
    "selection",
    "GeneratedNativeSourceGuard.HasKnownOwnershipMarker(entity)",
    "BlockTableRecord.ModelSpace",
    "ElementCategory.ArchitecturalWall",
    "ElementCategory.GlassWall",
    "ElementCategory.WallPier",
    "CadElementVerticalPlacement.Resolve(",
    "requires an open wall-centerline POLYLINE",
    '"L:" + handle',
    '"P:" + handle',
    "ResolveProjectPlaneScopes(",
    "rejectUnsupportedSources",
    "project plane-scope discovery supports at most",
)

require(
    "ownership",
    'RegAppName = "QS3D_WALL_JUNCTION"',
    'OwnershipVersion = "1"',
    'ProjectIdentityPrefix = "WJPR1:"',
    'DrawingIdentityPrefix = "WJDR1:"',
    'OwnerIdentityPrefix = "WJOW1:"',
    'SourceIdentityPrefix = "WJSG1:"',
    'ReadCoreHashToken(values, 4, "WJP1:")',
    'ReadOwnerToken(values, 5)',
    'ReadCoreHashToken(values, 6, "WJF1:")',
    "EnsureUniqueOwnerTokens(result)",
    "RequireCurrentProject(WallJunctionNativeRecord record, ProjectState project)",
    "ValidateGroupOwnerSet(IEnumerable<WallJunctionNativeRecord> records)",
    "PrepareOwnerInvalidation(",
    "if (!group.Any(x => x.OwnerIdentities.Any(targets.Contains))) continue;",
    "entity.Erase();",
    "SHA256.Create()",
)

require(
    "builder",
    "WallJunctionSelectionReader.Read(",
    "new WallJunctionPlanner().Plan(",
    "WallJunctionOwnershipPlanner.Plan(",
    "selectedIdSet.All(scope.Contains)",
    "x.OwnerWallIds.Any(selectedOwnerIds.Contains)",
    "IsSupersededByCurrentTopology(",
    "GeneratedWallJunctionNativeOwnershipService.ReadAllStrict(document, transaction)",
    "GeneratedWallJunctionNativeOwnershipService.RequireCurrentProject(record, project)",
    "GeneratedWallJunctionNativeOwnershipService.ValidateGroupOwnerSet(group)",
    "GeneratedWallJunctionNativeOwnershipService.MatchesPlan(record, plan)",
    "CreateFrustum(height, radius, radius, radius)",
    "plan.MinThicknessM / 2d",
    "plan.TopM - plan.BottomM",
    "GeneratedWallJunctionNativeOwnershipService.MarkGenerated(document, transaction, solid, plan)",
    "ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, \"Wall Junction 3D\")",
    "transaction.Commit();",
)

builder = texts.get("builder", "")
for forbidden in (
    "BoolUnite",
    "BoolSubtract",
    "GeneratedSolidHandle",
    "GeneratedGeometryService.CommitReplacement",
    "project.Touch()",
    "AuditTrail",
):
    if forbidden in builder:
        errors.append("WallJunctionSolidBuilder.cs must not consume wall hosts or mutate semantic ownership: " + forbidden)

read_existing = builder.find("GeneratedWallJunctionNativeOwnershipService.ReadAllStrict(document, transaction)")
first_erase = builder.find("Erase(transaction, record)")
first_mark = builder.find("GeneratedWallJunctionNativeOwnershipService.MarkGenerated(document, transaction, solid, plan)")
commit = builder.find("transaction.Commit();")
if min(read_existing, first_erase, first_mark, commit) < 0 or not (read_existing < first_erase < first_mark < commit):
    errors.append("WallJunctionSolidBuilder.cs must validate persisted ownership, erase the complete retiring group, mark every replacement, then commit")

require(
    "commands",
    '[CommandMethod("QS3DWALLJUNCTION3D", CommandFlags.UsePickSet)]',
    '[CommandMethod("QS3DWALLJUNCTIONHEALTH", CommandFlags.Modal)]',
    "CadSelectionGuard.AcquireCurrentSelection(document)",
    'ExistingProjectMutationContext.Require(document, "Wall Junction 3D")',
    "WallJunctionSolidBuilder.BuildSelected(document, project, selectedIds)",
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    "GeneratedWallJunctionRuntimeHealthService.Inspect(document, project)",
)
commands = texts.get("commands", "")
selection = commands.find("CadSelectionGuard.AcquireCurrentSelection(document)")
bind = commands.find('ExistingProjectMutationContext.Require(document, "Wall Junction 3D")')
if selection < 0 or bind < 0 or selection > bind:
    errors.append("QS3DWALLJUNCTION3D must finish selection/cancel before existing-project mutation binding")

require(
    "health",
    "WALL_JUNCTION_NATIVE_OWNER_DUPLICATE",
    "WALL_JUNCTION_NATIVE_GROUP_OWNER_MISMATCH",
    "WALL_JUNCTION_NATIVE_PROJECT_MISMATCH",
    "WALL_JUNCTION_NATIVE_OWNER_MISSING",
    "WALL_JUNCTION_NATIVE_STALE_EXTRA",
    "WALL_JUNCTION_NATIVE_OUTPUT_MISSING",
    "WALL_JUNCTION_NATIVE_FINGERPRINT_STALE",
    "WALL_JUNCTION_NATIVE_OUTPUT_SET_INCOMPLETE",
    "WallJunctionOwnershipPlanner.Plan(",
    "WallJunctionSelectionReader.ResolveProjectPlaneScopes(",
    "currentPlanAvailable",
    "GeneratedWallJunctionNativeOwnershipService.MatchesPlan(record, plan)",
    "StartOpenCloseTransaction()",
)
health = texts.get("health", "")
for forbidden in ("OpenMode.ForWrite", ".Erase()", "project.Touch()"):
    if forbidden in health:
        errors.append("GeneratedWallJunctionRuntimeHealthService.cs must remain read-only: " + forbidden)

require("source_guard", "GeneratedWallJunctionNativeOwnershipService.RegAppName")
require(
    "invalidator",
    "GeneratedWallJunctionNativeOwnershipService.PrepareOwnerInvalidation(",
    "EnsureCompleteLiveHandleSets(document, project, targets",
)
require("line_builder", "GeneratedWallJunctionNativeOwnershipService.PrepareOwnerInvalidation(")
require("path_builder", "GeneratedWallJunctionNativeOwnershipService.PrepareOwnerInvalidation(")
require(
    "health_aggregator",
    '"GeneratedWallJunctionRuntimeHealthService"',
    "GeneratedWallJunctionRuntimeHealthService.Inspect(document, project)",
)
require("health_all", 'normalized.StartsWith("WALL_JUNCTION_NATIVE_"', "GeneratedWallJunctionRuntimeHealthService.Handles(document)")
require("release", 'StartsWith("WALL_JUNCTION_NATIVE_"', "GeneratedWallJunctionRuntimeHealthService.Handles(document)")
require("ribbon", 'Button("Junction 3D", "QS3DWALLJUNCTION3D")', 'Button("Junction Health", "QS3DWALLJUNCTIONHEALTH")')
require("catalog", 'New("QS3DWALLJUNCTION3D"', 'New("QS3DWALLJUNCTIONHEALTH"')
require("workspace", 'Content="Junction 3D"', 'Click="OnWallJunction3DClick"')
require("workspace_code", "OnWallJunction3DClick", 'Send("QS3DWALLJUNCTION3D")')
require(
    "doc",
    "QS3DWALLJUNCTION3D",
    "QS3D_WALL_JUNCTION",
    "dedicated",
    "whole group",
    "CreateFrustum",
)

print("QS3D V25 physical wall-junction native materialization preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: V25 Wall Junction 3D has dedicated non-boolean native cores, strict WJP1/WJX1/WJF1 ownership, whole-group replacement/invalidation, read-only health, source exclusion and command/UI reachability; licensed runtime qualification remains a separate gate.")
