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

for contract in (
    'if (_cleanupBarrier)',
    'var cleanupFailure = _session.TryResetTransientStateBestEffort();',
    'private bool IsOwnerDocumentGenerationActive',
    'var ownerActive = IsOwnerDocumentGenerationActive;',
):
    if contract not in text:
        failures.append("review cleanup/action-state contract changed unexpectedly: " + contract)

for owner_affine in (
    '_highlight.IsEnabled = ownerActive && mutationsAllowed;',
    '_isolate.IsEnabled = ownerActive && mutationsAllowed;',
    '_section.IsEnabled = ownerActive && mutationsAllowed;',
    '_clearHighlight.IsEnabled = ownerActive && _session.HasHighlight;',
    '_restoreIsolation.IsEnabled = ownerActive && _session.HasIsolation;',
    '_restoreView.IsEnabled = ownerActive && _session.HasSectionView;',
):
    if owner_affine not in text:
        failures.append("generation-aware action-state contract changed unexpectedly: " + owner_affine)

# A foreign MDI document is not itself native-generation drift. The controller may
# disable mutations while inactive, but must retain same-generation cleanup debt.
activated_start = text.find("private void OnDocumentActivated")
activated_end = text.find("private void OnDocumentToBeDestroyed", activated_start)
activated = text[activated_start:activated_end] if activated_start >= 0 and activated_end > activated_start else ""
for contract in (
    'if (!_session.IsOwnerNativeGenerationCurrent)',
    'AbandonStaleGenerationIfNeeded();',
    '_cleanupBarrier = _session.HasTransientState;',
):
    if contract not in activated:
        failures.append("DocumentActivated must distinguish stale native generation from foreign MDI activity: " + contract)

if failures:
    for failure in failures:
        print("ERROR: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("Coordination review cleanup-barrier preflight passed")
