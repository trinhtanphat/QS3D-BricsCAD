#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilySubtype.cs"
errors = []


def method_body(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    brace = text.find("{", start)
    if brace < 0:
        errors.append("missing method body: " + signature)
        return ""
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[brace + 1:index]
    errors.append("unterminated method: " + signature)
    return ""


text = SOURCE.read_text(encoding="utf-8")

for required, message in (
    ("private long _familyHighlightAttachmentGeneration;", "Family reveal needs an attachment-generation fence"),
    ("private DispatcherOperation? _familyHighlightRefreshOperation;", "queued Family reveal must retain a cancellable DispatcherOperation"),
    ("Loaded += OnFamilySubtypeWorkspaceLoaded;", "Family subtype lifecycle must observe Workspace load generations"),
    ("Unloaded += OnFamilySubtypeWorkspaceUnloaded;", "Family subtype lifecycle must observe Workspace unload generations"),
    ("private void CancelFamilyHighlightRefresh()", "Family reveal lifecycle needs a centralized pending-operation cancellation helper"),
):
    if required not in text:
        errors.append(message)

refresh = method_body(text, "private void RefreshSelectedFamilyHighlight()")
for required, message in (
    ("var attachmentGeneration = _familyHighlightAttachmentGeneration;", "queued reveal must capture the scheduling attachment generation"),
    ("_familyHighlightRefreshOperation = Dispatcher.BeginInvoke", "queued reveal operation must be retained for unload cancellation"),
    ("!IsLoaded || attachmentGeneration != _familyHighlightAttachmentGeneration", "deferred reveal must reject unloaded or stale attachment generations"),
    ("RevealSelectedFamilyAndRefreshHighlight();", "current-generation callback must preserve centralized generator-safe reveal"),
):
    if required not in refresh:
        errors.append(message)

fence = refresh.find("!IsLoaded || attachmentGeneration != _familyHighlightAttachmentGeneration")
reveal = refresh.find("RevealSelectedFamilyAndRefreshHighlight();")
if fence >= 0 and reveal >= 0 and fence > reveal:
    errors.append("attachment-generation fence must run before Family reveal")

cancel = method_body(text, "private void CancelFamilyHighlightRefresh()")
for required in ("_familyHighlightRefreshOperation?.Abort();", "_familyHighlightRefreshOperation = null;", "_familyHighlightRefreshPending = false;"):
    if required not in cancel:
        errors.append("Family reveal cancellation missing: " + required)

loaded = method_body(text, "private void OnFamilySubtypeWorkspaceLoaded(object sender, RoutedEventArgs e)")
unloaded = method_body(text, "private void OnFamilySubtypeWorkspaceUnloaded(object sender, RoutedEventArgs e)")
if "_familyHighlightAttachmentGeneration++;" not in loaded or "RefreshSelectedFamilyHighlight();" not in loaded:
    errors.append("Workspace load must advance Family attachment generation and queue a fresh reveal")
if "_familyHighlightAttachmentGeneration++;" not in unloaded or "CancelFamilyHighlightRefresh();" not in unloaded:
    errors.append("Workspace unload must advance Family attachment generation and cancel pending reveal")

create = method_body(text, "private void CreateFamilyFromWorkspaceSubtype(bool launchSolid3D)")
if ".Message" in create:
    errors.append("Workspace subtype authoring must not publish raw Exception.Message")
if re.search(r"catch\s*\(\s*Exception\s+\w+\s*\)", create):
    errors.append("Workspace subtype authoring must use a non-capturing exception boundary")
if "ReportWorkspaceFailure(" not in create:
    errors.append("Workspace subtype authoring failure must route through stable redacted Workspace reporting")

for forbidden in ("FamilyList.UpdateLayout(", "EventManager.RegisterClassHandler"):
    if forbidden in text:
        errors.append("Family highlight affinity must not introduce unsafe workaround: " + forbidden)

print("QS3D Workspace Family highlight attachment-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: queued Family reveal is cancellable and attachment-generation fenced, and subtype authoring reports stable redacted failures.")
