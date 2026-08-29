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
    end = text.find("private static bool TryGetKnownCount<T>(", start)
    method = text[start:end] if start >= 0 and end > start else ""
    required = (
        "var hasKnownCount = TryGetKnownCount(items, out var knownCount);",
        "using (var enumerator = items.GetEnumerator())",
        "while (enumerator.MoveNext())",
        "if (hasKnownCount && observedCount >= knownCount)",
        "if (observedCount >= MaximumEntries)",
        "var item = enumerator.Current;",
        "if (hasKnownCount && knownCount != observedCount)",
        "var stillHasKnownCount = TryGetKnownCount(items, out var reboundKnownCount);",
        "reboundKnownCount != knownCount",
        "return snapshot.ToArray();",
    )
    positions = [method.find(token) for token in required]
    if not method or any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("MaterializeBounded must bind Count before traversal, reject overrun/streaming overflow before Current, reject under-yield, rebind Count, then return the immutable snapshot.")
    if method.count("TryGetKnownCount(items, out var") < 2:
        errors.append("MaterializeBounded must bind supported Count evidence both before and after caller-controlled traversal.")
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

print("PASS: coordination bounded materialization rebinds deterministic Count evidence and rejects overflow before Current.")
