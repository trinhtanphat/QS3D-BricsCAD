#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
LIVE = ROOT / "src/QS3D.BricsCAD.V25/Cad/PhysicalOpeningCutLiveStateService.cs"
CURVED = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurvedOpeningBooleanService.cs"
CORE = ROOT / "src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/HostLinkPhysicalCutSmoke.cs"
errors = []

for path in (LIVE, CURVED, CORE, SMOKE):
    if not path.is_file():
        errors.append("missing physical opening live target-state file: " + str(path.relative_to(ROOT)))

if LIVE.is_file():
    text = LIVE.read_text(encoding="utf-8")
    for token in (
        "PhysicalOpeningCutTargetState.TryRead(host, out var cutOpeningIds)",
        "PhysicalOpeningCutTargetState.Resolve(project, host, cutOpeningIds)",
        "PhysicalOpeningCutLiveFingerprint.Compute(document, transaction, project, host, source, fingerprintOpenings)",
        '"PHYSICAL_OPENING_CUT_TARGET_STATE_MISSING"',
        'Mode = curved ? "CurvedInputV1" : "StraightInputV1"',
    ):
        if token not in text:
            errors.append("live physical-cut target-state contract missing: " + token)
    if "LinkedOpenings(project, host.Id)" in text or "fingerprintOpenings = group.OrderBy" in text:
        errors.append("curved live-state must not infer baked cutters from currently linked openings")

if CURVED.is_file():
    text = CURVED.read_text(encoding="utf-8")
    for token in (
        "PhysicalOpeningCutTargetState.Write(update.Host, update.OpeningIds);",
        "PhysicalOpeningCutTargetState.TryRead(host, out var storedIds)",
        "OpeningIds = openingIds",
    ):
        if token not in text:
            errors.append("curved cut must persist/backfill exact target ids before live-state stamping: " + token)

if CORE.is_file():
    text = CORE.read_text(encoding="utf-8")
    for token in (
        "!seen.Add(id)",
        "if (!result.Add(id))",
        "Physical opening target-state contains duplicate opening id:",
    ):
        if token not in text:
            errors.append("physical opening Core target-state must fail closed on duplicate identity: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "CodecRejectsPaddedTargetsWithoutMutation();",
        'PhysicalOpeningCutTargetStateCodec.Normalize(new[] { "O1", " o2 " })',
        'PhysicalOpeningCutTargetStateCodec.Write(host, new[] { "O1", " o2 " })',
        "CodecRejectsDuplicateTargetsWithoutMutation();",
        'PhysicalOpeningCutTargetStateCodec.Normalize(new[] { "O1", "o1" })',
        'PhysicalOpeningCutTargetStateCodec.Write(host, new[] { "O1", "o1" })',
        'Equal("sentinel", host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey]);',
    ):
        if token not in text:
            errors.append("physical opening canonical-target regression smoke missing: " + token)

print("QS3D physical opening live target-state preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: straight and curved physical-cut live fingerprints use the persisted exact baked-opening target-set; padded and duplicate target identities fail closed before metadata mutation.")
