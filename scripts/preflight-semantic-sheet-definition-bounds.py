#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticSheetPlanner.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticSheetDefinitionBoundedSnapshotSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticSheetDefinitionBoundedSnapshotSmokeRegistration.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

required_source = [
    "internal const int MaxPlacements = 128;",
    "Placements = SnapshotPlacements(placements ?? throw new ArgumentNullException(nameof(placements)));",
    "private static IReadOnlyList<SemanticSheetPlacementDefinition> SnapshotPlacements",
    "SemanticSheetPlanner.MaxPlacements",
    "if (result.Count >= SemanticSheetPlanner.MaxPlacements)",
    "var placement = enumerator.Current;",
    "RevalidatePlacementKnownCount(placements, knownCount.Value);",
    "result.Add(placement);",
    "return result.AsReadOnly();",
]
for marker in required_source:
    if marker not in source:
        raise SystemExit(f"missing source contract: {marker}")

legacy = "new List<SemanticSheetPlacementDefinition>(placements).AsReadOnly();"
if legacy in source:
    raise SystemExit("legacy unbounded Semantic Sheet placement constructor materialization remains")

helper_start = source.index("private static IReadOnlyList<SemanticSheetPlacementDefinition> SnapshotPlacements")
guard = source.index("if (result.Count >= SemanticSheetPlanner.MaxPlacements)", helper_start)
current = source.index("var placement = enumerator.Current;", helper_start)
rebound = source.index("RevalidatePlacementKnownCount(placements, knownCount.Value);", current)
add = source.index("result.Add(placement);", rebound)
if not (guard < current < rebound < add):
    raise SystemExit("Semantic Sheet placement guard must execute before Current and post-Current Count rebound must execute before retention")

required_smoke = [
    "PlacementsStopAtFirstOverBoundItem();",
    "for (var i = 0; i <= 128; i++)",
    "Semantic sheet supports at most 128 view placements.",
    "Semantic Sheet constructor enumerated beyond the first over-bound placement.",
    "AcceptedPlacementsRemainDefensiveSnapshot();",
]
for marker in required_smoke:
    if marker not in smoke:
        raise SystemExit(f"missing smoke contract: {marker}")

if "SemanticSheetDefinitionBoundedSnapshotSmoke.Run();" not in registration:
    raise SystemExit("Semantic Sheet bounded snapshot smoke is not registered")

print("semantic sheet definition bounds preflight: PASS")