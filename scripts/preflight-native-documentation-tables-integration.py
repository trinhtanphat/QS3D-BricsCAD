#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25"
SHARED = ADAPTER / "Cad/ProjectOwnedNativeTableArtifactService.cs"
AGGREGATOR = ADAPTER / "Cad/GeneratedSolidRuntimeHealthService.cs"
RELEASE = ADAPTER / "ReleaseReadinessCommands.cs"

artifacts = {
    "door": {
        "builder": ADAPTER / "Cad/DoorOpeningNativeTableBuilder.cs",
        "commands": ADAPTER / "DoorOpeningNativeTableCommands.cs",
        "document_id": "DoorOpeningSchedule",
        "kind": "DoorOpeningTable",
        "prefix": "GeneratedDoorOpeningTable",
        "provider": "DoorOpeningNativeTableBuilder.Inspect(document, project)",
        "health_prefix": '"DOOR_OPENING_" + x.Code',
        "command_names": [
            "QS3DDOOROPENINGTABLE",
            "QS3DDOOROPENINGTABLEREFRESH",
            "QS3DDOOROPENINGTABLEREMOVE",
            "QS3DDOOROPENINGTABLEHEALTH",
        ],
    },
    "finish": {
        "builder": ADAPTER / "Cad/RoomFinishNativeTableBuilder.cs",
        "commands": ADAPTER / "RoomFinishNativeTableCommands.cs",
        "document_id": "RoomFinishSchedule",
        "kind": "RoomFinishTable",
        "prefix": "GeneratedRoomFinishTable",
        "provider": "RoomFinishNativeTableBuilder.Inspect(document, project)",
        "health_prefix": '"ROOM_FINISH_" + x.Code',
        "command_names": [
            "QS3DFINISHTABLE",
            "QS3DFINISHTABLEREFRESH",
            "QS3DFINISHTABLEREMOVE",
            "QS3DFINISHTABLEHEALTH",
        ],
    },
    "material": {
        "builder": ADAPTER / "Cad/MaterialUsageNativeTableBuilder.cs",
        "commands": ADAPTER / "MaterialUsageNativeTableCommands.cs",
        "document_id": "MaterialUsageSchedule",
        "kind": "MaterialUsageTable",
        "prefix": "GeneratedMaterialUsageTable",
        "provider": "MaterialUsageNativeTableBuilder.Inspect(document, project)",
        "health_prefix": '"MATERIAL_USAGE_" + x.Code',
        "command_names": [
            "QS3DMATERIALTABLE",
            "QS3DMATERIALTABLEREFRESH",
            "QS3DMATERIALTABLEREMOVE",
            "QS3DMATERIALTABLEHEALTH",
        ],
    },
}

errors = []
for path in [SHARED, AGGREGATOR, RELEASE] + [x[k] for x in artifacts.values() for k in ("builder", "commands")]:
    if not path.is_file():
        errors.append("missing integration file: " + str(path.relative_to(ROOT)))

if SHARED.is_file():
    text = SHARED.read_text(encoding="utf-8")
    for token in (
        'RegAppName = "QS3DDOC"',
        "ProjectStateSnapshot.Capture(project)",
        "HasMatchingOwnership(table, project.ProjectId, definition, storedFingerprint)",
        "MaxRows = 5000",
        "MaxColumns = 32",
        "MaxDetailedCellIssues = 32",
        "OpenMode.ForRead",
    ):
        if token not in text:
            errors.append("shared table service lost integration contract: " + token)
    if "GeneratedSolidHandle" in text or "GeneratedGeometryService.MarkGenerated" in text:
        errors.append("project-owned documentation tables must not masquerade as element-owned generated geometry")

ids = []
kinds = []
prefixes = []
all_expected_commands = []
for name, artifact in artifacts.items():
    ids.append(artifact["document_id"])
    kinds.append(artifact["kind"])
    prefixes.append(artifact["prefix"])
    all_expected_commands.extend(artifact["command_names"])

    builder = artifact["builder"]
    if builder.is_file():
        text = builder.read_text(encoding="utf-8")
        for token in (
            '"' + artifact["document_id"] + '"',
            '"' + artifact["kind"] + '"',
            '"' + artifact["prefix"] + '"',
            artifact["health_prefix"],
            "ProjectOwnedNativeTableArtifactService.Build",
            "ProjectOwnedNativeTableArtifactService.Remove",
            "ProjectOwnedNativeTableArtifactService.Inspect",
        ):
            if token not in text:
                errors.append(name + " builder missing integration token: " + token)

    command_file = artifact["commands"]
    if command_file.is_file():
        text = command_file.read_text(encoding="utf-8")
        for command in artifact["command_names"]:
            if ('CommandMethod("' + command + '"') not in text:
                errors.append(name + " command file missing " + command)
        if "StoredPosition(project)" not in text:
            errors.append(name + " refresh must use persisted WCS position")

if len(set(ids)) != len(ids):
    errors.append("native documentation Table document IDs must be unique")
if len(set(kinds)) != len(kinds):
    errors.append("native documentation Table document kinds must be unique")
if len(set(prefixes)) != len(prefixes):
    errors.append("native documentation Table metadata prefixes must be unique")
if any(a.startswith(b) or b.startswith(a) for i, a in enumerate(prefixes) for b in prefixes[i + 1:]):
    errors.append("native documentation Table metadata prefixes must not overlap by prefix")

command_counts = {}
if ADAPTER.is_dir():
    for path in ADAPTER.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        for command in re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text):
            command_counts[command] = command_counts.get(command, 0) + 1
for command in all_expected_commands:
    if command_counts.get(command, 0) != 1:
        errors.append(command + " must be declared exactly once; found " + str(command_counts.get(command, 0)))

if AGGREGATOR.is_file():
    text = AGGREGATOR.read_text(encoding="utf-8")
    if "AddProviderSafely(" not in text:
        errors.append("runtime health aggregator lost fail-isolated provider wrapper")
    for artifact in artifacts.values():
        if artifact["provider"] not in text:
            errors.append("runtime health aggregator missing provider: " + artifact["provider"])
        provider_pos = text.find(artifact["provider"])
        safe_pos = text.rfind("AddProviderSafely(", 0, provider_pos)
        if provider_pos >= 0 and safe_pos < 0:
            errors.append("runtime health provider is not fail-isolated: " + artifact["provider"])

if RELEASE.is_file():
    text = RELEASE.read_text(encoding="utf-8")
    if "GeneratedSolidRuntimeHealthService.Inspect(document, project)" not in text:
        errors.append("QS3DRELEASECHECK must consume the shared native runtime-health aggregator")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Door/Opening, Room Finish and Material Usage native Tables have unique commands/artifact identities/metadata prefixes, project-level QS3DDOC ownership, namespaced diagnostics, fail-isolated runtime providers and Release Check integration.")
