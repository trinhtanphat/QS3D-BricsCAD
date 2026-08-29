from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/DeepCostWorkflows.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DeepCostKnownCountNoOverreadSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var edgeEnumerator = edges.GetEnumerator())",
    "while (edgeEnumerator.MoveNext())",
    "var edge = edgeEnumerator.Current;",
    "using (var rateEnumerator = rates.GetEnumerator())",
    "while (rateEnumerator.MoveNext())",
    "var rate = rateEnumerator.Current;",
    "using (var itemEnumerator = items.GetEnumerator())",
    "while (itemEnumerator.MoveNext())",
    "var item = itemEnumerator.Current;",
    "using (var entryEnumerator = entries.GetEnumerator())",
    "while (entryEnumerator.MoveNext())",
    "var entry = entryEnumerator.Current;",
    "using (var projectEntryEnumerator = projectEntries.GetEnumerator())",
    "while (projectEntryEnumerator.MoveNext())",
    "var entry = projectEntryEnumerator.Current;",
    "RequireKnownCountStableAfterTraversal(edges, knownCount.Value);",
    "RequireKnownCountStableAfterTraversal(\n                rates,",
    "RequireKnownCountStableAfterTraversal(\n                items,",
    "RequireKnownCountStableAfterTraversal(\n                entries,",
    "RequireKnownCountStableAfterTraversal(\n                projectEntries,",
]
required_smoke = [
    "RateReferencesRejectBeforeUnexpectedCurrent();",
    "BuildUpRejectsBeforeUnexpectedCurrent();",
    "TradeAnalysisRejectsBeforeUnexpectedCurrent();",
    "BqCatalogRejectsBeforeUnexpectedCurrent();",
    "BqImportRejectsBeforeUnexpectedCurrent();",
    "StableCountedInputsRemainAccepted();",
    "Equal(2, source.MoveNextCalls);",
    "Equal(1, source.CurrentAccesses);",
    "[ModuleInitializer]",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit(
        "Deep-cost known-Count no-overread preflight failed; missing: " + ", ".join(missing)
    )

orderings = [
    ("edgeEnumerator", "if (knownCount.HasValue && index == knownCount.Value)", "if (index == MaximumEdges)", "var edge = edgeEnumerator.Current;"),
    ("rateEnumerator", "AdvancedCostCollectionContract.RequireCanProcessNext(", None, "var rate = rateEnumerator.Current;"),
    ("itemEnumerator", "AdvancedCostCollectionContract.RequireCanProcessNext(", None, "var item = itemEnumerator.Current;"),
    ("entryEnumerator", "AdvancedCostCollectionContract.RequireCanProcessNext(", None, "var entry = entryEnumerator.Current;"),
    ("projectEntryEnumerator", "AdvancedCostCollectionContract.RequireCanProcessNext(", None, "var entry = projectEntryEnumerator.Current;"),
]
for enumerator, count_guard, ceiling_guard, current_token in orderings:
    start = source.index("using (var " + enumerator)
    current = source.index(current_token, start)
    guard = source.index(count_guard, start)
    if guard > current:
        raise SystemExit(enumerator + " known-Count guard must fail before enumerator.Current.")
    if ceiling_guard is not None and source.index(ceiling_guard, start) > current:
        raise SystemExit(enumerator + " streaming ceiling must fail before enumerator.Current.")

for forbidden in [
    "foreach (var edge in edges)",
    "foreach (var rate in rates)",
    "foreach (var item in items)",
    "foreach (var entry in entries)",
    "foreach (var entry in projectEntries)",
]:
    if forbidden in source:
        raise SystemExit("Deep-cost caller-controlled foreach traversal remains: " + forbidden)

print("PASS deep-cost known-Count no-overread source guard")
