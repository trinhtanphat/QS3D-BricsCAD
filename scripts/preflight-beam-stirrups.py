#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Rebar/BeamStirrupLayoutPlanner.cs",
    "tests/QS3D.Core.SmokeTests/BeamStirrupLayoutSmoke.cs",
    "src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs",
    "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs",
    "src/QS3D.BricsCAD.V25/BeamStirrupCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedTieRebarOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
]
for rel in required:
    if not (ROOT / rel).is_file(): errors.append("missing beam-stirrup file: " + rel)

planner = ROOT / "src/QS3D.Core/Rebar/BeamStirrupLayoutPlanner.cs"
if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in ("LinearRebarLayoutPlanner.Plan", "SectionCoverM", "EndCoverM", "SectionLoop", "ActualSpacingM", "centerCoverM"):
        if needle not in text: errors.append("beam-stirrup planner missing: " + needle)

builder = ROOT / "src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs"
if builder.is_file():
    text = builder.read_text(encoding="utf-8")
    for needle in (
        "ElementCategory.Beam", "RebarStirrupNotation", "BeamStirrupLayoutPlanner.Plan",
        "MaxStirrupsPerElement = 1200", "MaxStirrupsPerBatch = 4000",
        "GeneratedRebarOwnershipGuard.Build(project)", "ownership.EnsureOwned(handle, element, HandlesKey)",
        'HandlesKey = "GeneratedBeamStirrupHandles"', '"Beam.Line.RectangularClosedLoop"',
        "duplicateSelectedSource", "CadGeometryGuard.Multiply(ux, station", "CadGeometryGuard.Hypot3",
        "geometry.rebar.beam.stirrup", "MaxStirrupsPerBatch - layout.Count", "BooleanOperationType.BoolUnite",
    ):
        if needle not in text: errors.append("beam-stirrup builder missing: " + needle)

health = ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs"
if health.is_file():
    text = health.read_text(encoding="utf-8")
    for needle in (
        "GeneratedBeamStirrupHandles", "BEAM_STIRRUP_GENERATED_OWNERSHIP_CONFLICT",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)",
        "BEAM_STIRRUP_GENERATED_STALE", "ElementCategory.Beam",
    ):
        if needle not in text: errors.append("beam-stirrup health missing: " + needle)

policy = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
if policy.is_file():
    text = policy.read_text(encoding="utf-8")
    for needle in ("RebarHandleKeys", "GeneratedBeamStirrupHandles", "GeneratedTieRebarHandles", "IsOwnerSlot", "IsRebarOwnerSlot"):
        if needle not in text: errors.append("generated ownership policy missing: " + needle)

commands = ROOT / "src/QS3D.BricsCAD.V25/BeamStirrupCommands.cs"
if commands.is_file():
    text = commands.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DREBARSTIRRUP3D"', 'CommandMethod("QS3DREBARSTIRRUPHEALTH"',
        "BeamStirrupSolidBuilder.BuildSelected", "GeneratedBeamStirrupHealthService().Inspect",
    ):
        if needle not in text: errors.append("beam-stirrup command missing: " + needle)

common_guard = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs"
if common_guard.is_file():
    text = common_guard.read_text(encoding="utf-8")
    for needle in ("CoreOwnershipPolicy.IsOwnerSlot", "CoreOwnershipPolicy.IsRebarOwnerSlot", "CoreOwnershipPolicy.RebarHandleKeys", "SourceHandles", "Refusing destructive erase"):
        if needle not in text: errors.append("common rebar ownership policy contract missing: " + needle)

tie_guard = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedTieRebarOwnershipGuard.cs"
if tie_guard.is_file():
    text = tie_guard.read_text(encoding="utf-8")
    for needle in ("CoreOwnershipPolicy.IsOwnerSlot", "CoreOwnershipPolicy.IsRebarOwnerSlot", "CoreOwnershipPolicy.RebarHandleKeys", "EnsureTieOwned"):
        if needle not in text: errors.append("column-tie ownership policy contract missing: " + needle)

invalidator = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs"
if invalidator.is_file():
    text = invalidator.read_text(encoding="utf-8")
    for needle in ("CoreOwnershipPolicy.RebarHandleKeys", "MetadataPrefixForHandleKey", "RemoveByPrefix"):
        if needle not in text: errors.append("generated-geometry invalidation missing: " + needle)

smoke = ROOT / "tests/QS3D.Core.SmokeTests/BeamStirrupLayoutSmoke.cs"
if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in ("CountLayoutBuildsClosedSectionLoop();", "SpacingLayoutIsBounded();", "ImpossibleSectionCoverIsRejected();", "AmbiguousDistributionInputIsRejected();"):
        if needle not in text: errors.append("beam-stirrup regression missing: " + needle)

hub = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
if hub.is_file():
    text = hub.read_text(encoding="utf-8")
    for command in ("QS3DBEAMREBAR3D", "QS3DREBARSTIRRUP3D", "QS3DREBARSTIRRUPHEALTH", "QS3DREBARTIES3D", "QS3DREBARTIEHEALTH"):
        if 'Tag="' + command + '"' not in text: errors.append("Domain Hub missing rebar command: " + command)

ribbon = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"
if ribbon.is_file():
    text = ribbon.read_text(encoding="utf-8")
    for command in ("QS3DBEAMREBAR3D", "QS3DREBARSTIRRUP3D", "QS3DREBARSTIRRUPHEALTH", "QS3DREBARTIES3D", "QS3DREBARTIEHEALTH"):
        if '"' + command + '"' not in text: errors.append("Ribbon missing rebar command: " + command)

print("QS3D Beam stirrup/rebar lifecycle preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Beam longitudinal/stirrup and column-tie UI parity, deterministic stirrup layout, duplicate-source protection, finite transforms, policy-driven cross-set ownership, health and dependent invalidation are present.")
