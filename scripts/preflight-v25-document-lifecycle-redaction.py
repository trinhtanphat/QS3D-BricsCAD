#!/usr/bin/env python3
"""Fail closed when V25 document lifecycle publishes raw exception details."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs"


def fail(message: str) -> None:
    print(f"ERROR: V25 document lifecycle redaction preflight failed: {message}", file=sys.stderr)
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


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    # User-visible lifecycle reporting must never publish raw exception text.
    guarded = (
        "private static void ReportDocumentDestroyTeardownErrors(",
        "private static void OnDrawingSaveComplete(",
        "private static void OnBeginDocumentClose(",
        "private static string TryWriteRecovery(",
        "private static void ReportLifecycleError(",
        "private static void EnsureProject(",
    )
    for signature in guarded:
        block = body(text, signature)
        if ".Message" in block:
            fail(f"{signature} still publishes Exception.Message")

    # Keep phase-specific truth instead of replacing every failure with one generic message.
    for token in (
        "DWG save completed, but the QS3D sidecar could not be saved.",
        "The drawing was kept open because QS3D could not save its sidecar.",
        "Recovery copy also failed; internal details were hidden.",
        "QS3D document lifecycle reconcile failed. Internal details were hidden.",
        "QS3D project load failed. Internal details were hidden.",
    ):
        if token not in text:
            fail(f"missing stable lifecycle diagnostic {token!r}")

    # A close failure must remain fail-closed and must not lose the veto.
    close = body(text, "private static void OnBeginDocumentClose(")
    for token in ("e.Veto()", "ProjectContextCoordinator.Save(document)", "TryWriteRecovery(document, saveError)"):
        if token not in close:
            fail(f"close/save truth contract missing {token!r}")

    # A DWG SaveComplete callback must continue to describe sidecar failure as post-DWG-save work.
    save = body(text, "private static void OnDrawingSaveComplete(")
    for token in ("ProjectContextCoordinator.TrySavePending", "TryWriteRecovery(document, saveError)"):
        if token not in save:
            fail(f"post-DWG-save sidecar contract missing {token!r}")

    # Stable failed-load memoization must retain revision gating and must not cache raw exception text.
    ensure = body(text, "private static void EnsureProject(")
    for token in ("RememberStableProjectLoadFailure", "attemptedRevision", "ResetForUnavailableProject"):
        if token not in ensure:
            fail(f"project-load reconcile contract missing {token!r}")
    if re.search(r"catch\s*\([^)]*Exception\s+\w+\s*\).*?\b\w+\.Message", ensure, re.S):
        fail("project-load reconcile still derives public status from captured exception text")

    print("OK: V25 document lifecycle uses stable redacted diagnostics while preserving save/close/reconcile truth.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
