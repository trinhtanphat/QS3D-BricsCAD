#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.exists():
        raise SystemExit(f"[FAIL] missing required file: {relative}")
    return path.read_text(encoding="utf-8")


errors: list[str] = []


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"{label}: missing {token!r}")


core = read("src/QS3D.Core/Domain/ElementVerticalPlacementService.cs")
resolver = read("src/QS3D.BricsCAD.V25/Cad/CadVerticalPlacementResolver.cs")
straight = read("src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs")
curved = read("src/QS3D.BricsCAD.V25/Cad/CurvedOpeningBooleanService.cs")
live = read("src/QS3D.BricsCAD.V25/Cad/PhysicalOpeningCutLiveFingerprint.cs")
auto_host = read("src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs")
policy = read("src/QS3D.Core/Diagnostics/LevelReferenceNativeIntegrationPolicy.cs")
smoke = read("tests/QS3D.Core.SmokeTests/LevelReferenceSmoke.cs")

for token in (
    "public sealed class HostedOpeningVerticalPlacement",
    "public static HostedOpeningVerticalPlacement ResolveHostedOpening(",
    "hostPlacement.BottomElevationM",
    'throw new InvalidOperationException("Opening " + opening.Id + " is below host " + host.Id + ".")',
    'throw new InvalidOperationException("Opening " + opening.Id + " exceeds the top of host " + host.Id + ".")',
):
    require(core, token, "Core hosted-opening placement")

semantic = resolver.find("ElementVerticalPlacementService.ResolveHostedOpening(")
host_guard = resolver.find('LevelReferenceNativeIntegrationPolicy.EnsureQualified(host, "Hosted opening host Level placement")')
opening_guard = resolver.find('LevelReferenceNativeIntegrationPolicy.EnsureQualified(opening, "Hosted opening Level placement")')
conversion = resolver.find("var hostCad = ToCadPlacement", semantic)
if min(semantic, host_guard, opening_guard, conversion) < 0 or not (semantic < host_guard < opening_guard < conversion):
    errors.append("CAD hosted-opening resolver must resolve semantically, enforce both Level policy guards, then convert to drawing units")
require(resolver, "public static bool HasConfiguredLevel(ProjectElement element)", "CAD Level detection")

for token in (
    "CadVerticalPlacementResolver.ResolveHostedOpening(",
    "HostHeightM = opening.HostedPlacement.Host.HeightM",
    "OpeningHeightM = opening.HostedPlacement.Opening.HeightM",
    "SillHeightM = opening.HostedPlacement.RelativeSillM",
    "private static double CutterCenterZ(",
    'legacy + ":LEVEL:" + PlacementToken',
):
    require(straight, token, "straight opening Level placement")
if straight.count("CadVerticalPlacementResolver.ResolveHostedOpening(") < 2:
    errors.append("straight opening cut must resolve hosted placement for both LINE and open-POLYLINE hosts")
stale = straight.find("host.IsGeneratedSolidStale()")
subtract = straight.find("BoolSubtract")
if stale < 0 or subtract < 0 or stale > subtract:
    errors.append("straight opening cut must reject stale generated hosts before Boolean subtraction")

for token in (
    "public CadHostedOpeningPlacement HostedPlacement",
    "var hostedPlacement = CadVerticalPlacementResolver.ResolveHostedOpening(",
    "HostHeightM = hostedPlacement.Host.HeightM",
    "OpeningHeightM = hostedPlacement.Opening.HeightM",
    "SillHeightM = hostedPlacement.RelativeSillM",
    "prepared.HostedPlacement",
    "OpeningPlacementToken(hostedPlacement)",
    'legacy + "|LEVEL:" + PlacementToken',
):
    require(curved, token, "curved opening Level placement")
curved_stale = curved.find("host.IsGeneratedSolidStale()")
curved_resolve = curved.find("CadVerticalPlacementResolver.ResolveHostedOpening(")
curved_subtract = curved.find("BoolSubtract")
if min(curved_stale, curved_resolve, curved_subtract) < 0 or not (curved_stale < curved_resolve < curved_subtract):
    errors.append("curved opening cut must reject stale hosts and resolve hosted placement before Boolean subtraction")

for token in (
    "var hostPlacement = CadVerticalPlacementResolver.Resolve(",
    "var placement = CadVerticalPlacementResolver.ResolveHostedOpening(",
    'text.Append("|level=").Append(PlacementToken(hostPlacement.Semantic))',
    'text.Append(":level:").Append(PlacementToken(placement.Opening.Semantic))',
):
    require(live, token, "physical opening live fingerprint Level placement")

for token in (
    "ReadOpeningLocation(document, transaction, project, opening)",
    "CadVerticalPlacementResolver.HasConfiguredLevel(opening)",
    "CadVerticalPlacementResolver.HasConfiguredLevel(wall)",
    "CadVerticalPlacementResolver.Resolve(",
    ": Midpoint(startZM, endZM)",
    ": elevationM;",
):
    require(auto_host, token, "Auto Host Level-aware elevation matching")

require(policy, "return false;", "Level native integration policy must remain fail-closed")
require(smoke, "HostedOpeningsResolveInsideTheHostFrame();", "hosted-opening regression registration")
for token in ("legacyHost", "hostRelativeOpening", "boundedOpening", "belowHost", "aboveHost"):
    require(smoke, token, "hosted-opening regression matrix")

if errors:
    for error in errors:
        print(f"[FAIL] {error}")
    sys.exit(1)

print("[PASS] hosted straight/curved opening planning, live fingerprints, and Auto Host share the canonical Level placement contract while legacy arithmetic stays explicit and native Level use remains policy-blocked")
