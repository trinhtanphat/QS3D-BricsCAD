#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing incremental-opening dependency: " + relative)
        return ""
    return path.read_text(encoding="utf-8")


service = read("src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs")
wrapper = read("src/QS3D.BricsCAD.V25/Cad/PhysicalOpeningCutTargetState.cs")
codec = read("src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs")
live = read("src/QS3D.BricsCAD.V25/Cad/PhysicalOpeningCutLiveStateService.cs")
invalidator = read("src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs")
doc = read("docs/LOCAL-V25-INCREMENTAL-OPENING-CUT.md")

required_service = [
    'hasCutSolid != hasCutFingerprint',
    'PhysicalOpeningCutTargetState.TryRead(host, out var previousIds)',
    'PhysicalOpeningCutTargetState.Resolve(project, host, previousIds)',
    'new HashSet<string>(previousIds, StringComparer.OrdinalIgnoreCase)',
    '.Concat(requestedElements)',
    '.Where(x => !previousSet.Contains(x.Opening.Id))',
    'PhysicalOpeningCutTargetState.Write(update.Host, update.OpeningIds)',
    'OpeningCount = finalPrepared.Cuts.Count',
    'NewOpeningCount = cutsToApply.Count',
    'legacy physical opening state không xác định được tập opening đã khoét',
    'physical opening state đã stale so với geometry/thông số hiện tại',
]
for token in required_service:
    if token not in service:
        errors.append("OpeningBooleanService missing incremental contract: " + token)

for token in [
    'public const string OpeningIdsKey = PhysicalOpeningCutTargetStateCodec.OpeningIdsKey',
    'PhysicalOpeningCutTargetStateCodec.TryRead',
    'PhysicalOpeningCutTargetStateCodec.Resolve',
    'PhysicalOpeningCutTargetStateCodec.Write',
    'PhysicalOpeningCutTargetStateCodec.Normalize',
]:
    if token not in wrapper:
        errors.append("PhysicalOpeningCutTargetState wrapper missing Core codec delegation: " + token)

for token in [
    'public const string OpeningIdsKey = "PhysicalOpeningCutOpeningIdsV1"',
    'private const int MaxOpeningIds = 4096',
    'private const int MaxElementIdLength = 128',
    'private const int MaxEncodedIdLength = 1024',
    'private const int MaxSerializedLength = 4 * 1024 * 1024',
    'if (raw.Length > MaxSerializedLength)',
    'if (tokens.Length > MaxOpeningIds)',
    'if (encoded.Length > MaxEncodedIdLength)',
    'if (id.Length == 0 ||',
    'id.Length > MaxElementIdLength ||',
    '!string.Equals(id, id.Trim(), StringComparison.Ordinal) ||',
    '!seen.Add(id))',
    'if (knownCount.HasValue && observedCount >= knownCount.Value)',
    'if (observedCount >= MaxOpeningIds)',
    'var raw = enumerator.Current;',
    'RequireKnownCountStable(source, knownCount, "after Current")',
    'if (serialized.Length > MaxSerializedLength)',
    'Convert.ToBase64String',
    'Convert.FromBase64String',
    'StringComparer.OrdinalIgnoreCase',
    'Physical opening target no longer exists',
    'is no longer linked to host',
]:
    if token not in codec:
        errors.append("PhysicalOpeningCutTargetStateCodec missing bounded/fail-closed contract: " + token)

known_overrun = codec.find('if (knownCount.HasValue && observedCount >= knownCount.Value)')
streaming_overrun = codec.find('if (observedCount >= MaxOpeningIds)')
current_read = codec.find('var raw = enumerator.Current;')
if min(known_overrun, streaming_overrun, current_read) < 0 or not (known_overrun < current_read and streaming_overrun < current_read):
    errors.append("PhysicalOpeningCutTargetStateCodec must reject known and streaming over-yield before unexpected Current")

for token in [
    'PhysicalOpeningCutTargetState.TryRead(host, out var cutOpeningIds)',
    'PhysicalOpeningCutTargetState.Resolve(project, host, cutOpeningIds)',
    '"PHYSICAL_OPENING_CUT_TARGET_STATE_MISSING"',
]:
    if token not in live:
        errors.append("PhysicalOpeningCutLiveStateService missing persisted cut-set health contract: " + token)

if "LinkedOpenings(project, host.Id)" in live:
    errors.append("PhysicalOpeningCutLiveStateService must fingerprint the persisted exact cut target-set, not the current HostWallId relationship set")

if 'RemoveByPrefix(element, "PhysicalOpeningCut")' not in invalidator:
    errors.append("host rebuild must invalidate PhysicalOpeningCut* metadata, including incremental target state")

for token in [
    "A -> B on the same host",
    "reselect A",
    "all-linked after selected-cut",
    "Partial/malformed/oversized metadata",
    "save, close, reopen",
    "PASS / FAIL / NOT TESTED",
]:
    if token not in doc:
        errors.append("local V25 handoff missing incremental regression: " + token)

previous_index = service.find("PhysicalOpeningCutTargetState.TryRead(host, out var previousIds)")
boolean_index = service.find("hostSolid.BooleanOperation(BooleanOperationType.BoolSubtract, cutter)")
if previous_index < 0 or boolean_index < 0 or previous_index > boolean_index:
    errors.append("previous physical cut state must be validated before BoolSubtract")

if re.search(r"(?<![A-Za-z0-9_])Fingerprint\s*=\s*requestedFingerprint\b", service):
    errors.append("service must not stamp only the current request fingerprint after an incremental cut")

print("QS3D incremental selected opening cut preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: straight-host selected opening cuts preserve a bounded explicit accumulated cut set through the shared Core codec, validate prior state before mutation, reject non-canonical/duplicate ids and over-yield before unexpected Current, subtract only newly selected openings, fingerprint the persisted exact cut set, and invalidate all physical-cut metadata on host rebuild.")
