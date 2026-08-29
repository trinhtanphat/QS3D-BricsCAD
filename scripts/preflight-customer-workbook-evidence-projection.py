#!/usr/bin/env python3
# Lane-Key: customer-workbook-formwork-dimension-evidence
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ROW = ROOT / "src/QS3D.Core/Reporting/QuantityReportRow.cs"
BUILDER = ROOT / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs"
EXPORTER = ROOT / "src/QS3D.Core/Export/QsCustomerWorkbookExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsCustomerWorkbookDgklLayoutSmoke.cs"


def fail(message, details=()):
    print("ERROR:", message)
    for detail in details:
        print(" -", detail)
    return 1


def main():
    paths = [ROW, BUILDER, EXPORTER, SMOKE]
    missing_files = [str(path.relative_to(ROOT)) for path in paths if not path.is_file()]
    if missing_files:
        return fail("customer workbook evidence projection files are missing", missing_files)

    row = ROW.read_text(encoding="utf-8")
    builder = BUILDER.read_text(encoding="utf-8")
    exporter = EXPORTER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    row_required = [
        "public double GrossFormworkM2 { get; set; }",
        "public double ConcreteContactDeductionM2 { get; set; }",
        "public double NetFormworkM2 { get; set; }",
        "public double WidthM { get; set; }",
        "public double HeightM { get; set; }",
        "public bool HasGrossFormworkM2Evidence { get; set; }",
        "public bool HasConcreteContactDeductionM2Evidence { get; set; }",
        "public bool HasNetFormworkM2Evidence { get; set; }",
        "public bool HasWidthMEvidence { get; set; }",
        "public bool HasHeightMEvidence { get; set; }",
    ]
    missing = [token for token in row_required if token not in row]
    if missing:
        return fail("QuantityReportRow is missing evidence-aware workbook fields", missing)

    builder_required = [
        'element.Quantities.ContainsKey("GrossFormworkM2")',
        'HasAnyQuantity(element, "ConcreteContactDeductionM2", "FormworkDeductionM2")',
        'HasAnyQuantity(element, "NetFormworkM2", "FormworkM2")',
        'element.Quantities.ContainsKey("WidthM")',
        'element.Quantities.ContainsKey("HeightM")',
        'row.HasGrossFormworkM2Evidence = AggregateEvidence(',
        'row.HasConcreteContactDeductionM2Evidence = AggregateEvidence(',
        'row.HasNetFormworkM2Evidence = AggregateEvidence(',
        'aggregate.GrossFormworkM2.Add(grossFormwork, element.Id + "/GrossFormworkM2")',
        'aggregate.ConcreteContactDeductionM2.Add(formworkDeduction, element.Id + "/ConcreteContactDeductionM2")',
        'aggregate.NetFormworkM2.Add(netFormwork, element.Id + "/NetFormworkM2")',
        'aggregate.FormworkM2.Add(netFormwork, element.Id + "/FormworkM2")',
        'aggregate.WidthM.Add(Q(element, "WidthM"), element.Id + "/WidthM")',
        'aggregate.HeightM.Add(Q(element, "HeightM"), element.Id + "/HeightM")',
        'row.GrossFormworkM2 = aggregate.GrossFormworkM2.Value("GrossFormworkM2")',
        'row.ConcreteContactDeductionM2 = aggregate.ConcreteContactDeductionM2.Value("ConcreteContactDeductionM2")',
        'row.NetFormworkM2 = aggregate.NetFormworkM2.Value("NetFormworkM2")',
        'row.FormworkM2 = aggregate.FormworkM2.Value("FormworkM2")',
        'row.WidthM = aggregate.WidthM.Value("WidthM")',
        'row.HeightM = aggregate.HeightM.Value("HeightM")',
    ]
    missing = [token for token in builder_required if token not in builder]
    if missing:
        return fail("ProjectQuantityReportBuilder does not project explicit evidence through compensated aggregation", missing)

    exporter_required = [
        "row.HasGrossFormworkM2Evidence || row.HasConcreteContactDeductionM2Evidence || row.HasNetFormworkM2Evidence",
        "var hasNetFormworkEvidence = row.HasNetFormworkM2Evidence || row.HasFormworkM2Evidence;",
        'Evidence(sb, Cell(5, excelRow), row.GrossFormworkM2, row.HasGrossFormworkM2Evidence);',
        'Evidence(sb, Cell(6, excelRow), row.ConcreteContactDeductionM2, row.HasConcreteContactDeductionM2Evidence);',
        'Evidence(sb, Cell(7, excelRow), row.NetFormworkM2, row.HasNetFormworkM2Evidence);',
        'Evidence(sb, Cell(5, excelRow), row.WidthM, row.HasWidthMEvidence);',
        'Evidence(sb, Cell(6, excelRow), row.HeightM, row.HasHeightMEvidence);',
        'Evidence(sb, Cell(10, excelRow), row.NetFormworkM2, row.HasNetFormworkM2Evidence);',
        '<sheet name=\\"TRACE_MODEL\\" sheetId=\\"4\\" state=\\"hidden\\"',
    ]
    missing = [token for token in exporter_required if token not in exporter]
    if missing:
        return fail("customer workbook exporter evidence contract is incomplete", missing)

    forbidden = [
        'if (row.HasFormworkM2Evidence) Number(sb, Cell(6, excelRow), 0d, DecimalStyle);',
        'Evidence(sb, Cell(5, excelRow), row.FormworkM2, row.HasFormworkM2Evidence);',
        'Evidence(sb, Cell(7, excelRow), row.FormworkM2, row.HasFormworkM2Evidence);',
        'Width and height are intentionally blank',
    ]
    found = [token for token in forbidden if token in exporter]
    if found:
        return fail("customer workbook still fabricates or suppresses evidence", found)

    smoke_required = [
        "COP_PHA explicit gross formwork evidence",
        "COP_PHA explicit formwork deduction evidence",
        "legacy net-only formwork must not fabricate CP gross",
        "legacy net-only formwork must not fabricate deduction zero",
        "CHI_TIET width evidence",
        "CHI_TIET height evidence",
        "CHI_TIET width stays blank without WidthM evidence",
        "COP_PHA grouped trace cardinality",
        "CHI_TIET trace cardinality",
        "TRACE_MODEL worksheet visibility",
    ]
    missing = [token for token in smoke_required if token not in smoke]
    if missing:
        return fail("customer workbook smoke is missing evidence/trace regressions", missing)

    print("PASS: customer workbook projects explicit formwork and dimensions through compensated aggregation, preserves legacy net-only formwork, leaves unsupported evidence blank, and retains hidden trace semantics.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
