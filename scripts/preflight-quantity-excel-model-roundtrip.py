#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml"
BRIDGE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.ExcelRoundTrip.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
READER = ROOT / "src/QS3D.Core/Export/XlsxHandleReader.cs"
EXPORTER = ROOT / "src/QS3D.Core/Export/XlsxQuantityExporter.cs"
V26 = ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"


def read(path):
    if not path.is_file():
        raise SystemExit("ERROR: missing required source file: " + str(path.relative_to(ROOT)))
    return path.read_text(encoding="utf-8")


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(label + ": missing " + repr(needle))


def forbid(text, needle, label, failures):
    if needle in text:
        failures.append(label + ": forbidden " + repr(needle))


def main():
    xaml = read(XAML)
    bridge = read(BRIDGE)
    commands = read(COMMANDS)
    reader = read(READER)
    exporter = read(EXPORTER)
    v26 = read(V26)
    failures = []

    require(xaml, 'Content="Xuất Excel" Click="OnExcelExportClick"', "Quantity Insight Excel export button", failures)
    require(xaml, 'Content="Truy ngược Excel" Click="OnExcelTracebackClick"', "Quantity Insight Excel traceback button", failures)

    require(bridge, "ProjectContextCoordinator.TryGetReadOnly", "existing-project read-only boundary", failures)
    require(bridge, "SameProjectIdentity(project)", "DWG/project refresh identity guard", failures)
    require(bridge, "SelectedScopeItems()", "tree scope projection", failures)
    require(bridge, "BuildPreviewRows(project, out _)", "canonical detached quantity preview", failures)
    require(bridge, "SameRow(displayedRow, matches[0])", "stale quantity/provenance rejection", failures)
    require(bridge, "SourceHandleResolver.Resolve(project, elementIds)", "canonical semantic-to-CAD provenance", failures)
    require(bridge, "Cad.CadHandleService.Resolve(document, handles)", "all-live Handle pre-resolution", failures)
    require(bridge, "if (resolved.Count != handles.Length)", "partial Handle refusal", failures)
    require(bridge, "document.Editor.SetImpliedSelection(resolved.ToArray());", "atomic PICKFIRST replacement after full resolution", failures)
    require(bridge, '"QS3DED2 "', "reuse existing ED2 exporter", failures)
    require(bridge, '"QS3DEXCELLOCATE "', "reuse existing Excel locate command", failures)
    forbid(bridge, "Cad.CadHandleService.Select(document, handles)", "partial selection before complete validation", failures)
    forbid(bridge, "Assembly.Load", "no sibling-plugin runtime loading", failures)
    forbid(bridge, "BLT3D", "no proprietary BLT3D dependency in new bridge", failures)

    require(commands, "XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);", "canonical ED2 quantity exporter", failures)
    require(commands, "XlsxHandleReader.ReadHandleLookup(dialog.FileName, row.Value)", "bounded XLSX identity reader", failures)
    require(commands, "ExcelLocateResolutionService.ResolveModern(doc, project, lookup)", "modern workbook provenance validator", failures)
    require(commands, "doc.Editor.SetImpliedSelection(resolved.ToArray());", "locate changes PICKFIRST only after resolution", failures)

    require(exporter, '"CHI_TIET"', "ED2 detail worksheet", failures)
    require(exporter, '"TONG_HOP"', "ED2 summary worksheet", failures)
    require(exporter, '"QS3D Drawing Fingerprint"', "drawing fingerprint provenance export", failures)
    require(exporter, '"QS3D Element ID"', "semantic ElementId provenance export", failures)
    require(exporter, '"CAD Handle (hex)"', "CAD Handle provenance export", failures)

    require(reader, "ResolveWorksheet(archive)", "worksheet resolution", failures)
    require(reader, "detailSheets.Count == 1 ? detailSheets[0] : sheets[0]", "prefer CHI_TIET for trace-back", failures)
    require(reader, "ED2 CHI_TIET row must contain exactly one Element ID.", "single-element detail identity", failures)
    require(reader, "QS3D Excel identity cells must contain literal values, not formulas.", "identity formula rejection", failures)
    require(reader, "QS3D Excel schema must contain exactly one Element ID, CAD Handle, and drawing fingerprint column.", "unambiguous modern identity schema", failures)

    require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared C# source parity", failures)
    require(v26, '<Page Include="..\\QS3D.BricsCAD.V25\\UI\\**\\*.xaml">', "V26 shared XAML parity", failures)

    if failures:
        print("QS3D Quantity Excel↔Model round-trip preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: Quantity Insight Excel export/traceback reuses canonical ED2 + Excel Locate with fail-closed provenance and V25/V26 shared-source parity.")
    print("NOTE: this is a static/source contract guard; licensed BricsCAD runtime qualification remains #72 LOCAL_ONLY.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
