#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"


def require(text, token, label):
    if token not in text:
        raise AssertionError(label + " missing token: " + token)


def require_order(text, label, *tokens):
    cursor = -1
    for token in tokens:
        position = text.find(token, cursor + 1)
        if position < 0:
            raise AssertionError(label + " missing ordered token: " + token)
        if position <= cursor:
            raise AssertionError(label + " has invalid token ordering: " + token)
        cursor = position


def method_slice(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        raise AssertionError("Missing method: " + signature)
    end = text.find(next_signature, start + len(signature))
    if end < 0:
        raise AssertionError("Missing following method boundary: " + next_signature)
    return text[start:end]


def main():
    text = COMMANDS.read_text(encoding="utf-8")

    helper = method_slice(
        text,
        "private static void FinalizeCommittedUi(Document document, string operation, Action ui)",
        "private static void Capture(ElementCategory category, string label)")
    require(helper, "ui();", "post-commit UI helper")
    require(helper, "catch (System.Exception uiError)", "post-commit UI helper")
    require(helper, 'operation + " đã hoàn tất; cảnh báo UI: " + uiError.Message', "post-commit UI warning")
    if "throw" in helper:
        raise AssertionError("Post-commit UI helper must remain non-fatal after the business operation succeeds.")

    require_order(
        text,
        "QS3DREGEN",
        'var count = RegenerateProject(project);',
        'FinalizeCommittedUi(doc, "QS3DREGEN"')
    require_order(
        text,
        "QS3DSAVE",
        'var path = ProjectContextCoordinator.Save(doc);',
        'FinalizeCommittedUi(doc, "QS3DSAVE"')
    require_order(
        text,
        "QS3DRELOAD",
        'ProjectContextCoordinator.Reload(doc);',
        'FinalizeCommittedUi(doc, "QS3DRELOAD"')
    require_order(
        text,
        "Tường KT capture",
        'SemanticCaptureService.Capture(doc, ElementCategory.ArchitecturalWall);',
        'FinalizeCommittedUi(doc, "QS3D Tường KT"')
    require_order(
        text,
        "QS3DFINISH",
        'SemanticCaptureService.GenerateRoomFinishes(doc);',
        'FinalizeCommittedUi(doc, "QS3DFINISH"')
    require_order(
        text,
        "generic semantic capture",
        'SemanticCaptureService.Capture(doc, category);',
        'FinalizeCommittedUi(doc, "QS3D " + label')

    print("PASS: committed semantic/persistence commands isolate non-fatal Palette/editor finalization.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
