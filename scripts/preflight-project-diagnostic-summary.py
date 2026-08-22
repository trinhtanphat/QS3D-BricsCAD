#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/ProjectDiagnosticSummaryExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummarySmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing diagnostic summary contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        'FormatName = "QS3D.DiagnosticSummary"',
        "FormatVersion = 1",
        "new ComprehensiveModelHealthService().Inspect(project)",
        '\\"elementCategories\\"',
        '\\"health\\"',
        '\\"byCode\\"',
        "CanonicalCode",
        "AtomicFileCommit.CreateTempPath(fullPath)",
        "stream.Flush(true);",
        "AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);",
        "AtomicFileCommit.TryDelete(tempPath);",
    ):
        if token not in text:
            errors.append("ProjectDiagnosticSummaryExporter missing summary/privacy/publication token: " + token)

    for forbidden in (
        "project.ProjectId",
        "project.Name",
        "project.DrawingPath",
        "project.DrawingFingerprint",
        ".SourceHandles",
        ".Properties",
        ".Quantities",
        ".Message",
        ".ElementId",
    ):
        if forbidden in text:
            errors.append("Diagnostic summary must not export private semantic/native payload: " + forbidden)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "SummaryContainsCountsWithoutProjectPayload();",
        "ExportReplacesAtomically();",
        "SECRET-PROJECT-ID",
        "PRIVATE-DWG-FINGERPRINT",
        "DEADBEEF",
        "PRIVATE-MARK",
        "Sensitive detail",
        "Forbid(json",
        "[ModuleInitializer]",
    ):
        if token not in text:
            errors.append("ProjectDiagnosticSummarySmoke missing privacy/atomic regression token: " + token)

if errors:
    print("QS3D privacy-safe diagnostic summary preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: diagnostic summary exports only schema/count/category/health-code aggregates, excludes project/native/semantic payload details, and publishes atomically.")
