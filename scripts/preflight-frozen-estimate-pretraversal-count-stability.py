#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/FrozenEstimateProjection.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/FrozenEstimatePreTraversalCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/frozen-estimate-pretraversal-count-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Frozen-estimate pre-traversal Count preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

loop_contract = """using (var enumerator = lines.GetEnumerator())
            {
                // GetEnumerator() itself is user code for arbitrary IEnumerable implementations.
                // Re-admit a known Count before the first traversal call so an enumerator-induced
                // drift is rejected with zero MoveNext/Current reads.
                if (hasKnownCount)
                    RequireStableKnownCount(lines, knownCount);

                while (enumerator.MoveNext())
                {
                    if (hasKnownCount)
                        RequireStableKnownCount(lines, knownCount);"""
if loop_contract not in source:
    raise SystemExit("Frozen-estimate source no longer revalidates Count after GetEnumerator and before first MoveNext.")

get_enumerator = source.index("using (var enumerator = lines.GetEnumerator())")
pre_traversal_rebind = source.index("RequireStableKnownCount(lines, knownCount);", get_enumerator)
move_next = source.index("while (enumerator.MoveNext())", get_enumerator)
post_move_rebind = source.index("RequireStableKnownCount(lines, knownCount);", move_next)
if not get_enumerator < pre_traversal_rebind < move_next < post_move_rebind:
    raise SystemExit("Frozen-estimate Count admission must occur after GetEnumerator and before the first MoveNext, then again before Current acceptance.")

for token in (
    "EnumeratorInducedCountDriftFailsBeforeMoveNext();",
    "StableCountedEmptySourceRemainsAccepted();",
    "StreamingEmptySourceRemainsAccepted();",
    "MoveNextCalls == 0",
    "Count changed during enumeration",
):
    if token not in smoke:
        raise SystemExit("Frozen-estimate smoke missing contract: " + token)

for phrase in (
    "Lane-Key: `issue-5272`",
    "before the first `MoveNext()`",
    "GetEnumerator()",
    "stable counted and streaming controls",
    "No licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Frozen-estimate runbook missing boundary: " + phrase)

print("PASS frozen estimate pre-traversal Count stability contract")