#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src/QS3D.BricsCAD.V25"
SHARED = ADAPTER / "Cad/ProjectOwnedNativeTableArtifactService.cs"
GENERIC = ADAPTER / "Cad/SemanticElementTableBuilder.cs"
GENERIC_COMMANDS = ADAPTER / "SemanticElementTableCommands.cs"
AGGREGATOR = ADAPTER / "Cad/GeneratedSolidRuntimeHealthService.cs"
RELEASE = ADAPTER / "ReleaseReadinessCommands.cs"
HUB = ADAPTER / "UI/ScheduleHubWindow.xaml"
HUB_CODE = ADAPTER / "UI/ScheduleHubWindow.xaml.cs"
INTERCHANGE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs"
QSDB = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
SNAPSHOT = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"

artifacts = {
    "door": {
        "builder": ADAPTER / "Cad/DoorOpeningNativeTableBuilder.cs",
        "commands": ADAPTER / "DoorOpeningNativeTableCommands.cs",
        "document_id": "DoorOpeningSchedule", "kind": "DoorOpeningTable", "prefix": "GeneratedDoorOpeningTable",
        "provider": "DoorOpeningNativeTableBuilder.Inspect(document, project)", "health_prefix": '"DOOR_OPENING_" + x.Code',
        "hub_command": "QS3DDOOROPENINGTABLE",
        "command_names": ["QS3DDOOROPENINGTABLE", "QS3DDOOROPENINGTABLEREFRESH", "QS3DDOOROPENINGTABLEREMOVE", "QS3DDOOROPENINGTABLEHEALTH"],
    },
    "finish": {
        "builder": ADAPTER / "Cad/RoomFinishNativeTableBuilder.cs",
        "commands": ADAPTER / "RoomFinishNativeTableCommands.cs",
        "document_id": "RoomFinishSchedule", "kind": "RoomFinishTable", "prefix": "GeneratedRoomFinishTable",
        "provider": "RoomFinishNativeTableBuilder.Inspect(document, project)", "health_prefix": '"ROOM_FINISH_" + x.Code',
        "hub_command": "QS3DFINISHTABLE",
        "command_names": ["QS3DFINISHTABLE", "QS3DFINISHTABLEREFRESH", "QS3DFINISHTABLEREMOVE", "QS3DFINISHTABLEHEALTH"],
    },
    "material": {
        "builder": ADAPTER / "Cad/MaterialUsageNativeTableBuilder.cs",
        "commands": ADAPTER / "MaterialUsageNativeTableCommands.cs",
        "document_id": "MaterialUsageSchedule", "kind": "MaterialUsageTable", "prefix": "GeneratedMaterialUsageTable",
        "provider": "MaterialUsageNativeTableBuilder.Inspect(document, project)", "health_prefix": '"MATERIAL_USAGE_" + x.Code',
        "hub_command": "QS3DMATERIALTABLE",
        "command_names": ["QS3DMATERIALTABLE", "QS3DMATERIALTABLEREFRESH", "QS3DMATERIALTABLEREMOVE", "QS3DMATERIALTABLEHEALTH"],
    },
    "bq": {
        "builder": ADAPTER / "Cad/BqNativeTableBuilder.cs",
        "commands": ADAPTER / "BqNativeTableCommands.cs",
        "document_id": "QuantityReportSchedule", "kind": "BqQuantityTable", "prefix": "GeneratedBqTable",
        "provider": "BqNativeTableBuilder.Inspect(document, project)", "health_prefix": '"BQ_" + x.Code',
        "hub_command": "QS3DBQTABLE",
        "command_names": ["QS3DBQTABLE", "QS3DBQTABLEREFRESH", "QS3DBQTABLEREMOVE", "QS3DBQTABLEHEALTH"],
    },
}

generic_identity = {
    "document_id": "SemanticElementSchedule", "kind": "SemanticElementTable", "prefix": "GeneratedSemanticElementTable",
    "provider": "GeneratedSemanticElementTableRuntimeHealthService.Inspect(document, project)", "hub_command": "QS3DELEMENTTABLE",
    "command_names": ["QS3DELEMENTTABLE", "QS3DELEMENTTABLEREFRESH", "QS3DELEMENTTABLEREMOVE", "QS3DELEMENTTABLEHEALTH"],
}

errors = []
required_paths = [SHARED, GENERIC, GENERIC_COMMANDS, AGGREGATOR, RELEASE, HUB, HUB_CODE, INTERCHANGE, QSDB, SNAPSHOT]
required_paths += [x[k] for x in artifacts.values() for k in ("builder", "commands")]
for path in required_paths:
    if not path.is_file(): errors.append("missing integration file: " + str(path.relative_to(ROOT)))

if SHARED.is_file():
    text = SHARED.read_text(encoding="utf-8")
    for token in ('RegAppName = "QS3DDOC"', "ProjectStateSnapshot.Capture(project)", "HasMatchingOwnership(table, project.ProjectId, definition, storedFingerprint)", "MaxRows = 5000", "MaxColumns = 32", "MaxDetailedCellIssues = 32", "OpenMode.ForRead"):
        if token not in text: errors.append("shared table service lost integration contract: " + token)
    if "GeneratedSolidHandle" in text or "GeneratedGeometryService.MarkGenerated" in text:
        errors.append("project-owned documentation tables must not masquerade as element-owned generated geometry")

ids = [generic_identity["document_id"]]
kinds = [generic_identity["kind"]]
prefixes = [generic_identity["prefix"]]
all_expected_commands = list(generic_identity["command_names"])

if GENERIC.is_file():
    text = GENERIC.read_text(encoding="utf-8")
    for token in ('DocumentId = "SemanticElementSchedule"', 'HandleKey = "GeneratedSemanticElementTableHandle"', 'DocumentKind = "SemanticElementTable"', 'RegAppName = "QS3DDOC"'):
        if token not in text: errors.append("generic semantic Table lost project-level identity token: " + token)
if GENERIC_COMMANDS.is_file():
    text = GENERIC_COMMANDS.read_text(encoding="utf-8")
    for command in generic_identity["command_names"]:
        if ('CommandMethod("' + command + '"') not in text: errors.append("generic semantic Table command file missing " + command)

for name, artifact in artifacts.items():
    ids.append(artifact["document_id"]); kinds.append(artifact["kind"]); prefixes.append(artifact["prefix"])
    all_expected_commands.extend(artifact["command_names"])
    if artifact["builder"].is_file():
        text = artifact["builder"].read_text(encoding="utf-8")
        for token in ('"' + artifact["document_id"] + '"', '"' + artifact["kind"] + '"', '"' + artifact["prefix"] + '"', artifact["health_prefix"], "ProjectOwnedNativeTableArtifactService.Build", "ProjectOwnedNativeTableArtifactService.Remove", "ProjectOwnedNativeTableArtifactService.Inspect"):
            if token not in text: errors.append(name + " builder missing integration token: " + token)
    if artifact["commands"].is_file():
        text = artifact["commands"].read_text(encoding="utf-8")
        for command in artifact["command_names"]:
            if ('CommandMethod("' + command + '"') not in text: errors.append(name + " command file missing " + command)
        if "StoredPosition(project)" not in text: errors.append(name + " refresh must use persisted WCS position")

if len(set(ids)) != len(ids): errors.append("native documentation Table document IDs must be unique")
if len(set(kinds)) != len(kinds): errors.append("native documentation Table document kinds must be unique")
if len(set(prefixes)) != len(prefixes): errors.append("native documentation Table metadata prefixes must be unique")
if any(a.startswith(b) or b.startswith(a) for i, a in enumerate(prefixes) for b in prefixes[i + 1:]): errors.append("native documentation Table metadata prefixes must not overlap by prefix")

command_counts = {}
if ADAPTER.is_dir():
    for path in ADAPTER.rglob("*.cs"):
        for command in re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8")):
            command_counts[command] = command_counts.get(command, 0) + 1
for command in all_expected_commands:
    if command_counts.get(command, 0) != 1: errors.append(command + " must be declared exactly once; found " + str(command_counts.get(command, 0)))

if AGGREGATOR.is_file():
    text = AGGREGATOR.read_text(encoding="utf-8")
    if "AddProviderSafely(" not in text: errors.append("runtime health aggregator lost fail-isolated provider wrapper")
    providers = [generic_identity["provider"]] + [artifact["provider"] for artifact in artifacts.values()]
    for provider in providers:
        if provider not in text:
            errors.append("runtime health aggregator missing provider: " + provider); continue
        if text.rfind("AddProviderSafely(", 0, text.find(provider)) < 0: errors.append("runtime health provider is not fail-isolated: " + provider)

if RELEASE.is_file() and "GeneratedSolidRuntimeHealthService.Inspect(document, project)" not in RELEASE.read_text(encoding="utf-8"):
    errors.append("QS3DRELEASECHECK must consume the shared native runtime-health aggregator")

if HUB.is_file():
    text = HUB.read_text(encoding="utf-8")
    for command in [generic_identity["hub_command"]] + [artifact["hub_command"] for artifact in artifacts.values()]:
        if ('Tag="' + command + '"') not in text: errors.append("Schedule Hub missing native Table launcher: " + command)
if HUB_CODE.is_file():
    text = HUB_CODE.read_text(encoding="utf-8")
    for token in (
        "OnCommandClick",
        "var normalizedCommand = command.Trim();",
        'EnsureActive("chạy " + normalizedCommand);',
        'SendStringToExecute(normalizedCommand + " ", true, false, false)',
    ):
        if token not in text: errors.append("Schedule Hub lost guarded generic command dispatch token: " + token)

if QSDB.is_file():
    text = QSDB.read_text(encoding="utf-8")
    for token in ('Map("metadata", project.Metadata)', 'ReadStringMap(root.Element("metadata"), "p", project.Metadata)'):
        if token not in text: errors.append("QSDB persistence must save/reload project-level native Table metadata: " + token)
if SNAPSHOT.is_file():
    text = SNAPSHOT.read_text(encoding="utf-8")
    for token in (
        "target.Metadata.Clear();",
        "foreach (var item in source.Metadata) target.Metadata[item.Key] = item.Value;",
        "target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion);",
    ):
        if token not in text: errors.append("ProjectStateSnapshot must include project Metadata and persistence state for rollback-safe native Table mutation: " + token)
if INTERCHANGE.is_file():
    text = INTERCHANGE.read_text(encoding="utf-8")
    if "project.Metadata" in text: errors.append("portable Semantic Snapshot must not serialize ProjectState.Metadata; drawing-local native Table handles/positions are not portable")
    for token in ("GeneratedHandleOwnershipPolicy.IsOwnerSlot(normalized)", 'normalized.StartsWith("Generated"', 'normalized.StartsWith("QS3D.Generated"', 'normalized.StartsWith("PhysicalOpeningCut"'):
        if token not in text: errors.append("Semantic Snapshot exporter lost generated/native property scrub token: " + token)

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors)); sys.exit(1)

print("PASS: generic, Door/Opening, Room Finish, Material Usage and BQ native Tables have unique commands/artifact identities/metadata prefixes, project-level QS3DDOC ownership, rollback-safe QSDB persistence, namespaced diagnostics, fail-isolated runtime/Release wiring, guarded Schedule Hub launchers and deliberate exclusion from portable Semantic Snapshot interchange.")
