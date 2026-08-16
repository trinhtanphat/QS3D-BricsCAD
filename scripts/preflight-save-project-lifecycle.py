#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectContextCoordinator.cs"
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

errors = []


def method_body(source, start_token, end_token, name):
    start = source.find(start_token)
    end = source.find(end_token, start + 1)
    if start < 0 or end <= start:
        errors.append("cannot isolate " + name)
        return ""
    return source[start:end]


coordinator = COORDINATOR.read_text(encoding="utf-8")

bind_body = method_body(
    coordinator,
    "private static ProjectState GetOrCreate(Document document, bool allowPathTransition)",
    "public static bool TryGetReadOnly(Document document, out ProjectState project)",
    "ProjectContextCoordinator.GetOrCreate",
)
if bind_body:
    sync = bind_body.rfind("SyncDrawingIdentity(project, document);")
    stamp = bind_body.find("var persistenceStamp = new ProjectPersistenceStamp(project);")
    if sync < 0 or stamp < 0 or sync > stamp:
        errors.append("GetOrCreate must normalize drawing identity before capturing the persistence baseline")

reload_body = method_body(
    coordinator,
    "public static ProjectState Reload(Document document)",
    "public static bool HasPendingChanges(Document document)",
    "ProjectContextCoordinator.Reload",
)
if reload_body:
    sync = reload_body.find("SyncDrawingIdentity(project, document);")
    stamp = reload_body.find("var persistenceStamp = new ProjectPersistenceStamp(project);")
    if sync < 0 or stamp < 0 or sync > stamp:
        errors.append("Reload must normalize drawing identity before capturing the persistence baseline")

pending_body = method_body(
    coordinator,
    "public static bool HasPendingChanges(Document document)",
    "public static bool TrySavePending(Document document, out string path)",
    "ProjectContextCoordinator.HasPendingChanges",
)
if pending_body:
    if "ValidateDrawingIdentityReadOnly(project, document);" not in pending_body:
        errors.append("HasPendingChanges must validate drawing identity without mutating project state")
    if "SyncDrawingIdentity(project, document);" in pending_body:
        errors.append("HasPendingChanges must remain side-effect-free and must not normalize drawing identity")
    if "if (!SameDrawingName(project.DrawingPath, document.Name)) return true;" not in pending_body:
        errors.append("HasPendingChanges must still report a DWG path transition as pending without mutating the project")

pending_save_body = method_body(
    coordinator,
    "public static bool TrySavePending(Document document, out string path)",
    "public static string SaveRecoveryCopy(Document document, Exception saveFailure)",
    "ProjectContextCoordinator.TrySavePending",
)
if pending_save_body and "SyncDrawingIdentity(project, document);" not in pending_save_body:
    errors.append("TrySavePending must retain write-boundary drawing identity normalization before persistence")

save_body = method_body(
    coordinator,
    "public static string Save(Document document)",
    "public static ProjectState Reload(Document document)",
    "ProjectContextCoordinator.Save",
)
if save_body:
    required = 'ExistingProjectMutationContext.Require(document, "Save Project")'
    if required not in save_body:
        errors.append("Save must bind an existing canonical project before persistence")
    if "GetOrCreate(document)" in save_body:
        errors.append("Save must not bootstrap a replacement project")
    bind = save_body.find(required)
    persist = save_body.find("Store.Save(project, path)")
    if bind < 0 or persist < 0 or bind > persist:
        errors.append("existing-project bind must occur before persistence")
    mark_saved = save_body.find("GetPersistenceStamp(document, project).MarkSaved(project);")
    if mark_saved < persist:
        errors.append("persistence baseline must be marked saved only after the sidecar commit")

commands = COMMANDS.read_text(encoding="utf-8")
command_start = commands.find('CommandMethod("QS3DSAVE"')
command_end = commands.find("[CommandMethod(", command_start + 1)
command_body = commands[command_start:command_end] if command_start >= 0 and command_end > command_start else ""
if not command_body:
    errors.append("cannot isolate QS3DSAVE command")
elif "ProjectContextCoordinator.Save(doc)" not in command_body:
    errors.append("QS3DSAVE must delegate to ProjectContextCoordinator.Save")

inbox = INBOX.read_text(encoding="utf-8")
for token in ("QS3DSAVE", "no replacement project", "same-ProjectId"):
    if token not in inbox:
        errors.append("LOCAL-AGENT-INBOX.md missing QS3DSAVE lifecycle evidence token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: clean bind/reload captures persistence only after identity normalization, pending-state inspection is side-effect-free while preserving Save-As detection, and QS3DSAVE persists only an existing canonical project.")
