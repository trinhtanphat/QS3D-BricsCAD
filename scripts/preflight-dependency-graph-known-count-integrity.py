#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/DependencyGraph.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DependencyGraphKnownCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/dependency-graph-known-count-integrity.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Dependency Count-integrity preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    'RejectKnownOversizedInput(elements, "Dependency graph rebuild", out var knownCountSources)',
    'RejectKnownOversizedInput(elements, "Dependency ordering", out var knownCountSources)',
    'using (var enumerator = elements.GetEnumerator())',
    'while (enumerator.MoveNext())',
    'RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency graph rebuild");',
    'RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency ordering");',
    'currentKnownCountSources != initialKnownCountSources',
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Dependency Count-integrity source contract missing: " + repr(missing))

rebuild_guard = source.index('RequireTraversalCapacity(knownCount, elementCount, "Dependency graph rebuild");')
rebuild_current = source.index("var element = enumerator.Current;", rebuild_guard)
if rebuild_guard > rebuild_current:
    raise SystemExit("Dependency rebuild known-Count guard must execute before IEnumerator.Current.")

ordering_guard = source.index('RequireTraversalCapacity(knownCount, materialized.Count, "Dependency ordering");')
ordering_current = source.index("var element = enumerator.Current;", ordering_guard)
if ordering_guard > ordering_current:
    raise SystemExit("Dependency ordering known-Count guard must execute before IEnumerator.Current.")

if "foreach (var element in elements)" in source:
    raise SystemExit("Caller-controlled dependency traversal must not regress to foreach.")

required_smoke = (
    "[ModuleInitializer]",
    "RebuildRejectsOverrunBeforeCurrentAndPreservesGraph",
    "OrderingRejectsOverrunBeforeCurrent",
    "RebuildRejectsPostTraversalCountDriftAndPreservesGraph",
    "OrderingRejectsPostTraversalNegativeCount",
    "OrderingRejectsPostTraversalCountConflict",
    "StableMultiInterfaceCountsRemainAccepted",
    "PureStreamingInputsRemainAccepted",
    "CurrentReads != 1",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Dependency Count-integrity smoke matrix missing: " + repr(missing_smoke))

print("PASS dependency graph known-Count observation integrity")
