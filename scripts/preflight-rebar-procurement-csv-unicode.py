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
        "private static readonly UTF8Encoding StrictUtf8WithBom = new UTF8Encoding(true, true);",
        "var content = ToCsv(rows);",
        "var fullPath = Path.GetFullPath(path);",
        "using (var writer = new StreamWriter(stream, StrictUtf8WithBom))",
        "var content = sb.ToString();",
        "StrictUtf8WithBom.GetByteCount(content);",
        "AtomicFileCommit.CreateTempPath(fullPath)",
        "AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);",
        "AtomicFileCommit.TryDelete(tempPath);",
        "if (rowCount >= MaxRowCount)",
        "var probe = safe.TrimStart();",
        "probe[0] == '=' || probe[0] == '+' || probe[0] == '-' || probe[0] == '@'",
    )
    for token in required:
        if token not in text:
            errors.append("RebarProcurementCsvExporter missing Unicode/publication contract: " + token)

    if "new UTF8Encoding(true)" in text:
        errors.append("Procurement CSV exporter must not use replacement-fallback UTF-8.")

    validation = text.find("StrictUtf8WithBom.GetByteCount(content);")
    projection_return = text.find("return content;", validation)
    export_projection = text.find("var content = ToCsv(rows);")
    path_resolution = text.find("var fullPath = Path.GetFullPath(path);")
    directory_creation = text.find("Directory.CreateDirectory(directory)")
    temp_creation = text.find("AtomicFileCommit.CreateTempPath(fullPath)")
    if min(validation, projection_return) < 0 or validation > projection_return:
        errors.append("Procurement CSV projection must validate strict UTF-8 before returning content.")
    if min(export_projection, path_resolution, directory_creation, temp_creation) < 0 or not (
        export_projection < path_resolution < directory_creation < temp_creation
    ):
        errors.append("Procurement CSV Export must validate/project before path, directory, and temp-file work.")

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
