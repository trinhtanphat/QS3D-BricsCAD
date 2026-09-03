#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "commands": ROOT / "src/QS3D.BricsCAD.V25/Commands.cs",
    "cad_handles": ROOT / "src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs",
    "builder": ROOT / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs",
    "row": ROOT / "src/QS3D.Core/Reporting/QuantityReportRow.cs",
    "exporter": ROOT / "src/QS3D.Core/Export/XlsxQuantityExporter.cs",
    "reader": ROOT / "src/QS3D.Core/Export/XlsxHandleReader.cs",
    "locate_service": ROOT / "src/QS3D.BricsCAD.V25/Services/ExcelLocateResolutionService.cs",
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
    "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
    "ResolveEd2Selection(project, selectionSnapshots ?? Array.Empty<EntitySnapshot>())",
    "var previewProject = ProjectStateSnapshot.CreateDetachedCopy(project);",
    "ProjectQuantityReportBuilder.Detail(previewProject, elementIds)",
    "ProjectQuantityReportBuilder.Group(previewProject, elementIds)",
    "EnsureEd2HandlesAreLive(doc, details)",
    "ED2 export blocked:",
    "XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary)",
    '[CommandMethod("QS3DEXCELLOCATE", CommandFlags.Modal)]',
    "ExcelLocateResolutionService.ResolveModern(doc, project, lookup)",
    "if (!lookup.UsesLegacyDecimalHandles)",
    "resolved = Cad.CadHandleService.Resolve(doc, handles)",
    "if (resolved.Count != handles.Count)",
    "doc.Editor.SetImpliedSelection(resolved.ToArray())",
))
require("locate_service", (
    "lookup.IsModernSchema",
    "lookup.IsEd2Detail",
    'string.Equals(lookup.WorksheetName, "CHI_TIET"',
    "lookup.ElementIds.Count != 1",
    "project.FindElement(elementId) == null",
    "excelHandles.SequenceEqual(projectHandles, StringComparer.OrdinalIgnoreCase)",
    "CadHandleService.Resolve(document, projectHandles)",
    "resolved.Count != projectHandles.Count",
))
require("cad_handles", (
    "NormalizeHexHandle",
    'StartsWith("0x", StringComparison.OrdinalIgnoreCase)',
    'value.ToString("X", CultureInfo.InvariantCulture)',
))
if "ShowEd2Workflow() => ShowQuantitySummary()" in texts.get("commands", ""):
    errors.append("QS3DED2 is still an alias of QS3DBQ instead of a scoped ED2 export.")
if "ProjectContextCoordinator.GetOrCreate(doc)" in texts.get("commands", "")[texts.get("commands", "").find('[CommandMethod("QS3DED2"'):texts.get("commands", "").find('[CommandMethod("QS3DBBS"')]:
    errors.append("QS3DED2 read-only export must not create/cache replacement project state.")

require("builder", (
    "public static IReadOnlyList<QuantityReportRow> Detail(ProjectState project, IEnumerable<string> elementIds)",
    "ResolveSelection(project, elementIds)",
    '"ELEMENT\\u001f" + elementId',
    "CanonicalGroupKey(floorId, zoneId, category, familyId, material, DensityKey(densityKgM3))",
    "private static string CanonicalGroupKey(params string[] parts)",
    'value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value',
    'FirstInstanceProperty(element, "Name", "TenCauKien")',
    'Effective(element, family, "Material")',
    "EffectiveDensity(element, family)",
    'OptionalNonNegativeQuantity(element, "WeightKg", "MassKg")',
    '"NetConcreteM3"',
    '"NetVolumeM3"',
    '"GrossConcreteM3"',
    '"GrossVolumeM3"',
    '"VolumeM3"',
    '"MeasuredVolumeM3"',
    "DensityKey(densityKgM3)",
    "QuantityReportAggregateState",
    'aggregate.MassKg.Add(massKg, element.Id + "/MassKg")',
    'row.MassKg = aggregate.MassKg.Value("MassKg")',
    "QuantityReportMath.AddCount(row.Count, 1)",
    "row.ElementIds.Add(elementId)",
))
if 'element.ZoneId + "\\u001f" + category' in texts.get("builder", "") or 'material + "\\u001f" + DensityKey(densityKgM3)' in texts.get("builder", ""):
    errors.append("ProjectQuantityReportBuilder must not regress grouped ED2 identity to delimiter-concatenated keys")

require("row", (
    "public string Zone { get; set; }",
    "public string FamilyId { get; set; }",
    "public string ElementName { get; set; }",
    "public string Material { get; set; }",
    "public string Note { get; set; }",
    "public double? DensityKgM3 { get; set; }",
    "public double? MassKg { get; set; }",
    "public string FloorZoneText",
))
require("exporter", (
    "public static void ExportEd2",
    "ED2 CHI_TIET must contain exactly one semantic element per row.",
    "detailIds.Add(elementId)",
    "summaryIds.SetEquals(detailIds)",
    "summaryHandles.SetEquals(detailHandles)",
    'name=\\\"CHI_TIET\\\"',
    'name=\\\"TONG_HOP\\\"',
    '"xl/worksheets/sheet2.xml"',
    "BuildEd2Sheet(rows)",
    'value.ToString("R", CultureInfo.InvariantCulture)',
    'formatCode=\\\"#,##0.000\\\"',
    'AppendNumberCell(sb, CellRef(6, r), row.Count, IntegerStyle)',
    'AppendEvidenceNumberCell(sb, CellRef(7, r), row.GrossConcreteM3, row.HasGrossConcreteM3Evidence, Decimal3Style)',
    'private static void AppendEvidenceNumberCell(StringBuilder sb, string cellRef, double value, bool hasEvidence, int style = Decimal2Style)',
    'if (!hasEvidence) return;',
    'AppendNullableNumberCell(sb, CellRef(20, r), row.MassKg, Decimal2Style)',
    '"STT", "Tên cấu kiện", "Loại", "Vật liệu", "Family ID", "Tầng/Zone"',
    '"Khối lượng riêng (kg/m³)"',
    '"Khối lượng (kg)"',
    '"Ghi chú"',
    'var range = "A1:Y"',
    "AppendNullableNumberCell",
    "row.ElementName",
    "row.Material",
    "row.FamilyId",
    "row.FloorZoneText",
    "row.DensityKgM3",
    "row.MassKg",
    "row.Note",
    "row.ElementIdText",
    "row.SourceHandleText",
    "row.DrawingFingerprint",
    "Ed2ColumnWidthsXml",
    'fgColor rgb=\\\"FFFFC000\\\"',
    'wrapText=\\\"1\\\"',
    'ht=\\\"30\\\" customHeight=\\\"1\\\"',
    'if (row.Count > 1) sb.Append(" ht=\\"96\\" customHeight=\\"1\\"")',
))
require("reader", (
    "public IReadOnlyList<string> ElementIds { get; }",
    "public bool IsModernSchema { get; }",
    "public bool IsEd2Detail { get; }",
    "ResolveWorksheet(archive)",
    'string.Equals(name, "CHI_TIET", StringComparison.OrdinalIgnoreCase)',
    "var targetMatches = 0;",
    "targetMatches++;",
    "targetMatches > 1",
    "result.ContainsKey(column)",
    "QS3D Excel row is missing its Element ID.",
    "QS3D Excel row is missing its CAD Handle provenance.",
    "QS3D Excel row is missing its drawing fingerprint.",
    "var preferLegacy = !isModernSchema && handleColumns.Count == 0 && decimalHandles.Count > 0 && string.IsNullOrWhiteSpace(drawingFingerprint);",
    "if (preferLegacy)",
    "worksheet.IsEd2Detail && !isModernSchema",
    "AddElementIds(elementIds, value, isModernSchema)",
    "AddHexHandles(explicitHandles, value, isModernSchema)",
    "duplicate Element ID token",
    "duplicate CAD Handle token after hexadecimal normalization",
))

duplicate_identity_smoke = ROOT / "tests/QS3D.Core.SmokeTests/XlsxHandleModernDuplicateIdentitySmoke.cs"
if not duplicate_identity_smoke.is_file():
    errors.append("missing ED2 duplicate identity smoke: " + str(duplicate_identity_smoke.relative_to(ROOT)))
else:
    duplicate_identity_text = duplicate_identity_smoke.read_text(encoding="utf-8")
    for token in (
        "RejectsDuplicateModernElementIds",
        "RejectsDuplicateModernHandleAliases",
        "PreservesUniqueModernIdentitySets",
        "PreservesLegacyHandleDeduplication",
        '"E1;e1"',
        '"A;0xA"',
        '"A;00A"',
    ):
        if token not in duplicate_identity_text:
            errors.append("XlsxHandleModernDuplicateIdentitySmoke.cs missing ED2 duplicate identity token: " + token)
require("window", ("OnEd2ExportClick", "OnExcelLocateClick", "BQ • 1 sheet",))
require("window_code", ('SendStringToExecute("QS3DED2 "', 'SendStringToExecute("QS3DEXCELLOCATE "', '"Zone"',))
require("hub", ('Tag="QS3DED2"', 'Tag="QS3DEXCELLOCATE"'))
require("ribbon", ('Button("ED2 • Excel ↔ CAD", "QS3DED2")', 'Button("Excel → CAD", "QS3DEXCELLOCATE")'))
require("quantity_smoke", (
    "DetailRowsPreserveOneElementProvenance",
    "same Floor/Family across different Zones",
    "Unknown ED2 element id must fail closed",
    "Ed2MaterialDensityMassParity",
    "Ed2DensityAndMassFailClosed",
    "1.875 * 2400 = 4500",
    "different effective material or density",
    "leave density and mass blank",
    "ExpectThrows<InvalidOperationException>",
    "ExpectThrows<OverflowException>",
))
require("excel_smoke", (
    "CreateReorderedEd2Workbook",
    "CloneWorkbookReplacingText",
    "ed2-header-downgrade.xlsx",
    "reordered.IsEd2Detail",
    "qs3d-blank-handle.xlsx",
    'workbook.Contains("CHI_TIET")',
    'workbook.Contains("TONG_HOP")',
    'detailSheet.Contains("Tên cấu kiện")',
    'detailSheet.Contains("Khối lượng riêng (kg/m³)")',
    'detailSheet.Contains(">4500<")',
    'detailSheet.Contains("r=\\\"H2\\\" s=\\\"5\\\"><v>1E-09</v>")',
    'styles.Contains("numFmtId=\\\"164\\\" formatCode=\\\"#,##0.000\\\"")',
    '!detailSheet.Contains("r=\\\"T3\\\"")',
    '!detailSheet.Contains("r=\\\"U3\\\"")',
))

print("QS3D ED2 Excel round-trip preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: ED2 scopes before aggregation on a detached read-only snapshot, preserves one-element CHI_TIET and collision-safe Zone/material/density-aware TONG_HOP provenance, exports material/family/density/mass/note fields with readable formatting, and fails closed on schema, quantity, fingerprint or live-Handle drift.")