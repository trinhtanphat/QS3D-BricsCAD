#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STAMP = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectPersistenceStamp.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectContextCoordinator.cs"
errors = []

if not STAMP.is_file():
    errors.append("missing ProjectPersistenceStamp.cs")
else:
    text = STAMP.read_text(encoding="utf-8")
    required = [
        'private const string RecoveredFromBackupKey = "QS3D.RecoveredFromBackup";',
        'project.Metadata.TryGetValue(RecoveredFromBackupKey, out var recovered)',
        'string.Equals(recovered, "true", StringComparison.OrdinalIgnoreCase)',
        'return true;',
        'return project.ChangeVersion != _savedChangeVersion ||',
        '!PersistedScalarsMatch(project)',
        '!MetadataMatches(project.Metadata, _savedMetadata)',
    ]
    for needle in required:
        if needle not in text:
            errors.append("missing persistence-stamp recovery contract: " + needle)

    requires_start = text.find("public bool RequiresSave(ProjectState project)")
    mark_start = text.find("public void MarkSaved(ProjectState project)", requires_start)
    if requires_start < 0 or mark_start < 0:
        errors.append("cannot locate RequiresSave boundaries")
    else:
        body = text[requires_start:mark_start]
        marker_check = body.find("project.Metadata.TryGetValue(RecoveredFromBackupKey")
        recovery_pending = body.find("return true;", marker_check)
        dirty_check = body.find("return project.ChangeVersion != _savedChangeVersion ||")
        scalar_check = body.find("!PersistedScalarsMatch(project)", dirty_check)
        metadata_check = body.find("!MetadataMatches(project.Metadata, _savedMetadata)", dirty_check)
        if min(marker_check, recovery_pending, dirty_check, scalar_check, metadata_check) < 0 or not marker_check < recovery_pending < dirty_check < scalar_check < metadata_check:
            errors.append("backup recovery must force pending before the complete persisted-state dirty comparison")

if not COORDINATOR.is_file():
    errors.append("missing ProjectContextCoordinator.cs")
else:
    text = COORDINATOR.read_text(encoding="utf-8")
    required = [
        'if (loaded.RecoveredFromBackup)',
        'project.Metadata["QS3D.RecoveredFromBackup"] = "true";',
        'var recoveryMetadata = CaptureRecoveryMetadata(project);',
        'ClearRecoveryMetadata(project);',
        'Store.Save(project, path);',
        'RestoreMetadata(project, recoveryMetadata);',
    ]
    for needle in required:
        if needle not in text:
            errors.append("missing coordinator backup-heal contract: " + needle)

    save_start = text.find("public static string Save(Document document)")
    reload_start = text.find("public static ProjectState Reload(Document document)", save_start)
    if save_start < 0 or reload_start < 0:
        errors.append("cannot locate coordinator Save boundaries")
    else:
        save = text[save_start:reload_start]
        capture = save.find("CaptureRecoveryMetadata(project)")
        clear = save.find("ClearRecoveryMetadata(project)")
        store = save.find("Store.Save(project, path);")
        restore = save.find("RestoreMetadata(project, recoveryMetadata);")
        if min(capture, clear, store, restore) < 0 or not capture < clear < store < restore:
            errors.append("recovery marker must clear only for save attempt and restore when healing save fails")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: backup fallback remains pending until a successful primary QSDB save clears the recovery marker.")
