#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "planner": ROOT / "src/QS3D.Core/Geometry/CurtainWallLayoutPlanner.cs",
    "detail": ROOT / "src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs",
    "opening_planner": ROOT / "src/QS3D.Core/Geometry/CurtainFrameOpeningPlanner.cs",
    "fingerprint": ROOT / "src/QS3D.Core/Geometry/CurtainWallFrameFingerprint.cs",
    "builder": ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
    "owner": ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameOwnershipGuard.cs",
    "invalidator": ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs",
    "health": ROOT / "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs",
    "host_link": ROOT / "src/QS3D.Core/Services/HostLinkService.cs",
    "regenerators": ROOT / "src/QS3D.Core/Services/SemanticRegenerators.cs",
    "frame_command": ROOT / "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs",
    "health_command": ROOT / "src/QS3D.BricsCAD.V25/CurtainWallFrameHealthCommands.cs",
    "build_command": ROOT / "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs",
    "ui": ROOT / "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml",
    "ui_code": ROOT / "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs",
    "defaults": ROOT / "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
    "health_smoke": ROOT / "tests/QS3D.Core.SmokeTests/GeneratedCurtainFrameHealthSmoke.cs",
    "logic_smoke": ROOT / "tests/QS3D.Core.SmokeTests/LogicRegressionSmoke.cs",
}
for path in files.values():
    if not path.is_file(): errors.append("missing native curtain file: " + str(path.relative_to(ROOT)))

if files["ui"].is_file():
    try: ET.parse(files["ui"])
    except ET.ParseError as exc: errors.append("CurtainWallWindow.xaml is not well-formed: " + str(exc))

checks = {
    "detail": [
        "VerticalFrames", "HorizontalFrames", "MaxDetailSolids", "PanelAreaM2",
        "projectedDetailSolids", "layout.PanelCount", "solidCount != projectedDetailSolids",
    ],
    "opening_planner": [
        "CurtainOpeningRect", "Interrupt", "MaxOpenings", "MaxOutputFragments",
    ],
    "fingerprint": [
        "CurtainWallFrameFingerprintInput", "CURTAIN_FRAME_V1", "SHA256.Create()",
        "MaxPanelWidthM", "MaxPanelHeightM", "PerimeterFrameWidthM", "MullionWidthM",
        "TransomWidthM", "FrameDepthM", "BottomOffsetM",
    ],
    "builder": [
        'HandlesKey = "GeneratedCurtainFrameHandles"', 'Mode = "LineFrameOverlay"',
        'OpeningAwareMode = "LineFrameOverlay.OpeningAware"',
        "CurtainWallDetailPlanner.Plan", "CurtainWallFrameFingerprint.Compute", "CurtainWallFrameFingerprintInput",
        "CurtainFrameOpeningPlanner.Interrupt", "ReadLinkedOpenings", "OpeningCutPlanner.Plan",
        "MaxFramesPerElement = 4096", "MaxFramesPerBatch = 8192",
        "GeneratedCurtainFrameOwnershipGuard.Build", "ownership.EnsureOwned", "CurtainFrameDepthM",
        "GeneratedCurtainFrameColumns", "GeneratedCurtainFrameRows", "GeneratedCurtainFrameBaseCount",
        "GeneratedCurtainFrameOpeningCount", "GeneratedCurtainFrameSourceLengthM",
        "GeneratedCurtainFrameHeightM", "GeneratedCurtainFrameConfigFingerprint", "CreateBox", "GlassWall", "LINE nằm ngang",
        "document.Editor.GetSelection()", "document.Editor.SetImpliedSelection",
        "CadGeometryGuard.Subtract", "CadGeometryGuard.Multiply", "CadGeometryGuard.Add", "CadGeometryGuard.Hypot",
    ],
    "owner": [
        'GeneratedCurtainFrameHandles', 'CoreOwnershipPolicy.IsOwnerSlot(property.Key)',
        'string.Equals(property.Key, HandlesKey, StringComparison.OrdinalIgnoreCase)', 'Refusing destructive erase',
    ],
    "invalidator": [
        'GeneratedCurtainFrameHandles', 'CoreOwnershipPolicy.RebarHandleKeys', 'MetadataPrefixForHandleKey',
        'RemoveByPrefix(element, "GeneratedCurtainFrame")', 'RemoveByPrefix(element, "PhysicalOpeningCut")',
        'GeneratedCurtainFrameOwnershipGuard.Build', 'EraseCurtainFrames',
    ],
    "health": [
        'GeneratedCurtainFrameHandles', 'CURTAIN_FRAME_GENERATED_SOLID_MISSING',
        'CURTAIN_FRAME_GRID_COUNT_MISMATCH', 'GeneratedCurtainFrameBaseCount', 'GeneratedCurtainFrameOpeningCount',
        'ExpectedPhysicalBaseFrameCount', 'matchingCurrentConfig', 'rawHandles.Length > 0',
        'LineFrameOverlay.OpeningAware', 'CURTAIN_FRAME_OPENING_MODE_MISMATCH',
        'GeneratedCurtainFrameDepthM', 'GeneratedCurtainFrameSourceLengthM', 'GeneratedCurtainFrameHeightM',
        'GeneratedCurtainFrameConfigFingerprint', 'CurtainWallFrameFingerprint.Compute',
        'CURTAIN_FRAME_CONFIG_FINGERPRINT_MISSING', 'CURTAIN_FRAME_CONFIG_STALE',
        'CURTAIN_FRAME_CONFIG_INVALID', 'ElementCategory.GlassWall',
        'GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)',
        'class OwnershipIndex', 'HashSet<string> Conflicts', 'ownership.Conflicts.Contains(handleIdentity)',
        'CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT', 'CURTAIN_FRAME_GENERATED_STALE',
    ],
    "host_link": [
        'MarkGeneratedCurtainFrameStale', 'MarkHostOpeningRelationChanged', 'linked/re-hosted', 'unlinked/re-hosted',
    ],
    "regenerators": [
        'host.MarkGeneratedCurtainFrameStale("Linked opening " + element.Id + " changed.")',
    ],
    "frame_command": ['CommandMethod("QS3DCURTAINFRAMES3D"', 'CurtainWallFrameSolidBuilder.BuildSelectedLineWalls'],
    "health_command": ['CommandMethod("QS3DCURTAINFRAMEHEALTH"', 'GeneratedCurtainFrameHealthService().Inspect'],
    "build_command": [
        'CommandMethod("QS3DCURTAIN3D"', 'WallSolidBuilder.BuildSelectedLineWalls',
        'PolylineWallSolidBuilder.BuildSelected', 'CurtainWallFrameSolidBuilder.BuildSelectedLineWalls',
    ],
    "ui": ['x:Name="FrameDepthBox"', 'Tag="QS3DCURTAINFRAMES3D"', 'Tag="QS3DCURTAINFRAMEHEALTH"'],
    "ui_code": ['CurtainFrameDepthM', 'FrameDepthBox.Text', 'yield return FrameDepthBox'],
    "defaults": ['CurtainFrameDepthM'],
    "health_smoke": [
        'ModuleInitializer', 'LaterGeneratedOwnerStillConflictsWithCurtainFrames',
        'ReducedPhysicalFrameCountMatchesNonZeroWidths', 'ZeroFrameSnapshotsRemainInspectable',
        'CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT', 'CURTAIN_FRAME_CONFIG_STALE',
    ],
    "logic_smoke": ['CurtainFramesStaleOnLinkRehostAndUnlink', '!wallA.IsGeneratedSolidStale()', '!wallB.IsGeneratedSolidStale()'],
}
for key, needles in checks.items():
    path = files[key]
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(str(path.relative_to(ROOT)) + " missing curtain native token: " + needle)

if files["detail"].is_file():
    text = files["detail"].read_text(encoding="utf-8")
    projected = text.find("var projectedDetailSolids")
    build_panels = text.find("BuildPanelCells(input, layout)")
    if projected < 0 or build_panels < 0 or projected > build_panels:
        errors.append("Curtain detail native-solid budget must be checked before panel-list allocation.")

if files["health"].is_file():
    text = files["health"].read_text(encoding="utf-8")
    if 'OptionalInteger(element, "GeneratedCurtainFrameBaseCount", true' not in text:
        errors.append("Curtain health must accept zero physical base frames when configured frame widths are zero.")
    if 'Integer(element, "GeneratedCurtainFrameCount", issues, "CURTAIN_FRAME_COUNT_INVALID", true)' not in text:
        errors.append("Curtain health must accept writer-owned GeneratedCurtainFrameCount=0 for all-zero frame configurations.")
    if 'if (!element.Properties.TryGetValue(HandlesKey, out var raw)) continue;' not in text:
        errors.append("Curtain health must inspect writer-owned empty handle snapshots instead of skipping all-zero frame configurations.")
    if 'config.PerimeterFrameWidthM > 0d ? 4 : 0' not in text or 'config.MullionWidthM > 0d ? columns - 1 : 0' not in text or 'config.TransomWidthM > 0d ? rows - 1 : 0' not in text:
        errors.append("Curtain health must derive physical base-frame count from nonzero frame widths, not conceptual grid boundaries alone.")

owners = {"QS3DCURTAIN3D": [], "QS3DCURTAINFRAMES3D": [], "QS3DCURTAINFRAMEHEALTH": []}
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    for command in owners:
        if re.search(r'\[CommandMethod\("' + re.escape(command) + r'"', text, re.IGNORECASE):
            owners[command].append(str(path.relative_to(ROOT)))
for command, found in owners.items():
    if len(found) != 1: errors.append(command + " must have exactly one CommandMethod owner; found: " + ", ".join(found))

if files["builder"].is_file():
    text = files["builder"].read_text(encoding="utf-8")
    for forbidden in ('GeneratedSolidHandle"] = string.Join', 'GeneratedSolidHandle"] = string.Join(";"', "PolylineWallSolidBuilder"):
        if forbidden in text: errors.append("curtain frame builder must not replace backing host ownership: " + forbidden)

print("QS3D native curtain-wall preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: GlassWall keeps its backing host and adds bounded, finite, selectable opening-aware curtain-frame overlays with deterministic fingerprint stale detection, relation/property stale propagation, policy-driven destructive/health ownership protection, complete invalidation metadata cleanup and UI/build command wiring. Guarded open/bulged path support is source-covered separately; exact BricsCAD V25 behavior remains runtime-gated.")
