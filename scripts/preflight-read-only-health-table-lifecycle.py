#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25"
COORDINATOR = ADAPTER / "ProjectContextCoordinator.cs"
MUTATION_CONTEXT = ADAPTER / "ExistingProjectMutationContext.cs"
RELEASE = ADAPTER / "ReleaseReadinessCommands.cs"
HEALTH_ALL = ADAPTER / "HealthAllCommands.cs"
HUB = ADAPTER / "UI/ScheduleHubWindow.xaml"

TABLES = {
    "bq": (ADAPTER / "BqNativeTableCommands.cs", "public void Health()", [
        "QS3DBQTABLE", "QS3DBQTABLEREFRESH", "QS3DBQTABLEHEALTH", "QS3DBQTABLEREMOVE",
    ]),
    "semantic": (ADAPTER / "SemanticElementTableCommands.cs", "public void CheckElementTableHealth()", [
        "QS3DELEMENTTABLE", "QS3DELEMENTTABLEREFRESH", "QS3DELEMENTTABLEHEALTH", "QS3DELEMENTTABLEREMOVE",
    ]),
    "finish": (ADAPTER / "RoomFinishNativeTableCommands.cs", "public void Health()", [
        "QS3DFINISHTABLE", "QS3DFINISHTABLEREFRESH", "QS3DFINISHTABLEHEALTH", "QS3DFINISHTABLEREMOVE",
    ]),
    "material": (ADAPTER / "MaterialUsageNativeTableCommands.cs", "public void Health()", [
        "QS3DMATERIALTABLE", "QS3DMATERIALTABLEREFRESH", "QS3DMATERIALTABLEHEALTH", "QS3DMATERIALTABLEREMOVE",
    ]),
    "door": (ADAPTER / "DoorOpeningNativeTableCommands.cs", "public void Health()", [
        "QS3DDOOROPENINGTABLE", "QS3DDOOROPENINGTABLEREFRESH", "QS3DDOOROPENINGTABLEHEALTH", "QS3DDOOROPENINGTABLEREMOVE",
    ]),
}

errors = []
required = [COORDINATOR, MUTATION_CONTEXT, RELEASE, HEALTH_ALL, HUB] + [value[0] for value in TABLES.values()]
for path in required:
    if not path.is_file():
        errors.append("missing read-only health/lifecycle file: " + str(path.relative_to(ROOT)))


def method_body(text, marker):
    start = text.find(marker)
    if start < 0:
        return ""
    brace = text.find("{", start)
    if brace < 0:
        return ""
    depth = 0
    for index in range(brace, len(text)):
        ch = text[index]
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return text[brace:index + 1]
    return ""


if COORDINATOR.is_file():
    text = COORDINATOR.read_text(encoding="utf-8")
    body = method_body(text, "public static bool TryGetReadOnly(")
    if not body:
        errors.append("ProjectContextCoordinator must expose TryGetReadOnly")
    for token in (
        "project = null!;",
        "TryGetExistingProjectPath(document, out var path)",
        "ValidateDrawingIdentityReadOnly(existing, document)",
        "ValidateDrawingIdentityReadOnly(project, document)",
    ):
        if token not in body:
            errors.append("TryGetReadOnly missing read-only contract token: " + token)
    for forbidden in (
        "GetProjectPath(", "SyncDrawingIdentity(", "CreateDefault(",
        "Projects[document] =", "UnsavedProjectKeys", ".Touch()", "AdoptDrawingIdentity(",
    ):
        if forbidden in body:
            errors.append("TryGetReadOnly must not mutate/create/cache project state: " + forbidden)

    path_body = method_body(text, "private static bool TryGetExistingProjectPath(")
    if not path_body:
        errors.append("ProjectContextCoordinator must resolve existing sidecar paths without allocating unsaved keys")
    elif "UnsavedProjectKeys" in path_body or "Guid.NewGuid" in path_body:
        errors.append("TryGetExistingProjectPath must not allocate transient project identity")

if MUTATION_CONTEXT.is_file():
    text = MUTATION_CONTEXT.read_text(encoding="utf-8")
    require_body = method_body(text, "public static ProjectState Require(")
    try_get_body = method_body(text, "public static bool TryGet(")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var observed)",
        "var canonical = ProjectContextCoordinator.GetOrCreate(document);",
        "string.Equals(canonical.ProjectId, expectedProjectId",
        "ProjectContextCoordinator.Forget(document);",
    ):
        if token not in try_get_body:
            errors.append("ExistingProjectMutationContext.TryGet missing canonical existing-project binding guard: " + token)
    for token in ("TryGet(document, out var project)", "không tạo project mới"):
        if token not in require_body:
            errors.append("ExistingProjectMutationContext.Require missing fail-closed existing-project contract: " + token)

for path, command_name in ((RELEASE, "QS3DRELEASECHECK"), (HEALTH_ALL, "QS3DHEALTHALL")):
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append(command_name + " must use read-only project inspection")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(command_name + " must not create/touch project state")
    if "không tạo project mới" not in text:
        errors.append(command_name + " must explain the no-project BLOCKED state")

all_lifecycle_commands = []
for name, (path, health_marker, commands) in TABLES.items():
    all_lifecycle_commands.extend(commands)
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    health = method_body(text, health_marker)
    if not health:
        errors.append(name + " native Table health method not found")
        continue
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in health:
        errors.append(name + " native Table health must use TryGetReadOnly")
    if "ProjectContextCoordinator.GetOrCreate(document)" in health or "ExistingProjectMutationContext" in health:
        errors.append(name + " native Table health must remain read-only and must not bind mutable project state")
    if "không tạo project mới" not in health:
        errors.append(name + " native Table health must explain BLOCKED no-project behavior")

    existing = method_body(text, "private static QS3D.Core.Domain.ProjectState RequireExistingProject(")
    if not existing:
        errors.append(name + " native Table must expose an existing-project guard for Build/Refresh/Remove")
    elif "ExistingProjectMutationContext.Require(document, operation)" not in existing:
        errors.append(name + " native Table existing-project guard must bind canonical existing state through ExistingProjectMutationContext.Require")
    if text.count("RequireExistingProject(document,") < 3:
        errors.append(name + " native Table Build/Refresh/Remove must each require the existing project explicitly")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(name + " native Table lifecycle must not directly create/cache replacement project state")

if HUB.is_file():
    text = HUB.read_text(encoding="utf-8")
    for command in all_lifecycle_commands:
        count = text.count('Tag="' + command + '"')
        if count != 1:
            errors.append("Schedule Hub must expose lifecycle command exactly once: " + command + " (found " + str(count) + ")")
    for token in (
        "Create/Refresh/Health/Remove",
        "Health/Release chỉ đọc project hiện có và không tạo project state mới",
    ):
        if token not in text:
            errors.append("Schedule Hub missing lifecycle/read-only UX contract: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Release Check, Health All and native Table health inspect existing QS3D state read-only; Build/Refresh/Remove bind canonical existing project state through the guarded mutation context, and Schedule Hub exposes each lifecycle command exactly once.")
