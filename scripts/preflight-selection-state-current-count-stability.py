#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / "src/QS3D.Core/Services/SelectionState.cs"
text = source_path.read_text(encoding="utf-8")

start = text.find("public void Replace(IEnumerable<string> ids)")
end = text.find("public void Clear()", start)
if start < 0 or end < 0:
    raise AssertionError("cannot isolate SelectionState.Replace")
body = text[start:end]

if body.count("enumerator.Current") != 1:
    raise AssertionError("SelectionState replacement must read Current exactly once per traversal")

tokens = [
    "RequireStableKnownCount(ids, knownCount);",
    "enumerator.MoveNext()",
    "RequireStableKnownCount(ids, knownCount);",
    "if (knownCount.HasValue && inputCount >= knownCount.Value)",
    "var raw = enumerator.Current;",
    "RequireStableKnownCount(ids, knownCount);",
    "inputCount++;",
    "if (string.IsNullOrWhiteSpace(raw)) continue;",
    "next.Add(raw.Trim());",
]

pos = -1
for token in tokens:
    nxt = body.find(token, pos + 1)
    if nxt < 0:
        raise AssertionError("SelectionState Current/Count ordering missing or out of order: " + token)
    pos = nxt

print("PASS SelectionState Current-induced known Count stability source guard")
sys.exit(0)
