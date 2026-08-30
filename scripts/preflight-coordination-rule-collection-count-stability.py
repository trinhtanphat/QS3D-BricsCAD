#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Coordination/CoordinationRuleMatrix.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/CoordinationRuleCollectionCountStabilitySmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing coordination rule Count-stability file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    start = text.find("internal static T[] MaterializeBounded<T>(")
    end = text.find("private static void RequireStableKnownCount<T>(", start)
    method = text[start:end] if start >= 0 and end > start else ""
    required = (
        "var hasKnownCount = TryGetKnownCount(items, out var knownCount);",
        "using (var enumerator = items.GetEnumerator())",
        "while (true)",
        "RequireStableKnownCount(items, knownCount, collectionLabel);",
        "var moved = enumerator.MoveNext();",
        "if (hasKnownCount && observedCount >= knownCount)",
        "if (observedCount >= MaximumEntries)",
        "var item = enumerator.Current;",
        "snapshot.Add(item);",
        "if (hasKnownCount && knownCount != observedCount)",
        "return snapshot.ToArray();",
    )
    positions = [method.find(token) for token in required]
    if not method or any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("MaterializeBounded must admit Count, rebind around MoveNext and after Current, reject overrun/overflow before Current, reject under-yield, then return the immutable snapshot.")

    if method:
        move = method.find("var moved = enumerator.MoveNext();")
        post_move = method.find("RequireStableKnownCount(items, knownCount, collectionLabel);", move)
        current = method.find("var item = enumerator.Current;", post_move)
        post_current = method.find("RequireStableKnownCount(items, knownCount, collectionLabel);", current)
        snapshot = method.find("snapshot.Add(item);", post_current)
        if min(move, post_move, current, post_current, snapshot) < 0 or not (move < post_move < current < post_current < snapshot):
            errors.append("MaterializeBounded must rebind admitted Count after MoveNext and after Current before retaining a rule.")
        if method.count("RequireStableKnownCount(items, knownCount, collectionLabel);") != 4:
            errors.append("MaterializeBounded must perform exactly four traversal Count rebounds: pre-MoveNext, post-MoveNext, post-Current, and post-traversal.")

    if "foreach (var item in items)" in method:
        errors.append("MaterializeBounded must not use foreach because cardinality admission must occur before Current is read.")

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "GenericCountDriftFailsClosed",
        "ReadOnlyCountDriftFailsClosed",
        "NonGenericCountDriftFailsClosed",
        "NegativePostTraversalCountFailsClosed",
        "ConflictingPostTraversalCountsFailClosed",
        "KnownCountUnderYieldFailsClosed",
        "KnownCountOverrunFailsClosed",
        "StableCountedCollectionSucceeds",
        "StreamingCollectionSucceeds",
        "ICollection<T>",
        "IReadOnlyCollection<T>",
        "ICollection",
    ):
        if token not in text:
            errors.append("coordination rule Count-stability smoke missing regression token: " + token)

print("QS3D coordination rule collection Count-stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: coordination bounded materialization rebinds deterministic Count evidence across traversal and rejects overflow before Current.")
