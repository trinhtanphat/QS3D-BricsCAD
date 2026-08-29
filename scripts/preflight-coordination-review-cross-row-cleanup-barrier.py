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
if selection_start < 0 or selection_end < 0:
    print("ERROR: selection-change cleanup boundary was not found", file=sys.stderr)
    raise SystemExit(1)
if "SetStatus(string.Empty);" in selection:
    print("ERROR: selection change must not erase cleanup-failure status before debt is discharged", file=sys.stderr)
    raise SystemExit(1)

cleanup_start = text.find("private void RunCleanup(")
cleanup_end = text.find("private void RunValidated(", cleanup_start)
cleanup_body = text[cleanup_start:cleanup_end]
if cleanup_start < 0 or cleanup_end < 0:
    print("ERROR: cleanup retry boundary was not found", file=sys.stderr)
    raise SystemExit(1)
required_cleanup = [
    "var cleanupBarrierBefore = _cleanupBarrier;",
    "_cleanupBarrier = cleanupBarrierBefore && _session.HasTransientState;",
]
missing_cleanup = [token for token in required_cleanup if token not in cleanup_body]
if missing_cleanup:
    print("ERROR: RunCleanup must preserve cross-row barrier provenance while retrying owned cleanup:", file=sys.stderr)
    for token in missing_cleanup:
        print(f" - {token}", file=sys.stderr)
    raise SystemExit(1)
if cleanup_body.count("_cleanupBarrier = cleanupBarrierBefore && _session.HasTransientState;") < 2:
    print("ERROR: RunCleanup must resolve the prior cross-row barrier after both success and failure", file=sys.stderr)
    raise SystemExit(1)
if "_cleanupBarrier = _session.HasTransientState;" in cleanup_body:
    print("ERROR: same-row cleanup must not invent a cross-row barrier from unrelated transient state", file=sys.stderr)
    raise SystemExit(1)
if "cleanupFailure" in cleanup_body:
    print("ERROR: RunCleanup must not inherit row-change failure state; retry admission is based on prior barrier provenance plus remaining ownership", file=sys.stderr)
    raise SystemExit(1)

validated_start = cleanup_end
validated_end = text.find("private IReadOnlyList<ObjectId> ResolveReviewTargets", validated_start)
validated_body = text[validated_start:validated_end]
if validated_start < 0 or validated_end < 0:
    print("ERROR: validated review boundary was not found", file=sys.stderr)
    raise SystemExit(1)
barrier_pos = validated_body.find("if (_cleanupBarrier)")
resolve_pos = validated_body.find("var resolved = ResolveReviewTargets();")
if barrier_pos < 0 or resolve_pos < 0 or barrier_pos >= resolve_pos:
    print("ERROR: RunValidated must fail closed on the explicit cross-row cleanup barrier before target resolution/native mutation", file=sys.stderr)
    raise SystemExit(1)
if "if (_session.HasTransientState)" in validated_body or "if (_cleanupBarrier || _session.HasTransientState)" in validated_body:
    print("ERROR: RunValidated must not block same-row composition merely because the current row owns transient state", file=sys.stderr)
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
