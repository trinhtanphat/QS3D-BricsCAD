#!/usr/bin/env python3
from pathlib import Path

# Lane issue-3683: guard the missing Model/CAD -> existing Excel detail-row direction.
ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CadToExcelCommands.cs"
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Services/ExcelModelRowActivationService.cs"
RESOLVER = ROOT / "src/QS3D.BricsCAD.V25/Services/ExcelLocateResolutionService.cs"
ROUNDTRIP = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.ExcelRoundTrip.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml"
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuantityReferenceRibbonAugmenter.cs"
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
    command = read(COMMAND)
    service = read(SERVICE)
    resolver = read(RESOLVER)
    roundtrip = read(ROUNDTRIP)
    xaml = read(XAML)
    ribbon = read(RIBBON)
    v26 = read(V26)
    failures = []

    require(command, '[CommandMethod("QS3DCADTOEXCEL", CommandFlags.UsePickSet)]', "CAD -> Excel command", failures)
    require(command, "SemanticReferenceHandles.MatchesSelection", "semantic selection binding", failures)
    require(command, "elements.Count != 1", "single semantic element boundary", failures)
    require(command, "SemanticReferenceHandles.GetSelectionAliases", "mixed selection refusal", failures)
    require(command, "ExcelModelRowActivationService.TryFindActiveWorkbookRow", "active workbook candidate discovery", failures)
    require(command, "QsCustomerWorkbookTraceReader.Read", "customer workbook hardened re-read", failures)
    require(command, "XlsxHandleReader.ReadHandleLookup", "ED2 hardened re-read", failures)
    require(command, "ExcelLocateResolutionService.ResolveCustomerTrace", "customer provenance/live validation", failures)
    require(command, "ExcelLocateResolutionService.ResolveModern", "ED2 provenance/live validation", failures)
    require(command, "project.ChangeVersion != reviewedVersion", "project drift refusal", failures)
    require(command, "ExcelModelRowActivationService.TryActivateValidatedRow", "post-validation Excel activation", failures)
    require_order(command, "QsCustomerWorkbookTraceReader.Read", "TryActivateValidatedRow", "customer disk validation before Excel activation", failures)
    require_order(command, "XlsxHandleReader.ReadHandleLookup", "TryActivateValidatedRow", "ED2 disk validation before Excel activation", failures)
    require_order(command, "ExcelLocateResolutionService.ResolveModern", "TryActivateValidatedRow", "live CAD/provenance validation before Excel activation", failures)
    forbid(command, "SetImpliedSelection", "CAD -> Excel must not replace CAD PICKFIRST", failures)

    require(service, "CLSIDFromProgID(ExcelProgId", "Excel CLSID lookup without launch", failures)
    require(service, "GetActiveObject(ref excelClassId", "running Excel ROT lookup", failures)
    require(service, 'GetProperty(application!, "ActiveWorkbook")', "active workbook only", failures)
    require(service, 'GetProperty(workbook, "Saved")', "saved workbook requirement", failures)
    require(service, 'Path.GetExtension(fullPath), ".xlsx"', "xlsx-only boundary", failures)
    require(service, 'TryGetWorksheet(worksheets, "CHI_TIET")', "CHI_TIET discovery", failures)
    require(service, 'TryGetWorksheet(worksheets, "TRACE_MODEL")', "customer TRACE_MODEL discovery", failures)
    require(service, 'new[] { "QS3D Element ID", "QS3D Drawing Fingerprint" }', "ED2 identity discovery", failures)
    require(service, 'new[] { "SHEET", "ROW", "QS3D Element ID", "QS3D Drawing Fingerprint" }', "customer identity discovery", failures)
    require(service, "MaxDiscoveryCells", "bounded COM discovery", failures)
    require(service, "Marshal.ReleaseComObject(value)", "COM cleanup", failures)
    require(service, "TryActivateValidatedRow", "separate navigation mutation", failures)
    require(service, 'InvokeMethod(detailSheet, "Activate")', "worksheet activation", failures)
    require(service, 'InvokeMethod(targetCell, "Select")', "exact row/cell activation", failures)
    forbid(service, "Activator.CreateInstance", "must never start Excel", failures)
    forbid(service, "Type.GetTypeFromProgID", "must never instantiate Excel by ProgID", failures)
    forbid(service, "Microsoft.Office.Interop.Excel", "no Office Interop dependency", failures)
    forbid(service, "Bricscad.", "Excel COM bridge remains host-object free", failures)
    forbid(service, "ObjectId", "Excel COM bridge retains no CAD ObjectId", failures)
    forbid(service, "Document", "Excel COM bridge retains no BricsCAD Document", failures)

    require(resolver, "Excel Element ID to CAD Handle provenance does not match", "canonical Handle parity", failures)
    require(resolver, "resolved.Count != projectHandles.Count", "complete live Handle validation", failures)

    require(roundtrip, "OnCadToExcelClick", "Quantity Insight CAD -> Excel handler", failures)
    require(roundtrip, '"QS3DCADTOEXCEL "', "Quantity Insight command dispatch", failures)
    require(xaml, 'Content="CAD → Excel" Click="OnCadToExcelClick"', "Quantity Insight action", failures)
    require(ribbon, '"QS3D_QTY_BLT_CAD_TO_EXCEL"', "quantity Ribbon action id", failures)
    require(ribbon, '"QS3DCADTOEXCEL"', "quantity Ribbon command", failures)

    require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared C# parity", failures)
    require(v26, '<Page Include="..\\QS3D.BricsCAD.V25\\UI\\**\\*.xaml">', "V26 shared XAML parity", failures)

    if failures:
        print("QS3D CAD -> Excel row preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: CAD -> Excel discovers only an already-running saved XLSX, reuses hardened ED2/customer provenance readers and live resolver before activating CHI_TIET, with bounded COM cleanup and V25/V26 parity.")
    print("NOTE: licensed Excel + BricsCAD interaction remains LOCAL_ONLY under #72; this gate is source/CI evidence only.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
