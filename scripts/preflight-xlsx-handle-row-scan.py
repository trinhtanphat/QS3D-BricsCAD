from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Export/XlsxHandleReader.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/XlsxHandleReaderRowScanSmoke.cs").read_text(encoding="utf-8")

required = [
    "private const int MaxHeaderRows = 10;",
    "var headerRows = new List<XElement>(MaxHeaderRows);",
    "var targetMatches = 0;",
    'foreach (var row in sheet.Descendants(ns + "row"))',
    "targetMatches++;",
    "headerRows.Count < MaxHeaderRows",
    "if (targetMatches > 1)",
    'throw new InvalidDataException("Excel worksheet contains duplicate row number " + rowNumber + ".");',
    "foreach (var headerRow in headerRows)",
]
for token in required:
    if token not in source:
        raise SystemExit(f"ERROR: XLSX handle row-scan contract missing token: {token}")

scan_anchor = 'foreach (var row in sheet.Descendants(ns + "row"))'
duplicate_anchor = "if (targetMatches > 1)"
scan_start = source.index(scan_anchor)
duplicate_start = source.index(duplicate_anchor, scan_start + len(scan_anchor))
scan_region = source[scan_start:duplicate_start]
for token in [
    "declaredRow == int.MaxValue || declaredRow > MaxRows",
    "targetMatches++;",
    "headerRows.Add(row);",
]:
    if token not in scan_region:
        raise SystemExit(f"ERROR: XLSX full row scan must perform validation/selection before duplicate rejection: {token}")
if not scan_region.rstrip().endswith("}"):
    raise SystemExit("ERROR: XLSX duplicate rejection must follow the completed worksheet-row scan")

for forbidden in [
    'var rows = sheet.Descendants(ns + "row").ToList();',
    'rows.Where(x => ParsePositiveInt((string?)x.Attribute("r")) == rowNumber).ToList()',
    'foreach (var headerRow in rows.Where',
]:
    if forbidden in source:
        raise SystemExit(f"ERROR: XLSX handle reader regressed to eager worksheet-row materialization: {forbidden}")

if "XlsxHandleReaderRowScanSmoke.Run();" not in registration:
    raise SystemExit("ERROR: XLSX handle row-scan smoke is not registered")
for token in [
    "duplicateTarget: true",
    "invalidTrailingRow: true",
    'ReadHandleLookup(valid, 20)',
    'duplicate-invalid-trailing.xlsx',
    'CreateWorkbook(duplicateWithInvalidTrailing, duplicateTarget: true, invalidTrailingRow: true)',
]:
    if token not in smoke:
        raise SystemExit(f"ERROR: XLSX handle row-scan smoke missing coverage token: {token}")

print("PASS bounded XLSX handle worksheet row scan")
