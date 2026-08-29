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
    r"public void Highlight\(IReadOnlyList<ObjectId> ids\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*public void ClearHighlight",
    text,
    re.S,
)
require(highlight is not None, "TransientReviewSession.Highlight was not found")
if highlight is not None:
    body = highlight.group("body")
    require("RequireTargets(ids);" in body and "ClearHighlight();" in body,
            "Highlight must preserve target validation and clear prior QS3D-owned highlight state")
    require("var pending = new List<ObjectId>();" in body,
            "Highlight must accumulate this attempt in local pending ownership")
    require("entity.Highlight();" in body,
            "Highlight must still use the native Entity.Highlight presentation operation")
    pending_add = body.find("pending.Add(id);")
    entity_highlight = body.find("entity.Highlight();")
    require(entity_highlight >= 0 and pending_add > entity_highlight,
            "local pending ownership must publish only after each native highlight call succeeds")
    require("_highlighted.AddRange(pending);" in body,
            "session-owned highlight state must publish only after the whole attempt succeeds")
    publish = body.find("_highlighted.AddRange(pending);")
    commit = body.find("transaction.Commit();")
    require(commit >= 0 and publish > commit,
            "persistent session ownership must publish only after successful transaction completion")
    require("catch" in body and "UnhighlightAttemptBestEffort(pending);" in body and "throw;" in body,
            "failed multi-entity highlight must compensate this attempt before rethrow")
    catch_pos = body.find("catch")
    rollback_pos = body.find("UnhighlightAttemptBestEffort(pending);", catch_pos)
    throw_pos = body.find("throw;", catch_pos)
    require(catch_pos >= 0 and catch_pos < rollback_pos < throw_pos,
            "highlight catch path must compensate before propagating failure")
    require("_highlighted.Add(id);" not in body,
            "session ownership must never publish incrementally inside the native highlight loop")

helper = re.search(
    r"private void UnhighlightAttemptBestEffort\(IReadOnlyList<ObjectId> pending\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*public void ClearHighlight",
    text,
    re.S,
)
require(helper is not None, "per-attempt highlight compensation helper was not found")
if helper is not None:
    body = helper.group("body")
    require("if (pending == null || pending.Count == 0 || _destroyed) return;" in body,
            "compensation must be inert for empty/destroyed attempts")
    require("_document.LockDocument()" in body and "StartTransaction()" in body,
            "compensation must re-enter the document through bounded native lifetime")
    require("entity?.Unhighlight();" in body,
            "compensation must best-effort unhighlight only this attempt's successfully highlighted entities")
    require("catch" in body,
            "one failed cleanup target must not prevent the rest of the attempt from being compensated")
    require("_highlighted" not in body,
            "attempt compensation must not mutate session-published ownership")

# Preserve established fail-closed review semantics and cleanup boundaries.
for token in (
    "var resolved = ResolveReviewTargets();",
    "effect(resolved);",
    "EvaluateRelink(project, issue.IssueId)",
    "CadHandleService.Resolve(_document, handles)",
    "public void ResetTransientStateBestEffort()",
    "public void AbandonDestroyedDocumentState()",
):
    require(token in text, "coordination review safety contract regressed: " + token)

if errors:
    print("Coordination review highlight rollback preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS Coordination review highlight publication is atomic with per-attempt best-effort rollback")
