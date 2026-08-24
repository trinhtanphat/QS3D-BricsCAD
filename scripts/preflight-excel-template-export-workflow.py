#!/usr/bin/env python3
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/ExcelTemplateCommands.cs"
CORE = ROOT / "src/QS3D.Core/Export/QsWorkbookTemplateEngine.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml"
UI = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.TemplateExport.cs"
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/QuantityReferenceRibbonAugmenter.cs"
V26 = ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"
SAMPLE = ROOT / "samples/excel-template-mapping.example.json"
DOC = ROOT / "docs/EXCEL-TEMPLATE-EXPORT.md"

failures = []


def read(path):
    if not path.is_file():
        failures.append("missing required file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text, needle, label):
    if needle not in text:
        failures.append(label + ": missing " + repr(needle))


def forbid(text, needle, label):
    if needle in text:
        failures.append(label + ": forbidden " + repr(needle))


def require_order(text, markers, label):
    positions = [text.find(marker) for marker in markers]
    if any(position < 0 for position in positions) or positions != sorted(positions) or len(set(positions)) != len(positions):
        failures.append(label + ": expected order " + " -> ".join(repr(marker) for marker in markers))


command = read(COMMAND)
core = read(CORE)
xaml = read(XAML)
ui = read(UI)
ribbon = read(RIBBON)
v26 = read(V26)
doc = read(DOC)

for needle in (
    'using QS3D.BricsCAD.V25.Services;',
    '[CommandMethod("QS3DEXCELTEMPLATE", CommandFlags.UsePickSet)]',
    'var reviewedProjectId = project.ProjectId;',
    'var reviewedVersion = project.ChangeVersion;',
    '"Selection Floor Zone All"',
    '"Detail Group"',
    '"Default Custom"',
    'Filter = "Excel Workbook (*.xlsx)|*.xlsx"',
    'Filter = "QS3D Template Mapping (*.json)|*.json"',
    'ValidateOutputPath(templateDialog.FileName, outputDialog.FileName);',
    'promptProject.ProjectId, reviewedProjectId',
    'promptProject.ChangeVersion != reviewedVersion',
    'DrawingUnitWorkflow.EnsureResolved(document, "QS3DEXCELTEMPLATE")',
    'currentProject.ProjectId, reviewedProjectId',
    'var exportVersion = currentProject.ChangeVersion;',
    'ProjectStateSnapshot.CreateDetachedCopy(currentProject)',
    'ProjectQuantityReportBuilder.Detail(preview',
    'ProjectQuantityReportBuilder.Group(preview',
    'EnsureHandlesAreLive(document, rows);',
    'finalProject.ProjectId, reviewedProjectId',
    'finalProject.ChangeVersion != exportVersion',
    'QsWorkbookTemplateExporter.Export(',
    'new DataContractJsonSerializer(typeof(CustomMappingContract))',
    'MaxMappingBytes = 64 * 1024',
    'using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))',
    'if (stream.Length <= 0 || stream.Length > MaxMappingBytes)',
    'var fieldName = item.Field ?? string.Empty;',
    'var columnName = item.Column ?? string.Empty;',
    'Enum.IsDefined(typeof(QsWorkbookTemplateField), field)',
    'new QsWorkbookTemplateDefinition("CHI_TIET", 2, mappings, 5000)',
    'QsWorkbookTemplateField.ElementIds',
    'QsWorkbookTemplateField.SourceHandles',
    'QsWorkbookTemplateField.DrawingFingerprint',
    'QsWorkbookTemplateField.TraceKey',
):
    require(command, needle, "template command")

for stale_symbol in (
    'project.Id;',
    'promptProject.Id',
    'currentProject.Id',
    'finalProject.Id',
):
    forbid(command, stale_symbol, "compile-safe ProjectState identity")

require_order(
    command,
    (
        'ValidateOutputPath(templateDialog.FileName, outputDialog.FileName);',
        'promptProject.ChangeVersion != reviewedVersion',
        'DrawingUnitWorkflow.EnsureResolved(document, "QS3DEXCELTEMPLATE")',
        'var exportVersion = currentProject.ChangeVersion;',
        'ProjectStateSnapshot.CreateDetachedCopy(currentProject)',
        'EnsureHandlesAreLive(document, rows);',
        'finalProject.ChangeVersion != exportVersion',
        'QsWorkbookTemplateExporter.Export(',
    ),
    "validated prompts -> prompt freshness -> unit bind -> detached quantity -> live provenance -> final freshness -> atomic exporter",
)

require_order(
    command,
    (
        'using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))',
        'if (stream.Length <= 0 || stream.Length > MaxMappingBytes)',
        'contract = serializer.ReadObject(stream) as CustomMappingContract;',
    ),
    "opened mapping stream -> bounded size check -> deserialize",
)

for forbidden in (
    "Microsoft.Office.Interop.Excel",
    "Activator.CreateInstance",
    "QsCustomerWorkbookExporter.Export",
    "XlsxQuantityExporter.ExportEd2",
    "new QuantityRuleEngine",
    "ProjectContextCoordinator.GetOrCreate",
):
    forbid(command, forbidden, "no duplicate engine/Interop/project bootstrap")

for needle in (
    "AtomicFileCommit.CreateTempPath(destination)",
    "XlsxPackageValidator.Validate(temp",
    "AtomicFileCommit.ReplaceWithoutBackup(temp, destination)",
    "Template export must not overwrite the template file in place.",
):
    require(core, needle, "Core atomic template renderer")

require(xaml, 'Content="Xuất theo mẫu Excel" Click="OnExcelTemplateExportClick"', "Quantity Insight template action")
require(ui, '"QS3DEXCELTEMPLATE "', "Quantity Insight template dispatch")
require(ribbon, '"QS3D_QTY_BLT_TEMPLATE_EXPORT"', "quantity Ribbon template id")
require(ribbon, '"Xuất theo\\nmẫu"', "quantity Ribbon template label")
require(ribbon, '"QS3DEXCELTEMPLATE"', "quantity Ribbon template command")
require(v26, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared C# source")
require(v26, '<Page Include="..\\QS3D.BricsCAD.V25\\UI\\**\\*.xaml">', "V26 shared XAML")

for needle in (
    "QS3DEXCELTEMPLATE",
    "Selection`, `Floor`, `Zone`, or `All",
    "Detail` or `Group",
    "samples/excel-template-mapping.example.json",
    "LOCAL_ONLY",
):
    require(doc, needle, "template export documentation")

valid_fields = {
    "Index", "Floor", "Zone", "FloorZone", "Category", "FamilyId", "FamilyName", "ElementName",
    "Material", "Note", "Count", "GrossConcreteM3", "DeductionM3", "NetConcreteM3", "FormworkM2",
    "LengthM", "OuterPerimeterM", "InnerPerimeterM", "DoorAreaM2", "SideAreaM2", "BottomAreaM2",
    "TopAreaM2", "OtherAreaM2", "DensityKgM3", "MassKg", "ElementIds", "SourceHandles",
    "DrawingFingerprint", "TraceKey",
}

if SAMPLE.is_file():
    try:
        payload = json.loads(SAMPLE.read_text(encoding="utf-8"))
        if payload.get("worksheet") != "CHI_TIET":
            failures.append("sample mapping must target CHI_TIET")
        if not isinstance(payload.get("firstDataRow"), int) or payload["firstDataRow"] < 1:
            failures.append("sample firstDataRow must be positive")
        if not isinstance(payload.get("reservedDataRows"), int) or payload["reservedDataRows"] < 1:
            failures.append("sample reservedDataRows must be positive")
        mappings = payload.get("mappings")
        if not isinstance(mappings, list) or not mappings:
            failures.append("sample mappings must be a non-empty list")
        else:
            fields = [entry.get("field") for entry in mappings]
            columns = [entry.get("column") for entry in mappings]
            if len(fields) != len(set(fields)):
                failures.append("sample mapping fields must be unique")
            if len(columns) != len(set(columns)):
                failures.append("sample mapping columns must be unique")
            unknown = sorted(set(fields) - valid_fields)
            if unknown:
                failures.append("sample mapping has unknown fields: " + ", ".join(unknown))
            for required in ("ElementIds", "SourceHandles", "DrawingFingerprint", "TraceKey"):
                if required not in fields:
                    failures.append("sample mapping must demonstrate provenance field: " + required)
    except Exception as error:
        failures.append("sample mapping JSON is invalid: " + str(error))
else:
    failures.append("missing sample mapping JSON")

print("QS3D Excel template export workflow preflight")
if failures:
    for failure in failures:
        print("ERROR:", failure)
    print("FAILED with", len(failures), "error(s).")
    sys.exit(1)

print("PASS: QS3DEXCELTEMPLATE uses canonical compile-safe host services, detached Detail/Group rows, bounded Default/Custom mapping, prompt/final freshness gates, complete live-Handle validation, atomic Core XLSX rewrite, Quantity Insight/Ribbon exposure and V25/V26 shared-source parity.")
print("NOTE: interactive dialogs and licensed BricsCAD template-rendering qualification remain LOCAL_ONLY under #72.")
