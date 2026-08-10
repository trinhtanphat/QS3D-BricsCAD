#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
LIVE = ROOT / "src/QS3D.BricsCAD.V25/Cad/PhysicalOpeningCutLiveStateService.cs"
CURVED = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurvedOpeningBooleanService.cs"
errors = []

for path in (LIVE, CURVED):
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

print("QS3D physical opening live target-state preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: straight and curved physical-cut live fingerprints are based on the persisted exact baked-opening target-set, never the current HostWallId relationship set.")
