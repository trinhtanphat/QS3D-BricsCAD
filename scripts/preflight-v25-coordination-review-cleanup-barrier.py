#!/usr/bin/env python3
"""Guard Coordination review actions from reopening mutations over residual transient state."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CoordinationManagerReviewUi.cs"
text = SOURCE.read_text(encoding="utf-8")

failures = []

unsafe = "_cleanupBarrier = cleanupBarrierBefore && _session.HasTransientState;"
safe = "_cleanupBarrier = _session.HasTransientState;"

if unsafe in text:
    failures.append("RunCleanup still gates residual transient state on the previous cleanup barrier")
if text.count(safe) < 3:
    failures.append("cleanup success/failure and validated-action failure must re-evaluate the barrier from actual residual state")
if "var cleanupBarrierBefore = _cleanupBarrier;" in text:
    failures.append("obsolete previous-barrier snapshot still participates in cleanup semantics")

validated_failure = '''catch (Exception ex)\n                {\n                    _cleanupBarrier = _session.HasTransientState;\n                    SetStatus(actionName + " bị từ chối: " + ex.Message);\n                }'''
if validated_failure not in text:
    failures.append("RunValidated failure does not fail closed when a native effect leaves transient cleanup debt")

# Preserve the fail-closed UX and retry affordances around the corrected state transition.
for contract in (
    'if (_cleanupBarrier)',
    '_highlight.IsEnabled = mutationsAllowed;',
    '_isolate.IsEnabled = mutationsAllowed;',
    '_section.IsEnabled = mutationsAllowed;',
    '_clearHighlight.IsEnabled = _session.HasHighlight;',
    '_restoreIsolation.IsEnabled = _session.HasIsolation;',
    '_restoreView.IsEnabled = _session.HasSectionView;',
    'var cleanupFailure = _session.TryResetTransientStateBestEffort();',
):
    if contract not in text:
        failures.append("review cleanup/action-state contract changed unexpectedly: " + contract)

if failures:
    for failure in failures:
        print("ERROR: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("Coordination review cleanup-barrier preflight passed")
