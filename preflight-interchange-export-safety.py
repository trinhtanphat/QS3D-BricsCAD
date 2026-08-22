#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeExportSafetySmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing interchange export safety contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "AtomicFileCommit.CreateTempPath(fullPath)",
        "stream.Flush(true);",
        "AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);",
        "AtomicFileCommit.TryDelete(tempPath);",
        "if (value.Kind != DateTimeKind.Utc)",
    ):
        if token not in text:
            errors.append("ProjectInterchangeJsonExporter.cs missing safety token: " + token)
    if "File.Copy(tempPath, fullPath, true)" in text or "File.Copy(fullPath, backupPath, true)" in text:
        errors.append("Interchange exporter must not maintain a second overwrite-copy atomic fallback.")
    if "value.ToUniversalTime()" in text:
        errors.append("Interchange exporter must reject non-UTC timestamps instead of interpreting them with machine timezone.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RejectsNonUtcBuild();",
        "FailedExportPreservesExistingDestination();",
        "SuccessfulExportReplacesDestination();",
        'Equal("old-good", File.ReadAllText(path)',
    ):
        if token not in text:
            errors.append("ProjectInterchangeExportSafetySmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: interchange JSON export uses the shared atomic publisher and rejects machine-dependent non-UTC timestamps.")
