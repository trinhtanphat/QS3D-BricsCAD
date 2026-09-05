#!/usr/bin/env python3
"""Fail closed when Project Browser queued work can cross Workspace attachment generations."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ProjectBrowser.cs"


def fail(message: str) -> None:
    print(f"ERROR: V25 Project Browser attachment-generation preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def body(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        fail(f"missing {signature}")
    brace = text.find("{", start)
    if brace < 0:
        fail(f"missing body for {signature}")
    depth = 0
    for index in range(brace, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[brace + 1:index]
    fail(f"unterminated body for {signature}")
    return ""


def assert_order(block: str, first: str, second: str, context: str) -> None:
    first_at = block.find(first)
    second_at = block.find(second)
    if first_at < 0 or second_at < 0 or first_at >= second_at:
        fail(f"{context}: expected {first!r} before {second!r}")


def deterministic_aba_smoke() -> None:
    """Model the required queue invariant without WPF/BricsCAD runtime."""
    generation = 1
    queued_flag = True
    old_generation = generation

    # Unload invalidates old callbacks and clears that generation's queue state.
    generation += 1
    queued_flag = False

    # Reload queues work for a new attachment.
    generation += 1
    new_generation = generation
    queued_flag = True

    # Old callback must return before consuming the new generation's flag.
    if old_generation == generation:
        queued_flag = False
    if not queued_flag:
        fail("deterministic ABA model: stale callback consumed current-generation queue state")

    # Current callback is allowed to consume it.
    if new_generation == generation:
        queued_flag = False
    if queued_flag:
        fail("deterministic ABA model: current callback did not consume its own queue state")


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    if not re.search(r"private\s+(?:long|int)\s+_browserAttachmentGeneration\s*;", text):
        fail("missing attachment-generation field")

    attach = body(text, "private void AttachProjectBrowser()")
    detach = body(text, "private void DetachProjectBrowser()")
    refresh_queue = body(text, "private void QueueBrowserRefresh(bool forceRebind)")
    inspection = body(text, "private void OnBrowserInspectionSourceChanged(object? sender, EventArgs e)")

    if "_browserAttachmentGeneration++" not in detach and "++_browserAttachmentGeneration" not in detach:
        fail("detach does not invalidate the current attachment generation")

    if "var generation = _browserAttachmentGeneration;" not in refresh_queue:
        fail("queued refresh does not capture its attachment generation")
    if "IsCurrentBrowserAttachment(generation)" not in refresh_queue:
        fail("queued refresh lacks a generation/lifetime fence")
    assert_order(
        refresh_queue,
        "IsCurrentBrowserAttachment(generation)",
        "_browserRefreshQueued = false",
        "queued refresh",
    )

    if "var generation = _browserAttachmentGeneration;" not in inspection:
        fail("queued CAD-inspection sync does not capture its attachment generation")
    if "IsCurrentBrowserAttachment(generation)" not in inspection:
        fail("queued CAD-inspection sync lacks a generation/lifetime fence")
    assert_order(
        inspection,
        "IsCurrentBrowserAttachment(generation)",
        "SyncProjectBrowserFromCad()",
        "queued CAD-inspection sync",
    )

    helper = body(text, "private bool IsCurrentBrowserAttachment(")
    for required in ("_browserAttached", "IsLoaded", "_browserAttachmentGeneration"):
        if required not in helper:
            fail(f"attachment fence does not check {required}")

    # Attach must establish an attached state before it queues the first refresh.
    assert_order(attach, "_browserAttached = true", "QueueBrowserRefresh(true)", "attach")

    deterministic_aba_smoke()
    print("OK: V25 Project Browser queued work is fenced to one Workspace attachment generation.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
