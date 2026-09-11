#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CoordinationIssueExcelWorkbook.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CoordinationIssueExcelWorkbookSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
errors: list[str] = []

required = {
    "bounded export row admission": "MaxExportIssueRows",
    "hostile cardinality regression": "RejectsOversizedExportBeforeProjectionMaterialization",
}
for label, token in required.items():
    haystack = smoke if "regression" in label else source
    if token not in haystack:
        errors.append(f"missing {label}: {token}")

if "private const int MaxRows = 1048576;" in source:
    errors.append("Excel-scale export cardinality remains admitted before package byte budgets")
if "var issueRows = new List<IReadOnlyList<string>>(rows.Count);" in source:
    errors.append("export still materializes the complete issue-row graph before ZIP creation")
if "CoordinationIssueExcelLifecycle.Project(snapshot);" in source:
    projection = source.find("CoordinationIssueExcelLifecycle.Project(snapshot);")
    admission = source.find("MaxExportIssueRows")
    if admission < 0 or admission > projection:
        errors.append("bounded source cardinality must be admitted before lifecycle projection")

for token, label in {
    "pre-existing destination": "destination atomicity assertion",
    "owned temp": "owned-temp cleanup assertion",
}.items():
    if token not in smoke:
        errors.append(f"missing {label}: {token}")

if errors:
    raise SystemExit(
        "ERROR: Coordination Issue XLSX export remains resource-amplifiable before bounded package writing:\n - "
        + "\n - ".join(errors)
    )

print("PASS: Coordination Issue XLSX export bounds source/projection work before package materialization")
