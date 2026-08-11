#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeImportCommands.cs"
errors = []

if not COMMANDS.is_file():
    errors.append("missing generic interchange import selector: " + str(COMMANDS.relative_to(ROOT)))
else:
    text = COMMANDS.read_text(encoding="utf-8")
    command = '[CommandMethod("QS3DINTERCHANGEIMPORT", CommandFlags.Modal)]'
    read = 'var json = ReadGuardedSnapshotText(dialog.FileName);'
    validate = 'ProjectInterchangeValidatedSnapshotReader.Read(json);'
    bootstrap = 'var project = ProjectContextCoordinator.GetOrCreate(document);'

    for token in (command, read, validate, bootstrap, 'ProjectInterchangeImportPreview.Plan(project, json)'):
        if token not in text:
            errors.append("generic interchange import selector missing lifecycle token: " + token)

    read_at = text.find(read)
    validate_at = text.find(validate)
    bootstrap_at = text.find(bootstrap)
    preview_at = text.find('ProjectInterchangeImportPreview.Plan(project, json)')
    if min(read_at, validate_at, bootstrap_at, preview_at) < 0 or not read_at < validate_at < bootstrap_at < preview_at:
        errors.append("generic interchange import must guarded-read -> strict-validate -> bootstrap target -> preview")

    if text.count(validate) != 1:
        errors.append("generic interchange import must perform exactly one explicit strict prevalidation before target bootstrap")
    if text.count(bootstrap) != 1:
        errors.append("generic interchange import may bootstrap exactly once, only after strict snapshot validation")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: generic interchange import validates the guarded snapshot before any target project bootstrap.")
