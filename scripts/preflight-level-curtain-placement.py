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

for label, text, source_base in (
    ("LINE curtain frame", line_frame, "line.StartPoint.Z"),
    ("path curtain frame", path_frame, "polyline.Elevation"),
    ("LINE curtain panel", line_panel, "line.StartPoint.Z"),
    ("path curtain panel", path_panel, "polyline.Elevation"),
):
    require(text, "CadVerticalPlacementResolver.Resolve(", label)
    require(text, source_base + ", heightM, bottomOffsetM", label)
    require(text, "var effectiveHeightM = placement.HeightM;", label)
    require(text, "? placement.Semantic.BottomElevationM", label)
    require(text, "var baseZ = placement.BottomDrawingUnits;", label)
    resolve = text.find("CadVerticalPlacementResolver.Resolve(")
    destructive = min(
        value for value in (
            text.find("ValidatePrevious", resolve),
            text.find("ErasePrevious", resolve),
        ) if value >= 0
    ) if resolve >= 0 and any(text.find(token, resolve) >= 0 for token in ("ValidatePrevious", "ErasePrevious")) else -1
    if resolve < 0 or destructive < 0 or resolve >= destructive:
        errors.append(label + " must resolve Level placement before native replacement/erase")

for label, text in (("LINE frame openings", line_frame), ("path frame openings", path_frame)):
    require(text, "CadVerticalPlacementResolver.ResolveHostedOpening(", label)
    require(text, "HostHeightM = hostedPlacement.Host.HeightM", label)
    require(text, "OpeningHeightM = hostedPlacement.Opening.HeightM", label)
    require(text, "SillHeightM = hostedPlacement.RelativeSillM", label)

if panel_support.count("CadVerticalPlacementResolver.ResolveHostedOpening(") != 2:
    errors.append("LINE/path panel clipping must each use the shared hosted-opening Level resolver")
for token in (
    "HostHeightM = hostedPlacement.Host.HeightM",
    "OpeningHeightM = hostedPlacement.Opening.HeightM",
    "SillHeightM = hostedPlacement.RelativeSillM",
):
    require(panel_support, token, "panel opening clipping")

for token in (
    "var hostPlacement = CadVerticalPlacementResolver.Resolve(",
    "var hostedPlacement = CadVerticalPlacementResolver.ResolveHostedOpening(",
    'AppendPlacement(text, "|host-placement=", hostPlacement)',
    'AppendPlacement(text, ":opening-placement=", hostedPlacement.Opening)',
    '":relative-sill="',
):
    require(frame_live, token, "curtain frame live fingerprint")

for token in (
    "var placement = CadVerticalPlacementResolver.Resolve(",
    "var effectiveHeightM = placement.HeightM;",
    "? placement.Semantic.BottomElevationM",
    "ReadLineOpenings(",
    "ReadPathOpenings(",
):
    require(panel_live, token, "curtain panel live/config fingerprint")

for token in (
    "LevelReferenceNativeIntegrationPolicy.HasConfiguredReferences(element)",
    'LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, "Curtain frame config health")',
    "ElementVerticalPlacementService.Resolve(project, element, 0d, currentHeight, currentBottom)",
    "currentHeight = placement.HeightM;",
    "currentBottom = placement.BottomElevationM;",
):
    require(frame_health, token, "curtain frame config health")

require(policy, "return false;", "Level native integration policy must remain fail-closed")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] Curtain LINE/path frames and panels resolve host/opening Level placement before native mutation, carry effective placement into config/live health, preserve legacy fingerprints, and remain policy-blocked pending exact V25 qualification")
