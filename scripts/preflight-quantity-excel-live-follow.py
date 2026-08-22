#!/usr/bin/env python3
from pathlib import Path

# Lane issue-3506: guard only the active-Excel P1 bridge layered on the landed ED2 traceback.
ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml"
LIVE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.ExcelLiveFollow.cs"
ACTIVE = ROOT / "src/QS3D.BricsCAD.V25/Services/ExcelActiveSelectionService.cs"
RESOLVER = ROOT / "src/QS3D.BricsCAD.V25/Services/ExcelLocateResolutionService.cs"
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


def require_order(text, first, second, label, failures):
    a = text.find(first)
    b = text.find(second)
    if a < 0 or b < 0 or a >= b:
        failures.append(label + ": expected " + repr(first) + " before " + repr(second))


def main():
    xaml = read(XAML)
    live = read(LIVE)
    active = read(ACTIVE)
    resolver = read(RESOLVER)
    v26 = read(V26)
    failures = []

    require(xaml, 'Content="Dòng Excel đang chọn" Click="OnExcelActiveRowClick"', "active Excel row action", failures)
    require(xaml, 'x:Name="ExcelFollowCheck" Content="Bám Excel"', "live-follow opt-in", failures)
    require(xaml, 'Checked="OnExcelFollowChecked" Unchecked="OnExcelFollowUnchecked"', "live-follow lifecycle events", failures)

    require(active, 'CLSIDFromProgID(ExcelProgId', "Excel CLSID lookup without starting Excel", failures)
    require(active, 'GetActiveObject(ref excelClassId', "running-object-table lookup", failures)
    require(active, 'GetProperty(application, "ActiveWorkbook")', "active workbook late binding", failures)
    require(active, 'GetProperty(application, "ActiveSheet")', "active worksheet late binding", failures)
    require(active, 'GetProperty(application, "ActiveCell")', "active cell late binding", failures)
    require(active, 'Path.GetExtension(fullPath), ".xlsx"', "bounded xlsx-only automatic path", failures)
    require(active, "Marshal.ReleaseComObject(value)", "bounded COM cleanup", failures)
    forbid(active, "new Excel", "must not launch Excel", failures)
    forbid(active, "Microsoft.Office.Interop.Excel", "no Excel interop assembly dependency", failures)
    forbid(active, "Bricscad.", "active Excel reader remains host-object free", failures)
    forbid(active, "ObjectId", "active Excel reader retains no native CAD ids", failures)
    forbid(active, "Document", "active Excel reader retains no native Document", failures)

    require(live, "DispatcherTimer", "bounded UI-thread follow timer", failures)
    require(live, "PaletteCoordinator.IsQuantityInsightVisible", "stop when Quantity Insight is hidden", failures)
    require(live, "Unloaded += OnExcelFollowPanelUnloaded", "stop on panel unload", failures)
    require(live, 'snapshot.WorksheetName, "CHI_TIET"', "active sheet must be CHI_TIET", failures)
    require(live, "_lastExcelFollowObservedIdentity", "same-row follow de-duplication", failures)
    require(live, "XlsxHandleReader.ReadHandleLookup(snapshot.WorkbookPath, snapshot.RowNumber)", "reuse bounded canonical XLSX reader", failures)
    require(live, "lookup.IsModernSchema", "legacy rows excluded from automatic follow", failures)
    require(live, "lookup.IsEd2Detail", "automatic path limited to ED2 detail", failures)
    require(live, "ExcelLocateResolutionService.ResolveModern", "reuse fail-closed semantic/provenance resolver", failures)
    require(live, "project.ChangeVersion", "project revision captured before native selection", failures)
    require(live, "currentProject.ChangeVersion != projectVersion", "project revision revalidated before native selection", failures)
    require(live, "document!.Editor.SetImpliedSelection(resolution.ObjectIds.ToArray());", "atomic PICKFIRST replacement", failures)
    require(live, "ViewportCommands.TryZoomSelection(document)", "direct document-bound zoom", failures)
    require(live, "Cad.EntitySnapshotReader.ReadImpliedSelection(document)", "quantity review selection synchronization", failures)
    require(live, "SetInspectionReadOnly(snapshots, currentProject)", "quantity review highlight synchronization", failures)
    require_order(
        live,
        "ExcelLocateResolutionService.ResolveModern",
        "document!.Editor.SetImpliedSelection",
        "all provenance/live resolution before PICKFIRST",
        failures)
    forbid(live, "QS3DEXCELLOCATE ", "P1 must not queue the manual row-prompt command", failures)
    forbid(live, "Assembly.Load", "no sibling/proprietary plugin loading", failures)
    forbid(live, "BLT3D", "legacy BLT rows never enter automatic follow", failures)

    require(resolver, "!lookup.IsModernSchema || !lookup.IsEd2Detail", "canonical modern ED2 resolver boundary", failures)
    require(resolver, "Excel Element ID to CAD Handle provenance does not match", "canonical provenance equality check", failures)
    require(resolver, "resolved.Count != projectHandles.Count", "canonical complete live Handle resolution", failures)

    require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared C# parity", failures)
    require(v26, '<Page Include="..\\QS3D.BricsCAD.V25\\UI\\**\\*.xaml">', "V26 shared XAML parity", failures)

    if failures:
        print("QS3D Quantity Excel live-follow preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: active Excel CHI_TIET row + Bám Excel reuse the canonical ED2 reader/resolver, fail closed before PICKFIRST, clean COM/timer state, and share V25/V26 source.")
    print("NOTE: Excel+BricsCAD interactive behavior remains LOCAL_ONLY under #72; this gate is source/CI evidence only.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
