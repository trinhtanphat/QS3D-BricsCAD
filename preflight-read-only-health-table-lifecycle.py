#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25"
COORDINATOR = ADAPTER / "ProjectContextCoordinator.cs"
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
required = [COORDINATOR, RELEASE, HEALTH_ALL, HUB] + [value[0] for value in TABLES.values()]
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
    if "ProjectContextCoordinator.GetOrCreate(document)" in health:
        errors.append(name + " native Table health must not create/touch project state")
    if "không tạo project mới" not in health:
        errors.append(name + " native Table health must explain BLOCKED no-project behavior")

    existing_project_mutations = text.count("RequireExistingProject(document")
    if existing_project_mutations < 3:
        errors.append(name + " native Table Build/Refresh/Remove must require an existing project before explicit mutation")
    if "private static QS3D.Core.Domain.ProjectState RequireExistingProject" not in text:
        errors.append(name + " native Table must centralize the existing-project guard")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(name + " native Table lifecycle must not create a replacement project")

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

print("PASS: Release Check, Health All and all five native Table lifecycles require existing QS3D state without creating replacement project identity; Build/Refresh/Remove retain explicit mutation paths and Schedule Hub exposes each command exactly once.")
