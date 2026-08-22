#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
IMPORTER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeAppendOnlyImporter.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeCommands.cs"
PROJECT_TOOLS = ROOT / "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeAppendOnlyImporterSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/INTERCHANGE-APPEND-ONLY-IMPORT.md"
errors = []

for path in (IMPORTER, COMMANDS, PROJECT_TOOLS, SMOKE, REGISTRATION, DOC):
    if not path.is_file():
        errors.append("missing append-only interchange contract file: " + str(path.relative_to(ROOT)))

if IMPORTER.is_file():
    text = IMPORTER.read_text(encoding="utf-8")
    for token in (
        'ImportMode = "AppendOnly"',
        'public static ProjectInterchangeAppendOnlyImportPlan Plan',
        'return Prepare(target, json).Plan',
        'ProjectInterchangeValidatedSnapshotReader.Read(json)',
        'ProjectStateSnapshot.Capture(target)',
        'PreflightCollisions(target, source)',
        'element.DrawingFingerprint = string.Empty',
        'element.MarkDirty(ElementDirtyFlags.All)',
        'snapshot.Restore(target)',
        '"ImportInterchangeAppendOnly"',
        'LastSourceHandlesDiscardedKey',
        'sourceHandlesToDiscard = checked(',
    ):
        if token not in text:
            errors.append("append-only importer missing contract token: " + token)
    if 'element.SourceHandles.Add' in text or 'GeneratedSolidHandle' in text or 'GeneratedHandles' in text:
        errors.append("append-only importer must not rebind source/generated CAD ownership")

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DINTERCHANGEAPPEND", CommandFlags.Modal)]',
        'ReadGuardedSnapshotText(dialog.FileName)',
        'ProjectInterchangeJsonValidator.MaxFileBytes',
        'new UTF8Encoding(false, true)',
        'ProjectInterchangeValidatedSnapshotReader.Read(json)',
        'var project = ProjectContextCoordinator.GetOrCreate(document);',
        'ProjectInterchangeImportPreview.Plan(project, json)',
        'preview.CollisionCount > 0',
        'ProjectInterchangeAppendOnlyImporter.Plan(project, json)',
        'MessageBoxButton.YesNo',
        'var currentProject = InterchangeConfirmationGuard.RequireFresh(',
        'previewChangeVersion,',
        '"Interchange Append"',
        'ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)',
        'Chưa tự lưu .qsdb',
    ):
        if token not in text:
            errors.append("interchange append command missing safety/freshness token: " + token)
    if text.count('ProjectContextCoordinator.GetOrCreate(document)') != 1:
        errors.append("standalone append may bootstrap exactly one reviewed target, only after strict typed snapshot validation")
    if text.count('ProjectInterchangeValidatedSnapshotReader.Read(json)') != 1:
        errors.append("standalone append must perform exactly one strict typed snapshot validation before target bootstrap")
    if 'File.ReadAllText(dialog.FileName)' in text:
        errors.append("append command must not re-read the selected file through an unbounded second path")
    if '[CommandMethod("QS3DINTERCHANGEIMPORT"' in text:
        errors.append("dedicated append command source must remain separate from the generic policy selector")
    guarded_read = text.find('var json = ReadGuardedSnapshotText(dialog.FileName);')
    json_validation = text.find('var validation = ProjectInterchangeJsonValidator.Validate(json);')
    typed_validation = text.find('ProjectInterchangeValidatedSnapshotReader.Read(json)')
    preview_bind = text.find('var project = ProjectContextCoordinator.GetOrCreate(document);')
    confirmation = text.find('MessageBoxButton.YesNo')
    freshness = text.find('var currentProject = InterchangeConfirmationGuard.RequireFresh(')
    mutation = text.find('ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)')
    if min(guarded_read, json_validation, typed_validation, preview_bind, confirmation, freshness, mutation) < 0 or not guarded_read < json_validation < typed_validation < preview_bind < confirmation < freshness < mutation:
        errors.append("standalone append lifecycle must be guarded read -> JSON validation -> strict typed validation -> target bootstrap/review -> confirmation -> non-creating freshness guard -> mutation")

if PROJECT_TOOLS.is_file():
    text = PROJECT_TOOLS.read_text(encoding="utf-8")
    if text.count('Tag="QS3DINTERCHANGEAPPEND"') != 1:
        errors.append("Project Tools must expose the guarded dedicated append-only command exactly once")
    if text.count('Tag="QS3DINTERCHANGEIMPORT"') != 1:
        errors.append("Project Tools must expose the separate generic policy selector exactly once")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        'ImportAppendsPortableStateAndDiscardsCadOwnership()',
        'AppendPlanIsReadOnlyAndRejectsNameCollision()',
        'CollisionFailsBeforeMutation()',
        'InvalidSnapshotFailsBeforeMutation()',
        'ApplyFailureRollsBackPartialMutation()',
        'ProjectInterchangeAppendOnlyImporter.Plan(target, sourceJson)',
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
    for token in ('QS3DINTERCHANGEAPPEND', 'does **not** copy', 'Still open for issue #84', 'InterchangeConfirmationGuard.RequireFresh'):
        if token not in text:
            errors.append("append-only import documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: dedicated append-only import is bounded, strict-typed-prevalidated before bootstrap, collision-blocked, freshness-guarded, rollback-safe and remains distinct from the generic policy selector.")
