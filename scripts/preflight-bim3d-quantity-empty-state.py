#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMAND_REL = "src/QS3D.BricsCAD.V25/QuantityEngine2Commands.cs"
WINDOW_REL = "src/QS3D.BricsCAD.V25/UI/QuantityCalculationResultWindow.cs"


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
    command = read(COMMAND_REL)
    window = read(WINDOW_REL)

    require_order(
        command,
        (
            "var summary = QuantityEngine2Summary.Build(rows, regenerated);",
            "if (summary.ElementCount == 0)",
            "QuantityCalculationResultWindow.ShowNoElements(noElementsMessage)",
            "PaletteCoordinator.ShowBimWorkspace();",
            "PaletteCoordinator.SetStatus(",
            "return;",
            "QuantityCalculationResultWindow.ShowSuccess(summary);",
        ),
        COMMAND_REL,
    )
    require(command, "Hãy Tạo mới/Capture cấu kiện QS3D rồi chạy lại Engine2.", COMMAND_REL)
    forbid(command, "throw new InvalidOperationException(\n                        \"Chưa có cấu kiện hợp lệ để tính khối lượng.", COMMAND_REL)

    empty_start = command.find("if (summary.ElementCount == 0)")
    empty_end = command.find("try\n                {\n                    PaletteCoordinator.RefreshProject();", empty_start)
    if empty_start < 0 or empty_end < 0:
        raise SystemExit("FAIL: could not isolate Engine2 zero-element orchestration block")
    empty_block = command[empty_start:empty_end]
    for forbidden in (
        "QS3DRECOGNIZE",
        "RecognitionApplyBatchService",
        "SemanticCaptureService",
        "SendStringToExecute",
        "ProjectContextCoordinator.GetOrCreate",
    ):
        forbid(empty_block, forbidden, COMMAND_REL + "::zero-element")

    for needle in (
        "public static bool ShowNoElements(string message)",
        "Chưa có cấu kiện QS3D để tính khối lượng.",
        "Về Mô hình",
        "offerModeling: true",
        "AttentionBrush",
        "_openModelRequested = true;",
        "public static void ShowError(string message)",
    ):
        require(window, needle, WINDOW_REL)

    require_order(
        window,
        (
            "public static bool ShowNoElements(string message)",
            "offerModeling: true",
            "window.ShowDialog();",
            "return window._openModelRequested;",
            "public static void ShowError(string message)",
        ),
        WINDOW_REL,
    )

    print("PASS: Engine2 treats a valid zero-semantic-element project as an actionable modeling/capture empty state, preserves real error handling, and does not guess or auto-capture arbitrary CAD geometry.")


if __name__ == "__main__":
    main()
