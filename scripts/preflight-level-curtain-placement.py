#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors: list[str] = []


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing Curtain Level-placement file: " + relative)
        return ""
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(label + ": missing " + repr(token))


line_frame = read("src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs")
path_frame = read("src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs")
line_panel = read("src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelSolidBuilder.cs")
path_panel = read("src/QS3D.BricsCAD.V25/Cad/CurtainWallPathPanelSolidBuilder.cs")
panel_support = read("src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelBuilderSupport.cs")
frame_live = read("src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameLiveFingerprint.cs")
panel_live = read("src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelLiveStateService.cs")
frame_health = read("src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs")
policy = read("src/QS3D.Core/Diagnostics/LevelReferenceNativeIntegrationPolicy.cs")

for label, text, source_base, snapshot_prefix in (
    ("LINE curtain frame", line_frame, "line.StartPoint.Z", "GeneratedCurtainFrame"),
    ("path curtain frame", path_frame, "polyline.Elevation", "GeneratedCurtainFrame"),
    ("LINE curtain panel", line_panel, "line.StartPoint.Z", "GeneratedCurtainPanel"),
    ("path curtain panel", path_panel, "polyline.Elevation", "GeneratedCurtainPanel"),
):
    require(text, "CadElementVerticalPlacement.Resolve(", label)
    require(text, source_base, label)
    require(text, "var heightM = verticalPlacement.HeightM;", label)
    require(text, "var baseZ = verticalPlacement.BottomDrawing;", label)
    require(text, "BottomOffsetM = verticalPlacement.FingerprintBottomM", label)
    require(text, 'CadElementVerticalPlacement.CommitSnapshot(update.Element, "' + snapshot_prefix + '"', label)
    resolve = text.find("CadElementVerticalPlacement.Resolve(")
    destructive = min(
        value for value in (
            text.find("ValidatePrevious", resolve),
            text.find("ErasePrevious", resolve),
        ) if value >= 0
    ) if resolve >= 0 and any(text.find(token, resolve) >= 0 for token in ("ValidatePrevious", "ErasePrevious")) else -1
    if resolve < 0 or destructive < 0 or resolve >= destructive:
        errors.append(label + " must resolve Level placement before native replacement/erase")

for label, text in (("LINE frame openings", line_frame), ("path frame openings", path_frame)):
    require(text, "CadHostedOpeningVerticalPlacement.Resolve(", label)
    require(text, "HostHeightM = hostHeightM", label)
    require(text, "OpeningHeightM = heightM", label)
    require(text, "SillHeightM = sillM", label)

if panel_support.count("CadVerticalPlacementResolver.ResolveHostedOpening(") != 2:
    errors.append("LINE/path panel clipping must each use the forwarding compatibility facade over the shared hosted-opening Level resolver")
for token in (
    "CadElementVerticalPlacement hostPlacement",
    "HostHeightM = hostHeightM",
    "OpeningHeightM = heightM",
    "SillHeightM = sillM",
):
    require(panel_support, token, "panel opening clipping")

for token in (
    "var hostPlacement = CadElementVerticalPlacement.Resolve(",
    "var openingPlacement = CadHostedOpeningVerticalPlacement.Resolve(",
    "var height = openingPlacement.HeightM;",
    "var sill = openingPlacement.SillHeightM;",
):
    require(frame_live, token, "curtain frame live fingerprint")

for token in (
    "var verticalPlacement = CadElementVerticalPlacement.Resolve(",
    "var heightM = verticalPlacement.HeightM;",
    "BottomOffsetM = verticalPlacement.FingerprintBottomM",
    "ReadLineOpenings(",
    "ReadPathOpenings(",
):
    require(panel_live, token, "curtain panel live/config fingerprint")

for token in (
    "ElementVerticalPlacementService.HasAnyLevelConfiguration(element)",
    "ElementVerticalPlacementService.Resolve(project, element, 0d, 1d, 0d)",
    "currentHeight = placement.HeightM;",
    "currentBottom = placement.BottomElevationM;",
):
    require(frame_health, token, "curtain frame config health")

require(policy, "case ElementCategory.GlassWall:", "GlassWall Level source qualification")
require(policy, "return false;", "Level native integration policy must remain fail-closed")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Curtain LINE/path frames and panels use the branch-lazy host/opening Level adapters before native mutation, persist vertical snapshots, carry effective placement into config/live health, preserve legacy fingerprints, and remain runtime-pending until exact V25 qualification")
