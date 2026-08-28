#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/DependencyGraph.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DependencyGraphKnownCountContractSmoke.cs"

for path in (SOURCE, SMOKE):
    if not path.is_file():
        raise SystemExit("Dependency known-Count preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    'RequireTraversalCapacity(knownCount, elementCount, "Dependency graph rebuild");',
    'RequireTraversalCapacity(knownCount, materialized.Count, "Dependency ordering");',
    'if (knownCount.HasValue && observedCount >= knownCount.Value)',
    'throw TraversalCountError(operation);',
    'RequireObservedCount(knownCount, elementCount, "Dependency graph rebuild");',
    'RequireObservedCount(knownCount, materialized.Count, "Dependency ordering");',
    'if (count.Value > MaxElementInputCount)',
    'reports conflicting known element counts',
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Dependency known-Count source contract missing: " + repr(missing))

rebuild_guard = source.index('RequireTraversalCapacity(knownCount, elementCount, "Dependency graph rebuild");')
rebuild_increment = source.index("elementCount++;", rebuild_guard)
rebuild_null = source.index("if (element == null)", rebuild_guard)
rebuild_duplicate = source.index("if (nextElements.ContainsKey(element.Id))", rebuild_guard)
if not (rebuild_guard < rebuild_increment < rebuild_null < rebuild_duplicate):
    raise SystemExit("Dependency rebuild known-Count overrun guard must precede element processing.")

ordering_guard = source.index('RequireTraversalCapacity(knownCount, materialized.Count, "Dependency ordering");')
ordering_limit = source.index("if (materialized.Count >= MaxElementInputCount)", ordering_guard)
ordering_null = source.index("if (element == null)", ordering_guard)
ordering_add = source.index("materialized.Add(element);", ordering_guard)
if not (ordering_guard < ordering_limit < ordering_null < ordering_add):
    raise SystemExit("Dependency ordering known-Count overrun guard must precede materialization/validation.")

required_smoke = (
    "KnownCountOverrunPrecedesUnexpectedNullAndPreservesGraph();",
    "KnownCountOverrunPrecedesDuplicateValidation();",
    "DirtyOrderingKnownCountOverrunPrecedesUnexpectedNullValidation();",
    "DishonestKnownCountStopsAtFirstUnexpectedElement();",
    "PureStreamingStillStopsAtIndependentBoundary();",
    'MoveNextCalls != 2',
    'MoveNextCalls != MaxElementInputCount + 1',
    '"count changed during enumeration"',
    '"exceeds the supported"',
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Dependency known-Count regression matrix missing: " + repr(missing_smoke))

if "DishonestKnownCountStillStopsAtStreamingBoundary" in smoke:
    raise SystemExit("Stale dependency smoke still treats dishonest known Count as a global-streaming-bound case.")

print("PASS dependency graph known-Count overrun ordering")
