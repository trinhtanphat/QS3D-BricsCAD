#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Geometry/OpeningHostMatcher.cs",
    "src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs",
    "tests/QS3D.Core.SmokeTests/OpeningHostMatcherSmoke.cs",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
]
for rel in required:
    if not (ROOT / rel).exists():
        errors.append("missing auto-host file: " + rel)

command_owners = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    if re.search(r'\[CommandMethod\("QS3DAUTOLINKHOSTS"', text, re.IGNORECASE):
        command_owners.append(str(path.relative_to(ROOT)))
if len(command_owners) != 1:
    errors.append("QS3DAUTOLINKHOSTS must have exactly one CommandMethod owner; found: " + ", ".join(command_owners))

matcher = ROOT / "src/QS3D.Core/Geometry/OpeningHostMatcher.cs"
if matcher.exists():
    text = matcher.read_text(encoding="utf-8")
    for needle in (
        "OpeningHostMatchStatus.Ambiguous",
        "MaxSegments = 20000",
        "centerlineDistance - segment.ThicknessM / 2d",
        "bestByHost",
        "ambiguityToleranceM",
        "ClosestPointOnSegment",
        "StringComparer.OrdinalIgnoreCase",
    ):
        if needle not in text:
            errors.append("opening host matcher guard missing: " + needle)

adapter = ROOT / "src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs"
if adapter.exists():
    text = adapter.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DAUTOLINKHOSTS"',
        '"AutoHostMaxGapM"',
        '"AutoHostAmbiguityM"',
        '"AutoHostElevationToleranceM"',
        "HostLinkService",
        "FloorId",
        "ZoneId",
        "BulgeArcTessellator.Tessellate",
        "OpeningHostMatchStatus.Ambiguous",
        "ScopeCompatible",
        "ResolveLiveIds",
        "QS3DCUTOPENINGS",
    ):
        if needle not in text:
            errors.append("auto-host BricsCAD safety/wiring missing: " + needle)
    if "OpeningBooleanService.CutLinkedOpenings" in text:
        errors.append("auto-host command must not silently apply physical opening booleans")

smoke = ROOT / "tests/QS3D.Core.SmokeTests/OpeningHostMatcherSmoke.cs"
if smoke.exists():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "[ModuleInitializer]",
        "NearestHostWins();",
        "ThicknessReducesEffectiveGap();",
        "SimilarHostsAreAmbiguous();",
        "SameHostSegmentsDoNotCreateAmbiguity();",
        "EndpointDistanceIsHandled();",
        "NoMatchOutsideRange();",
        "InvalidInputsAreRejected();",
    ):
        if needle not in text:
            errors.append("auto-host regression coverage missing: " + needle)

hub = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
if hub.exists() and 'Tag="QS3DAUTOLINKHOSTS"' not in hub.read_text(encoding="utf-8"):
    errors.append("Full Domain Hub does not expose QS3DAUTOLINKHOSTS")

ribbon = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
if ribbon.exists() and '"QS3DAUTOLINKHOSTS"' not in ribbon.read_text(encoding="utf-8"):
    errors.append("Ribbon does not expose QS3DAUTOLINKHOSTS")

print("QS3D safe opening auto-host preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: deterministic host matching, ambiguity/elevation guards, manual physical-cut separation, regression coverage and UI wiring are present.")
