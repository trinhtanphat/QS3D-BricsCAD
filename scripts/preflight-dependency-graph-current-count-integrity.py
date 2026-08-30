from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/DependencyGraph.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DependencyGraphCurrentCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/dependency-graph-current-count-integrity.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("dependency Current-count integrity file missing: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

current = "var element = enumerator.Current;"
if source.count(current) != 2:
    raise SystemExit("DependencyGraph Current traversal shape changed")

positions = []
start = 0
while True:
    pos = source.find(current, start)
    if pos < 0:
        break
    positions.append(pos)
    start = pos + len(current)

for index, pos in enumerate(positions):
    rebound = source.index("RequireStableKnownCount(elements, knownCount, knownCountSources,", pos)
    if index == 0:
        processing = min(
            source.index("elementCount++;", pos),
            source.index("if (element == null)", pos),
            source.index("nextElements.ContainsKey(element.Id)", pos),
        )
    else:
        processing = min(
            source.index("if (element == null)", pos),
            source.index("ValidateDependencies(element);", pos),
            source.index("materialized.Add(element);", pos),
        )
    if not pos < rebound < processing:
        raise SystemExit("DependencyGraph post-Current Count rebound ordering changed at traversal " + str(index + 1))

for token in (
    "RebuildCurrentCountDriftPreemptsMalformedDependencyAndPreservesGraph",
    "OrderingCurrentCountDriftPreemptsMalformedDependency",
    '"element count changed during enumeration"',
    "Equal(1, hostile.MoveNextCalls",
    "Equal(1, hostile.CurrentReads",
    "[ModuleInitializer]",
):
    if token not in smoke:
        raise SystemExit("dependency Current-count smoke token missing: " + token)

for token in (
    "post-`Current`",
    "Count drift",
    "before semantic processing",
    "Rebuild",
    "TopologicalDirtyOrder",
    "NOT_APPLICABLE",
):
    if token not in runbook:
        raise SystemExit("dependency Current-count runbook token missing: " + token)

print("PASS DependencyGraph post-Current Count stability before semantic processing")
