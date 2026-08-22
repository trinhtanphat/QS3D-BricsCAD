#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "commands": ROOT / "src/QS3D.BricsCAD.V25/Commands.cs",
    "builder": ROOT / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs",
    "row": ROOT / "src/QS3D.Core/Reporting/QuantityReportRow.cs",
    "exporter": ROOT / "src/QS3D.Core/Export/XlsxQuantityExporter.cs",
    "reader": ROOT / "src/QS3D.Core/Export/XlsxHandleReader.cs",
    "window": ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml",
    "window_code": ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs",
    "hub": ROOT / "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml",
    "ribbon": ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
    "quantity_smoke": ROOT / "tests/QS3D.Core.SmokeTests/ProjectQuantitySmoke.cs",
    "excel_smoke": ROOT / "tests/QS3D.Core.SmokeTests/ReviewHardeningSmoke.cs",
}

texts = {}
for name, path in files.items():
    if not path.is_file():
        errors.append("missing ED2 round-trip file: " + str(path.relative_to(ROOT)))
    else:
        texts[name] = path.read_text(encoding="utf-8")

def require(name, tokens):
    text = texts.get(name, "")
    for token in tokens:
        if token not in text:
            errors.append(files[name].name + " missing ED2 token: " + token)

require("commands", (
    '[CommandMethod("QS3DED2", CommandFlags.UsePickSet)]',
    '"Selection Floor Zone All"',
    "ResolveEd2Selection(project, snapshots)",
    "ProjectQuantityReportBuilder.Detail(project, elementIds)",
    "ProjectQuantityReportBuilder.Group(project, elementIds)",
    "XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary)",
    '[CommandMethod("QS3DEXCELLOCATE", CommandFlags.Modal)]',
    "lookup.ElementIds",
    "!excelHandles.SequenceEqual(projectHandles, StringComparer.OrdinalIgnoreCase)",
    "if (!lookup.UsesLegacyDecimalHandles)",
    "var resolved = Cad.CadHandleService.Resolve(doc, handles)",
    "if (resolved.Count != handles.Count)",
    "doc.Editor.SetImpliedSelection(resolved.ToArray())",
))
if "ShowEd2Workflow() => ShowQuantitySummary()" in texts.get("commands", ""):
    errors.append("QS3DED2 is still an alias of QS3DBQ instead of a scoped ED2 export.")

require("builder", (
    "public static IReadOnlyList<QuantityReportRow> Detail(ProjectState project, IEnumerable<string> elementIds)",
    "ResolveSelection(project, elementIds)",
    "element.ZoneId + \"\\u001f\" + category",
    "row.ElementIds.Add(elementId)",
))
require("row", ("public string Zone { get; set; }",))
require("exporter", (
    "public static void ExportEd2",
    "ED2 CHI_TIET must contain exactly one semantic element per row.",
    "detailIds.Add(elementId)",
    "summaryIds.SetEquals(detailIds)",
    "summaryHandles.SetEquals(detailHandles)",
    'name=\\\"CHI_TIET\\\"',
    'name=\\\"TONG_HOP\\\"',
    '"xl/worksheets/sheet2.xml"',
))
require("reader", (
    "public IReadOnlyList<string> ElementIds { get; }",
    "public bool IsModernSchema { get; }",
    "public bool IsEd2Detail { get; }",
    "ResolveWorksheet(archive)",
    'string.Equals(name, "CHI_TIET", StringComparison.OrdinalIgnoreCase)',
    "targets.Count > 1",
    "result.ContainsKey(column)",
    "QS3D Excel row is missing its Element ID.",
    "QS3D Excel row is missing its CAD Handle provenance.",
    "QS3D Excel row is missing its drawing fingerprint.",
    "!isModernSchema && decimalHandles.Count > 0",
))
require("window", ("OnEd2ExportClick", "OnExcelLocateClick", "BQ • 1 sheet",))
require("window_code", ('SendStringToExecute("QS3DED2 "', 'SendStringToExecute("QS3DEXCELLOCATE "', '"Zone"',))
require("hub", ('Tag="QS3DED2"', 'Tag="QS3DEXCELLOCATE"'))
require("ribbon", ('new RibbonButtonSpec("ED2 • Excel ↔ CAD", "QS3DED2")', 'new RibbonButtonSpec("Excel → CAD", "QS3DEXCELLOCATE")'))
require("quantity_smoke", ("DetailRowsPreserveOneElementProvenance", "same Floor/Family across different Zones", "Unknown ED2 element id must fail closed",))
require("excel_smoke", ("CreateReorderedEd2Workbook", "reordered.IsEd2Detail", "qs3d-blank-handle.xlsx", 'workbook.Contains("CHI_TIET")', 'workbook.Contains("TONG_HOP")'))

print("QS3D ED2 Excel round-trip preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: ED2 scopes before aggregation, exports one-element CHI_TIET plus Zone-aware TONG_HOP, and Excel-to-CAD lookup fails closed on schema, provenance, fingerprint or live-Handle drift.")
