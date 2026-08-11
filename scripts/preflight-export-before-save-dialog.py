#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"
BBS_CSV = ROOT / "src" / "QS3D.BricsCAD.V25" / "BbsCsvCommands.cs"


def method_slice(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        raise AssertionError("Missing method: " + signature)
    end = text.find(next_signature, start + len(signature))
    if end < 0:
        raise AssertionError("Missing following boundary: " + next_signature)
    return text[start:end]


def require_order(text, label, *tokens):
    cursor = -1
    for token in tokens:
        pos = text.find(token, cursor + 1)
        if pos < 0:
            raise AssertionError(label + " missing token: " + token)
        cursor = pos


def main():
    commands = COMMANDS.read_text(encoding="utf-8")
    ed2 = method_slice(commands, "public void ExportEd2Workflow()", "public void ExportBbs()")
    bbs = method_slice(commands, "public void ExportBbs()", "public void Regenerate()")
    csv = method_slice(BBS_CSV.read_text(encoding="utf-8"), "public void ExportCsv()", "private static void FinalizeUi(")

    require_order(
        ed2,
        "ED2 preflight",
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
        "project.Elements.Count == 0",
        "DrawingUnitWorkflow.EnsureResolved(doc, \"QS3DED2\")",
        "ResolveEd2Selection(project",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "details.Count == 0",
        "EnsureEd2HandlesAreLive(doc, details)",
        "new SaveFileDialog",
        "dialog.ShowDialog()",
        "XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary)")

    require_order(
        bbs,
        "BBS XLSX preflight",
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "ProjectRebarScheduleBuilder.Build(previewProject)",
        "rows.Count == 0",
        "QuantityReportMath.Add",
        "new SaveFileDialog",
        "dialog.ShowDialog()",
        "XlsxRebarScheduleExporter.Export(dialog.FileName, rows)")

    require_order(
        csv,
        "BBS CSV preflight",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "ProjectRebarScheduleBuilder.Build(snapshot)",
        "rows.Count == 0",
        "QuantityReportMath.Add",
        "new SaveFileDialog",
        "dialog.ShowDialog()",
        "RebarCsvExporter.Export(dialog.FileName, rows)")

    print("PASS: ED2/BBS XLSX/CSV validate exportability before Save dialogs and write only after confirmation.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("ERROR:", exc)
        raise SystemExit(1)
