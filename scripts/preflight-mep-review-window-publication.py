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

def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"{label}: missing required token {token!r}")

def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        errors.append(f"{label}: forbidden token {token!r}")

for token in (
    "private static MepReviewWorkspaceWindow? _published;",
    "private static MepReviewWorkspaceWindow? _pending;",
    "MepReviewWorkspaceWindow? candidate = null;",
    "var pending = _pending;",
    "if (pending != null && !TryClosePendingWindow(pending))",
    "var published = _published;",
    "published.IsLoaded",
    "ReleasePublishedWindow(published);",
    "candidate = new MepReviewWorkspaceWindow();",
    "_pending = window;",
    "window.Closed += (_, __) => ReleaseWindow(window);",
    "BricsApplication.ShowModelessWindow(window);",
    "if (!window.IsLoaded)",
    "_published = window;",
    "ReleasePendingWindow(window);",
    "candidate = null;",
    "if (candidate != null)",
    "TryClosePendingWindow(candidate);",
    "private static void ReleaseWindow(MepReviewWorkspaceWindow window)",
    "private static void ReleasePublishedWindow(MepReviewWorkspaceWindow window)",
    "if (!ReferenceEquals(_published, window)) return;",
    "_published = null;",
    "private static void ReleasePendingWindow(MepReviewWorkspaceWindow window)",
    "if (!ReferenceEquals(_pending, window)) return;",
    "_pending = null;",
    "private static bool TryClosePendingWindow(MepReviewWorkspaceWindow window)",
    "if (!ReferenceEquals(_pending, window)) return true;",
    "if (ReferenceEquals(_published, window))",
    "if (window.IsLoaded) return false;",
    "ex.GetType().Name",
    "DocumentManager.MdiActiveDocument",
    "MepRecognitionProfileProvider.Save(profile)",
    "MepRecognitionProfileProvider.Reload()",
):
    require(source, token, "source")

for token in (
    "private static MepReviewWorkspaceWindow? _window;",
    "if (_window.IsVisible)",
    "if (published.IsVisible)",
    "TryCloseUnpublishedWindow",
    '"\\nQS3DMEPREVIEW error: " + ex.Message',
    '"Không queue được " + command + ": " + ex.Message',
    "private readonly Document",
    "private Document",
    "private readonly ObjectId",
    "private ObjectId",
    "private readonly DBObject",
    "private DBObject",
    "private readonly Solid3d",
    "private Solid3d",
):
    forbid(source, token, "source")

show_start = source.find("public void ShowReviewWorkspace()")
release_start = source.find("private static void ReleaseWindow", show_start + 1)
show = source[show_start:release_start] if show_start >= 0 and release_start > show_start else ""
ordered = (
    "var pending = _pending;",
    "candidate = new MepReviewWorkspaceWindow();",
    "_pending = window;",
    "window.Closed += (_, __) => ReleaseWindow(window);",
    "BricsApplication.ShowModelessWindow(window);",
    "if (!window.IsLoaded)",
    "_published = window;",
    "ReleasePendingWindow(window);",
)
positions = [show.find(token) for token in ordered]
if min(positions) < 0:
    errors.append("source: unable to prove drain -> construct -> pending -> show -> loaded -> publish ordering")
elif positions != sorted(positions) or len(set(positions)) != len(positions):
    errors.append("source: MEP Review publication ordering is not monotonic")
else:
    release_pending_position = positions[-1]
    cleanup_transfer_position = show.find(
        "candidate = null;",
        release_pending_position + len("ReleasePendingWindow(window);"),
    )
    if cleanup_transfer_position < 0 or cleanup_transfer_position <= release_pending_position:
        errors.append("source: local cleanup ownership must transfer only after pending ownership is released")

close_start = source.find("private static bool TryClosePendingWindow")
class_start = source.find("internal sealed class MepReviewWorkspaceWindow", close_start + 1)
close_body = source[close_start:class_start] if close_start >= 0 and class_start > close_start else ""
for token in (
    "if (!ReferenceEquals(_pending, window)) return true;",
    "if (ReferenceEquals(_published, window))",
    "ReleasePendingWindow(window);",
    "try { window.Close(); } catch (System.Exception) { }",
    "if (window.IsLoaded) return false;",
):
    require(close_body, token, "pending-close fail-closed")

if source.count("_published = window;") != 1:
    errors.append("source: authoritative published owner must be assigned exactly once")
if source.count("_pending = window;") != 1:
    errors.append("source: candidate must become pending-owned exactly once")

runbook_folded = runbook.casefold()
for token in (
    "issue-4859",
    "issue-4956",
    "LOCAL_ONLY",
    "NO_RESULT",
    "no remote LOCAL_PASS",
    "repeated invocation",
    "active-document switching",
    "profile edit/save/reload",
    "show exception",
    "non-loaded show",
    "pending close failure",
    "pending recovery",
    "stale callback isolation",
    "error redaction",
):
    if token.casefold() not in runbook_folded:
        errors.append(f"runbook: missing qualification token {token!r}")

if errors:
    print("MEP Review window publication preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("MEP Review window publication preflight: PASS")
