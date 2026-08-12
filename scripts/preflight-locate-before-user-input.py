#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"


def method_slice(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        raise AssertionError("Missing method: " + signature)
    end = text.find(next_signature, start + len(signature))
    if end < 0:
        raise AssertionError("Missing following method boundary: " + next_signature)
    return text[start:end]


def require_order(text, label, *tokens):
    cursor = -1
    for token in tokens:
        pos = text.find(token, cursor + 1)
        if pos < 0:
            raise AssertionError(label + " missing token: " + token)
        cursor = pos


def main():
    text = SOURCE.read_text(encoding="utf-8")
    locate = method_slice(text, "public void Locate()", "public void LocateFromExcel()")
    excel = method_slice(text, "public void LocateFromExcel()", "private static IReadOnlyList<string> ResolveEd2Selection")

    require_order(
        locate,
        "QS3DLOCATE preflight",
        "Guard(doc, \"QS3DLOCATE\"",
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
        "new PromptStringOptions",
        "doc.Editor.GetString(options)",
        "project.FindElement(result.StringResult)")

    require_order(
        excel,
        "QS3DEXCELLOCATE preflight",
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
        "new OpenFileDialog",
        "dialog.ShowDialog()",
        "new PromptIntegerOptions",
        "XlsxHandleReader.ReadHandleLookup",
        "ExcelLocateResolutionService.ResolveModern(doc, project, lookup)")

    if "ProjectContextCoordinator.GetOrCreate" in locate or "ProjectContextCoordinator.GetOrCreate" in excel:
        raise AssertionError("Locate commands must remain read-only and non-creating.")

    print("PASS: generic and Excel Locate require an existing project before user input/dialogs.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
