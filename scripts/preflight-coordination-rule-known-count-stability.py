from pathlib import Path

SOURCE = Path("src/QS3D.Core/Coordination/CoordinationRuleMatrix.cs")
text = SOURCE.read_text(encoding="utf-8")

start = text.index("internal static T[] MaterializeBounded<T>")
end = text.index("private static void RequireStableKnownCount<T>", start)
body = text[start:end]

anchors = [
    "RequireStableKnownCount(items, knownCount, collectionLabel);",
    "var moved = enumerator.MoveNext();",
    "RequireStableKnownCount(items, knownCount, collectionLabel);",
    "if (!moved)",
    "if (hasKnownCount && observedCount >= knownCount)",
    "var item = enumerator.Current;",
    "RequireStableKnownCount(items, knownCount, collectionLabel);",
    "snapshot.Add(item);",
    "observedCount++;",
]

position = 0
for anchor in anchors:
    found = body.find(anchor, position)
    if found < 0:
        raise SystemExit(
            "ERROR: coordination rule known-Count stability guard requires ordered anchor: " + anchor
        )
    position = found + len(anchor)

if body.count("RequireStableKnownCount(items, knownCount, collectionLabel);") != 4:
    raise SystemExit(
        "ERROR: coordination rule traversal must rebind Count before MoveNext, after MoveNext, after Current, and after traversal"
    )

if body.index("if (hasKnownCount && observedCount >= knownCount)") > body.index("var item = enumerator.Current;"):
    raise SystemExit("ERROR: coordination rule over-yield guard must execute before Current")

print("PASS coordination rule known-Count traversal stability")
