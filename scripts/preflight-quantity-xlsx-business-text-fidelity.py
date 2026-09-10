from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Export/XlsxQuantityExporter.cs").read_text(encoding="utf-8")
required = [
    "ValidateIdentityText(value, rowIndex, fieldName, sheetLabel);",
    "well-formed UTF-16",
    "XML 1.0",
    "char.IsHighSurrogate",
    "char.IsLowSurrogate",
]
identity_calls = [
    'ValidateIdentityCellText(row.Floor, rowIndex, "Floor", "Quantity XLSX");',
    'ValidateIdentityCellText(row.Zone, rowIndex, "Zone", "Quantity XLSX");',
    'ValidateIdentityCellText(row.Category, rowIndex, "Category", "Quantity XLSX");',
    'ValidateIdentityCellText(row.Category, rowIndex, "Category", sheetLabel);',
    'ValidateIdentityCellText(row.Material, rowIndex, "Material", sheetLabel);',
    'ValidateIdentityCellText(row.FamilyId, rowIndex, "FamilyId", sheetLabel);',
    'ValidateIdentityCellText(row.DrawingFingerprint, rowIndex, "DrawingFingerprint", sheetLabel);',
    'ValidateIdentityCellText(floor, rowIndex, "Floor", sheetLabel);',
    'ValidateIdentityCellText(zone, rowIndex, "Zone", sheetLabel);',
]
required.extend(identity_calls)
missing = [token for token in required if token not in source]
if missing:
    raise SystemExit("Quantity XLSX business-text fidelity guard FAILED: missing " + "; ".join(missing))
validate_cell = source.find("private static void ValidateIdentityCellText(")
business_call = source.find("ValidateIdentityText(value, rowIndex, fieldName, sheetLabel);", validate_cell)
length_check = source.find("MaxCellTextCharacters", validate_cell)
export_core = source.find("private static void ExportCore(")
if not (validate_cell >= 0 and business_call > validate_cell and length_check > validate_cell and business_call < export_core):
    raise SystemExit("Quantity XLSX business-text fidelity guard FAILED: validation is not bound to the pre-filesystem cell-validation path.")
print("PASS quantity XLSX business-text fidelity")
