from pathlib import Path

SOURCE = Path("src/QS3D.Core/Documentation/SemanticTitleBlockParameterMapBuilder.cs")
text = SOURCE.read_text(encoding="utf-8")

start = text.index("private static List<SemanticTitleBlockParameterDefinition> MaterializeDefinitionsBounded(")
end = text.index("private static void RevalidateKnownCountAfterTraversal(", start)
body = text[start:end]

anchors = [
    "RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);",
    "var moved = enumerator.MoveNext();",
    "RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);",
    "if (!moved)",
    "if (knownCount.HasValue && observedCount >= knownCount.Value)",
    "if (observedCount >= MaxParameters)",
    "var definition = enumerator.Current;",
    "RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);",
    "result.Add(definition);",
    "observedCount++;",
]

position = 0
for anchor in anchors:
    found = body.find(anchor, position)
    if found < 0:
        raise SystemExit(
            "ERROR: semantic title-block known-Count stability guard requires ordered anchor: " + anchor
        )
    position = found + len(anchor)

if body.count("RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);") != 4:
    raise SystemExit(
        "ERROR: semantic title-block traversal must rebind Count before MoveNext, after MoveNext, after Current, and after traversal"
    )

known_overrun = body.index("if (knownCount.HasValue && observedCount >= knownCount.Value)")
current = body.index("var definition = enumerator.Current;")
post_current = body.index("RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);", current)
retention = body.index("result.Add(definition);", post_current)
if not (known_overrun < current < post_current < retention):
    raise SystemExit(
        "ERROR: semantic title-block traversal must reject over-yield before Current and rebind Count after Current before retention"
    )

if "while (enumerator.MoveNext())" in body:
    raise SystemExit(
        "ERROR: semantic title-block traversal must expose explicit pre/post MoveNext Count rebound boundaries"
    )

print("PASS semantic title-block transient known-Count traversal stability")
