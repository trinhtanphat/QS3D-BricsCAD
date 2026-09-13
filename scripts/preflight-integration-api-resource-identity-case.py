from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "BenchmarkParity" / "QsIntegrationApiV1.cs"
text = SOURCE.read_text(encoding="utf-8")
start = text.index("public sealed class QsIntegrationApiV1")
api = text[start:]

required = [
    "string.Equals(request.ProjectId, snapshot.Project.ProjectId, StringComparison.Ordinal)",
    "string.Equals(x.Binding.WorkbookId, workbookId, StringComparison.Ordinal)",
    '"PROJECT_NOT_FOUND"',
    '"WORKBOOK_IDENTITY_MISMATCH"',
]
for marker in required:
    if marker not in api:
        raise SystemExit(f"missing exact resource identity marker: {marker}")

for forbidden in [
    "string.Equals(request.ProjectId, snapshot.Project.ProjectId, StringComparison.OrdinalIgnoreCase)",
    "string.Equals(x.Binding.WorkbookId, workbookId, StringComparison.OrdinalIgnoreCase)",
]:
    if forbidden in api:
        raise SystemExit(f"resource identity must not be case-insensitive: {forbidden}")

print("integration API exact resource identity guard: PASS")
