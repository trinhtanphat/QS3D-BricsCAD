#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IMPORTER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeAppendOnlyImporter.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeCommands.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeAppendOnlyImporterSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/INTERCHANGE-APPEND-ONLY-IMPORT.md"
errors = []

for path in (IMPORTER, COMMANDS, SMOKE, REGISTRATION, DOC):
    if not path.is_file():
        errors.append("missing append-only interchange contract file: " + str(path.relative_to(ROOT)))

if IMPORTER.is_file():
    text = IMPORTER.read_text(encoding="utf-8")
    for token in (
        'ImportMode = "AppendOnly"',
        'ProjectInterchangeValidatedSnapshotReader.Read(json)',
        'ProjectStateSnapshot.Capture(target)',
        'PreflightCollisions(target, source)',
        'element.DrawingFingerprint = string.Empty',
        'element.MarkDirty(ElementDirtyFlags.All)',
        'snapshot.Restore(target)',
        '"ImportInterchangeAppendOnly"',
        'LastSourceHandlesDiscardedKey',
    ):
        if token not in text:
            errors.append("append-only importer missing contract token: " + token)
    if 'element.SourceHandles.Add' in text or 'GeneratedSolidHandle' in text or 'GeneratedHandles' in text:
        errors.append("append-only importer must not rebind source/generated CAD ownership")

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DINTERCHANGEAPPEND", CommandFlags.Modal)]',
        'ProjectInterchangeJsonValidator.ValidateFile(dialog.FileName)',
        'ProjectInterchangeImportPreview.Plan(project, json)',
        'preview.CollisionCount > 0',
        'MessageBoxButton.YesNo',
        'ProjectInterchangeAppendOnlyImporter.Import(project, json)',
        'Chưa tự lưu .qsdb',
    ):
        if token not in text:
            errors.append("interchange append command missing safety token: " + token)
    if '[CommandMethod("QS3DINTERCHANGEIMPORT"' in text:
        errors.append("generic interchange import command must not be introduced by the append-only slice")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        'ImportAppendsPortableStateAndDiscardsCadOwnership()',
        'CollisionFailsBeforeMutation()',
        'InvalidSnapshotFailsBeforeMutation()',
        'Equal(string.Empty, importedBase.DrawingFingerprint)',
        'Equal(0, importedBase.SourceHandles.Count)',
        '"ImportInterchangeAppendOnly"',
    ):
        if token not in text:
            errors.append("append-only importer smoke missing contract token: " + token)

if REGISTRATION.is_file() and 'ProjectInterchangeAppendOnlyImporterSmoke.Run();' not in REGISTRATION.read_text(encoding="utf-8"):
    errors.append("append-only importer smoke is not registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in ('QS3DINTERCHANGEAPPEND', 'does **not** copy', 'Still open for issue #84'):
        if token not in text:
            errors.append("append-only import documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: append-only interchange mutation is validated, collision-blocked, rollback-protected, explicitly confirmed and does not claim portable CAD ownership. Runtime V25 qualification is still required.")
