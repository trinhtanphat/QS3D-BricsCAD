#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "exporter": ROOT / "src/QS3D.Core/Export/QsCustomerWorkbookExporter.cs",
    "reader": ROOT / "src/QS3D.Core/Export/QsCustomerWorkbookTraceReader.cs",
    "commands": ROOT / "src/QS3D.BricsCAD.V25/CustomerExcelCommands.cs",
    "resolver": ROOT / "src/QS3D.BricsCAD.V25/Services/ExcelLocateResolutionService.cs",
    "ribbon": ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuantityReferenceRibbonAugmenter.cs",
    "smoke": ROOT / "tests/QS3D.Core.SmokeTests/CustomerWorkbookTraceSmoke.cs",
    "registration": ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs",
    "ed2_guard": ROOT / "scripts/preflight-ed2-excel-roundtrip.py",
}

texts = {}
for name, path in files.items():
    if not path.is_file():
        errors.append("missing customer Excel file: " + str(path.relative_to(ROOT)))
    else:
        texts[name] = path.read_text(encoding="utf-8")


def require(name, tokens):
    text = texts.get(name, "")
    for token in tokens:
        if token not in text:
            errors.append(files[name].name + " missing customer Excel token: " + token)


require("exporter", (
    'public const string DgklSheet = "DGKL"',
    'public const string FormworkSheet = "COP_PHA"',
    'public const string DetailSheet = "CHI_TIET"',
    'public const string TraceSheet = "TRACE_MODEL"',
    'public const string TraceHeader = "TRACE_KEY"',
    "ValidateScope(details, summaries)",
    "row Count must equal its QS3D Element ID provenance cardinality",
    "must be canonical without surrounding whitespace",
    "ulong.TryParse",
    "AppendTextElement",
    "if (hasEvidence) Number",
    'name=\\\"DGKL\\\"',
    'name=\\\"COP_PHA\\\"',
    'name=\\\"CHI_TIET\\\"',
    'name=\\\"TRACE_MODEL\\\"',
    '"QS3D Element ID"',
    '"CAD Handle (hex)"',
    '"QS3D Drawing Fingerprint"',
    "SHA256.Create()",
    "AtomicFileCommit.ReplaceWithoutBackup",
))
require("reader", (
    "RequireExactCustomerSheets",
    "ReadCriticalBusinessTraceKey",
    "ReadTraceProjection",
    "TRACE_MODEL lookup is missing or ambiguous",
    "TRACE_MODEL identity cells must be literal values",
    "Customer workbook CHI_TIET trace must reference exactly one QS3D Element ID",
    "StringSplitOptions.None",
    "ulong.TryParse",
    "must be a canonical literal value",
    "DtdProcessing = DtdProcessing.Prohibit",
    "External worksheet relationships are not supported",
))
require("commands", (
    '[CommandMethod("QS3DEXCEL", CommandFlags.UsePickSet)]',
    '"Selection Floor Zone All"',
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    "ProjectQuantityReportBuilder.Detail(preview",
    "ProjectQuantityReportBuilder.Group(preview",
    "EnsureHandlesAreLive(document, details)",
    "QsCustomerWorkbookExporter.Export(dialog.FileName, details, summary)",
    '[CommandMethod("QS3DEXCELTRACE", CommandFlags.Modal)]',
    '"DGKL COP_PHA CHI_TIET"',
    "QsCustomerWorkbookTraceReader.Read(dialog.FileName, sheet, row.Value)",
    "ExcelLocateResolutionService.ResolveCustomerTrace(document, project, trace)",
    "document.Editor.SetImpliedSelection(resolution.ObjectIds.ToArray())",
    'SendStringToExecute("QS3DZOOMSELECTED "',
))
require("resolver", (
    "ResolveCustomerTrace",
    "MdiActiveDocument",
    "foreach (var elementId in ids)",
    "project.FindElement(elementId) == null",
    "SourceHandleResolver.Resolve(project, ids)",
    "excelHandles.SequenceEqual(projectHandles, StringComparer.OrdinalIgnoreCase)",
    "CadHandleService.Resolve(document, projectHandles)",
    "resolved.Count != projectHandles.Count",
))
require("ribbon", (
    'private const string TabId = "QS3D_QTY"',
    'SetProperty(quantityTab, "Title", "QS3D")',
    '"Xuất\\nExcel"',
    '"QS3DEXCEL"',
    '"Excel →\\nCAD"',
    '"QS3DEXCELTRACE"',
))
require("smoke", (
    "CustomerWorkbookRoundTripsDetailAndAggregateTrace",
    "CustomerWorkbookPreservesEvidenceBlankVersusMeasuredZero",
    "CustomerWorkbookRejectsMalformedProvenance",
    'QsCustomerWorkbookTraceReader.Read(path, "DGKL", 2)',
    'QsCustomerWorkbookTraceReader.Read(path, "COP_PHA", 2)',
    'QsCustomerWorkbookTraceReader.Read(path, "CHI_TIET", 2)',
    'RequireMissingCell(detail, "I2"',
    '"8000000000000000"',
))
require("registration", ("CustomerWorkbookTraceSmoke.Run();",))
require("ed2_guard", (
    '[CommandMethod("QS3DED2", CommandFlags.UsePickSet)]',
    '[CommandMethod("QS3DEXCELLOCATE", CommandFlags.Modal)]',
    "ExcelLocateResolutionService.ResolveModern(doc, project, lookup)",
))

print("QS3D customer Excel + reverse trace preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: customer DGKL/COP_PHA/CHI_TIET/TRACE_MODEL export and fail-closed aggregate/detail reverse locate are source-guarded while ED2 compatibility remains intact.")
