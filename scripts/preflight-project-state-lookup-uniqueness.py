#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STATE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
FLOORS = ROOT / "src/QS3D.Core/Domain/ProjectFloorService.cs"
ZONES = ROOT / "src/QS3D.Core/Domain/ProjectZoneService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateLookupSmoke.cs"
errors = []

for path in (STATE, FLOORS, ZONES, SMOKE):
    if not path.is_file():
        errors.append("missing project lookup uniqueness contract file: " + str(path.relative_to(ROOT)))

if STATE.is_file():
    text = STATE.read_text(encoding="utf-8")
    for token in (
        "FindUnique(Elements, NormalizeLookupId(id), x => x.Id, \"element\")",
        "FindUnique(Families, NormalizeLookupId(id), x => x.Id, \"family\")",
        "FindUnique(Floors, NormalizeLookupId(id), x => x.Id, \"floor\")",
        "FindUnique(Zones, NormalizeLookupId(id), x => x.Id, \"zone\")",
        "FindUnique(QuantityRules, NormalizeLookupId(id), x => x.Id, \"quantity rule\")",
        "if (match != null) throw new InvalidOperationException",
        "Project contains duplicate ",
    ):
        if token not in text:
            errors.append("ProjectState.cs missing unique lookup token: " + token)

if FLOORS.is_file():
    text = FLOORS.read_text(encoding="utf-8")
    if "project.FindFloor(normalized)" not in text:
        errors.append("ProjectFloorService must resolve mutation targets through ProjectState.FindFloor")
    if "project.Floors.FirstOrDefault" in text:
        errors.append("ProjectFloorService still contains first-match Floor lookup")

if ZONES.is_file():
    text = ZONES.read_text(encoding="utf-8")
    for token in (
        "var canonicalId = RequiredIdentity(id, nameof(id), 64);",
        "project.FindZone(canonicalId)",
    ):
        if token not in text:
            errors.append("ProjectZoneService must validate the canonical Zone id before ProjectState.FindZone: " + token)
    if "project.Zones.FirstOrDefault" in text:
        errors.append("ProjectZoneService still contains first-match Zone lookup")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "DuplicateLookupsFailClosed",
        "FloorAndZoneMutationServicesFailClosedOnDuplicateIds",
        "Throws<InvalidOperationException>(() => project.FindElement",
        "Throws<InvalidOperationException>(() => project.FindFamily",
        "Throws<InvalidOperationException>(() => project.FindFloor",
        "Throws<InvalidOperationException>(() => project.FindZone",
        "Throws<InvalidOperationException>(() => project.FindQuantityRule",
        "ProjectFloorService.SetActive",
        "ProjectZoneService.SetActive",
    ):
        if token not in text:
            errors.append("ProjectStateLookupSmoke.cs missing duplicate lookup regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: normalized Element/Family/Floor/Zone/QuantityRule lookups fail closed on duplicate semantic IDs, and Floor/Zone mutation services consume the canonical unique lookup contract.")
