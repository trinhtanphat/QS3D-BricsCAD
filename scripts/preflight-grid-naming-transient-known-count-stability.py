from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/GridNamingService.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/GridNamingTransientKnownCountSmoke.cs").read_text(encoding="utf-8")

start = source.index("public static IReadOnlyList<GridLabelAssignment> Renumber(")
end = source.index("public static string FormatLabel", start)
renumber = source[start:end]

required = (
    "using (var enumerator = orderedGridElementIds.GetEnumerator())",
    "RequireStableKnownCountDuringTraversal(project, orderedGridElementIds, knownCount, targetEnumerationVersion);",
    "if (!enumerator.MoveNext()) break;",
    "var value = enumerator.Current;",
    "RevalidateKnownCountAfterTraversal(project, orderedGridElementIds, knownCount, targetEnumerationVersion);",
)
for token in required:
    assert token in renumber, f"missing Grid transient known-Count production contract: {token}"

pre_move = renumber.index("RequireStableKnownCountDuringTraversal(project, orderedGridElementIds, knownCount, targetEnumerationVersion);")
move = renumber.index("if (!enumerator.MoveNext()) break;")
post_move = renumber.index("RequireStableKnownCountDuringTraversal(project, orderedGridElementIds, knownCount, targetEnumerationVersion);", pre_move + 1)
overrun = renumber.index("if (knownCount.HasValue && ids.Count == knownCount.Value)")
cap = renumber.index("if (ids.Count == MaxGridBatch)")
current = renumber.index("var value = enumerator.Current;")
assert pre_move < move < post_move < overrun < cap < current, (
    "Grid renumber traversal must rebind Count before MoveNext and immediately after successful MoveNext, "
    "then apply admitted-count/project-cap gates before semantic Current"
)
assert "foreach (var value in orderedGridElementIds)" not in renumber, (
    "Grid renumber must not regress to caller-controlled foreach before transient Count gates"
)

for token in (
    "TransientGrowthRejectsBeforeCurrentAndMutation",
    "TransientShrinkRejectsBeforeCurrentAndMutation",
    "TransientNegativeRejectsBeforeCurrentAndMutation",
    "TransientConflictRejectsBeforeCurrentAndMutation",
    "StableCountedInputStillRenumbers",
    "StreamingInputStillRenumbers",
    "CurrentReads == 0",
    "project.ChangeVersion == version",
    "[ModuleInitializer]",
):
    assert token in smoke, f"missing Grid transient known-Count smoke coverage: {token}"

print("PASS: Grid naming transient known-Count traversal contract is locked")
