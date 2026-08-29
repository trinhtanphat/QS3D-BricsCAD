from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepTbqProjection.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepTbqKnownCountNoOverreadSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var enumerator = groups.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (index == MaxGroups)",
    "if (hasKnownCount && index >= knownCount)",
    "var group = enumerator.Current;",
    "RequireStableKnownCount(groups, knownCount);",
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

current = source.index("var group = enumerator.Current;")
if source.index("if (index == MaxGroups)") > current:
    raise SystemExit("MEP/TBQ streaming ceiling must fail before enumerator.Current.")
if source.index("if (hasKnownCount && index >= knownCount)") > current:
    raise SystemExit("MEP/TBQ known-Count overrun must fail before enumerator.Current.")
if source.index("RequireStableKnownCount(groups, knownCount);") < current:
    raise SystemExit("MEP/TBQ final Count rebinding must remain after traversal.")

print("PASS MEP/TBQ known-Count no-overread source guard")
