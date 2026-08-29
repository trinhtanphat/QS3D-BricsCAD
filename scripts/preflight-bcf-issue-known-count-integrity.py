#!/usr/bin/env python3
from pathlib import Path

SOURCE = Path("src/QS3D.Core/Export/BcfIssueExchange.cs")
text = SOURCE.read_text(encoding="utf-8")

start = text.index("internal static List<T> MaterializeBounded<T>(")
end = text.index("private static int? ValidateKnownCounts<T>(", start)
body = text[start:end]

required = [
    "using (var enumerator = values.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (knownCount.HasValue && observedCount >= knownCount.Value)",
    "if (observedCount >= maximumCount)",
    "items.Add(enumerator.Current)",
    "if (knownCount.HasValue && observedCount != knownCount.Value)",
    "var currentKnownCount = ValidateKnownCounts(",
]
for token in required:
    if token not in body:
        raise SystemExit(f"BCF known-Count integrity guard missing required token: {token}")

forbidden = [
    "foreach (var value in values)",
    "corroboratedKnownCount && knownCount.HasValue && observedCount >= knownCount.Value",
    "items.Add(value)",
]
for token in forbidden:
    if token in body:
        raise SystemExit(f"BCF known-Count integrity guard found unsafe traversal token: {token}")

move_next = body.index("while (enumerator.MoveNext())")
count_guard = body.index("if (knownCount.HasValue && observedCount >= knownCount.Value)")
cap_guard = body.index("if (observedCount >= maximumCount)")
current_read = body.index("items.Add(enumerator.Current)")
rebind = body.index("var currentKnownCount = ValidateKnownCounts(")

if not (move_next < count_guard < current_read and move_next < cap_guard < current_read < rebind):
    raise SystemExit("BCF known-Count integrity guard requires MoveNext -> Count/cap admission -> Current -> rebind ordering")

print("PASS BCF bounded collection known-Count Current no-overread/rebind source guard")
