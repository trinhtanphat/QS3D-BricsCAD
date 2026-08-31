#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticSheetAutoLayoutPlanner.cs"


def require_order(block, label, tokens):
    cursor = -1
    for token in tokens:
        next_cursor = block.find(token, cursor + 1)
        if next_cursor < 0:
            print(f"ERROR: {label} is missing ordered token: {token}")
            return False
        cursor = next_cursor
    return True


def main():
    text = SOURCE.read_text(encoding="utf-8")
    item_count = 'RequireKnownCountStillMatches(items, knownCount, "automatic sheet layout items");'
    view_count = 'RequireKnownCountStillMatches(availableViews, knownCount, "automatic sheet layout available views");'
    required = [
        item_count,
        "var moved = enumerator.MoveNext();",
        "var item = enumerator.Current;",
        "result.Add(item);",
        view_count,
        "var view = enumerator.Current;",
        "result.Add(id, view);",
        "private static void RequireKnownCountStillMatches<T>",
    ]
    missing = [token for token in required if token not in text]
    if missing:
        print("ERROR: semantic sheet auto-layout traversal Count contract is incomplete:")
        for token in missing:
            print(" -", token)
        return 1

    items = text[text.index("private static List<SemanticSheetAutoLayoutItem> MaterializeItemsBounded"):text.index("private static Dictionary<string, SemanticViewPlan> BuildViewIndex")]
    if items.count(item_count) < 4 or not require_order(
        items,
        "item traversal Count boundary",
        [
            item_count,
            "var moved = enumerator.MoveNext();",
            item_count,
            "if (!moved) break;",
            "var item = enumerator.Current;",
            item_count,
            "result.Add(item);",
            item_count,
            "RequireTraversalMatchesKnownCount(knownCount, result.Count",
        ],
    ):
        print("ERROR: item traversal must rebind admitted Count before/after MoveNext, after Current before retention, and before final publication.")
        return 1

    views = text[text.index("private static Dictionary<string, SemanticViewPlan> BuildViewIndex"):text.index("private static int? RequireKnownCountsWithinLimit")]
    if views.count(view_count) < 4 or not require_order(
        views,
        "available-view traversal Count boundary",
        [
            view_count,
            "var moved = enumerator.MoveNext();",
            view_count,
            "if (!moved) break;",
            "var view = enumerator.Current;",
            view_count,
            "count++;",
            "result.Add(id, view);",
            view_count,
            "RequireTraversalMatchesKnownCount(knownCount, count",
        ],
    ):
        print("ERROR: available-view traversal must rebind admitted Count before/after MoveNext, after Current before indexing, and before final publication.")
        return 1

    helper = text[text.index("private static int? RequireKnownCountsWithinLimit"):text.index("private static void ValidateOptions")]
    helper_tokens = [
        "values is ICollection<T> collection",
        "values is IReadOnlyCollection<T> readOnlyCollection",
        "values is ICollection nonGenericCollection",
        "invalid negative known count",
        "conflicting known counts",
        "RequireKnownCountsWithinLimit(values, label)",
        "known Count changed during traversal",
        "known Count does not match traversal cardinality",
    ]
    helper_missing = [token for token in helper_tokens if token not in helper]
    if helper_missing:
        print("ERROR: auto-layout Count helper no longer binds all admitted channels or fail-closed outcomes:")
        for token in helper_missing:
            print(" -", token)
        return 1

    print("PASS: semantic sheet auto-layout pins all admitted Count channels at MoveNext, Current-retention, and final publication boundaries.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())