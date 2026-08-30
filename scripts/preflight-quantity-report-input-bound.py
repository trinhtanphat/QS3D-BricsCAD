from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Reporting" / "QuantityReportBuilder.cs"
text = SOURCE.read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit("FAIL: " + message)


require(
    "internal const int MaximumInputElements = 10000;" in text,
    "QuantityReportBuilder must retain the explicit 10000-element input ceiling.",
)
require(
    "if (count > MaximumInputElements)\n                throw TooManyInputElements();" in text,
    "known Count admission must reject oversize input before traversal.",
)

move = text.index("var moved = enumerator.MoveNext();")
known_overrun = text.index("if (knownCount.HasValue && observedCount >= knownCount.Value)", move)
stream_bound = text.index("if (observedCount >= MaximumInputElements)", known_overrun)
current = text.index("var element = enumerator.Current;", stream_bound)
post_current_count = text.index("RequireStableKnownElementCount(elements, knownCount);", current)
semantic_acceptance = text.index("observedCount++;", post_current_count)

require(
    move < known_overrun < stream_bound < current < post_current_count < semantic_acceptance,
    "required traversal order is MoveNext -> known overrun -> 10000 ceiling -> Current -> Count rebound -> semantic acceptance.",
)
require(
    "foreach (var element in elements)" not in text,
    "QuantityReportBuilder must retain explicit enumeration so the ceiling stays before Current.",
)
require(
    '"Quantity report supports at most " + MaximumInputElements + " input elements."' in text,
    "the deterministic input-bound diagnostic must remain present.",
)

print("PASS quantity report input traversal bound")
