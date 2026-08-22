#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SESSION = ROOT / "src/QS3D.Core/Services/ProjectSession.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectSessionAuditSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (SESSION, SMOKE, REG):
    if not path.is_file():
        errors.append("missing project-session recovery contract file: " + str(path.relative_to(ROOT)))

if SESSION.is_file():
    text = SESSION.read_text(encoding="utf-8")
    for token in (
        "Audit = AuditTrail.ForProject(Project);",
        "public AuditTrail Audit { get; private set; }",
        "private bool _recoveredFromBackup;",
        "var snapshot = ProjectStateSnapshot.Capture(Project);",
        'Audit.Record("PROJECT_SAVE", string.Empty, string.Empty);',
        "snapshot.Restore(Project);",
    ):
        if token not in text:
            errors.append("ProjectSession.cs missing recovery/audit token: " + token)

    save_marker = "public void Save()"
    reload_marker = "public void Reload()"
    dispose_marker = "public void Dispose()"
    if save_marker not in text or reload_marker not in text or dispose_marker not in text:
        errors.append("ProjectSession.cs is missing Save/Reload/Dispose method markers")
    else:
        save = text.split(save_marker, 1)[1].split(reload_marker, 1)[0]
        reload = text.split(reload_marker, 1)[1].split(dispose_marker, 1)[0]

        for token in (
            "if (_recoveredFromBackup)",
            "_store.SavePreservingValidatedBackup(Project, Path);",
            "_store.Save(Project, Path);",
            "_recoveredFromBackup = false;",
        ):
            if token not in save:
                errors.append("ProjectSession.Save missing recovery publication token: " + token)

        if all(token in save for token in (
            'Audit.Record("PROJECT_SAVE", string.Empty, string.Empty);',
            "_store.SavePreservingValidatedBackup(Project, Path);",
            "_store.Save(Project, Path);",
            "_recoveredFromBackup = false;",
        )):
            audit_index = save.index('Audit.Record("PROJECT_SAVE", string.Empty, string.Empty);')
            preserve_index = save.index("_store.SavePreservingValidatedBackup(Project, Path);")
            normal_index = save.index("_store.Save(Project, Path);")
            clear_index = save.index("_recoveredFromBackup = false;")
            if audit_index > min(preserve_index, normal_index):
                errors.append("PROJECT_SAVE audit must be staged before either store publication path")
            if clear_index < max(preserve_index, normal_index):
                errors.append("ProjectSession recovery provenance must clear only after successful store publication")

        for token in (
            "var result = _store.LoadWithBackupFallback(Path);",
            "var project = result.Project;",
            "var audit = AuditTrail.ForProject(project);",
            'audit.Record("PROJECT_RELOAD", string.Empty, string.Empty);',
            "Project = project;",
            "Audit = audit;",
            "_recoveredFromBackup = result.RecoveredFromBackup;",
        ):
            if token not in reload:
                errors.append("ProjectSession.Reload missing recovery staging token: " + token)

        if all(token in reload for token in (
            'audit.Record("PROJECT_RELOAD", string.Empty, string.Empty);',
            "Project = project;",
            "Audit = audit;",
            "_recoveredFromBackup = result.RecoveredFromBackup;",
        )):
            audit_index = reload.index('audit.Record("PROJECT_RELOAD", string.Empty, string.Empty);')
            project_index = reload.index("Project = project;")
            audit_bind_index = reload.index("Audit = audit;")
            provenance_index = reload.index("_recoveredFromBackup = result.RecoveredFromBackup;")
            if audit_index > min(project_index, audit_bind_index, provenance_index):
                errors.append("Reload must finish staging audit state before swapping live session bindings/provenance")
            if provenance_index < max(project_index, audit_bind_index):
                errors.append("Reload recovery provenance must publish with the staged project/audit bindings")

        for leaked in (
            'Audit.Record("PROJECT_SAVE", string.Empty, Path);',
            'audit.Record("PROJECT_RELOAD", string.Empty, Path);',
        ):
            if leaked in text:
                errors.append("ProjectSession audit must not persist machine-local project paths: " + leaked)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "SavePersistsAuditAndReloadRebindsTrail();",
        "FailedReloadKeepsExistingSessionBinding();",
        "BothInvalidReloadKeepsExistingSessionBinding();",
        "BackupRecoverySavePreservesValidatedBackupAndClearsMode();",
        "PrimaryReloadClearsRecoveryPublicationMode();",
        "FailedRecoverySaveRollsBackAndKeepsValidatedBackup();",
        'File.WriteAllText(path + ".bak", "<broken-backup");',
        "store.SavePreservingValidatedBackup(new ProjectState",
        "ThrowsIoFailure(() => session.Save());",
        'Require(store.Load(path + ".bak").Name == "Known Good"',
        'RequireAction(session.Project, "PROJECT_SAVE");',
    ):
        if token not in text:
            errors.append("ProjectSessionAuditSmoke.cs missing recovery regression token: " + token)

if REG.is_file() and "ProjectSessionAuditSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("project-session recovery smoke is not registered")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] project-session recovery is statically guarded for redacted audit events, fallback reload, atomic binding, validated-backup publication, failure rollback and recovery-mode clearing")
