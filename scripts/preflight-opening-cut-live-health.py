#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    target = ROOT / path
    if not target.exists():
        print(f"[FAIL] missing {path}")
        sys.exit(1)
    return target.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        print(f"[FAIL] {label}: missing {token}")
        sys.exit(1)


fingerprint = read("src/QS3D.BricsCAD.V25/Cad/PhysicalOpeningCutLiveFingerprint.cs")
boolean_builder = read("src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs")
state = read("src/QS3D.BricsCAD.V25/Cad/PhysicalOpeningCutLiveStateService.cs")
straight = read("src/QS3D.BricsCAD.V25/OpeningBooleanCommands.cs")
curved = read("src/QS3D.BricsCAD.V25/CurvedOpeningBooleanCommands.cs")
health = read("src/QS3D.BricsCAD.V25/HealthAllCommands.cs")
release = read("src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs")
invalidator = read("src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs")

for token in [
    '"ThicknessM"', '"HeightM"',
    '"WidthM"', '"BooleanClearanceM"',
    '"WallArcSagittaM"', '"PhysicalOpeningMaximumOffsetM"',
    '"PhysicalOpeningAmbiguityM"', '"WallMiterLimit"',
    "CadElementVerticalPlacement.Resolve(", "FingerprintBottomM",
    "CadHostedOpeningVerticalPlacement.Resolve(",
    "SHA256.Create()", "GeometricExtents", "GetBulgeAt",
]:
    require(fingerprint, token, "live fingerprint inputs")

for token in [
    '"PhysicalOpeningCutLiveFingerprint"',
    '"PhysicalOpeningCutLiveMode"',
    '"PHYSICAL_OPENING_CUT_LIVE_FINGERPRINT_MISSING"',
    '"PHYSICAL_OPENING_CUT_SOLID_MISMATCH"',
    '"PHYSICAL_OPENING_CUT_LIVE_STALE"',
    '"PHYSICAL_OPENING_CUT_LIVE_MODE_MISMATCH"',
    '"PHYSICAL_OPENING_CUT_LIVE_INVALID"',
]:
    require(state, token, "live-state health")

require(straight, "PhysicalOpeningCutLiveStateService.StampStraight(document, project, openingIds)", "straight cut stamp")
require(boolean_builder, "host.IsGeneratedSolidStale()", "straight cut stale-host refusal")
require(curved, "PhysicalOpeningCutLiveStateService.StampCurved(document, project)", "curved cut stamp")
require(health, "PhysicalOpeningCutLiveStateService.Inspect(document, project)", "Health All wiring")
require(health, "CurtainWallFrameLiveStateService.Inspect(document, project)", "Health All curtain live wiring")
require(health, 'normalized.Contains("PHYSICAL_OPENING_CUT")', "Health All locate")
require(release, "PhysicalOpeningCutLiveStateService.Inspect(document, project)", "Release Check wiring")
require(invalidator, 'RemoveByPrefix(element, "PhysicalOpeningCut")', "host rebuild invalidation")

print("[PASS] physical opening cut live fingerprint is stamped, invalidated, and enforced by Health/Release")
