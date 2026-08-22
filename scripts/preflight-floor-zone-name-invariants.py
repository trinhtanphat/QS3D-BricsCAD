#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/ProjectState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/FloorZoneNameInvariantSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing floor/zone name invariant contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    if text.count("private string _name;") < 3:
        errors.append("ZoneDefinition and FloorDefinition must keep validated name backing fields alongside ProjectFamily.")
    if text.count("set => _name = Require(value, nameof(value));") < 2:
        errors.append("ZoneDefinition and FloorDefinition setters must validate and canonicalize names.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in ('floor.Name = " Floor renamed ";', 'zone.Name = " Zone renamed ";', 'floor.Name = "   "', 'zone.Name = "\\t"'):
        if token not in text:
            errors.append("FloorZoneNameInvariantSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: FloorDefinition and ZoneDefinition names remain trimmed and non-blank through public setters.")
