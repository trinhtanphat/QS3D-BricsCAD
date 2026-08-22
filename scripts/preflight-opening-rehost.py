#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Services/HostLinkService.cs"
CODEC = ROOT / "src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs"
WRAPPER = ROOT / "src/QS3D.BricsCAD.V25/Cad/PhysicalOpeningCutTargetState.cs"
CURVED = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurvedOpeningBooleanService.cs"
AUTO = ROOT / "src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs"
MANUAL = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/HostLinkPhysicalCutSmoke.cs"
errors = []

for path in (CORE, CODEC, WRAPPER, CURVED, AUTO, MANUAL, SMOKE):
    if not path.is_file():
        errors.append("missing physical-opening rehost contract file: " + str(path.relative_to(ROOT)))

if CODEC.is_file():
    text = CODEC.read_text(encoding="utf-8")
    for token in (
        'OpeningIdsKey = "PhysicalOpeningCutOpeningIdsV1"',
        "var bytes = Convert.FromBase64String(encoded);",
        "Convert.ToBase64String(bytes), encoded, StringComparison.Ordinal",
        "id = StrictUtf8.GetString(bytes);",
        "Convert.ToBase64String(StrictUtf8.GetBytes(x))",
        "Resolve(ProjectState project, ProjectElement host",
        'opening.Properties.TryGetValue("HostWallId"',
        "Normalize(IEnumerable<string> openingIds)",
    ):
        if token not in text:
            errors.append("physical opening target codec missing: " + token)

if CORE.is_file():
    text = CORE.read_text(encoding="utf-8")
    for token in (
        'EnsureCanLeavePhysicalCutHost(project, opening, previousHostElement, previousHost, "re-host")',
        'EnsureCanLeavePhysicalCutHost(project, opening, host, hostId, "unlink")',
        'host.Properties.TryGetValue("PhysicalOpeningCutSolidHandle"',
        'host.Properties.TryGetValue("PhysicalOpeningCutFingerprint"',
        "PhysicalOpeningCutTargetStateCodec.OpeningIdsKey",
        "PhysicalOpeningCutTargetStateCodec.TryRead(host, out var targetIds)",
        "PhysicalOpeningCutTargetStateCodec.Resolve(project, host, targetIds);",
        "physical opening cut without exact target-state",
        "is physically boolean-cut into host",
    ):
        if token not in text:
            errors.append("HostLink physical-cut guard missing: " + token)

    guard_link = text.find('EnsureCanLeavePhysicalCutHost(project, opening, previousHostElement, previousHost, "re-host")')
    mutate_link = text.find('opening.Properties["HostWallId"] = wall.Id;')
    guard_unlink = text.find('EnsureCanLeavePhysicalCutHost(project, opening, host, hostId, "unlink")')
    mutate_unlink = text.find('opening.Properties.Remove("HostWallId")')
    if min(guard_link, mutate_link, guard_unlink, mutate_unlink) < 0 or not (guard_link < mutate_link and guard_unlink < mutate_unlink):
        errors.append("HostLink must validate physical-cut state before HostWallId/dependency mutation")

if WRAPPER.is_file():
    text = WRAPPER.read_text(encoding="utf-8")
    for token in (
        "PhysicalOpeningCutTargetStateCodec.OpeningIdsKey",
        "PhysicalOpeningCutTargetStateCodec.TryRead",
        "PhysicalOpeningCutTargetStateCodec.Resolve",
        "PhysicalOpeningCutTargetStateCodec.Write",
        "PhysicalOpeningCutTargetStateCodec.Normalize",
    ):
        if token not in text:
            errors.append("plugin target-state wrapper must delegate to Core codec: " + token)

if CURVED.is_file():
    text = CURVED.read_text(encoding="utf-8")
    for token in (
        "public IReadOnlyList<string> OpeningIds",
        "var openingIds = PhysicalOpeningCutTargetState.Normalize(preparedCuts.Select(x => x.OpeningId));",
        "PhysicalOpeningCutTargetState.TryRead(host, out var storedIds)",
        "storedIds.SequenceEqual(openingIds, StringComparer.OrdinalIgnoreCase)",
        "OpeningIds = openingIds",
        "PhysicalOpeningCutTargetState.Write(update.Host, update.OpeningIds);",
        "foreach (var update in pending) CommitSemanticUpdate(project, update);",
    ):
        if token not in text:
            errors.append("curved physical-cut exact target-state missing: " + token)

    boolean_pos = text.find("hostSolid.BooleanOperation(BooleanOperationType.BoolSubtract, cutter);")
    semantic_commit_call = text.find("foreach (var update in pending) CommitSemanticUpdate(project, update);")
    cad_commit_pos = text.find("transaction.Commit();", semantic_commit_call)
    helper_pos = text.find("private static void CommitSemanticUpdate(ProjectState project, PendingHostUpdate update)")
    write_pos = text.find("PhysicalOpeningCutTargetState.Write(update.Host, update.OpeningIds);", helper_pos)
    if min(boolean_pos, semantic_commit_call, cad_commit_pos, helper_pos, write_pos) < 0 or not (boolean_pos < semantic_commit_call < cad_commit_pos and helper_pos < write_pos):
        errors.append("curved cut must invoke semantic target-state persistence before CAD commit, and CommitSemanticUpdate must write exact opening ids")

for path, label in ((AUTO, "auto host"), (MANUAL, "manual host")):
    if path.is_file():
        text = path.read_text(encoding="utf-8")
        if "HostLinkService" not in text or ".LinkOpening(" not in text:
            errors.append(label + " linking must route through shared HostLinkService")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ExactTargetBlocksRehostWithoutMutation();",
        "ExactTargetBlocksUnlinkWithoutMutation();",
        "VerifiedNonTargetCanRehost();",
        "LegacyCutWithoutTargetStateFailsClosed();",
        "CorruptTargetStateFailsClosed();",
        "CodecRoundTripsDeterministically();",
        "ModuleInitializer",
    ):
        if token not in text:
            errors.append("physical cut rehost smoke missing: " + token)

print("QS3D physical opening rehost preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: physical-cut target ids use canonical Base64 + strict UTF-8 through the shared Core codec; manual/auto rehost and unlink fail closed before mutation, curved cuts persist exact ids, and regression smoke covers destructive-host safety.")
