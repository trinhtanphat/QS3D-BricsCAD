#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/RebarProcurementCsvExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RebarProcurementCsvUnicodeIntegritySmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing procurement CSV Unicode integrity file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "private const int MaxRowCount = 10000;",
        "private const long MaxCsvBytes = 16L * 1024L * 1024L;",
        "private static readonly UTF8Encoding StrictUtf8WithBom = new UTF8Encoding(true, true);",
        "var snapshots = SnapshotRows(rows);",
        "ValidateCsvByteCount(snapshots);",
        "var fullPath = Path.GetFullPath(path);",
        "using (var writer = new StreamWriter(stream, StrictUtf8WithBom))",
        "WriteCsv(writer, snapshots);",
        "StrictUtf8WithBom.GetByteCount(value)",
        "AtomicFileCommit.CreateTempPath(fullPath)",
        "AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);",
        "AtomicFileCommit.TryDelete(tempPath);",
        "if (rowCount >= MaxRowCount)",
        "var probe = safe.TrimStart();",
        "IsFormulaPrefix(probe[0])",
    )
    for token in required:
        if token not in text:
            errors.append("RebarProcurementCsvExporter missing Unicode/publication contract: " + token)

    if "new UTF8Encoding(true)" in text:
        errors.append("Procurement CSV exporter must not use replacement-fallback UTF-8.")
    if "var content = ToCsv(rows);" in text or "writer.Write(content);" in text:
        errors.append("Procurement CSV Export must not regress to eager whole-file publication.")

    export_start = text.find("public static void Export(")
    export_end = text.find("public static string ToCsv(", export_start)
    export_method = text[export_start:export_end]
    snapshot = export_method.find("var snapshots = SnapshotRows(rows);")
    validation = export_method.find("ValidateCsvByteCount(snapshots);", snapshot)
    path_resolution = export_method.find("var fullPath = Path.GetFullPath(path);", validation)
    directory_creation = export_method.find("Directory.CreateDirectory(directory)", path_resolution)
    temp_creation = export_method.find("AtomicFileCommit.CreateTempPath(fullPath)", directory_creation)
    if min(snapshot, validation, path_resolution, directory_creation, temp_creation) < 0 or not (
        snapshot < validation < path_resolution < directory_creation < temp_creation
    ):
        errors.append("Procurement CSV Export must snapshot and strict-UTF8 validate before path, directory, and temp-file work.")

    to_csv_start = text.find("public static string ToCsv(")
    snapshot_rows_start = text.find("private static List<RebarProcurementSummary> SnapshotRows", to_csv_start)
    to_csv_method = text[to_csv_start:snapshot_rows_start]
    to_csv_snapshot = to_csv_method.find("var snapshots = SnapshotRows(rows);")
    to_csv_validation = to_csv_method.find("ValidateCsvByteCount(snapshots);", to_csv_snapshot)
    to_csv_projection = to_csv_method.find("WriteCsv(writer, snapshots);", to_csv_validation)
    if min(to_csv_snapshot, to_csv_validation, to_csv_projection) < 0 or not (
        to_csv_snapshot < to_csv_validation < to_csv_projection
    ):
        errors.append("Procurement CSV ToCsv must validate strict UTF-8 and the byte ceiling before string projection.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    required = (
        "[ModuleInitializer]",
        "LoneSurrogatesFailClosed",
        'BuildRow("group-high-\\uD800", "CB400-V")',
        'BuildRow("group-low-\\uDC00", "CB400-V")',
        "MalformedUnicodeHasNoFilesystemSideEffects",
        "!Directory.Exists(absentRoot)",
        "File.ReadAllBytes(existingPath).SequenceEqual(sentinel)",
        "beforeFiles.SequenceEqual(afterFiles, StringComparer.Ordinal)",
        "SupplementaryUnicodePreservesBomAndIdentity",
        'const string groupId = "group-rocket-\\uD83D\\uDE80";',
        'const string grade = "grade-rocket-\\uD83D\\uDE80";',
        "bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF",
        "string.Equals(expectedCsv, persisted, StringComparison.Ordinal)",
        "new UTF8Encoding(false, true)",
    )
    for token in required:
        if token not in text:
            errors.append("Procurement CSV Unicode smoke missing regression contract: " + token)

if errors:
    print("QS3D Rebar procurement CSV Unicode integrity preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Rebar procurement CSV rejects malformed UTF-16 before filesystem work, preserves valid supplementary Unicode and the UTF-8 BOM, and retains formula/bound/atomic publication contracts.")
