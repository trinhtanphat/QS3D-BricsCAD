#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs"


def fail(message: str) -> None:
    print("Update Center close cleanup preflight FAILED:", file=sys.stderr)
    print("- " + message, file=sys.stderr)
    raise SystemExit(1)


def require(condition: bool, message: str) -> None:
    if not condition:
        fail(message)


if not SOURCE.is_file():
    fail("missing UpdateCenterWindow.cs")

text = SOURCE.read_text(encoding="utf-8")

for token in (
    "private bool _coordinatorAttached;",
    "UpdateCoordinator.Instance.StateChanged += OnStateChanged;",
    "_coordinatorAttached = true;",
    "Closed += (_, __) => DetachCoordinator();",
    "internal void DetachCoordinator()",
    "if (!_coordinatorAttached) return;",
    "UpdateCoordinator.Instance.StateChanged -= OnStateChanged;",
    "_coordinatorAttached = false;",
    "finally",
    "window.DetachCoordinator();",
):
    require(token in text, "close-cleanup contract missing: " + token)

constructor = text.find("internal UpdateCenterWindow()")
subscribe = text.find("UpdateCoordinator.Instance.StateChanged += OnStateChanged;", constructor)
attach = text.find("_coordinatorAttached = true;", subscribe)
closed = text.find("Closed += (_, __) => DetachCoordinator();", attach)
require(
    min(constructor, subscribe, attach, closed) >= 0 and constructor < subscribe < attach < closed,
    "constructor must subscribe once, record ownership, then delegate Closed cleanup to DetachCoordinator",
)

detach = text.find("internal void DetachCoordinator()")
detach_end = text.find("private async System.Threading.Tasks.Task ScheduleUpdateAsync()", detach)
require(detach >= 0 and detach_end > detach, "cannot isolate DetachCoordinator")
detach_body = text[detach:detach_end]
detach_guard = detach_body.find("if (!_coordinatorAttached) return;")
detach_remove = detach_body.find("UpdateCoordinator.Instance.StateChanged -= OnStateChanged;", detach_guard)
detach_clear = detach_body.find("_coordinatorAttached = false;", detach_remove)
require(
    min(detach_guard, detach_remove, detach_clear) >= 0 and detach_guard < detach_remove < detach_clear,
    "DetachCoordinator must be idempotent and clear event ownership after removing the singleton handler",
)

host = text.find("internal static class UpdateCenterWindowHost")
close = text.find("internal static void Close()", host)
require(host >= 0 and close > host, "cannot isolate UpdateCenterWindowHost.Close")
close_body = text[close:]
try_pos = close_body.find("try")
window_close = close_body.find("window.Close();", try_pos)
catch_pos = close_body.find("catch", window_close)
finally_pos = close_body.find("finally", catch_pos)
detach_pos = close_body.find("window.DetachCoordinator();", finally_pos)
clear_pos = close_body.find("if (ReferenceEquals(_window, window)) _window = null;", detach_pos)
require(
    min(try_pos, window_close, catch_pos, finally_pos, detach_pos, clear_pos) >= 0
    and try_pos < window_close < catch_pos < finally_pos < detach_pos < clear_pos,
    "host Close must detach and clear ownership from finally after either successful or failed Window.Close",
)

require(
    "Closed += (_, __) => UpdateCoordinator.Instance.StateChanged -= OnStateChanged;" not in text,
    "Closed-only coordinator unsubscription returned",
)
require(
    close_body.count("window.Close();") == 1,
    "host close must keep exactly one normal WPF Window.Close request",
)

print(
    "Update Center close cleanup preflight PASS: coordinator subscription cleanup is explicit, idempotent and guaranteed from the host close finally path."
)
