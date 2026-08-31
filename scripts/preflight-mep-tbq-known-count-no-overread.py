from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepTbqProjection.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepTbqKnownCountNoOverreadSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var enumerator = groups.GetEnumerator())",
    "RequireStableKnownCount(groups, knownCount);",
    "var moved = enumerator.MoveNext();",
    "if (!moved)",
    "if (index == MaxGroups)",
    "if (hasKnownCount && index >= knownCount)",
    "var group = enumerator.Current;",
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

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit(
        "MEP/TBQ known-Count no-overread preflight failed; missing: " + ", ".join(missing)
    )

start = source.index("public IReadOnlyList<MepTbqReportRow> BuildReport(")
end = source.index("public string SerializeCsv(", start)
region = source[start:end]
move = region.index("var moved = enumerator.MoveNext();")
second_rebound = region.index("RequireStableKnownCount(groups, knownCount);", move)
cap = region.index("if (index == MaxGroups)", second_rebound)
overrun = region.index("if (hasKnownCount && index >= knownCount)", cap)
current = region.index("var group = enumerator.Current;", overrun)
under_yield = region.index("if (hasKnownCount && index != knownCount)", current)
final_rebound = region.index("RequireStableKnownCount(groups, knownCount);", under_yield)
if not (move < second_rebound < cap < overrun < current < under_yield < final_rebound):
    raise SystemExit("MEP/TBQ known-Count traversal must reject drift/overrun before semantic Current and rebind after traversal.")

print("PASS MEP/TBQ known-Count no-overread source guard")
