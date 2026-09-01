from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/QsWorkbookTemplateEngine.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsWorkbookTemplateTracePackageBoundSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

required_source = [
    "internal const long MaxTemplateWorkbookBytes = 128L * 1024L * 1024L;",
    "internal static void ValidateTemplatePackageLength(long length)",
    'throw new InvalidDataException("XLSX template workbook is too large for bounded processing.");',
    "var fullPath = Path.GetFullPath(path);",
    'if (!File.Exists(fullPath)) throw new FileNotFoundException("Template workbook was not found.", fullPath);',
    "QsWorkbookTemplateExporter.ValidateTemplatePackageLength(new FileInfo(fullPath).Length);",
    "new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)",
]

for token in required_source:
    if token not in source:
        raise SystemExit("FAIL: missing trace package-bound source contract: " + token)

trace_start = source.index("public static class QsWorkbookTemplateTraceReader")
trace_text = source[trace_start:]
validate_pos = trace_text.index("QsWorkbookTemplateExporter.ValidateTemplatePackageLength")
archive_pos = trace_text.index("new ZipArchive")
if validate_pos >= archive_pos:
    raise SystemExit("FAIL: trace reader must admit package size before ZIP construction")

if "private const long MaxTemplateWorkbookBytes" in source:
    raise SystemExit("FAIL: template workbook package ceiling must be shared with trace reader, not private to exporter")

for token in [
    "OversizedWorkbookFailsAtPackageAdmission();",
    "128L * 1024L * 1024L + 1L",
    '"XLSX template workbook is too large for bounded processing."',
    "QsWorkbookTemplateTraceReader.Read(path, definition, 2);",
]:
    if token not in smoke:
        raise SystemExit("FAIL: missing deterministic trace package-bound smoke contract: " + token)

if "QsWorkbookTemplateTracePackageBoundSmoke.Run();" not in registration:
    raise SystemExit("FAIL: trace package-bound smoke is not registered")

print("PASS: QS workbook template trace package admission is bounded before ZIP parsing")
