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
        errors.append("missing project-session audit contract file: " + str(path.relative_to(ROOT)))

if SESSION.is_file():
    text = SESSION.read_text(encoding="utf-8")
    for token in (
        "Audit = AuditTrail.ForProject(Project);",
        "public AuditTrail Audit { get; private set; }",
        "var snapshot = ProjectStateSnapshot.Capture(Project);",
        'Audit.Record("PROJECT_SAVE", string.Empty, Path);',
        "_store.Save(Project, Path);",
        "snapshot.Restore(Project);",
        "Project = _store.Load(Path);",
        'Audit.Record("PROJECT_RELOAD", string.Empty, Path);',
    ):
        if token not in text:
            errors.append("ProjectSession.cs missing bound/persisted audit token: " + token)
    if text.index('Audit.Record("PROJECT_SAVE", string.Empty, Path);') > text.index("_store.Save(Project, Path);"):
        errors.append("PROJECT_SAVE audit must be staged before store save so the same successful save persists it")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "SavePersistsAuditAndReloadRebindsTrail();",
        "new QsdbProjectStore().Load(path)",
        'RequireAction(persisted, "PROJECT_SAVE");',
        'RequireAction(session.Project, "PROJECT_RELOAD");',
        'session.Audit.Record("AFTER_RELOAD"',
        "session.Audit.Events.Count != session.Project.AuditEvents.Count",
    ):
        if token not in text:
            errors.append("ProjectSessionAuditSmoke.cs missing persistence/rebind regression token: " + token)

if REG.is_file() and "ProjectSessionAuditSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("project-session audit smoke is not registered")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] project-session audit is statically guarded to bind to current project state, persist save events, rollback failed-save audit mutation and rebind after reload")
