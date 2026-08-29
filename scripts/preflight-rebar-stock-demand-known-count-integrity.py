from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Rebar/RebarStockDemand.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RebarStockDemandKnownCountIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var cutEnumerator = requiredCuts.GetEnumerator())",
    "while (cutEnumerator.MoveNext())",
    "if (cuts.Count == knownRequiredCutCount)",
    "var cut = cutEnumerator.Current;",
    "if (cuts.Count != knownRequiredCutCount)",
    "var reboundRequiredCutCount = ValidateKnownRequiredCutCount(requiredCuts, nameof(requiredCuts));",
    "if (reboundRequiredCutCount != knownRequiredCutCount)",
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
count_guard = source.index("if (cuts.Count == knownRequiredCutCount)", start)
current = source.index("var cut = cutEnumerator.Current;", start)
if count_guard > current:
    raise SystemExit("Known-Count overrun guard must execute before cutEnumerator.Current.")

if "foreach (var cut in requiredCuts)" in source:
    raise SystemExit("Caller-controlled requiredCuts foreach traversal must not remain.")

print("PASS rebar stock-demand known-Count integrity source guard")
