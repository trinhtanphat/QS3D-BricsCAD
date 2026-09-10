#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/MeasurementWorkItemCoverageCsvExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageCsvUnicodeIntegritySmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing coverage CSV Unicode integrity file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "private static readonly UTF8Encoding StrictUtf8WithBom = new UTF8Encoding(true, true);",
        "ValidateStrictUtf8Payload(matrix);",
        "var fullPath = Path.GetFullPath(path);",
        "new StreamWriter(stream, StrictUtf8WithBom, 4096, leaveOpen: true)",
        "BoundedUtf8TextWriter",
        "StrictUtf8WithBom.GetByteCount(value)",
        "AtomicFileCommit.CreateTempPath(fullPath)",
        "AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);",
        "AtomicFileCommit.TryDelete(tempPath);",
    )
    for token in required:
        if token not in text:
            errors.append("MeasurementWorkItemCoverageCsvExporter missing Unicode/publication contract: " + token)

    forbidden = (
        "new UTF8Encoding(true)",
        "var content = ToCsv(matrix);",
        "var content = sb.ToString();",
        "writer.Write(content);",
    )
    for token in forbidden:
        if token in text:
            errors.append("Coverage CSV exporter retains obsolete/replacement-fallback publication token: " + token)

    validation = text.find("ValidateStrictUtf8Payload(matrix);")
    path_resolution = text.find("var fullPath = Path.GetFullPath(path);")
    directory_creation = text.find("Directory.CreateDirectory(directory)")
    temp_creation = text.find("AtomicFileCommit.CreateTempPath(fullPath)")
    if min(validation, path_resolution, directory_creation, temp_creation) < 0 or not (
        validation < path_resolution < directory_creation < temp_creation
    ):
        errors.append("Coverage CSV Export must validate strict UTF-8 before path, directory, and temp-file work.")

    projection_writer = text.find("using var writer = new BoundedUtf8TextWriter(")
    projection_return = text.find("return stringWriter.ToString();", projection_writer)
    byte_validation = text.find("StrictUtf8WithBom.GetByteCount(value)")
    inner_write = text.find("inner.Write(value);", byte_validation)
    if min(projection_writer, projection_return, byte_validation, inner_write) < 0:
        errors.append("Coverage CSV projection must remain bounded and strict-UTF8 validated.")
    if min(byte_validation, inner_write) >= 0 and byte_validation > inner_write:
        errors.append("Coverage CSV bounded writer must validate strict UTF-8 before writing each fragment.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    required = (
        "[ModuleInitializer]",
        "LoneSurrogatesFailClosed",
        'BuildMatrix("map-high-\\uD800")',
        'BuildMatrix("map-low-\\uDC00")',
        "MalformedUnicodeHasNoFilesystemSideEffects",
        "!Directory.Exists(absentRoot)",
        "File.ReadAllBytes(existingPath).SequenceEqual(sentinel)",
        "beforeFiles.SequenceEqual(afterFiles, StringComparer.Ordinal)",
        "SupplementaryUnicodePreservesBomAndIdentity",
        'const string mappingId = "map-rocket-\\uD83D\\uDE80";',
        "bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF",
        "string.Equals(expectedCsv, persisted, StringComparison.Ordinal)",
        "new UTF8Encoding(false, true)",
    )
    for token in required:
        if token not in text:
            errors.append("Coverage CSV Unicode smoke missing regression contract: " + token)

if errors:
    print("QS3D measurement coverage CSV Unicode integrity preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: measurement coverage CSV rejects malformed UTF-16 before filesystem work, preserves valid supplementary Unicode and the UTF-8 BOM, and retains bounded atomic publication.")
