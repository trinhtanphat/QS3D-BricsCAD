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


handler = re.search(
    r"private void OnWindowClosing\(object sender, CancelEventArgs e\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private void OnWindowClosed",
    text,
    re.S,
)
require(handler is not None, "OnWindowClosing handler was not found")
if handler is not None:
    body = handler.group("body")
    live_guard = "if (!_attached || _disposeInProgress || _disposed) return;"
    pre_cancel_guard = "if (e.Cancel) return;"
    cleanup = "_session.TryResetTransientStateBestEffort()"
    require(live_guard in body, "close handler must remain inert outside a live attached controller")
    require(pre_cancel_guard in body, "close handler must honor a cancellation already owned by another subscriber")
    live_pos = body.find(live_guard)
    cancel_guard_pos = body.find(pre_cancel_guard)
    cleanup_pos = body.find(cleanup)
    require(0 <= live_pos < cancel_guard_pos < cleanup_pos,
            "incoming cancellation must short-circuit before any transient cleanup attempt")

    pre_cancel_prefix = body[:cleanup_pos]
    forbidden_before_cleanup = [
        "SetStatus(",
        "UpdateActionState();",
        "_cleanupBarrier =",
        "_session.HasTransientState",
    ]
    for token in forbidden_before_cleanup:
        require(token not in pre_cancel_prefix,
                "pre-cancel path must not mutate controller/session state before returning: " + token)

    require("e.Cancel = false;" not in body,
            "Coordination review must never clear cancellation owned by another subscriber")
    require("e.Cancel = true;" in body,
            "Coordination review must retain authority to veto close when its own cleanup is incomplete")

if errors:
    print("Coordination review window-close cancellation arbitration preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS Coordination review Closing arbitration preserves prior subscriber cancellation without native mutation")
