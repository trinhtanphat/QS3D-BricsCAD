#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Coordination/CoordinationRuleMatrix.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/CoordinationRuleCollectionKnownCountNoOverreadSmoke.cs").read_text(encoding="utf-8")
runbook = ROOT / "docs/FEATURE-RUNBOOKS/coordination-rule-known-count-no-overread.md"

required_source = [
    "using (var enumerator = items.GetEnumerator())",
    "while (true)",
    "var moved = enumerator.MoveNext();",
    "if (hasKnownCount && observedCount >= knownCount)",
    "if (observedCount >= MaximumEntries)",
    "var item = enumerator.Current;",
    "snapshot.Add(item);",
    "known Count changed during traversal",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"coordination Count no-overread source guard missing token: {token}")

start = source.index("internal static T[] MaterializeBounded<T>")
end = source.index("private static void RequireStableKnownCount<T>", start)
method = source[start:end]

pre_move = method.index("RequireStableKnownCount(items, knownCount, collectionLabel);")
move = method.index("var moved = enumerator.MoveNext();", pre_move)
post_move = method.index("RequireStableKnownCount(items, knownCount, collectionLabel);", move)
known = method.index("if (hasKnownCount && observedCount >= knownCount)", post_move)
ceiling = method.index("if (observedCount >= MaximumEntries)", known)
current = method.index("var item = enumerator.Current;", ceiling)
post_current = method.index("RequireStableKnownCount(items, knownCount, collectionLabel);", current)
snapshot = method.index("snapshot.Add(item);", post_current)
if not (pre_move < move < post_move < known < ceiling < current < post_current < snapshot):
    raise SystemExit(
        "coordination collection must rebind Count around MoveNext and after Current while preserving overrun/ceiling-before-Current ordering"
    )

if method.count("RequireStableKnownCount(items, knownCount, collectionLabel);") != 4:
    raise SystemExit(
        "coordination collection must rebind known Count before MoveNext, after MoveNext, after Current, and after traversal"
    )

if "foreach (var item in items)" in source:
    raise SystemExit("coordination collection must not regress to foreach before cardinality admission")

required_smoke = [
    "[ModuleInitializer]",
    "KnownCountOverrunRejectsBeforeExtraCurrent",
    "StreamingCeilingRejectsBeforeOverflowCurrent",
    "UnderYieldAndCountDriftReject",
    "ConflictingAndNegativeCountsRejectBeforeTraversal",
    "HonestCountedAndStreamingInputsRemainAccepted",
    "Equal(1, source.CurrentReads, \"known Count overrun Current\")",
    "Equal(10000, source.CurrentReads, \"streaming ceiling Current\")",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"coordination Count no-overread smoke guard missing token: {token}")

if not runbook.is_file():
    raise SystemExit("coordination Count no-overread runbook is missing")

print("PASS coordination rule collection known-Count no-overread guard")
