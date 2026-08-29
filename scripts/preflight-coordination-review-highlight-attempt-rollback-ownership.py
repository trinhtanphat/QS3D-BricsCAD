#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/CoordinationManagerReviewUi.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


highlight = re.search(
    r"public void Highlight\(IReadOnlyList<ObjectId> ids\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private IReadOnlyList<ObjectId> UnhighlightAttemptBestEffort",
    text,
    re.S,
)
require(highlight is not None, "Highlight must call retry-aware per-attempt compensation")
if highlight is not None:
    body = highlight.group("body")
    require("var pending = new List<ObjectId>();" in body,
            "Highlight must retain attempt-local successful native highlight ownership")
    require("_highlighted.AddRange(pending);" in body,
            "successful Highlight must publish session ownership after transaction completion")
    commit = body.find("transaction.Commit();")
    publish = body.find("_highlighted.AddRange(pending);")
    require(commit >= 0 and publish > commit,
            "successful Highlight ownership must remain commit-before-publication")
    require("var rollbackPending = UnhighlightAttemptBestEffort(pending);" in body,
            "failed Highlight must receive the exact rollback IDs whose cleanup is unconfirmed")
    require("_highlighted.AddRange(rollbackPending);" in body,
            "failed rollback IDs must transfer into persistent retry ownership")
    rollback = body.find("var rollbackPending = UnhighlightAttemptBestEffort(pending);")
    retry_publish = body.find("_highlighted.AddRange(rollbackPending);")
    throw_pos = body.find("throw;", retry_publish)
    require(rollback >= 0 and rollback < retry_publish < throw_pos,
            "retry ownership must publish before the original Highlight exception is rethrown")

helper = re.search(
    r"private IReadOnlyList<ObjectId> UnhighlightAttemptBestEffort\(IReadOnlyList<ObjectId> pending\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*public void ClearHighlight",
    text,
    re.S,
)
require(helper is not None, "retry-aware UnhighlightAttemptBestEffort helper was not found")
if helper is not None:
    body = helper.group("body")
    require("var unreleased = new List<ObjectId>();" in body,
            "attempt compensation must track native cleanup failures explicitly")
    require("unreleased.Add(id);" in body,
            "failed per-entity compensation must retain that ID for retry")
    require("return pending.ToArray();" in body,
            "outer compensation failure must conservatively retain the whole attempted set")
    require("return unreleased.AsReadOnly();" in body,
            "successful compensation transaction must return only unconfirmed IDs")
    require("transaction.Commit();" in body,
            "attempt compensation must finish its transaction before cleanup ownership is classified")
    require("_highlighted" not in body,
            "helper must classify rollback ownership locally; caller publishes persistent ownership")

# Preserve surrounding safety contracts.
for token in (
    "public void ClearHighlight()",
    "failed entities remain owned for retry",
    "public void AbandonDestroyedDocumentState()",
    "ResetTransientStateBestEffort(true);",
    "EvaluateRelink(project, issue.IssueId)",
    "CadHandleService.Resolve(_document, handles)",
):
    require(token in text, "coordination review safety contract regressed: " + token)

if errors:
    print("Coordination review highlight-attempt rollback ownership preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS failed highlight-attempt compensation preserves exact retry ownership")
