#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ENGINE_REL = "src/QS3D.BricsCAD.V25/QuantityEngine2Commands.cs"
WINDOW_REL = "src/QS3D.BricsCAD.V25/UI/QuantityCalculationResultWindow.cs"
COMMANDS_REL = "src/QS3D.BricsCAD.V25/Commands.cs"


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required contract: {needle}")


def forbid(text, needle, rel):
    if needle in text:
        raise SystemExit(f"FAIL: {rel} contains forbidden contract: {needle}")


def require_order(text, needles, rel):
    cursor = -1
    for needle in needles:
        pos = text.find(needle, cursor + 1)
        if pos < 0:
            raise SystemExit(f"FAIL: {rel} missing ordered contract: {needle}")
        if pos <= cursor:
            raise SystemExit(f"FAIL: {rel} has wrong ordering near: {needle}")
        cursor = pos


def main():
    engine = read(ENGINE_REL)
    window = read(WINDOW_REL)
    commands = read(COMMANDS_REL)

    require_order(
        engine,
        (
            "QuantityEngine2Summary.Build(rows, regenerated)",
            "if (summary.ElementCount == 0)",
            "return;",
            "var openQuantityReview = QuantityCalculationResultWindow.ShowSuccess(summary);",
            "if (openQuantityReview)",
            "new Commands().ShowQuantitySummary();",
        ),
        ENGINE_REL,
    )

    success_start = engine.find("var openQuantityReview = QuantityCalculationResultWindow.ShowSuccess(summary);")
    catch_start = engine.find("catch (Exception ex)", success_start)
    if success_start < 0 or catch_start < 0:
        raise SystemExit("FAIL: could not isolate Engine2 success handoff")
    handoff = engine[success_start:catch_start]
    for forbidden in (
        "SendStringToExecute",
        "SemanticCaptureService",
        "RecognitionApplyBatchService",
        "ProjectContextCoordinator.GetOrCreate",
        "Application.ShowModelessWindow",
    ):
        forbid(handoff, forbidden, ENGINE_REL + "::success-handoff")

    for needle in (
        "public static bool ShowSuccess(QuantityEngine2Summary summary)",
        "offerQuantity: true",
        "ok.Content = \"Xem khối lượng\"",
        "_openQuantityRequested = true;",
        "return window._openQuantityRequested;",
        "public static bool ShowNoElements(string message)",
        "offerModeling: true",
        "public static void ShowError(string message)",
    ):
        require(window, needle, WINDOW_REL)

    require_order(
        window,
        (
            "public static bool ShowSuccess(QuantityEngine2Summary summary)",
            "window.ShowDialog();",
            "return window._openQuantityRequested;",
            "public static bool ShowNoElements(string message)",
            "return window._openModelRequested;",
            "public static void ShowError(string message)",
        ),
        WINDOW_REL,
    )

    for needle in (
        '[CommandMethod("QS3DBQ", CommandFlags.UsePickSet)]',
        "public void ShowQuantitySummary()",
        "ProjectQuantityReportBuilder.Group(previewProject)",
        "new QuantitySummaryWindow(doc, rows, locate, recalculate)",
        "Application.ShowModelessWindow",
    ):
        require(commands, needle, COMMANDS_REL)

    print("PASS: Engine2 success closes its modal result first, then hands off to the existing QS3DBQ detailed-review workflow without command-string dispatch or a second quantity engine.")


if __name__ == "__main__":
    main()
