from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Rebar/RebarStockDemand.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RebarStockDemandKnownCountIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var cutEnumerator = requiredCuts.GetEnumerator())",
    "while (cutEnumerator.MoveNext())",
    "RequireStableKnownRequiredCutCount(requiredCuts, knownRequiredCutCount, nameof(requiredCuts));",
    "if (cuts.Count == knownRequiredCutCount)",
    "var cut = cutEnumerator.Current;",
    "if (cuts.Count != knownRequiredCutCount)",
    "private static void RequireStableKnownRequiredCutCount(",
    "var current = ValidateKnownRequiredCutCount(requiredCuts, parameterName);",
    "if (current != expected)",
]
required_smoke = [
    "RejectKnownCountOverrunBeforeUnexpectedCurrent();",
    "RejectKnownCountUnderYield();",
    "RejectPostTraversalCountDrift();",
    "RejectPostTraversalCountConflict();",
    "RejectPostTraversalNegativeCount();",
    "StableCountedInputRemainsAccepted();",
    "Equal(2, source.MoveNextCalls);",
    "Equal(1, source.CurrentAccesses);",
    "[ModuleInitializer]",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Rebar stock-demand Count-integrity preflight failed; missing: " + ", ".join(missing))

start = source.index("using (var cutEnumerator = requiredCuts.GetEnumerator())")
move = source.index("while (cutEnumerator.MoveNext())", start)
post_move = source.index(
    "RequireStableKnownRequiredCutCount(requiredCuts, knownRequiredCutCount, nameof(requiredCuts));",
    move,
)
count_guard = source.index("if (cuts.Count == knownRequiredCutCount)", post_move)
current = source.index("var cut = cutEnumerator.Current;", count_guard)
post_current = source.index(
    "RequireStableKnownRequiredCutCount(requiredCuts, knownRequiredCutCount, nameof(requiredCuts));",
    current,
)
under_yield = source.index("if (cuts.Count != knownRequiredCutCount)", post_current)
post_traversal = source.index(
    "RequireStableKnownRequiredCutCount(requiredCuts, knownRequiredCutCount, nameof(requiredCuts));",
    under_yield,
)

if not (move < post_move < count_guard < current < post_current < under_yield < post_traversal):
    raise SystemExit(
        "Known-Count integrity ordering must remain MoveNext -> Count rebound -> overrun guard -> Current -> Count rebound -> under-yield -> final Count rebound."
    )

if "foreach (var cut in requiredCuts)" in source:
    raise SystemExit("Caller-controlled requiredCuts foreach traversal must not remain.")

print("PASS rebar stock-demand known-Count integrity source guard")
