#!/usr/bin/env python3
"""Static contract gate for Curtain panel-by-panel native glass.

This gate intentionally proves source wiring only. LOCAL-002 remains PENDING_LOCAL
until the exact SHA passes licensed BricsCAD V25 runtime qualification.
"""

from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "element": ROOT / "src/QS3D.Core/Domain/ProjectElement.cs",
    "panel_opening": ROOT / "src/QS3D.Core/Geometry/CurtainWallOpeningPanelPlanner.cs",
    "panel_fingerprint": ROOT / "src/QS3D.Core/Geometry/CurtainWallPanelFingerprint.cs",
    "panel_health": ROOT / "src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs",
    "composite_health": ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs",
    "host_link": ROOT / "src/QS3D.Core/Services/HostLinkService.cs",
    "regenerators": ROOT / "src/QS3D.Core/Services/SemanticRegenerators.cs",
    "panel_support": ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelBuilderSupport.cs",
    "line_builder": ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelSolidBuilder.cs",
    "path_builder": ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPathPanelSolidBuilder.cs",
    "owner_guard": ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainPanelOwnershipGuard.cs",
    "native_owner": ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainPanelNativeOwnershipService.cs",
    "live_state": ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelLiveStateService.cs",
    "runtime_health": ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainPanelRuntimeHealthService.cs",
    "selection_guard": ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallBuildSelectionGuard.cs",
    "invalidator": ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs",
    "command": ROOT / "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs",
    "health_all": ROOT / "src/QS3D.BricsCAD.V25/HealthAllCommands.cs",
    "release": ROOT / "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs",
    "inbox": ROOT / "docs/LOCAL-AGENT-INBOX.md",
    "runbook": ROOT / "docs/CURTAIN-NATIVE-PANELS.md",
}

for name, path in files.items():
    if not path.is_file():
        errors.append(f"missing {name} source: {path.relative_to(ROOT)}")


def require(name, *tokens):
    path = files[name]
    if not path.is_file():
        return ""
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            errors.append(f"{path.relative_to(ROOT)} missing Curtain panel contract: {token}")
    return text


element = require(
    "element",
    "GeneratedCurtainPanelStateKey",
    '"QS3D.GeneratedCurtainPanel.State"',
    "GeneratedCurtainPanelStaleSnapshotKey",
    '"QS3D.GeneratedCurtainPanel.StaleSnapshot"',
    'GeneratedCurtainPanelHandlesKey = "GeneratedCurtainPanelHandles"',
    "GeneratedCurtainPanelBuildStateKey",
    '"GeneratedCurtainPanelBuildState"',
    '"@COMPLETE_EMPTY"',
    "MarkGeneratedCurtainPanelStale",
    "IsGeneratedCurtainPanelStale",
    "ClearGeneratedCurtainPanelStale",
)

require(
    "panel_opening",
    "CurtainWallOpeningPanelPlanner",
    "CurtainWallOpeningPanelPlan",
    "CurtainWallPanelPiece",
    "MaxInputPanels",
    "MaxOpenings",
    "MaxOutputPieces",
    "Pieces",
)

require(
    "panel_fingerprint",
    "CurtainWallPanelFingerprintInput",
    "CurtainWallPanelFingerprint",
    "SHA256.Create()",
    "SourceLengthM",
    "PanelDepthM",
    "BottomOffsetM",
    "SourceKind",
    "PathSegmentCount",
    "Pieces",
    '"CURTAIN_PANEL_V1"',
)

require(
    "panel_health",
    '"GeneratedCurtainPanelHandles"',
    'BuildStateKey = "GeneratedCurtainPanelBuildState"',
    "CURTAIN_PANEL_BUILD_STATE_INVALID",
    "IsGeneratedCurtainPanelStale()",
    "CURTAIN_PANEL_GENERATED_SOLID_MISSING",
    "CURTAIN_PANEL_GENERATED_OWNERSHIP_CONFLICT",
    "CURTAIN_PANEL_GENERATED_STALE",
    "GeneratedCurtainPanelConfigFingerprint",
    "CURTAIN_PANEL_CONFIG_FINGERPRINT_INVALID",
    "GeneratedCurtainPanelMode",
)
require("composite_health", "new GeneratedCurtainPanelHealthService().Inspect")
require("host_link", "MarkGeneratedCurtainPanelStale")
require("regenerators", "MarkGeneratedCurtainPanelStale")

common_builder_tokens = (
    "MaxPanelsPerElement = 4096",
    "MaxPanelsPerBatch = 8192",
    "CurtainWallDetailPlanner.Plan",
    "CurtainWallOpeningPanelPlanner.Plan",
    "CurtainWallPanelFingerprint.Compute",
    "GeneratedCurtainPanelOwnershipGuard.Build",
    "GeneratedCurtainPanelNativeOwnershipService.MarkGenerated",
    "GeneratedCurtainPanelCount",
    "GeneratedCurtainPanelBaseCount",
    "GeneratedCurtainPanelOpeningCount",
    "GeneratedCurtainPanelColumns",
    "GeneratedCurtainPanelRows",
    "GeneratedCurtainPanelDepthM",
    "GeneratedCurtainPanelSourceLengthM",
    "GeneratedCurtainPanelHeightM",
    "GeneratedCurtainPanelConfigFingerprint",
    "GeneratedCurtainPanelAreaM2",
    'p["GeneratedCurtainPanelBuildState"] = "Complete";',
    'p.Remove("GeneratedCurtainPanelLiveFingerprint");',
    "ClearGeneratedCurtainPanelStale",
    "ValidatePrevious",
    "ErasePrevious",
)
line_builder = require(
    "line_builder",
    "BuildSelectedLineWalls",
    'Mode = "LinePanelSolids"',
    'OpeningAwareMode = "LinePanelSolids.OpeningAware"',
    *common_builder_tokens,
)
path_builder = require(
    "path_builder",
    "BuildSelectedOpenPolylines",
    'Mode = "PathPanelSolids"',
    'OpeningAwareMode = "PathPanelSolids.OpeningAware"',
    'GeneratedCurtainPanelSourceKind"] = "OpenPolyline"',
    "GeneratedCurtainPanelPathSegmentCount",
    "GeneratedCurtainPanelMappedCount",
    "CurtainPathFramePlanner.Plan",
    *common_builder_tokens,
)

for label, text in (("LINE", line_builder), ("PATH", path_builder)):
    if not text:
        continue
    budget = text.find("MaxPanelsPerElement")
    validate = text.find("ValidatePrevious(", budget)
    erase = text.find("ErasePrevious(", validate)
    append = text.find("AppendEntity(", erase)
    if min(budget, validate, erase, append) < 0 or not (budget < validate < erase < append):
        errors.append(
            f"Curtain {label} panel builder must enforce native count bounds and validate the complete old set before erase/append"
        )
    for forbidden in ('GeneratedSolidHandle"] = string.Join', 'GeneratedCurtainFrameHandles"] = string.Join'):
        if forbidden in text:
            errors.append(f"Curtain {label} panel builder must not overwrite host/frame ownership: {forbidden}")
    clear_live = text.find('p.Remove("GeneratedCurtainPanelLiveFingerprint");')
    complete = text.find('p["GeneratedCurtainPanelBuildState"] = "Complete";', clear_live)
    if min(clear_live, complete) < 0 or clear_live >= complete:
        errors.append(f"Curtain {label} panel Commit must remove the old live fingerprint before publishing Complete metadata")

require(
    "panel_support",
    'HandlesKey = "GeneratedCurtainPanelHandles"',
    "ReadLineOpenings",
    "ReadPathOpenings",
    "OpeningCutPlanner.Plan",
    "CurtainPathFramePlanner.ProjectPoint",
    "ValidatePrevious",
    'TryGetValue("GeneratedCurtainPanelBuildState", out var state)',
    "if (!hasHandles)",
    "if (recordedCount == 0) return",
    "orphaning native solids",
    "if (recordedCount != expected.Count)",
    "ErasePrevious",
    "ownership.EnsureOwned",
    "if (!seen.Add(canonical))",
    "GeneratedCurtainPanelNativeOwnershipService.RequireMatchingOwnership",
)
panel_support = files["panel_support"].read_text(encoding="utf-8") if files["panel_support"].is_file() else ""
if panel_support:
    orphan_order = (
        panel_support.find("if (!hasHandles)"),
        panel_support.find("if (recordedCount == 0) return"),
        panel_support.find("orphaning native solids"),
        panel_support.find("if (recordedCount != expected.Count)"),
        panel_support.find("CadHandleService.Resolve", panel_support.find("if (recordedCount != expected.Count)")),
    )
    if min(orphan_order) < 0 or list(orphan_order) != sorted(orphan_order):
        errors.append("Curtain panel replacement must accept explicit completed-empty metadata, reject positive-count blank handles, reject recorded-count/handle-set mismatch, then resolve live CAD")
require(
    "owner_guard",
    'HandlesKey = "GeneratedCurtainPanelHandles"',
    "CoreOwnershipPolicy.IsOwnerSlot",
    "EnsureOwned",
)
require(
    "native_owner",
    'RegAppName = "QS3D_CURTAIN_PANEL"',
    'HandlesKey = "GeneratedCurtainPanelHandles"',
    "CanonicalOwnerSlot",
    "MarkGenerated",
    "RequireMatchingOwnership",
)
require(
    "live_state",
    'HandlesKey = "GeneratedCurtainPanelHandles"',
    'FingerprintKey = "GeneratedCurtainPanelLiveFingerprint"',
    "TryStampSelected",
    "HasPanelBuild",
    "CURTAIN_PANEL_CONFIG_STALE",
)
require(
    "runtime_health",
    "GeneratedCurtainPanelRuntimeHealthService",
    "Inspect(Document document, ProjectState project)",
    '"GeneratedCurtainPanelHandles"',
)
require(
    "selection_guard",
    "CurtainWallBuildSelectionGuard",
    "Validate(Document document, ProjectState project)",
    "BlockTableRecord.ModelSpace",
    "exactly one canonical source",
    "source.OwnerId != modelSpaceId",
    "canonicalMetadata.Count != 1",
    "liveSources.Count != 1 || liveSources[0] != id",
)
require(
    "invalidator",
    'GeneratedCurtainPanelHandles',
    'RemoveByPrefix(element, "GeneratedCurtainPanel")',
    "GeneratedCurtainPanelOwnershipGuard.Build",
    "GeneratedCurtainPanelNativeOwnershipService.RequireMatchingOwnership",
)

command = require(
    "command",
    'CommandMethod("QS3DCURTAIN3D"',
    'phase = "canonical source prevalidation";',
    "CurtainWallBuildSelectionGuard.Validate(document, project)",
    "ProjectStateSnapshot.Capture(project)",
    "using (var commandTransaction = document.Database.TransactionManager.StartTransaction())",
    'phase = "LINE host replacement";',
    'phase = "open-POLYLINE host replacement";',
    'phase = "LINE frame replacement";',
    'phase = "open/bulged path frame replacement";',
    'phase = "LINE panel replacement";',
    'phase = "open/bulged path panel replacement";',
    "CurtainWallPanelSolidBuilder.BuildSelectedLineWalls(document, project)",
    "CurtainWallPathPanelSolidBuilder.BuildSelectedOpenPolylines(document, project)",
    "commandTransaction.Commit();",
    "rollback.Restore(project);",
    "CurtainWallPanelLiveStateService.TryStampSelected",
)
if command:
    ordered = (
        "CurtainWallBuildSelectionGuard.Validate(document, project)",
        "ProjectStateSnapshot.Capture(project)",
        "RegenerateDirty(project)",
        "using (var commandTransaction = document.Database.TransactionManager.StartTransaction())",
        'phase = "LINE host replacement";',
        'phase = "open-POLYLINE host replacement";',
        'phase = "LINE frame replacement";',
        'phase = "open/bulged path frame replacement";',
        'phase = "LINE panel replacement";',
        'phase = "open/bulged path panel replacement";',
        "commandTransaction.Commit();",
        "nativeCommitted = true;",
        "CurtainWallPanelLiveStateService.TryStampSelected",
    )
    positions = [command.find(token) for token in ordered]
    if min(positions) < 0 or positions != sorted(positions):
        errors.append("QS3DCURTAIN3D must keep all six host/frame/panel phases inside one ordered outer transaction and stamp panel live state only after commit")
    if "Curtain 3D PARTIAL COMMIT" in command:
        errors.append("QS3DCURTAIN3D must not restore obsolete partial-commit reporting")

require(
    "health_all",
    'PropertyHandles(project, "GeneratedCurtainPanelHandles")',
    "new GeneratedCurtainPanelHealthService().Inspect",
    'normalized.Contains("CURTAIN_PANEL")',
    "GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project)",
)
require(
    "release",
    "new GeneratedCurtainPanelHealthService().Inspect",
    "GeneratedCurtainPanelRuntimeHealthService.Inspect(document, project)",
)

smoke_text = "\n".join(
    path.read_text(encoding="utf-8")
    for path in (ROOT / "tests/QS3D.Core.SmokeTests").glob("*.cs")
)
for token in (
    "CurtainWallOpeningPanelPlanner.Plan",
    "GeneratedCurtainPanelHealthService.HandlesKey",
    "GeneratedCurtainPanelHealthService",
    "MarkGeneratedCurtainPanelStale",
    "CURTAIN_PANEL_GENERATED_OWNERSHIP_CONFLICT",
):
    if token not in smoke_text:
        errors.append("Core smoke source missing Curtain native-panel regression token: " + token)

require("inbox", "## LOCAL-002", "PENDING_LOCAL", "docs/CURTAIN-NATIVE-PANELS.md")
require("runbook", "LOCAL-002 / PENDING_LOCAL", "GeneratedCurtainPanelHandles", "Exact local evidence matrix")

print("QS3D Curtain native-panel source preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: Curtain LINE/path panel cells are bounded, opening-clipped, ownership-safe, independently stale/healthy, "
    "wired into the single outer Curtain3D transaction and included in aggregate health/release/selection source paths. "
    "This is static source evidence only; LOCAL-002 remains PENDING_LOCAL for exact-SHA BricsCAD V25 proof."
)
