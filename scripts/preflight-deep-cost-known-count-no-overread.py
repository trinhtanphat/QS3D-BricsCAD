from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Cost" / "DeepCostWorkflows.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "DeepCostKnownCountNoOverreadSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var edgeEnumerator = edges.GetEnumerator())",
    "if (!edgeEnumerator.MoveNext())",
    "var edge = edgeEnumerator.Current;",
    "using (var rateEnumerator = rates.GetEnumerator())",
    "if (!rateEnumerator.MoveNext())",
    "var rate = rateEnumerator.Current;",
    "using (var itemEnumerator = items.GetEnumerator())",
    "if (!itemEnumerator.MoveNext())",
    "var item = itemEnumerator.Current;",
    "using (var entryEnumerator = entries.GetEnumerator())",
    "if (!entryEnumerator.MoveNext())",
    "var entry = entryEnumerator.Current;",
    "using (var projectEntryEnumerator = projectEntries.GetEnumerator())",
    "if (!projectEntryEnumerator.MoveNext())",
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
    move = source.index("if (!" + enumerator + ".MoveNext())", start)
    current = source.index(current_token, move)
    guard = source.index(count_guard, move)
    if move > guard or guard > current:
        raise SystemExit(enumerator + " must order MoveNext -> known-Count guard -> enumerator.Current.")
    if ceiling_guard is not None:
        ceiling = source.index(ceiling_guard, move)
        if ceiling < move or ceiling > current:
            raise SystemExit(enumerator + " streaming ceiling must remain after MoveNext and before enumerator.Current.")

for forbidden in [
    "foreach (var edge in edges)",
    "foreach (var rate in rates)",
    "foreach (var item in items)",
    "foreach (var entry in entries)",
    "foreach (var entry in projectEntries)",
    "while (edgeEnumerator.MoveNext())",
    "while (rateEnumerator.MoveNext())",
    "while (itemEnumerator.MoveNext())",
    "while (entryEnumerator.MoveNext())",
    "while (projectEntryEnumerator.MoveNext())",
]:
    if forbidden in source:
        raise SystemExit("Deep-cost caller-controlled stale traversal remains: " + forbidden)

print("PASS deep-cost known-Count no-overread source guard")
