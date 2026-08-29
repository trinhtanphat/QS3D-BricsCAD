#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CoordinationManagerReviewUi.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    "private bool _cleanupBarrier;",
    "var cleanupFailure = _session.TryResetTransientStateBestEffort();",
    "_cleanupBarrier = cleanupFailure != null || _session.HasTransientState;",
    "var mutationsAllowed = actionable && !_cleanupBarrier;",
    "_clearHighlight.IsEnabled = _session.HasHighlight;",
    "_restoreIsolation.IsEnabled = _session.HasIsolation;",
    "_restoreView.IsEnabled = _session.HasSectionView;",
    "public bool HasTransientState => HasHighlight || HasIsolation || HasSectionView;",
    "public Exception? TryResetTransientStateBestEffort()",
    "_cleanupBarrier = false;",
]
missing = [token for token in required if token not in text]
if missing:
    print("ERROR: coordination review cross-row cleanup barrier contract missing:", file=sys.stderr)
    for token in missing:
        print(f" - {token}", file=sys.stderr)
    raise SystemExit(1)

selection_start = text.find("private void OnSelectionChanged")
selection_end = text.find("private void OnDocumentActivated", selection_start)
selection = text[selection_start:selection_end]
if "SetStatus(string.Empty);" in selection:
    print("ERROR: selection change must not erase cleanup-failure status before debt is discharged", file=sys.stderr)
    raise SystemExit(1)

for handler, cleanup in [
    ("OnClearHighlight", "RunCleanup(\"Clear Highlight\""),
    ("OnRestoreIsolation", "RunCleanup(\"Restore Isolation\""),
    ("OnRestoreView", "RunCleanup(\"Restore View\""),
]:
    start = text.find(f"private void {handler}")
    end = text.find("private void ", start + 20)
    body = text[start:end]
    if cleanup not in body or "RunValidated(" in body:
        print(f"ERROR: {handler} must retry owned cleanup without resolving the newly selected row", file=sys.stderr)
        raise SystemExit(1)

print("PASS coordination review cross-row cleanup barrier")
