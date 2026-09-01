#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / "src/QS3D.Core/Services/BulkEditService.cs"
text = source_path.read_text(encoding="utf-8")


def region(start: str, end: str) -> str:
    a = text.find(start)
    b = text.find(end, a + len(start))
    if a < 0 or b < 0:
        raise AssertionError(f"cannot isolate {start}")
    return text[a:b]


def require_order(body: str, tokens: list[str], label: str) -> None:
    pos = -1
    for token in tokens:
        nxt = body.find(token, pos + 1)
        if nxt < 0:
            raise AssertionError(f"{label}: missing or out-of-order token: {token}")
        pos = nxt

owned = region(
    "private static IReadOnlyList<ProjectElement> OwnedDistinct(ProjectState project, IEnumerable<ProjectElement> elements)",
    "private static void RequireCurrentElementOwnership")
materialized = region(
    "private static IReadOnlyList<string> MaterializeBounded(IEnumerable<string> values, string label)",
    "private static int? SnapshotKnownCount")

if owned.count("enumerator.Current") != 1:
    raise AssertionError("BulkEdit object targets must read Current exactly once per traversal")
if materialized.count("enumerator.Current") != 1:
    raise AssertionError("BulkEdit id targets must read Current exactly once per traversal")

require_order(owned, [
    "RequireKnownCountStable(elements, knownCount, knownCountSources, \"Bulk edit target collection\");",
    "enumerator.MoveNext()",
    "RequireKnownCountStable(elements, knownCount, knownCountSources, \"Bulk edit target collection\");",
    "RequireCanObserveNext(knownCount, inputCount, \"Bulk edit target collection\");",
    "var element = enumerator.Current;",
    "RequireKnownCountStable(elements, knownCount, knownCountSources, \"Bulk edit target collection\");",
    "inputCount++;",
    "var elementId = (element.Id ?? string.Empty).Trim();",
], "BulkEdit object target traversal")

require_order(materialized, [
    "RequireKnownCountStable(values, knownCount, knownCountSources, label);",
    "enumerator.MoveNext()",
    "RequireKnownCountStable(values, knownCount, knownCountSources, label);",
    "RequireCanObserveNext(knownCount, inputCount, label);",
    "var value = enumerator.Current;",
    "RequireKnownCountStable(values, knownCount, knownCountSources, label);",
    "inputCount++;",
    "result.Add(value);",
], "BulkEdit id target traversal")

print("PASS bulk edit target Current-induced known Count stability source guard")
sys.exit(0)
