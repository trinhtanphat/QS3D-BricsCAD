#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticSheetAutoLayoutPlanner.cs"


def main():
    text = SOURCE.read_text(encoding="utf-8")
    required = [
        "RequireKnownCountStillMatches(items, knownCount, \"automatic sheet layout items\");",
        "var moved = enumerator.MoveNext();",
        "var item = enumerator.Current;",
        "result.Add(item);",
        "RequireKnownCountStillMatches(availableViews, knownCount, \"automatic sheet layout available views\");",
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
    item_move = items.index("var moved = enumerator.MoveNext();")
    item_current = items.index("var item = enumerator.Current;")
    item_add = items.index("result.Add(item);")
    if items.count("RequireKnownCountStillMatches(items, knownCount") < 4 or not item_move < item_current < item_add:
        print("ERROR: item traversal must rebind admitted Count around MoveNext and after Current before retention.")
        return 1

    views = text[text.index("private static Dictionary<string, SemanticViewPlan> BuildViewIndex"):text.index("private static int? RequireKnownCountsWithinLimit")]
    view_move = views.index("var moved = enumerator.MoveNext();")
    view_current = views.index("var view = enumerator.Current;")
    view_add = views.index("result.Add(id, view);")
    if views.count("RequireKnownCountStillMatches(availableViews, knownCount") < 4 or not view_move < view_current < view_add:
        print("ERROR: available-view traversal must rebind admitted Count around MoveNext and after Current before indexing.")
        return 1

    print("PASS: semantic sheet auto-layout rebinds admitted Count channels across both caller-controlled traversals.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
