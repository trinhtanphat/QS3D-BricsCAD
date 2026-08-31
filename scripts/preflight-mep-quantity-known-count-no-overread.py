from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepQuantity.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepQuantityKnownCountNoOverreadSmoke.cs"
MID_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepQuantityMidTraversalCountDriftSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
mid_smoke = MID_SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var enumerator = elements.GetEnumerator())",
    "while (true)",
    "EnsureKnownCountStable(elements, knownCount);",
    "if (!enumerator.MoveNext())",
    "if (index == MaxElements)",
    "if (index >= knownCount)",
    "var element = enumerator.Current;",
    "if (hasKnownCount && index != knownCount)",
    "private static void EnsureKnownCountStable",
]
required_smoke = [
    "KnownCountOverrunRejectsBeforeExtraCurrent();",
    "StreamingCeilingRejectsBeforeExtraCurrent();",
    "UnderYieldStillFailsClosed();",
    "StableCountedAndStreamingInputsRemainAccepted();",
    "Equal(1, source.CurrentAccesses);",
    "Equal(10000, source.CurrentAccesses);",
    "[ModuleInitializer]",
]
required_mid_smoke = [
    "CountDriftBeforeMoveNextFailsBeforeAdvancement();",
    "CountDriftAfterMoveNextFailsBeforeCurrent();",
    "TransientCountDriftCannotRestoreBeforePublication();",
    "Equal(0, source.CurrentReads",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
missing += [token for token in required_mid_smoke if token not in mid_smoke]
if missing:
    raise SystemExit(
        "MEP quantity known-Count no-overread preflight failed; missing: "
        + ", ".join(missing)
    )

loop = source.index("while (true)")
first_rebind = source.index("EnsureKnownCountStable(elements, knownCount);", loop)
move_next = source.index("if (!enumerator.MoveNext())", loop)
ceiling = source.index("if (index == MaxElements)", move_next)
second_rebind = source.index("EnsureKnownCountStable(elements, knownCount);", first_rebind + 1)
overrun = source.index("if (index >= knownCount)", second_rebind)
current = source.index("var element = enumerator.Current;", overrun)
final_mismatch = source.index("if (hasKnownCount && index != knownCount)", current)
final_rebind = source.index("EnsureKnownCountStable(elements, knownCount);", final_mismatch)
publication = source.index("var result = new List<MepQuantityGroup>(builders.Count);", final_rebind)

if not (loop < first_rebind < move_next < ceiling < second_rebind < overrun < current < final_mismatch < final_rebind < publication):
    raise SystemExit(
        "MEP quantity no-overread ordering must remain Count -> MoveNext -> cap -> Count -> overrun -> Current -> exact-count -> Count -> publication."
    )

print("PASS MEP quantity known-Count no-overread and mid-traversal stability source guard")
