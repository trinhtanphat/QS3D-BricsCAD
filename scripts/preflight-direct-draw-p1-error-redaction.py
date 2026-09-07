#!/usr/bin/env python3
"""Fail closed when V25 Direct Draw authoring publishes raw exception details or lies after commit."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCES = (
    ROOT / "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs",
    ROOT / "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs",
    ROOT / "src/QS3D.BricsCAD.V25/DirectDrawWindowCommands.cs",
    ROOT / "src/QS3D.BricsCAD.V25/DirectDrawSlabOpeningCommands.cs",
)
REPORTER = ROOT / "src/QS3D.BricsCAD.V25/Services/DirectDrawUiFailureReporter.cs"


def fail(message: str) -> None:
    print(f"ERROR: Direct Draw authoring error-redaction preflight failed: {message}", file=sys.stderr)
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
    for source in SOURCES:
        text = source.read_text(encoding="utf-8")
        name = source.name
        if ".Message" in text:
            fail(f"{name} still references Exception.Message; command/modeless reporting must be redacted")

        guard = body(text, "private static void Guard(Document document, string operation, Action action)")
        if re.search(r"catch\s*\(\s*Exception\s+\w+\s*\)", guard):
            fail(f"{name} Guard still captures an exception object for user-visible reporting")
        if "DirectDrawUiFailureReporter.ReportOperationFailure(document, operation);" not in guard:
            fail(f"{name} Guard does not delegate to the fail-safe stable operation failure reporter")
        if "PaletteCoordinator.SetStatus(" in guard or "document.Editor.WriteMessage(" in guard:
            fail(f"{name} Guard performs presentation writes instead of delegating to the fail-safe reporter")

        if "DirectDrawUiFailureReporter.ReportPostCommitWarning(document);" not in text:
            fail(f"{name} FinalizeUi does not route presentation-only failure through the stable post-commit warning")
        if re.search(r"catch\s*\(\s*Exception\s+\w+\s*\)\s*\{\s*DirectDrawUiFailureReporter\.ReportPostCommitWarning", text, re.S):
            fail(f"{name} post-commit reporting unnecessarily captures an exception object")

    reporter = REPORTER.read_text(encoding="utf-8")
    if ".Message" in reporter:
        fail("shared reporter must never inspect or publish Exception.Message")
    for token in (
        "không thể hoàn tất thao tác",
        "Vui lòng thử lại",
        "đã commit",
        "đồng bộ giao diện chưa hoàn tất",
        "refresh giao diện",
        "document.Editor.WriteMessage(",
        "PaletteCoordinator.SetStatus(",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
    ):
        if token not in reporter:
            fail(f"shared reporter missing {token!r}")

    operation_reporter = body(reporter, "internal static void ReportOperationFailure(Document document, string operation)")
    post_commit_reporter = body(reporter, "internal static void ReportPostCommitWarning(Document document)")
    for block, context in ((operation_reporter, "operation failure"), (post_commit_reporter, "post-commit warning")):
        if "TryWriteEditor(document," not in block or "TrySetPaletteForCurrentDocument(document," not in block:
            fail(f"{context} does not isolate Editor and Palette publication through dedicated safe helpers")

    palette_helper = body(reporter, "private static void TrySetPaletteForCurrentDocument(Document document, string message)")
    affinity = "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)"
    status = "PaletteCoordinator.SetStatus(message)"
    if affinity not in palette_helper or status not in palette_helper or palette_helper.find(affinity) >= palette_helper.find(status):
        fail("process-wide Palette publication is not exact-source-document fenced before SetStatus")
    if "try" not in palette_helper or "catch" not in palette_helper:
        fail("Palette publication must be exception-isolated")

    editor_helper = body(reporter, "private static void TryWriteEditor(Document document, string message)")
    if "try" not in editor_helper or "catch" not in editor_helper:
        fail("Editor publication must be exception-isolated")

    for forbidden in (
        "LockDocument(",
        "StartTransaction(",
        "StartOpenCloseTransaction(",
        "SetImpliedSelection(",
        "Dispatcher",
        "SendStringToExecute(",
        "ProjectContextCoordinator.GetOrCreate",
        "+=",
    ):
        if forbidden in reporter:
            fail(f"shared presentation reporter must not own mutation/subscription/deferred work: found {forbidden!r}")

    print("OK: Direct Draw authoring failures are redacted, fail-safe, source-document fenced, and post-commit truthful across P1/opening/window/slabOpen.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
