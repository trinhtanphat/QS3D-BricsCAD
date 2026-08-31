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
    'while (true)',
    'RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency graph rebuild");',
    'RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency ordering");',
    'currentKnownCountSources != initialKnownCountSources',
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Dependency Count-integrity source contract missing: " + repr(missing))

if "while (enumerator.MoveNext())" in source:
    raise SystemExit("Dependency traversal must rebind known Count before caller-controlled MoveNext.")
if "foreach (var element in elements)" in source:
    raise SystemExit("Caller-controlled dependency traversal must not regress to foreach.")


def require_explicit_traversal(method_start, method_end, label, count_expression):
    start = source.index(method_start)
    end = source.index(method_end, start + len(method_start))
    method = source[start:end]
    stable = 'RequireStableKnownCount(elements, knownCount, knownCountSources, "' + label + '");'
    move = method.index("if (!enumerator.MoveNext())")
    pre = method.rfind(stable, 0, move)
    termination = method.index(stable, move + 1)
    capacity = method.index('RequireTraversalCapacity(knownCount, ' + count_expression + ', "' + label + '");', move + 1)
    post = method.rfind(stable, move + 1, capacity)
    current = method.index("var element = enumerator.Current;", capacity)
    final = method.rfind(stable)

    if pre < 0 or not (pre < move < post < capacity < current):
        raise SystemExit(label + " must preserve Count rebound -> MoveNext -> Count rebound -> capacity -> Current ordering.")
    if termination <= move or termination >= method.index("break;", move):
        raise SystemExit(label + " must rebind known Count on terminating MoveNext before leaving enumeration.")
    if final <= current:
        raise SystemExit(label + " must preserve a final known-Count rebound after traversal.")


require_explicit_traversal(
    "public void Rebuild",
    "public bool TryGetElement",
    "Dependency graph rebuild",
    "elementCount")
require_explicit_traversal(
    "public IReadOnlyList<ProjectElement> TopologicalDirtyOrder",
    "private static OrderingSnapshot CaptureOrderingSnapshot",
    "Dependency ordering",
    "materialized.Count")

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
