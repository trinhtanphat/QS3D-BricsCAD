#!/usr/bin/env python3
"""Fail closed when V25 document lifecycle publishes raw exception details or stale-document UI."""

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


def require(block: str, token: str, label: str) -> None:
    if token not in block:
        fail(f"missing {label}: {token!r}")


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

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

    for token in (
        "DWG save completed, but the QS3D sidecar could not be saved.",
        "The drawing was kept open because QS3D could not save its sidecar.",
        "Recovery copy was written successfully.",
        "Recovery copy also failed; internal details were hidden.",
        "QS3D document lifecycle reconcile failed. Internal details were hidden.",
        "QS3D project load failed. Internal details were hidden.",
    ):
        if token not in text:
            fail(f"missing stable lifecycle diagnostic {token!r}")

    close = body(text, "private static void OnBeginDocumentClose(")
    for token in ("e.Veto()", "ProjectContextCoordinator.Save(document)", "TryWriteRecovery(document, saveError)"):
        require(close, token, "close/save truth contract")

    save = body(text, "private static void OnDrawingSaveComplete(")
    for token in ("ProjectContextCoordinator.TrySavePending", "TryWriteRecovery(document, saveError)"):
        require(save, token, "post-DWG-save sidecar contract")

    recovery = body(text, "private static string TryWriteRecovery(")
    require(recovery, "ProjectContextCoordinator.SaveRecoveryCopy(document, saveError)", "recovery attempt")
    if '" Recovery copy: " + path' in recovery or "recoveryError.Message" in recovery:
        fail("recovery status still publishes recovery path or raw failure detail")

    ensure = body(text, "private static void EnsureProject(")
    for token in ("RememberStableProjectLoadFailure", "attemptedRevision", "ResetForUnavailableProject"):
        require(ensure, token, "project-load reconcile contract")
    if re.search(r"catch\s*\([^)]*Exception\s+\w+\s*\).*?\b\w+\.Message", ensure, re.S):
        fail("project-load reconcile still derives public status from captured exception text")
    if ensure.count("if (refreshUi)") < 3:
        fail("project-load failure paths must gate global palette publication on active refresh affinity")

    reconcile = body(text, "private static void ReconcileDocument(")
    for token in (
        "refreshActiveUi = refreshUi && IsActiveDocument(document)",
        "EnsureProject(document, refreshActiveUi)",
        "if (refreshActiveUi) SelectionSyncCoordinator.Refresh(document)",
    ):
        require(reconcile, token, "execution-time active-document reconcile fence")

    active = body(text, "private static bool IsActiveDocument(")
    require(active, "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)", "active-document identity check")

    report = body(text, "private static void Report(")
    require(report, "IsActiveDocument(document)", "global status active-document fence")
    require(report, "PaletteCoordinator.SetStatus(message)", "global lifecycle status publication")

    print("OK: V25 document lifecycle redacts failures and fences modeless UI to the active document while preserving save/close/reconcile truth.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
