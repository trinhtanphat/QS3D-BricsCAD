#!/usr/bin/env python3
"""Fail closed when V25 Direct Draw P1 publishes raw exception details or lies after commit."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs"


def fail(message: str) -> None:
    print(f"ERROR: Direct Draw P1 error-redaction preflight failed: {message}", file=sys.stderr)
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


def assert_before(text: str, first: str, second: str, context: str) -> None:
    first_at = text.find(first)
    second_at = text.find(second)
    if first_at < 0 or second_at < 0 or first_at >= second_at:
        fail(f"{context}: expected {first!r} before {second!r}")


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    if ".Message" in text:
        fail("DirectDrawP1Commands still references Exception.Message; command/modeless reporting must be redacted")

    guard = body(text, "private static void Guard(Document document, string operation, Action action)")
    if re.search(r"catch\s*\(\s*Exception\s+\w+\s*\)", guard):
        fail("Guard still captures an exception object for user-visible reporting")
    if "ReportOperationFailure(document, operation);" not in guard:
        fail("Guard does not delegate to the fail-safe stable operation failure reporter")
    if "PaletteCoordinator.SetStatus(" in guard or "document.Editor.WriteMessage(" in guard:
        fail("Guard performs unisolated presentation writes instead of delegating to the fail-safe reporter")

    reporter = body(text, "private static void ReportOperationFailure(Document document, string operation)")
    for token in ("document.Editor.WriteMessage(", "PaletteCoordinator.SetStatus(", "không thể hoàn tất thao tác", "Vui lòng thử lại"):
        if token not in reporter:
            fail(f"operation failure reporter missing {token!r}")
    if reporter.count("try") < 2 or reporter.count("catch") < 2:
        fail("operation failure reporter must exception-isolate Editor and Palette publication independently")

    finalize = body(text, "private static void FinalizeUi(Document document, ProjectElement element, ObjectId sourceId, string generatedHandle)")
    if re.search(r"catch\s*\(\s*Exception\s+\w+\s*\)", finalize):
        fail("FinalizeUi still captures an exception object after native commit")
    if "ReportPostCommitUiWarning(document);" not in finalize:
        fail("FinalizeUi does not route presentation-only failure through the stable post-commit warning")

    warning = body(text, "private static void ReportPostCommitUiWarning(Document document)")
    for token in ("đã commit", "đồng bộ giao diện chưa hoàn tất", "refresh giao diện", "document.Editor.WriteMessage(", "PaletteCoordinator.SetStatus("):
        if token not in warning:
            fail(f"post-commit warning missing {token!r}")
    if warning.count("try") < 2 or warning.count("catch") < 2:
        fail("post-commit warning must exception-isolate Editor and Palette publication independently")

    execute = body(text, "private static void Execute(")
    assert_before(execute, "catch (Exception operationError)", "FinalizeUi(document, createdElement!, sourceId, generatedHandle);", "post-commit truth")
    after_catch = execute[execute.find("FinalizeUi(document, createdElement!, sourceId, generatedHandle);"):]
    if "throw" in after_catch:
        fail("presentation-only finalization must not rethrow after committed authoring")

    print("OK: Direct Draw P1 failures are redacted, reporting is fail-safe, and post-commit UI failure preserves commit truth.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
