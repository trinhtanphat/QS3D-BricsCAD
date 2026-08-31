from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Rebar/RebarStockDemand.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/RebarStockDemandSmoke.cs").read_text(encoding="utf-8")

ctor_start = source.index("public RebarStockDemand(")
ctor_end = source.index("public string GroupId", ctor_start)
ctor = source[ctor_start:ctor_end]

required = [
    "var knownRequiredCutCount = ValidateKnownRequiredCutCount(requiredCuts, nameof(requiredCuts));",
    "while (cutEnumerator.MoveNext())",
    "RequireStableKnownRequiredCutCount(requiredCuts, knownRequiredCutCount, nameof(requiredCuts));",
    "if (cuts.Count == knownRequiredCutCount)",
    "if (cuts.Count >= MaxCutRequirements)",
    "var cut = cutEnumerator.Current;",
    "cuts.Add(cut);",
]
for marker in required:
    if marker not in ctor:
        raise SystemExit(f"rebar stock demand transient Count guard missing marker: {marker}")

move = ctor.index("while (cutEnumerator.MoveNext())")
post_move = ctor.index("RequireStableKnownRequiredCutCount(requiredCuts, knownRequiredCutCount, nameof(requiredCuts));", move)
overrun = ctor.index("if (cuts.Count == knownRequiredCutCount)", post_move)
bound = ctor.index("if (cuts.Count >= MaxCutRequirements)", overrun)
current = ctor.index("var cut = cutEnumerator.Current;", bound)
post_current = ctor.index("RequireStableKnownRequiredCutCount(requiredCuts, knownRequiredCutCount, nameof(requiredCuts));", current)
semantic = ctor.index("if (cut == null)", post_current)
retain = ctor.index("cuts.Add(cut);", semantic)
if not (move < post_move < overrun < bound < current < post_current < semantic < retain):
    raise SystemExit("rebar stock demand traversal must be MoveNext -> Count -> overrun/bound -> Current -> Count -> semantic acceptance -> retention")

for marker in [
    "TransientMoveNextCountDriftFailsClosed();",
    "TransientCurrentCountDriftFailsClosed();",
    "StableCountedListStillSucceeds();",
    "HostileRequiredCuts",
]:
    if marker not in smoke:
        raise SystemExit(f"rebar stock demand transient Count smoke missing marker: {marker}")

print("rebar stock demand transient Count preflight: PASS")
