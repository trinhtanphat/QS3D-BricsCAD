#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/MepReviewWorkspaceCommands.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/mep-review-window-publication.md"
errors = []

if not SOURCE.exists():
    errors.append(f"missing source: {SOURCE.relative_to(ROOT)}")
if not RUNBOOK.exists():
    errors.append(f"missing runbook: {RUNBOOK.relative_to(ROOT)}")

if errors:
    print("MEP Review window publication preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

source = SOURCE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

required = [
    "private static MepReviewWorkspaceWindow? _window;",
    "var published = _window;",
    "published.IsLoaded",
    "ReleasePublishedWindow(published)",
    "MepReviewWorkspaceWindow? candidate = null;",
    "window.Closed += (_, __) => ReleasePublishedWindow(window)",
    "BricsApplication.ShowModelessWindow(window)",
    "if (!window.IsLoaded) return;",
    "_window = window;",
    "candidate = null;",
    "finally",
    "if (candidate != null) TryCloseUnpublishedWindow(candidate);",
    "if (!ReferenceEquals(_window, window)) return;",
    "try { window.Close(); } catch (System.Exception) { }",
    "DocumentManager.MdiActiveDocument",
    "MepRecognitionProfileProvider.Save(profile)",
    "MepRecognitionProfileProvider.Reload()",
]
for token in required:
    if token not in source:
        errors.append(f"source: missing required lifecycle/safety token {token!r}")

for forbidden in (
    "if (_window.IsVisible)",
    "if (published.IsVisible)",
    "private readonly Document",
    "private Document",
    "private readonly ObjectId",
    "private ObjectId",
    "private readonly DBObject",
    "private DBObject",
    "private readonly Solid3d",
    "private Solid3d",
):
    if forbidden in source:
        errors.append(f"source: forbidden stale publication/native-retention token {forbidden!r}")

sequence = [
    "window.Closed += (_, __) => ReleasePublishedWindow(window)",
    "BricsApplication.ShowModelessWindow(window)",
    "if (!window.IsLoaded) return;",
    "_window = window;",
]
positions = [source.find(token) for token in sequence]
if any(position < 0 for position in positions) or positions != sorted(positions) or len(set(positions)) != len(positions):
    errors.append("source: expected Closed -> host show -> Loaded check -> publication ordering")
else:
    publication_position = positions[-1]
    cleanup_transfer_position = source.find("candidate = null;", publication_position + len(sequence[-1]))
    if cleanup_transfer_position < 0 or cleanup_transfer_position <= publication_position:
        errors.append("source: cleanup ownership must transfer only after authoritative publication")

if source.count("_window = window;") != 1:
    errors.append("source: authoritative window must be published exactly once")

runbook_folded = runbook.casefold()
for token in (
    "Lane-Key: issue-4859",
    "LOCAL_ONLY",
    "NO_RESULT",
    "no remote LOCAL_PASS",
    "repeated invocation",
    "active-document switching",
    "profile edit/save/reload",
    "ShowModelessWindow -> IsLoaded -> publish",
):
    if token.casefold() not in runbook_folded:
        errors.append(f"runbook: missing qualification token {token!r}")

if errors:
    print("MEP Review window publication preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("MEP Review window publication preflight: PASS")
