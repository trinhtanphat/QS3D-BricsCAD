#!/usr/bin/env python3
from pathlib import Path
import re

SOURCE = Path("src/QS3D.BricsCAD.V25/UI/CoordinationManagerReviewUi.cs")
text = SOURCE.read_text(encoding="utf-8")

start = text.find("public void ClearHighlight()")
end = text.find("public void Isolate(IReadOnlyList<ObjectId> ids)", start)
if start < 0 or end < 0:
    raise SystemExit("FAIL coordination highlight cleanup ownership: ClearHighlight boundary not found")
body = text[start:end]

for token in (
    "var pending = _highlighted.ToArray();",
    "var released = new List<ObjectId>();",
    "Exception? cleanupFailure = null;",
    "released.Add(id);",
    "cleanupFailure = cleanupFailure ?? ex;",
    "transaction.Commit();",
    "_highlighted.Remove(id);",
    "if (cleanupFailure != null)",
    "throw new InvalidOperationException",
):
    if token not in body:
        raise SystemExit(f"FAIL coordination highlight cleanup ownership: missing {token}")

commit = body.find("transaction.Commit();")
release_loop = body.find("foreach (var id in released)")
release = body.find("_highlighted.Remove(id);", release_loop)
failure_publish = body.find("if (cleanupFailure != null)", release)
if not (0 <= commit < release_loop < release < failure_publish):
    raise SystemExit("FAIL coordination highlight cleanup ownership: successful ownership release must occur only after native transaction commit, before incomplete-cleanup failure is surfaced")

live = body.split("if (_destroyed)", 1)[-1]
if "_highlighted.Clear();" in live.split("using (_document.LockDocument())", 1)[-1]:
    raise SystemExit("FAIL coordination highlight cleanup ownership: live cleanup must not clear the whole ownership set")

catch = re.search(r"catch\s*\(Exception ex\)\s*\{(?P<body>.*?)\n\s*\}", body, re.S)
if not catch or "cleanupFailure = cleanupFailure ?? ex;" not in catch.group("body"):
    raise SystemExit("FAIL coordination highlight cleanup ownership: per-entity native failure must remain observable")
if "released.Add(id);" in catch.group("body"):
    raise SystemExit("FAIL coordination highlight cleanup ownership: failed entity must not be published as released")

if "if (_destroyed)" not in body or "_highlighted.Clear();" not in body:
    raise SystemExit("FAIL coordination highlight cleanup ownership: destroyed-document explicit abandon semantics must remain")

print("PASS coordination review per-entity highlight cleanup retry ownership")
