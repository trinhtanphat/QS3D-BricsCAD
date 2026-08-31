from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Services/RegenerationEngine.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/RegenerationConstructorIntegritySmoke.cs").read_text(encoding="utf-8")

required_source = [
    "_regenerators = MaterializeRegenerators(regenerators);",
    "private static List<IElementRegenerator> MaterializeRegenerators",
    "var knownCount = ReadKnownRegeneratorCount(regenerators);",
    "var moved = enumerator.MoveNext();",
    "EnsureKnownRegeneratorCountStable(regenerators, knownCount);",
    "if (knownCount.HasValue && materialized.Count >= knownCount.Value)",
    "var current = enumerator.Current;",
    "materialized.Add(current);",
    "ICollection<IElementRegenerator>",
    "IReadOnlyCollection<IElementRegenerator>",
    "ICollection nonGenericCollection",
    "reported conflicting Count values",
]
for needle in required_source:
    if needle not in source:
        raise SystemExit(f"regeneration constructor known-count preflight missing production contract: {needle}")

if "new List<IElementRegenerator>(regenerators)" in source:
    raise SystemExit("regeneration constructor known-count preflight rejects implicit collection materialization")

move_index = source.index("var moved = enumerator.MoveNext();")
post_move_index = source.index("EnsureKnownRegeneratorCountStable(regenerators, knownCount);", move_index)
overrun_index = source.index("if (knownCount.HasValue && materialized.Count >= knownCount.Value)", post_move_index)
current_index = source.index("var current = enumerator.Current;", overrun_index)
post_current_index = source.index("EnsureKnownRegeneratorCountStable(regenerators, knownCount);", current_index)
null_index = source.index("if (current == null)", post_current_index)
retain_index = source.index("materialized.Add(current);", null_index)
if not (move_index < post_move_index < overrun_index < current_index < post_current_index < null_index < retain_index):
    raise SystemExit("regeneration constructor known-count traversal ordering is not fail-closed")

required_smoke = [
    "RejectZeroCountOverYieldBeforeCurrent",
    "RejectTransientMoveNextCountDrift",
    "RejectTransientCurrentCountDrift",
    "RejectKnownCountUnderYield",
    "AcceptStableCountedSource",
    "AcceptPureStreamingSource",
    "CurrentReads != 0",
]
for needle in required_smoke:
    if needle not in smoke:
        raise SystemExit(f"regeneration constructor known-count preflight missing regression coverage: {needle}")

print("PASS regeneration constructor known-Count integrity")
