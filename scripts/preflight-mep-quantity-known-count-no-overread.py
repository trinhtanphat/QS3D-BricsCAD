from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepQuantity.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepQuantityKnownCountNoOverreadSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "using (var enumerator = elements.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (index == MaxElements)",
    "if (hasKnownCount && index >= knownCount)",
    "var element = enumerator.Current;",
    "var hasFinalKnownCount = TryGetKnownCount(elements, out var finalKnownCount);",
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
        "MEP quantity known-Count no-overread preflight failed; missing: "
        + ", ".join(missing)
    )

current = source.index("var element = enumerator.Current;")
if source.index("if (index == MaxElements)") > current:
    raise SystemExit("Streaming element ceiling must fail before reading enumerator.Current.")
if source.index("if (hasKnownCount && index >= knownCount)") > current:
    raise SystemExit("Known-Count overrun must fail before reading enumerator.Current.")
if source.index("var hasFinalKnownCount = TryGetKnownCount(elements, out var finalKnownCount);") < current:
    raise SystemExit("Post-traversal Count rebinding must remain after materialization.")

print("PASS MEP quantity known-Count no-overread source guard")
