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
adapter = read("src/QS3D.BricsCAD.V25/Cad/CadElementVerticalPlacement.cs")
straight = read("src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs")
curved = read("src/QS3D.BricsCAD.V25/Cad/CurvedOpeningBooleanService.cs")
live = read("src/QS3D.BricsCAD.V25/Cad/PhysicalOpeningCutLiveFingerprint.cs")
auto_host = read("src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs")
policy = read("src/QS3D.Core/Diagnostics/LevelReferenceNativeIntegrationPolicy.cs")
smoke = read("tests/QS3D.Core.SmokeTests/LevelReferenceSmoke.cs")

for token in (
    "public sealed class HostedOpeningVerticalPlacement",
    "public static HostedOpeningVerticalPlacement ResolveHostedOpening(",
    "ElementVerticalPlacement hostPlacement",
    "hostPlacement.BottomElevationM",
    'throw new InvalidOperationException("Opening " + opening.Id + " is below its host.")',
    'throw new InvalidOperationException("Opening " + opening.Id + " exceeds the top of its host.")',
    "Math.Max(0d, relativeSillM)",
):
    require(core, token, "Core hosted-opening placement")

for token in (
    "internal sealed class CadElementVerticalPlacement",
    "internal sealed class CadHostedOpeningVerticalPlacement",
    'LevelReferenceNativeIntegrationPolicy.EnsureQualified(opening, "Hosted opening Level placement")',
    "ElementVerticalPlacementService.ResolveHostedOpening(",
    "double legacyHeightM = double.NaN;",
    "double legacySillM = double.NaN;",
    "else if (bottomLevelId.Length > 0 && topLevelId.Length == 0)",
    "host.Placement",
    "placement.Opening.HeightM",
    "placement.RelativeSillM",
):
    require(adapter, token, "shared CAD hosted-opening adapter")
for token in (
    "internal static class CadVerticalPlacementResolver",
    "CadElementVerticalPlacement.ResolveExplicitLegacy(",
    "return CadHostedOpeningVerticalPlacement.Resolve(",
    "CadElementVerticalPlacement.HasAnyLevelConfiguration(element)",
):
    require(adapter, token, "legacy automation-probe compatibility facade")

for token in (
    "CadHostedOpeningVerticalPlacement.Resolve(",
    "HostHeightM = hostPlacement.HeightM",
    "OpeningHeightM = opening.HeightM",
    "SillHeightM = opening.SillM",
    "hostPlacement.BottomDrawing",
    "SourceBaseDrawing(hostSource, host.Id)",
):
    require(straight, token, "straight opening Level placement")
stale = straight.find("host.IsGeneratedSolidStale()")
subtract = straight.find("BoolSubtract")
if stale < 0 or subtract < 0 or stale > subtract:
    errors.append("straight opening cut must reject stale generated hosts before Boolean subtraction")
if "CadVerticalPlacementResolver" in straight:
    errors.append("straight opening cut must consume only the shared CAD placement adapter")

for token in (
    "CadHostedOpeningVerticalPlacement.Resolve(",
    "HostHeightM = heightM",
    "OpeningHeightM = openingHeightM",
    "SillHeightM = sillM",
    "hostPlacement.BottomDrawing",
    "hostPlacement.FingerprintBottomM",
):
    require(curved, token, "curved opening Level placement")
curved_stale = curved.find("host.IsGeneratedSolidStale()")
curved_resolve = curved.find("CadHostedOpeningVerticalPlacement.Resolve(")
curved_subtract = curved.find("BoolSubtract")
if min(curved_stale, curved_resolve, curved_subtract) < 0 or not (curved_stale < curved_resolve < curved_subtract):
    errors.append("curved opening cut must reject stale hosts and resolve hosted placement before Boolean subtraction")
if "CadVerticalPlacementResolver" in curved:
    errors.append("curved opening cut must consume only the shared CAD placement adapter")

for token in (
    "var hostPlacement = CadElementVerticalPlacement.Resolve(",
    "var openingPlacement = CadHostedOpeningVerticalPlacement.Resolve(",
    '.Append("|height=").Append(Number(hostPlacement.HeightM))',
    ".Append(':').Append(Number(openingPlacement.HeightM))",
    ".Append(':').Append(Number(openingPlacement.SillHeightM))",
):
    require(live, token, "physical opening live fingerprint Level placement")
if "CadVerticalPlacementResolver" in live:
    errors.append("physical opening live fingerprint must consume only the shared CAD placement adapter")

for token in (
    "ReadOpeningLocation(document, transaction, project, opening)",
    "CadElementVerticalPlacement.HasAnyLevelConfiguration(opening)",
    "CadElementVerticalPlacement.HasAnyLevelConfiguration(wall)",
    "private static bool VerticalMatch(",
    "opening.ReferenceElevationM >= host.BottomElevationM - toleranceM",
    "opening.TopElevationM.Value <= host.TopElevationM + toleranceM",
    "Math.Abs(hostBottomM - opening.ReferenceElevationM) <= toleranceM",
):
    require(auto_host, token, "Auto Host Level-aware elevation matching")

for category in (
    "ArchitecturalWall",
    "GlassWall",
    "WallPier",
    "StructuralWall",
    "Door",
    "WallOpening",
):
    require(policy, f"case ElementCategory.{category}:", "Level native integration policy")
require(policy, "return true;", "integrated Level category policy")
require(policy, "default:", "unsupported Level category policy")
require(policy, "return false;", "unsupported Level category policy")
require(smoke, "HostedOpeningsResolveInsideTheHostFrame();", "hosted-opening regression registration")
for token in ("legacyHost", "hostRelativeOpening", "boundedOpening", "belowHost", "aboveHost"):
    require(smoke, token, "hosted-opening regression matrix")

if errors:
    for error in errors:
        print(f"[FAIL] {error}")
    sys.exit(1)

print("[PASS] hosted straight/curved opening planning, live fingerprints, and Auto Host share the canonical branch-lazy Level placement contract while legacy arithmetic stays explicit")
